using System;
using DarkDescent.Localization;
using UnityEngine;

namespace DarkDescent.Interaction
{
    /// <summary>
    /// Qualcosa che si clicca per andarci: le scale, gli oggetti a terra, poi porte e PNG (ADR-006).
    /// Il collider per il click sta sul layer Interactable, sullo stesso oggetto o su un figlio.
    /// Sotto il cursore si evidenzia: chi lo disegna ascolta <see cref="HighlightChanged"/>. Quando
    /// il player lo raggiunge, il controller chiama <see cref="Use"/>: chi sa cosa fare ascolta
    /// <see cref="Used"/> (le scale non ne hanno bisogno, le cambia il loro trigger).
    /// </summary>
    [DisallowMultipleComponent]
    public class Interactable : MonoBehaviour
    {
        [Tooltip("Dove il player si ferma per usarlo, sul NavMesh. Vuoto = la posizione di questo oggetto.")]
        [SerializeField] private Transform _approachPoint;

        [Tooltip("La chiave del nome mostrato nell'HUD quando il cursore ci passa sopra. Vuota = nessun nome.")]
        [SerializeField] private string _labelKey;

        [Tooltip("Il valore per il segnaposto {0} del nome, come il numero del livello di una scala. Vuoto = nessuno.")]
        [SerializeField] private string _labelArgument;

        private ILabelSource _labelSource;

        /// <summary>Acceso o spento: il cursore è arrivato o se n'è andato.</summary>
        public event Action<bool> HighlightChanged;

        /// <summary>L'etichetta è cambiata, per esempio "inventario pieno": chi la mostra la rilegge.</summary>
        public event Action<Interactable> LabelChanged;

        /// <summary>Il player l'ha raggiunto dopo un click: l'argomento è chi lo usa.</summary>
        public event Action<GameObject> Used;

        public Vector3 ApproachPoint => _approachPoint != null ? _approachPoint.position : transform.position;

        public string LabelKey => _labelKey;

        public bool IsHighlighted { get; private set; }

        /// <summary>Il nome nella lingua attiva; vuoto se non ne ha. Si chiede quando serve, mai a ogni frame.</summary>
        public string GetLabel(Localizer localizer)
        {
            if (_labelSource != null)
            {
                return _labelSource.GetLabel(localizer);
            }

            if (string.IsNullOrEmpty(_labelKey))
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(_labelArgument) ? localizer.Get(_labelKey) : localizer.Format(_labelKey, _labelArgument);
        }

        /// <summary>Un nome composto dal codice, come quello di un oggetto a terra, al posto della chiave.</summary>
        public void SetLabelSource(ILabelSource source)
        {
            _labelSource = source;
            NotifyLabelChanged();
        }

        /// <summary>Il nome è cambiato senza cambiare sorgente ("inventario pieno"): chi lo mostra lo rilegge.</summary>
        public void NotifyLabelChanged()
        {
            LabelChanged?.Invoke(this);
        }

        public void Use(GameObject user)
        {
            Used?.Invoke(user);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlighted == IsHighlighted)
            {
                return;
            }

            IsHighlighted = highlighted;
            HighlightChanged?.Invoke(highlighted);
        }
    }
}
