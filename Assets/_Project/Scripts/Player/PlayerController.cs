using UnityEngine;

namespace DarkDescent.Player
{
    /// <summary>
    /// Collega input e movimento: ascolta il reader, trova il punto sotto il cursore, comanda il motor.
    /// È l'unico dei tre a conoscere camera, layer e mondo 3D.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Layer su cui un click fa camminare.")]
        [SerializeField] private LayerMask _walkableLayers;

        [Tooltip("Layer che fermano il raggio senza essere camminabili: un click su di essi non fa nulla.")]
        [SerializeField] private LayerMask _blockingLayers;

        [SerializeField, Min(1f)] private float _maxRayDistance = 100f;

        [Tooltip("Ogni quanti secondi si aggiorna la destinazione tenendo premuto il tasto.")]
        [SerializeField, Min(0.02f)] private float _holdRepathInterval = 0.1f;

        [Tooltip("Vuoto = Camera.main, risolta una volta in Awake.")]
        [SerializeField] private Camera _camera;

        private PlayerMotor _motor;
        private PlayerInputReader _input;
        private float _holdTimer;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _input = GetComponent<PlayerInputReader>();

            // Camera.main è una ricerca per tag: una volta qui, mai per frame
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void OnEnable()
        {
            _input.MoveCommandStarted += HandleMoveCommandStarted;
        }

        private void OnDisable()
        {
            _input.MoveCommandStarted -= HandleMoveCommandStarted;
        }

        private void Update()
        {
            // tenendo premuto si continua a seguire il cursore, come in Diablo;
            // il motor scarta da solo le destinazioni quasi uguali alla corrente
            if (!_input.IsMoveCommandHeld)
            {
                return;
            }

            _holdTimer -= Time.deltaTime;
            if (_holdTimer > 0f)
            {
                return;
            }

            _holdTimer = _holdRepathInterval;
            MoveToCursor();
        }

        private void HandleMoveCommandStarted()
        {
            _holdTimer = _holdRepathInterval;
            MoveToCursor();
        }

        private void MoveToCursor()
        {
            if (TryGetPointUnderCursor(out Vector3 point))
            {
                _motor.MoveTo(point);
            }
        }

        // Il raggio usa camminabili + bloccanti. Con i soli camminabili attraverserebbe i cubi e colpirebbe
        // il pavimento dietro: un click su un ostacolo porterebbe il player alle sue spalle.
        private bool TryGetPointUnderCursor(out Vector3 point)
        {
            point = default;
            if (_camera == null)
            {
                return false;
            }

            Ray ray = _camera.ScreenPointToRay(_input.PointerScreenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance, _walkableLayers | _blockingLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if ((_walkableLayers.value & (1 << hit.collider.gameObject.layer)) == 0)
            {
                return false;
            }

            point = hit.point;
            return true;
        }
    }
}
