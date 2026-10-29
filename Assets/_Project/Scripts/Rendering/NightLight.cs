using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Una luce che si accende dal tramonto all'alba (D5 della M18): le lanterne e le finestre del borgo.
    /// La accende la <see cref="DayCycleView"/> del livello, con quanto è buio. Con la luce brilla il vetro
    /// dei suoi modelli, dove la maschera dell'atlante lo dice (D5 della M19), e nei filmati la candela si
    /// spegne e si riaccende da sola, una casa alla volta.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class NightLight : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [Tooltip("I modelli il cui vetro brilla con la luce: la casa, la lanterna.")]
        [SerializeField] private Renderer[] _glows = new Renderer[0];

        [Tooltip("Il colore del vetro acceso del tutto, in HDR.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _glowColor = Color.black;

        private Light _light;
        private float _intensity;
        private MaterialPropertyBlock _block;
        private float _flame = 1f;
        private float _flameTarget = 1f;
        private float _flameSpeed;

        /// <summary>Quanto è buio per la vista del giorno: 0 di giorno, 1 di notte.</summary>
        public float Lit { get; private set; }

        /// <summary>La candela: 1 accesa, 0 spenta, in mezzo mentre si spegne o si riaccende.</summary>
        public float Flame => _flame;

        /// <summary>Quanto è accesa davvero: il buio per la candela.</summary>
        public float Level => Lit * _flame;

        /// <summary>Quanti modelli brillano con lei.</summary>
        public int GlowCount => _glows.Length;

        /// <summary>0 spenta, 1 accesa del tutto: in mezzo nel passaggio tra giorno e tramonto.</summary>
        public void SetLit(float amount)
        {
            Init();
            Lit = Mathf.Clamp01(amount);
            Apply();
        }

        /// <summary>Spegne la candela in <paramref name="seconds"/> secondi, subito con 0.</summary>
        public void Snuff(float seconds)
        {
            FadeFlame(0f, seconds);
        }

        /// <summary>Riaccende la candela in <paramref name="seconds"/> secondi, subito con 0.</summary>
        public void Relight(float seconds)
        {
            FadeFlame(1f, seconds);
        }

        private void FadeFlame(float target, float seconds)
        {
            Init();
            _flameTarget = target;
            if (seconds > 0f)
            {
                _flameSpeed = 1f / seconds;
                return;
            }

            _flame = target;
            Apply();
        }

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            if (_flame == _flameTarget)
            {
                return;
            }

            _flame = Mathf.MoveTowards(_flame, _flameTarget, _flameSpeed * Time.deltaTime);
            Apply();
        }

        private void Apply()
        {
            float level = Level;
            _light.enabled = level > 0.01f;
            _light.intensity = _intensity * level;
            if (_glows.Length == 0)
            {
                return;
            }

            _block ??= new MaterialPropertyBlock();
            foreach (var glow in _glows)
            {
                if (glow == null)
                {
                    continue;
                }

                glow.GetPropertyBlock(_block);
                _block.SetColor(EmissionColor, _glowColor * level);
                glow.SetPropertyBlock(_block);
            }
        }

        private void Init()
        {
            if (_light != null)
            {
                return;
            }

            _light = GetComponent<Light>();
            _intensity = _light.intensity;
        }
    }
}
