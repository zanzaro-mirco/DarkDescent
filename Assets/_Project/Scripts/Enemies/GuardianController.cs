using System;
using System.Collections.Generic;
using DarkDescent.Combat;
using DarkDescent.Core;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Vorrak il Custode (D8–D11 della M24), sopra un <see cref="EnemyAI"/> con i numeri e il colpo caricato di un bruto più largo e più
    /// forte. Aspetta nella sala della scala giù della profondità 4, senza porta: la scala resta aperta, e saltarlo vuol dire
    /// passargli accanto. Due fasi (<see cref="GuardianPhases"/>): sopra la metà della vita solo il colpo caricato; sotto, una volta
    /// sola, evoca tre scheletri e poi, a intervalli, carica in linea retta contro il cavaliere, annunciato da una striscia rossa a
    /// terra e seguito da una pausa in cui si può colpire. Chi lega gli evocati al cavaliere, apre il suo tesoro e mostra la sua
    /// vita è il composition root, sugli eventi. Il re ha il suo controllo (<see cref="BossController"/>): porta, anello, tre fasi.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyAI), typeof(Health), typeof(MeleeAttack))]
    public class GuardianController : MonoBehaviour
    {
        /// <summary>A che punto è la carica: ferma, striscia a terra, corsa, pausa dopo la corsa.</summary>
        public enum ChargeStage
        {
            None,
            WindUp,
            Run,
            Recover,
        }

        [Header("Lo scontro")]
        [Tooltip("Lo scontro comincia quando il cavaliere arriva a tanti metri da dove aspetta.")]
        [SerializeField, Min(1f)] private float _engageRadius = 8f;

        [Tooltip("Svegliato prima, da un incantesimo, lo scontro comincia se il cavaliere è entro tanti metri.")]
        [SerializeField, Min(1f)] private float _roomRadius = 10f;

        [SerializeField] private AudioSource _audio;

        [Tooltip("Il ruggito all'inizio dello scontro.")]
        [SerializeField] private AudioClip _roarClip;

        [Header("Evocazione (seconda fase)")]
        [SerializeField] private EnemyAI _summonPrefab;

        [SerializeField, Min(1)] private int _summonCount = 3;

        [Tooltip("A quanti metri dal Custode nascono gli evocati, su punti del NavMesh.")]
        [SerializeField, Min(1f)] private float _summonDistance = 3.5f;

        [Header("La carica (seconda fase)")]
        [Tooltip("Ogni quanti secondi, a carica finita, ne prepara un'altra.")]
        [SerializeField, Min(1f)] private float _chargeInterval = 8f;

        [Tooltip("Appena in seconda fase, dopo quanti secondi la prima.")]
        [SerializeField, Min(0.5f)] private float _firstChargeDelay = 4f;

        [Tooltip("Quanto dura la striscia rossa prima che parta: il tempo per levarsi di mezzo.")]
        [SerializeField, Min(0.3f)] private float _chargeWindup = 1.1f;

        [Tooltip("La velocità della corsa, in metri al secondo.")]
        [SerializeField, Min(1f)] private float _chargeSpeed = 13f;

        [Tooltip("La corsa finisce dopo tanti metri, o prima contro un muro o il cavaliere.")]
        [SerializeField, Min(1f)] private float _chargeDistance = 12f;

        [Tooltip("Non carica da più vicino di così. Zero: carica anche da vicino, e il cavaliere che resta fermo davanti a lui la prende subito.")]
        [SerializeField, Min(0f)] private float _chargeMinDistance;

        [Tooltip("La larghezza della striscia e di quello che colpisce, in metri.")]
        [SerializeField, Min(0.5f)] private float _chargeWidth = 2f;

        [Tooltip("Quanto si aggiunge alla metà della larghezza per il corpo del cavaliere.")]
        [SerializeField, Min(0f)] private float _targetAllowance = 0.45f;

        [Tooltip("Il danno della carica, come i colpi: la difficoltà e il Furioso lo moltiplicano.")]
        [SerializeField] private Vector2Int _chargeDamage = new Vector2Int(30, 42);

        [Tooltip("Dopo la corsa sta fermo tanti secondi: la finestra per colpirlo.")]
        [SerializeField, Min(0.3f)] private float _chargeRecover = 1.6f;

        [SerializeField] private TelegraphLine _chargeTelegraph;

        private EnemyAI _ai;
        private Health _health;
        private MeleeAttack _attack;
        private NavMeshAgent _agent;
        private Health _player;
        private IRandomSource _random;
        private readonly List<EnemyAI> _summons = new List<EnemyAI>();
        private bool _hasSummoned;
        private float _chargeTimer;
        private float _stageTimer;
        private float _traveled;
        private bool _chargeHit;
        private Vector3 _chargeDirection;

        /// <summary>Lo scontro è cominciato: la sua vita resta in alto.</summary>
        public event Action<GuardianController> FightStarted;

        /// <summary>Lo scontro è finito senza vittoria: il cavaliere è tornato in vita all'ingresso.</summary>
        public event Action<GuardianController> FightReset;

        /// <summary>È morto: il suo tesoro si apre.</summary>
        public event Action<GuardianController> Defeated;

        /// <summary>Un evocato appena nato, da legare al cavaliere come i nemici del livello.</summary>
        public event Action<EnemyAI> Summoned;

        public bool IsFighting { get; private set; }

        public GuardianPhase Phase { get; private set; }

        public ChargeStage Stage { get; private set; }

        public bool HasSummoned => _hasSummoned;

        public IReadOnlyList<EnemyAI> Summons => _summons;

        public Health Health => _health;

        public float ChargeDistance => _chargeDistance;

        public float ChargeWidth => _chargeWidth;

        public float ChargeSpeed => _chargeSpeed;

        public Vector3 ChargeDirection => _chargeDirection;

        public void Bind(Health player, IRandomSource random)
        {
            _player = player;
            _random = random;
        }

        private void Awake()
        {
            _ai = GetComponent<EnemyAI>();
            _health = GetComponent<Health>();
            _attack = GetComponent<MeleeAttack>();
            _agent = GetComponent<NavMeshAgent>();
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

            // da morto il cavaliere aspetta di tornare in vita: ResetFight. Stordito o congelato (M20, M21) si ferma tutto: la carica salta
            if (_player.IsDead)
            {
                return;
            }

            if (_ai.IsImmobilized)
            {
                if (Stage != ChargeStage.None)
                {
                    EndCharge();
                }

                return;
            }

            UpdatePhase();
            if (Stage != ChargeStage.None)
            {
                TickCharge(Time.deltaTime);
                return;
            }

            if (Phase == GuardianPhase.Wrath)
            {
                _chargeTimer -= Time.deltaTime;
                if (_chargeTimer <= 0f && !_attack.IsSwinging && FlatDistance(_player.transform.position, transform.position) >= _chargeMinDistance)
                {
                    BeginCharge();
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
            bool awake = _ai.State != EnemyState.Idle && _ai.State != EnemyState.Return && _ai.State != EnemyState.Patrol;
            return distance <= _engageRadius || (awake && distance <= _roomRadius);
        }

        private void StartFight()
        {
            IsFighting = true;
            Phase = GuardianPhase.Strike;
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
            var phase = GuardianPhases.For(_health.Current, _health.Max);
            if (phase == Phase)
            {
                return;
            }

            Phase = phase;
            if (phase == GuardianPhase.Wrath)
            {
                _chargeTimer = _firstChargeDelay;
                if (!_hasSummoned)
                {
                    _hasSummoned = true;
                    Summon();
                }
            }
        }

        private void Summon()
        {
            _summons.RemoveAll(s => s == null);
            Vector3 origin = transform.position;
            float offset = _random != null ? (float)_random.NextDouble() * 360f : 0f;
            for (int i = 0; i < _summonCount; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, offset + 360f * i / _summonCount, 0f) * Vector3.forward;
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

        // La striscia a terra nella direzione del cavaliere adesso: in quella direzione caricherà, ovunque il cavaliere sia poi. Fermo per
        // tutto il tempo (niente colpi né passi), come il re con l'anello
        private void BeginCharge()
        {
            Vector3 toward = _player.transform.position - transform.position;
            toward.y = 0f;
            _chargeDirection = toward.sqrMagnitude > 0.0001f ? toward.normalized : transform.forward;
            Stage = ChargeStage.WindUp;
            _stageTimer = _chargeWindup;
            _traveled = 0f;
            _chargeHit = false;
            _attack.Interrupt(_chargeWindup + _chargeDistance / _chargeSpeed + _chargeRecover + 0.3f);
            transform.rotation = Quaternion.LookRotation(_chargeDirection, Vector3.up);
            if (_chargeTelegraph != null)
            {
                _chargeTelegraph.Show(transform.position, _chargeDirection, _chargeDistance, _chargeWidth, _chargeWindup);
            }
        }

        private void TickCharge(float deltaTime)
        {
            switch (Stage)
            {
                case ChargeStage.WindUp:
                    _stageTimer -= deltaTime;
                    if (_stageTimer <= 0f)
                    {
                        Stage = ChargeStage.Run;
                        if (_chargeTelegraph != null)
                        {
                            _chargeTelegraph.Hide();
                        }
                    }

                    break;
                case ChargeStage.Run:
                    RunStep(deltaTime);
                    break;
                case ChargeStage.Recover:
                    _stageTimer -= deltaTime;
                    if (_stageTimer <= 0f)
                    {
                        EndCharge();
                    }

                    break;
            }
        }

        // un passo della corsa: l'agent si muove dentro il NavMesh, quindi un muro lo ferma; il cavaliere sulla strada la finisce con il colpo
        private void RunStep(float deltaTime)
        {
            float step = _chargeSpeed * deltaTime;
            Vector3 before = transform.position;
            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.Move(_chargeDirection * step);
            }

            float moved = FlatDistance(before, transform.position);
            _traveled += moved;
            if (!_chargeHit && !_player.IsDead && IsOnTheLine(_player.transform.position))
            {
                _chargeHit = true;
                int amount = _random != null ? _chargeDamage.x + (int)(_random.NextDouble() * (_chargeDamage.y - _chargeDamage.x + 1)) : _chargeDamage.x;
                _player.TakeDamage(new DamageInfo(Mathf.Min(amount, _chargeDamage.y) * _attack.DamageMultiplier, DamageType.Physical, gameObject));
            }

            // la corsa finisce: contro il cavaliere, a fine strada o contro un muro (si è mosso troppo poco)
            if (_chargeHit || _traveled >= _chargeDistance || moved < step * 0.25f)
            {
                Stage = ChargeStage.Recover;
                _stageTimer = _chargeRecover;
            }
        }

        // il cavaliere sta nel rettangolo davanti al Custode: fino a HitReach metri avanti, dentro la metà della larghezza (più il suo corpo) e non dietro
        private bool IsOnTheLine(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            offset.y = 0f;
            float along = Vector3.Dot(offset, _chargeDirection);
            float across = Mathf.Abs(Vector3.Dot(offset, Vector3.Cross(Vector3.up, _chargeDirection)));
            return along >= -0.5f && along <= HitReach && across <= _chargeWidth * 0.5f + _targetAllowance;
        }

        // quanto davanti a sé colpisce correndo: il corpo e un passo
        private const float HitReach = 2.2f;

        private void EndCharge()
        {
            Stage = ChargeStage.None;
            _chargeTimer = _chargeInterval;
            if (_chargeTelegraph != null)
            {
                _chargeTelegraph.Hide();
            }
        }

        /// <summary>
        /// Il cavaliere è tornato in vita all'ingresso: gli evocati spariscono e il Custode, rimandato a casa a vita piena dal suo EnemyAI,
        /// ricomincia dalla prima fase, con le tre evocazioni di nuovo da fare.
        /// </summary>
        public void ResetFight()
        {
            if (_health.IsDead || !IsFighting)
            {
                return;
            }

            IsFighting = false;
            Phase = GuardianPhase.Strike;
            _hasSummoned = false;
            EndCharge();
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

        // morto il Custode, i suoi evocati cadono con lui
        private void HandleDied()
        {
            IsFighting = false;
            EndCharge();
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
