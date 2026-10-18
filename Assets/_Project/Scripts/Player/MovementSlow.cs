using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Player
{
    /// <summary>
    /// Il rallentamento del cavaliere (il Gelido dei campioni, D5 della M12): per qualche secondo
    /// l'agent va più piano e il modello prende una tinta fredda. È l'unico che cambia la velocità
    /// dell'agent: un secondo colpo rinnova la durata senza sommarsi, e alla fine torna quella di prima.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public class MovementSlow : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("La tinta del modello mentre è rallentato.")]
        [SerializeField] private Color _tint = new Color(0.6f, 0.8f, 1f, 1f);

        private NavMeshAgent _agent;
        private MaterialPropertyBlock _block;
        private Renderer[] _renderers;
        private float _baseSpeed;
        private float _fraction;
        private float _remaining;

        public bool IsSlowed => _remaining > 0f;

        /// <summary>Quanto della velocità si perde ora, tra 0 e 1.</summary>
        public float Fraction => IsSlowed ? _fraction : 0f;

        public float BaseSpeed => _baseSpeed;

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
            _agent.speed = _baseSpeed * (1f - _fraction);
            if (!wasSlowed)
            {
                // i modelli in mano cambiano con l'equipaggiamento: si prendono a ogni rallentamento, non a ogni frame
                _renderers = GetComponentsInChildren<Renderer>();
                SetTint(_tint);
            }
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _baseSpeed = _agent.speed;
            _block = new MaterialPropertyBlock();
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
            _agent.speed = _baseSpeed;
            SetTint(Color.white);
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
