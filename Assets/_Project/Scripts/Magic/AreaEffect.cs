using UnityEngine;

namespace DarkDescent.Magic
{
    /// <summary>
    /// L'effetto di un incantesimo ad area (D8 della M9): un cerchio di fiamme che nasce ai piedi del
    /// cavaliere e si allarga fino al raggio dell'incantesimo, con un lampo di luce. Il danno lo fa
    /// <see cref="PlayerSpells"/>; questo è solo da vedere e sentire. Uno per prefab, riusato.
    /// </summary>
    [DisallowMultipleComponent]
    public class AreaEffect : MonoBehaviour
    {
        [Tooltip("Le fiamme, emesse sul bordo di un cerchio che si allarga.")]
        [SerializeField] private ParticleSystem _flames;

        [SerializeField] private Light _light;

        [SerializeField] private AudioSource _audio;

        [SerializeField] private AudioClip _clip;

        [Tooltip("Quanto ci mette il cerchio ad arrivare al raggio.")]
        [SerializeField, Min(0.05f)] private float _expandDuration = 0.3f;

        [Tooltip("Il raggio da cui parte il cerchio.")]
        [SerializeField, Min(0.05f)] private float _startRadius = 0.4f;

        [Tooltip("Le fiamme che nascono tutte insieme sul bordo, arrivato al raggio, e restano un poco.")]
        [SerializeField, Min(0)] private int _rimBurst = 160;

        [Tooltip("Quanto vivono le fiamme del bordo.")]
        [SerializeField, Min(0.05f)] private float _rimLifetime = 0.7f;

        [Tooltip("Quanto resta acceso in tutto, fiamme che si spengono comprese.")]
        [SerializeField, Min(0.1f)] private float _lifetime = 1.1f;

        private float _radius;
        private float _elapsed;
        private float _lightIntensity;
        private bool _rimDone;

        /// <summary>Vero dal lancio finché le ultime fiamme non si sono spente.</summary>
        public bool IsPlaying => gameObject.activeSelf;

        private void Awake()
        {
            _lightIntensity = _light != null ? _light.intensity : 0f;
        }

        /// <summary>Parte da <paramref name="center"/> e si allarga fino a <paramref name="radius"/>.</summary>
        public void Play(Vector3 center, float radius)
        {
            transform.position = center;
            _radius = radius;
            _elapsed = 0f;
            _rimDone = false;
            gameObject.SetActive(true);
            SetRadius(_startRadius);
            _flames.Clear();
            _flames.Play();
            if (_audio != null && _clip != null)
            {
                _audio.PlayOneShot(_clip);
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float expand = Mathf.Clamp01(_elapsed / _expandDuration);
            SetRadius(Mathf.Lerp(_startRadius, _radius, 1f - (1f - expand) * (1f - expand)));
            if (expand >= 1f && !_rimDone)
            {
                // mentre si allarga le fiamme vivono poco e lasciano un'onda; sul bordo restano
                _rimDone = true;
                _flames.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                var rim = new ParticleSystem.EmitParams { startLifetime = _rimLifetime };
                _flames.Emit(rim, _rimBurst);
            }

            if (_light != null)
            {
                _light.intensity = _lightIntensity * (1f - Mathf.Clamp01(_elapsed / _lifetime));
                _light.range = Mathf.Max(_radius * 1.5f, 1f);
            }

            if (_elapsed >= _lifetime)
            {
                gameObject.SetActive(false);
            }
        }

        private void SetRadius(float radius)
        {
            var shape = _flames.shape;
            shape.radius = radius;
        }
    }
}
