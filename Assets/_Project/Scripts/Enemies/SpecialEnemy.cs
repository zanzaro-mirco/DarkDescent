using System;
using System.Collections.Generic;
using DarkDescent.Combat;
using DarkDescent.Player;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Un nemico speciale (M12): campione o unico, con i suoi modificatori. Gli unici lo hanno nel
    /// prefab, con modificatori e tinta fissi (D6); un nemico normale lo riceve dal builder quando la
    /// mappa lo fa campione (direttiva <c>@champions</c>). Il composition root lo lega con le
    /// <see cref="ChampionSettings"/>: allora prende vita e danno del suo grado, la scala, la tinta,
    /// l'anello sotto i piedi (D9) e il comportamento dei modificatori (D5).
    /// </summary>
    [DisallowMultipleComponent]
    public class SpecialEnemy : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private EnemyRank _rank = EnemyRank.Champion;

        [Tooltip("Gli id dei modificatori: fissi per gli unici, scelti dal seme per i campioni.")]
        [SerializeField] private string[] _modifiers = Array.Empty<string>();

        [Tooltip("La tinta del modello degli unici; i campioni prendono il colore del loro grado.")]
        [SerializeField] private Color _tint = Color.white;

        private readonly List<ChampionModifier> _active = new List<ChampionModifier>();
        private Health _health;
        private MeleeAttack _attack;
        private RangedAttack _ranged;
        private RankNumbers _numbers;
        private GameObject _ring;
        private Health _player;
        private MovementSlow _playerSlow;
        private bool _playerSubscribed;
        private ChampionModifier _frozen;
        private ChampionModifier _regenerating;
        private ChampionModifier _enraged;
        private bool _isEnraged;
        private float _sinceHit;
        private float _regenTimer;

        public EnemyRank Rank => _rank;

        public IReadOnlyList<string> Modifiers => _modifiers;

        public Color Tint => _tint;

        /// <summary>I modificatori risolti dal composition root; vuoto prima del <see cref="Bind"/>.</summary>
        public IReadOnlyList<ChampionModifier> ActiveModifiers => _active;

        /// <summary>I numeri del suo grado, o null prima del <see cref="Bind"/>.</summary>
        public RankNumbers Numbers => _numbers;

        public bool IsBound => _numbers != null;

        /// <summary>Quanta esperienza dà in più di un nemico normale dello stesso tipo.</summary>
        public float ExperienceMultiplier => _numbers != null ? _numbers.Experience : 1f;

        /// <summary>Il Furioso è scattato: sotto la soglia della vita fa più danno.</summary>
        public bool IsEnraged => _isEnraged;

        /// <summary>L'anello sotto i piedi, o null.</summary>
        public GameObject Ring => _ring;

        /// <summary>Lo fa campione con questi modificatori: lo chiama il builder, prima che il nemico si svegli.</summary>
        public void Promote(EnemyRank rank, IReadOnlyList<string> modifiers)
        {
            _rank = rank;
            _modifiers = new string[modifiers != null ? modifiers.Count : 0];
            for (int i = 0; i < _modifiers.Length; i++)
            {
                _modifiers[i] = modifiers[i];
            }
        }

        /// <summary>
        /// Gli dà i numeri del grado e i modificatori, una volta sola; <paramref name="player"/> e
        /// <paramref name="playerSlow"/> servono al Gelido.
        /// </summary>
        public void Bind(ChampionSettings settings, Health player, MovementSlow playerSlow)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            UnsubscribePlayer();
            _player = player;
            _playerSlow = playerSlow;
            if (_numbers == null)
            {
                Apply(settings);
            }

            if (isActiveAndEnabled)
            {
                SubscribePlayer();
            }
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _attack = GetComponent<MeleeAttack>();
            _ranged = GetComponent<RangedAttack>();
        }

        private void OnEnable()
        {
            _health.Damaged += HandleDamaged;
            _health.Died += HandleDied;
            SubscribePlayer();
        }

        private void OnDisable()
        {
            _health.Damaged -= HandleDamaged;
            _health.Died -= HandleDied;
            UnsubscribePlayer();
        }

        private void Update()
        {
            if (_regenerating == null || _health.IsDead || _health.Current >= _health.Max)
            {
                return;
            }

            // fermo per un po' dopo ogni colpo, poi una parte della vita al secondo, a scatti di un secondo
            _sinceHit += Time.deltaTime;
            if (_sinceHit < _regenerating.Duration)
            {
                return;
            }

            _regenTimer += Time.deltaTime;
            if (_regenTimer >= 1f)
            {
                _regenTimer -= 1f;
                _health.Heal(_health.Max * _regenerating.Magnitude);
            }
        }

        private void Apply(ChampionSettings settings)
        {
            _numbers = settings.For(_rank) ?? new RankNumbers();
            _active.Clear();
            foreach (var id in _modifiers)
            {
                var modifier = settings.Find(id);
                if (modifier != null)
                {
                    _active.Add(modifier);
                }
            }

            _health.MultiplyMax(_numbers.Life);
            SetDamage(_numbers.Damage);
            transform.localScale *= _numbers.Scale;
            ApplyTint(_rank == EnemyRank.Unique ? _tint : Color.Lerp(Color.white, _numbers.Color, settings.ChampionTint));
            if (settings.Ring != null)
            {
                _ring = Instantiate(settings.Ring, transform);
                _ring.name = "SpecialRing";
                _ring.transform.localPosition = Vector3.up * settings.RingHeight;
                var renderer = _ring.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    block.SetColor(BaseColorId, _numbers.Color);
                    renderer.SetPropertyBlock(block);
                }
            }

            foreach (var modifier in _active)
            {
                switch (modifier.Kind)
                {
                    case ChampionModifierKind.Fast:
                        float speed = 1f + modifier.Magnitude;
                        if (TryGetComponent(out NavMeshAgent agent))
                        {
                            agent.speed *= speed;
                        }

                        if (_attack != null)
                        {
                            _attack.SetSpeedMultiplier(speed);
                        }

                        if (_ranged != null)
                        {
                            _ranged.SetSpeedMultiplier(speed);
                        }

                        break;
                    case ChampionModifierKind.Frozen:
                        _frozen = modifier;
                        break;
                    case ChampionModifierKind.Regenerating:
                        _regenerating = modifier;
                        break;
                    case ChampionModifierKind.Unstoppable:
                        if (TryGetComponent(out HitRecovery recovery))
                        {
                            recovery.enabled = false;
                        }

                        if (_attack != null)
                        {
                            _attack.SetUnblockable(true);
                        }

                        break;
                    case ChampionModifierKind.Enraged:
                        _enraged = modifier;
                        break;
                }
            }
        }

        private void SetDamage(float multiplier)
        {
            if (_attack != null)
            {
                _attack.SetDamageMultiplier(multiplier);
            }

            if (_ranged != null)
            {
                _ranged.SetDamageMultiplier(multiplier);
            }
        }

        // la tinta con un MaterialPropertyBlock: i modelli KayKit condividono il materiale (trappola della M12)
        private void ApplyTint(Color color)
        {
            var block = new MaterialPropertyBlock();
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                renderer.SetPropertyBlock(block);
            }
        }

        private void HandleDamaged(DamageInfo info, float applied)
        {
            _sinceHit = 0f;
            _regenTimer = 0f;
            if (_enraged != null && !_isEnraged && !_health.IsDead && _health.Current <= _health.Max * _enraged.Threshold)
            {
                _isEnraged = true;
                SetDamage(_numbers.Damage * (1f + _enraged.Magnitude));
            }
        }

        private void HandleDied()
        {
            if (_ring != null)
            {
                _ring.SetActive(false);
            }
        }

        // il Gelido: ogni colpo suo che arriva al cavaliere lo rallenta
        private void HandlePlayerDamaged(DamageInfo info, float applied)
        {
            if (info.Source == gameObject && _frozen != null && _playerSlow != null && !_health.IsDead)
            {
                _playerSlow.Apply(_frozen.Magnitude, _frozen.Duration);
            }
        }

        private void SubscribePlayer()
        {
            if (_player != null && !_playerSubscribed)
            {
                _player.Damaged += HandlePlayerDamaged;
                _playerSubscribed = true;
            }
        }

        private void UnsubscribePlayer()
        {
            if (_player != null && _playerSubscribed)
            {
                _player.Damaged -= HandlePlayerDamaged;
            }

            _playerSubscribed = false;
        }
    }
}
