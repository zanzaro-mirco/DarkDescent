using DarkDescent.Characters;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Magic;
using UnityEngine;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Il lancio di un nemico a distanza (D6 della M9): si gira verso il bersaglio, fa il gesto e,
    /// quando la mano arriva avanti, il dardo parte verso dove il bersaglio è in quel momento. Un
    /// lancio ogni <c>_interval</c> secondi. I dardi si possono parare con lo scudo (D7). Lo
    /// collega il CompositionRoot con il pool dei proiettili e i tiri del combattimento.
    /// </summary>
    [DisallowMultipleComponent]
    public class RangedAttack : MonoBehaviour
    {
        [Tooltip("Il proiettile, preso dal pool dei proiettili.")]
        [SerializeField] private GameObject _projectile;

        [SerializeField, Min(0)] private int _minDamage = 4;

        [SerializeField, Min(0)] private int _maxDamage = 8;

        [Tooltip("Secondi tra un lancio e il successivo.")]
        [SerializeField, Min(0.1f)] private float _interval = 2f;

        [Tooltip("Secondi tra l'inizio del gesto e la partenza del dardo.")]
        [SerializeField, Min(0f)] private float _castDelay = 0.4f;

        [Tooltip("Metri al secondo: più lento del dardo del cavaliere, per poterlo schivare.")]
        [SerializeField, Min(0.1f)] private float _speed = 9f;

        [SerializeField, Min(0.1f)] private float _range = 14f;

        [Tooltip("Altezza da terra da cui parte il dardo: quella del petto del cavaliere.")]
        [SerializeField, Min(0f)] private float _launchHeight = 1.1f;

        [SerializeField, Min(0f)] private float _launchForward = 0.5f;

        [Tooltip("Il cavaliere: il solo che i dardi del mago colpiscono.")]
        [SerializeField] private LayerMask _targetLayers;

        [Tooltip("Gradi al secondo con cui si gira verso il bersaglio mentre lancia.")]
        [SerializeField, Min(0f)] private float _turnSpeed = 720f;

        [SerializeField] private AudioSource _audio;

        [SerializeField] private AudioClip _castClip;

        private CharacterAnimatorDriver _animator;
        private ProjectilePools _pools;
        private IRandomSource _random;
        private Health _target;
        private float _cooldown;
        private float _castTimer = -1f;
        private float _damageMultiplier = 1f;
        private float _difficultyMultiplier = 1f;
        private float _speedMultiplier = 1f;

        /// <summary>Il gesto è partito e il dardo non ancora.</summary>
        public bool IsCasting => _castTimer >= 0f;

        /// <summary>Pronto a lanciare: collegato, fermo e senza ricarica.</summary>
        public bool IsReady => !IsCasting && _cooldown <= 0f && _pools != null && _projectile != null;

        public int MinDamage => _minDamage;

        /// <summary>Il danno in più del grado e della difficoltà insieme (M14).</summary>
        public float DamageMultiplier => _damageMultiplier * _difficultyMultiplier;

        /// <summary>Moltiplica il danno dei dardi: i campioni e gli unici, e il Furioso (M12).</summary>
        public void SetDamageMultiplier(float multiplier)
        {
            _damageMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>Moltiplica il danno dei dardi per la difficoltà (D5 della M14), insieme a quello del grado.</summary>
        public void SetDifficultyMultiplier(float multiplier)
        {
            _difficultyMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>Lancia più spesso di tanto: il Rapido (M12).</summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public int MaxDamage => _maxDamage;

        public void Bind(ProjectilePools pools, IRandomSource random)
        {
            _pools = pools;
            _random = random;
        }

        /// <summary>Cambia i tiri di danno e blocco: i test li fissano.</summary>
        public void SetRandomSource(IRandomSource random)
        {
            _random = random;
        }

        /// <summary>Comincia il gesto verso <paramref name="target"/>; il dardo parte dopo il ritardo.</summary>
        public void Begin(Health target)
        {
            if (!IsReady || target == null)
            {
                return;
            }

            _target = target;
            _castTimer = _castDelay / _speedMultiplier;
            _cooldown = _interval / _speedMultiplier;
            if (_animator != null)
            {
                _animator.PlayCast();
            }

            if (_audio != null && _castClip != null)
            {
                _audio.PlayOneShot(_castClip);
            }
        }

        /// <summary>Annulla un gesto in corso: il dardo non parte.</summary>
        public void Cancel()
        {
            _castTimer = -1f;
            _target = null;
        }

        /// <summary>Si gira verso il bersaglio, a velocità limitata.</summary>
        public void Face(Vector3 point, float deltaTime)
        {
            Vector3 flat = point - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), _turnSpeed * deltaTime);
            }
        }

        private void Awake()
        {
            _animator = GetComponentInChildren<CharacterAnimatorDriver>();
        }

        private void OnDisable()
        {
            Cancel();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (_cooldown > 0f)
            {
                _cooldown -= deltaTime;
            }

            if (!IsCasting)
            {
                return;
            }

            if (_target == null || _target.IsDead)
            {
                Cancel();
                return;
            }

            Face(_target.transform.position, deltaTime);
            _castTimer -= deltaTime;
            if (_castTimer < 0f)
            {
                Launch();
            }
        }

        private void Launch()
        {
            Vector3 aim = _target.transform.position - transform.position;
            aim.y = 0f;
            _target = null;
            _castTimer = -1f;
            if (aim.sqrMagnitude < 0.0001f)
            {
                aim = transform.forward;
            }

            Vector3 direction = aim.normalized;
            Vector3 from = transform.position + Vector3.up * _launchHeight + direction * _launchForward;
            float damage = _minDamage + (int)(_random.NextDouble() * (_maxDamage - _minDamage + 1));
            _pools.Launch(_projectile, from, direction, _speed, _range, Mathf.Min(damage, _maxDamage) * DamageMultiplier, gameObject, _targetLayers, _random);
        }
    }
}
