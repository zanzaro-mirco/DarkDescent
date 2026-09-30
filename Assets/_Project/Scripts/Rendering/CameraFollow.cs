using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Segue un target con smoothing. La rotazione si imposta solo nell'Inspector;
    /// la posizione si ricava da quella, così il target resta sempre al centro dell'inquadratura.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        [Tooltip("Distanza dal target lungo l'asse di vista. In ortografica non cambia l'inquadratura: conta solo per i clipping plane e per la distanza delle ombre URP.")]
        [SerializeField, Min(1f)] private float _distance = 20f;

        [Tooltip("Tempo indicativo per raggiungere il target, in secondi. 0 = aggancio rigido.")]
        [SerializeField, Min(0f)] private float _smoothTime = 0.15f;

        // stato interno di SmoothDamp: deve sopravvivere tra un frame e l'altro, non va azzerato a mano
        private Vector3 _velocity;

        private void Start()
        {
            // senza questo, al primo frame la camera parte da dove è stata lasciata in scena e "vola" verso il target
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, GetDesiredPosition(), ref _velocity, _smoothTime);
        }

        private void SnapToTarget()
        {
            if (_target == null)
            {
                return;
            }

            transform.position = GetDesiredPosition();
            _velocity = Vector3.zero;
        }

        // un offset Vector3 fisso andrebbe tenuto allineato a mano con la rotazione: se non lo è, il target
        // esce dal centro. Ricavarlo da transform.forward ogni frame costa niente e permette di ritoccare
        // la rotazione anche in Play Mode.
        private Vector3 GetDesiredPosition()
        {
            return _target.position - transform.forward * _distance;
        }
    }
}
