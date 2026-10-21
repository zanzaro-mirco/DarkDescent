using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Player
{
    /// <summary>
    /// Il rallentamento del cavaliere (il Gelido dei campioni, D5 della M12): per qualche secondo
    /// l'agent va più piano e il modello prende una tinta fredda. È l'unico che cambia la velocità
    /// dell'agent: un secondo colpo rinnova la durata senza sommarsi, e alla fine torna quella di prima.
    /// Dalla M13 tiene anche la velocità in più del santuario del Vento.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public class MovementSlow : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("La tinta del modello mentre è rallentato.")]
        [SerializeField] private Color _tint = new Color(0.45f, 0.7f, 1f, 1f);

        [Tooltip("I cristalli di ghiaccio attorno al cavaliere, acceso solo mentre è rallentato (v1.1.1).")]
        [SerializeField] private GameObject _effect;

        private NavMeshAgent _agent;
        private MaterialPropertyBlock _block;
        private Renderer[] _renderers;
        private float _baseSpeed;
        private float _fraction;
        private float _remaining;
        private float _haste;

        public bool IsSlowed => _remaining > 0f;

        /// <summary>Quanto della velocità si perde ora, tra 0 e 1.</summary>
        public float Fraction => IsSlowed ? _fraction : 0f;

        public float BaseSpeed => _baseSpeed;

        /// <summary>Quanto della velocità si guadagna ora, dal santuario del Vento (M13): 0,25 vuol dire +25%.</summary>
        public float Haste => _haste;

        /// <summary>I cristalli di ghiaccio, o null.</summary>
        public GameObject Effect => _effect;

        /// <summary>Rallenta di <paramref name="fraction"/> per <paramref name="duration"/> secondi; già rallentato, tiene il più forte e il più lungo.</summary>
        public void Apply(float fraction, float duration)
        {
            if (duration <= 0f || fraction <= 0f)
            {
                return;
            }

            bool wasSlowed = IsSlowed;
            _fraction = wasSlowed ? Mathf.Max(_fraction, Mathf.Clamp01(fraction)) : Mathf.Clamp01(fraction);
            _remaining = Mathf.Max(_remaining, duration);
            ApplySpeed();
            if (!wasSlowed)
            {
                // i modelli in mano cambiano con l'equipaggiamento: si prendono a ogni rallentamento, non a ogni frame
                _renderers = GetComponentsInChildren<Renderer>();
                SetTint(_tint);
                if (_effect != null)
                {
                    _effect.SetActive(true);
                }
            }
        }

        /// <summary>
        /// La velocità in più del santuario del Vento (M13), finché non la si toglie con 0: si somma al
        /// rallentamento, che ne toglie una parte come alla velocità di sempre.
        /// </summary>
        public void SetHaste(float fraction)
        {
            _haste = Mathf.Max(0f, fraction);
            ApplySpeed();
        }

        private void ApplySpeed()
        {
            _agent.speed = _baseSpeed * (1f - Fraction) * (1f + _haste);
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _baseSpeed = _agent.speed;
            _block = new MaterialPropertyBlock();
            if (_effect != null)
            {
                _effect.SetActive(false);
            }
        }

        private void Update()
        {
            if (!IsSlowed)
            {
                return;
            }

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                Clear();
            }
        }

        private void OnDisable()
        {
            if (IsSlowed)
            {
                Clear();
            }
        }

        private void Clear()
        {
            _remaining = 0f;
            _fraction = 0f;
            ApplySpeed();
            SetTint(Color.white);
            if (_effect != null)
            {
                _effect.SetActive(false);
            }
        }

        private void SetTint(Color color)
        {
            if (_renderers == null)
            {
                return;
            }

            foreach (var renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, color);
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
