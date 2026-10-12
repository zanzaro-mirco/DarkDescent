using System;
using System.Collections.Generic;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Levels;
using DarkDescent.Magic;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Il Re delle ossa (D8 della M10), sopra un <see cref="EnemyAI"/> con i numeri e il colpo caricato
    /// del bruto. Aspetta al centro della sua sala; quando il cavaliere è ben dentro, la porta si chiude
    /// e comincia lo scontro. Tre fasi per vita (<see cref="BossPhases"/>): sopra il 60% solo mischia;
    /// fino al 30% evoca un gruppo dello sciame a intervalli; sotto è più veloce e lancia l'anello di
    /// fuoco, annunciato da un cerchio rosso a terra. Chi lega gli evocati al cavaliere, ricorda la
    /// vittoria e mostra la sua vita è il composition root, sugli eventi.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyAI), typeof(Health), typeof(MeleeAttack))]
    public class BossController : MonoBehaviour
    {
        [Header("Lo scontro")]
        [Tooltip("Lo scontro comincia, e la porta si chiude, quando il cavaliere arriva a tanti metri dal centro della sala: ben dentro, la porta è a 12.")]
        [SerializeField, Min(1f)] private float _engageRadius = 8f;

        [Tooltip("Svegliato prima, da un incantesimo, lo scontro comincia se il cavaliere è entro tanti metri dal centro: dentro la sala.")]
        [SerializeField, Min(1f)] private float _roomRadius = 10f;

        [SerializeField] private AudioSource _audio;

        [Tooltip("Il ruggito all'inizio dello scontro.")]
        [SerializeField] private AudioClip _roarClip;

        [Header("Evocazione (fase 2)")]
        [SerializeField] private EnemyAI _summonPrefab;

        [SerializeField, Min(1)] private int _summonCount = 3;

        [Tooltip("Mai più di tanti evocati vivi insieme.")]
        [SerializeField, Min(1)] private int _maxSummons = 6;

        [SerializeField, Min(1f)] private float _summonInterval = 12f;

        [Tooltip("A quanti metri dal boss nascono gli evocati, su punti del NavMesh della sala.")]
        [SerializeField, Min(1f)] private float _summonDistance = 3.5f;

        [Header("Furia (fase 3)")]
        [Tooltip("La velocità in furia, in multipli di quella normale.")]
        [SerializeField, Min(1f)] private float _furySpeed = 1.4f;

        [SerializeField, Min(1f)] private float _ringInterval = 7f;

        [Tooltip("Quanto dura il cerchio a terra prima che l'anello parta: il tempo per uscirne.")]
        [SerializeField, Min(0.2f)] private float _ringWindup = 1.4f;

        [SerializeField, Min(1f)] private float _ringRadius = 5f;

        [Tooltip("Quanto si aggiunge al raggio per il corpo del cavaliere.")]
        [SerializeField, Min(0f)] private float _targetAllowance = 0.45f;

        [SerializeField] private Vector2Int _ringDamage = new Vector2Int(28, 38);

        [SerializeField] private TelegraphCircle _ringTelegraph;

        [Tooltip("Le fiamme dell'anello: lo stesso effetto dell'incantesimo del cavaliere.")]
        [SerializeField] private AreaEffect _ringEffectPrefab;

        private EnemyAI _ai;
        private Health _health;
        private MeleeAttack _attack;
        private NavMeshAgent _agent;
        private Health _player;
        private BossGate _gate;
        private IRandomSource _random;
        private AreaEffect _ringEffect;
        private readonly List<EnemyAI> _summons = new List<EnemyAI>();
        private float _baseSpeed;
        private float _summonTimer;
        private float _ringTimer;
        private float _castTimer;

        /// <summary>Lo scontro è cominciato: la porta si chiude, la sua vita resta in alto.</summary>
        public event Action<BossController> FightStarted;

        /// <summary>Lo scontro è finito senza vittoria: il cavaliere è tornato in vita all'ingresso.</summary>
        public event Action<BossController> FightReset;

        /// <summary>È morto: la vittoria va nel salvataggio.</summary>
        public event Action<BossController> Defeated;

        /// <summary>Un evocato appena nato, da legare al cavaliere come i nemici del livello.</summary>
        public event Action<EnemyAI> Summoned;

        public bool IsFighting { get; private set; }

        public BossPhase Phase { get; private set; }

        public bool IsCastingRing { get; private set; }

        public float RingRadius => _ringRadius;

        public IReadOnlyList<EnemyAI> Summons => _summons;

        public Health Health => _health;

        public void Bind(Health player, BossGate gate, IRandomSource random)
        {
            _player = player;
            _gate = gate;
            _random = random;
        }

        private void Awake()
        {
            _ai = GetComponent<EnemyAI>();
            _health = GetComponent<Health>();
            _attack = GetComponent<MeleeAttack>();
            _agent = GetComponent<NavMeshAgent>();
            _baseSpeed = _agent.speed;

            // nella scena attiva, quella del livello: si scarica con lui
            if (_ringEffectPrefab != null)
            {
                _ringEffect = Instantiate(_ringEffectPrefab);
                _ringEffect.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            _health.Died += HandleDied;
        }

        private void OnDisable()
        {
            _health.Died -= HandleDied;
        }

        private void Update()
        {
            if (_health.IsDead || _player == null)
            {
                return;
            }

            if (!IsFighting)
            {
                if (ShouldEngage())
                {
                    StartFight();
                }

                return;
            }

            // da morto il cavaliere aspetta di tornare in vita: ResetFight
            if (_player.IsDead)
            {
                return;
            }

            UpdatePhase();
            if (IsCastingRing)
            {
                _castTimer -= Time.deltaTime;
                if (_castTimer <= 0f)
                {
                    ReleaseRing();
                }

                return;
            }

            if (Phase == BossPhase.Summon)
            {
                _summonTimer -= Time.deltaTime;
                if (_summonTimer <= 0f)
                {
                    _summonTimer = _summonInterval;
                    Summon();
                }
            }
            else if (Phase == BossPhase.Fury)
            {
                _ringTimer -= Time.deltaTime;
                if (_ringTimer <= 0f && !_attack.IsSwinging)
                {
                    _ringTimer = _ringInterval;
                    BeginRing();
                }
            }
        }

        private bool ShouldEngage()
        {
            if (_player.IsDead)
            {
                return false;
            }

            float distance = FlatDistance(_player.transform.position, _ai.HomePosition);
            bool awake = _ai.State != EnemyState.Idle && _ai.State != EnemyState.Return;
            return distance <= _engageRadius || (awake && distance <= _roomRadius);
        }

        private void StartFight()
        {
            IsFighting = true;
            Phase = BossPhase.Melee;
            _summonTimer = 0f;
            _ringTimer = 1f;
            if (_gate != null)
            {
                _gate.Close();
            }

            if (_audio != null && _roarClip != null)
            {
                _audio.PlayOneShot(_roarClip);
            }

            _ai.Alert();
            FightStarted?.Invoke(this);
        }

        // la vita scende soltanto durante lo scontro: le fasi vanno solo avanti
        private void UpdatePhase()
        {
            var phase = BossPhases.For(_health.Current, _health.Max);
            if (phase == Phase)
            {
                return;
            }

            Phase = phase;
            if (phase == BossPhase.Summon)
            {
                _summonTimer = 0f;
            }
            else if (phase == BossPhase.Fury)
            {
                _agent.speed = _baseSpeed * _furySpeed;
                _ringTimer = 0.5f;
            }
        }

        private void Summon()
        {
            _summons.RemoveAll(s => s == null || s.GetComponent<Health>().IsDead);
            int count = Mathf.Min(_summonCount, _maxSummons - _summons.Count);
            Vector3 origin = transform.position;
            float offset = _random != null ? (float)_random.NextDouble() * 360f : 0f;
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, offset + 360f * i / count, 0f) * Vector3.forward;
                if (!NavMesh.SamplePosition(origin + direction * _summonDistance, out var hit, 2f, NavMesh.AllAreas)
                    || NavMesh.Raycast(origin, hit.position, out _, NavMesh.AllAreas))
                {
                    continue;
                }

                Vector3 facing = _player.transform.position - hit.position;
                facing.y = 0f;
                var summoned = Instantiate(_summonPrefab, hit.position, facing.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(facing) : transform.rotation);
                _summons.Add(summoned);
                Summoned?.Invoke(summoned);
            }
        }

        private void BeginRing()
        {
            IsCastingRing = true;
            _castTimer = _ringWindup;

            // fermo mentre carica: niente colpi né passi
            _attack.Interrupt(_ringWindup + 0.2f);
            if (_ringTelegraph != null)
            {
                _ringTelegraph.Show(_ringRadius, _ringWindup);
            }
        }

        private void ReleaseRing()
        {
            CancelRing();
            Vector3 center = transform.position;
            if (_ringEffect != null)
            {
                _ringEffect.Play(center, _ringRadius);
            }

            if (!_player.IsDead && FlatDistance(_player.transform.position, center) <= _ringRadius + _targetAllowance)
            {
                int amount = _random != null ? _ringDamage.x + (int)(_random.NextDouble() * (_ringDamage.y - _ringDamage.x + 1)) : _ringDamage.x;
                _player.TakeDamage(new DamageInfo(Mathf.Min(amount, _ringDamage.y), DamageType.Fire, gameObject));
            }
        }

        private void CancelRing()
        {
            IsCastingRing = false;
            if (_ringTelegraph != null)
            {
                _ringTelegraph.Hide();
            }
        }

        /// <summary>
        /// Il cavaliere è tornato in vita all'ingresso: la porta si riapre, gli evocati spariscono e il
        /// boss, rimandato a casa a vita piena dal suo EnemyAI, ricomincia dalla prima fase.
        /// </summary>
        public void ResetFight()
        {
            if (_health.IsDead || !IsFighting)
            {
                return;
            }

            IsFighting = false;
            CancelRing();
            Phase = BossPhase.Melee;
            _agent.speed = _baseSpeed;
            if (_gate != null)
            {
                _gate.Open();
            }

            foreach (var summoned in _summons)
            {
                if (summoned != null)
                {
                    Destroy(summoned.gameObject);
                }
            }

            _summons.Clear();
            FightReset?.Invoke(this);
        }

        // morto il re, i suoi evocati cadono con lui e la porta si apre
        private void HandleDied()
        {
            IsFighting = false;
            CancelRing();
            if (_gate != null)
            {
                _gate.Open();
            }

            foreach (var summoned in _summons)
            {
                if (summoned != null && summoned.TryGetComponent(out Health health) && !health.IsDead)
                {
                    health.TakeDamage(new DamageInfo(health.Current, DamageType.Physical, gameObject));
                }
            }

            _summons.Clear();
            Defeated?.Invoke(this);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
