using UnityEngine;

namespace DarkDescent.Interaction
{
    /// <summary>
    /// Qualcosa che si clicca per andarci: oggi le scale, poi porte, oggetti a terra e PNG (ADR-006).
    /// Il collider per il click sta sul layer Interactable, sullo stesso oggetto o su un figlio.
    /// </summary>
    [DisallowMultipleComponent]
    public class Interactable : MonoBehaviour
    {
        [Tooltip("Dove il player si ferma per usarlo, sul NavMesh. Vuoto = la posizione di questo oggetto.")]
        [SerializeField] private Transform _approachPoint;

        public Vector3 ApproachPoint => _approachPoint != null ? _approachPoint.position : transform.position;
    }
}
