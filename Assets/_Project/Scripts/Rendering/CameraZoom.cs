using DarkDescent.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Lo zoom con la rotella (D11 della M8). La camera è ortografica: lo zoom cambia la dimensione
    /// della vista, dal 60% al 140% di quella della scena. La scelta resta tra le preferenze, come la
    /// lingua: è del giocatore, non della partita.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineCamera))]
    public class CameraZoom : MonoBehaviour
    {
        /// <summary>Dove resta il fattore di zoom tra un avvio e l'altro.</summary>
        public const string Preference = "camera_zoom";

        private readonly ZoomLevel _zoom = new ZoomLevel();
        private CinemachineCamera _camera;
        private PlayerInputReader _reader;
        private float _baseSize;
        private bool _subscribed;

        /// <summary>Lo zoom, per i test.</summary>
        public ZoomLevel Zoom => _zoom;

        /// <summary>La dimensione della vista senza zoom, quella della scena.</summary>
        public float BaseSize => _baseSize;

        /// <summary>Come l'automappa: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(PlayerInputReader reader)
        {
            Unsubscribe();
            _reader = reader;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void Awake()
        {
            _camera = GetComponent<CinemachineCamera>();
            _baseSize = _camera.Lens.OrthographicSize;
            _zoom.Set(PlayerPrefs.GetFloat(Preference, 1f));
            Apply();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        // senza tempo scalato: lo zoom non si ferma durante l'hit stop
        private void Update()
        {
            if (_zoom.Advance(Time.unscaledDeltaTime))
            {
                Apply();
            }
        }

        private void Subscribe()
        {
            if (_subscribed || _reader == null)
            {
                return;
            }

            _reader.ZoomScrolled += HandleZoomScrolled;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _reader.ZoomScrolled -= HandleZoomScrolled;
            _subscribed = false;
        }

        private void HandleZoomScrolled(int notches)
        {
            if (_zoom.Scroll(notches))
            {
                PlayerPrefs.SetFloat(Preference, _zoom.Target);
                PlayerPrefs.Save();
            }
        }

        private void Apply()
        {
            _camera.Lens.OrthographicSize = _baseSize * _zoom.Current;
        }
    }
}
