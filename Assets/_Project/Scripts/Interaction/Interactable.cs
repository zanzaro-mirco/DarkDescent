using System;
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

        [Tooltip("Il nome mostrato nell'HUD quando il cursore ci passa sopra.")]
        [SerializeField] private string _label;

        /// <summary>Acceso o spento: il cursore è arrivato o se n'è andato.</summary>
        public event Action<bool> HighlightChanged;

        /// <summary>L'etichetta è cambiata, per esempio "inventario pieno": chi la mostra la rilegge.</summary>
        public event Action<Interactable> LabelChanged;

        /// <summary>Il player l'ha raggiunto dopo un click: l'argomento è chi lo usa.</summary>
        public event Action<GameObject> Used;

        public Vector3 ApproachPoint => _approachPoint != null ? _approachPoint.position : transform.position;

        public string Label => _label;

        public bool IsHighlighted { get; private set; }

        public void SetLabel(string label)
        {
            if (label == _label)
            {
                return;
            }

            _label = label;
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
