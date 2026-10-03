using System;
using UnityEngine;

namespace DarkDescent.Interaction
{
    /// <summary>
    /// Qualcosa che si clicca per andarci: oggi le scale, poi porte, oggetti a terra e PNG (ADR-006).
    /// Il collider per il click sta sul layer Interactable, sullo stesso oggetto o su un figlio.
    /// Sotto il cursore si evidenzia: chi lo disegna ascolta <see cref="HighlightChanged"/>.
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

        public Vector3 ApproachPoint => _approachPoint != null ? _approachPoint.position : transform.position;

        public string Label => _label;

        public bool IsHighlighted { get; private set; }

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
