using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Tiene la luce del cavaliere alta e spostata verso la camera. Proprio sopra la testa, l'elmo si
    /// bruciava di bianco e il resto del corpo, quello che la camera vede, restava al buio. Da qui
    /// illumina il lato visibile e l'ombra cade dietro di lui. Più in alto la luce cala più piano: il
    /// cerchio a terra è più largo e più uniforme, come in Diablo.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class PlayerLightRig : MonoBehaviour
    {
        [Tooltip("Chi la luce segue. Vuoto = il padre.")]
        [SerializeField] private Transform _target;

        [Tooltip("Da cui prendere la direzione verso la camera. Vuoto = Camera.main, risolta in Awake.")]
        [SerializeField] private Transform _orientation;

        [Tooltip("Altezza sopra i piedi del target.")]
        [SerializeField, Min(0f)] private float _height = 6f;

        [Tooltip("Quanto la luce si sposta, in orizzontale, verso la camera.")]
        [SerializeField, Min(0f)] private float _towardCamera = 2.5f;

        public float Height => _height;

        public float TowardCamera => _towardCamera;

        private void Awake()
        {
            if (_target == null)
            {
                _target = transform.parent;
            }

            // Camera.main è una ricerca per tag: una volta qui, mai per frame
            if (_orientation == null && Camera.main != null)
            {
                _orientation = Camera.main.transform;
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            transform.position = PositionFor(_target.position, _orientation != null ? _orientation.forward : Vector3.forward);
        }

        /// <summary>Dove sta la luce per un target ai piedi in <paramref name="feet"/> e una camera che guarda verso <paramref name="cameraForward"/>.</summary>
        public Vector3 PositionFor(Vector3 feet, Vector3 cameraForward)
        {
            // solo la componente orizzontale: verso la camera vuol dire contro la direzione in cui guarda
            Vector3 flat = new Vector3(cameraForward.x, 0f, cameraForward.z).normalized;
            return feet + Vector3.up * _height - flat * _towardCamera;
        }
    }
}
