using DarkDescent.Stats;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Player
{
    /// <summary>
    /// Il rallentamento del cavaliere (il Gelido dei campioni, D5 della M12): per qualche secondo
    /// l'agent va più piano e il modello prende una tinta fredda. È l'unico che cambia la velocità
    /// dell'agent: un secondo colpo rinnova la durata senza sommarsi, e alla fine torna quella di prima.
    /// Dalla M13 tiene anche la velocità in più del santuario del Vento; dalla M20 il Passo pesante
    /// dell'albero (la statistica della velocità di movimento) e le abilità che fanno camminare piano.
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
        private float _factor = 1f;
        private CharacterStats _stats;

        public bool IsSlowed => _remaining > 0f;

        /// <summary>Quanto della velocità si perde ora, tra 0 e 1.</summary>
        public float Fraction => IsSlowed ? _fraction : 0f;

        public float BaseSpeed => _baseSpeed;

        /// <summary>Quanto della velocità si guadagna ora, dal santuario del Vento (M13): 0,25 vuol dire +25%.</summary>
        public float Haste => _haste;

        /// <summary>La velocità di un'abilità in corso, in parte di quella normale (M20): 0,5 per il Muro di scudi, 1 senza.</summary>
        public float AbilityFactor => _factor;

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

        /// <summary>La velocità di un'abilità finché dura (M20): sotto 1 cammina piano, sopra corre (la Carica); 1 la rimette.</summary>
        public void SetAbilityFactor(float factor)
        {
            _factor = Mathf.Clamp(factor, 0f, 4f);
            ApplySpeed();
        }

        // il Vento e il Passo pesante si sommano; il rallentamento e l'abilità tolgono una parte del totale
        private void ApplySpeed()
        {
            float bonus = _stats != null ? _stats.Sheet.Get(StatType.MoveSpeed) / 100f : 0f;
            _agent.speed = _baseSpeed * (1f - Fraction) * (1f + _haste + bonus) * _factor;
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _baseSpeed = _agent.speed;
            TryGetComponent(out _stats);
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

        private void OnEnable()
        {
            if (_stats != null)
            {
                _stats.Sheet.Changed += ApplySpeed;
                ApplySpeed();
            }
        }

        private void OnDisable()
        {
            if (_stats != null)
            {
                _stats.Sheet.Changed -= ApplySpeed;
            }

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
