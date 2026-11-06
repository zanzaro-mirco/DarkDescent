using DarkDescent.Characters;
using DarkDescent.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Il corpo di un nemico (D8 della M7): le decisioni le prende un <see cref="EnemyBrain"/> con gli
    /// stati del suo <see cref="EnemyArchetype"/>, qui ci sono gli occhi (un raggio), le gambe e le
    /// braccia (MeleeAttack, che si avvicina e colpisce da solo) e quello che succede al corpo da morto.
    /// Un nemico a distanza (il mago, M9) ha anche un <see cref="RangedAttack"/> e si muove da solo.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(MeleeAttack), typeof(Health))]
    public class EnemyAI : MonoBehaviour, IRangedBody, IFleeBody, IPatrolBody
    {
        [Tooltip("Il tipo di nemico: numeri e stati.")]
        [SerializeField] private EnemyArchetype _archetype;

        [Tooltip("Layer che bloccano la vista: dietro un ostacolo il bersaglio non viene notato.")]
        [SerializeField] private LayerMask _sightBlockers;

        [Tooltip("Tornando a casa, a questa distanza dal punto è arrivato.")]
        [SerializeField, Min(0.05f)] private float _homeTolerance = 0.3f;

        private NavMeshAgent _agent;
        private MeleeAttack _attack;
        private Health _health;
        private Collider _collider;
        private Health _target;
        private EnemyBrain _brain;
        private RangedAttack _ranged;
        private readonly StunTimer _stun = new StunTimer();

        // il gelo del Mago (M21): il congelamento segue la regola dello stordimento, il rallentamento no
        private readonly StunTimer _freeze = new StunTimer();
        private readonly SlowTimer _slow = new SlowTimer();
        private float _slowBase;
        private float _slowSpeed;

        // dove sta andando il mago: la destinazione si rinnova solo quando il bersaglio si è spostato
        private Vector3 _approachPoint;

        // la fuga (D2–D5 della M24): una sola volta per vita, e senza un posto dove andare non riprova subito
        private FleeWatch _watch;
        private bool _hasFled;
        private bool _fleeBoosted;
        private float _fleeRetryAt;

        // dove l'ha messo il livello: ci torna quando il cavaliere torna in vita (D15 della M7) e
        // quando inseguendo si allontana troppo
        private Vector3 _homePosition;
        private Quaternion _homeRotation;

        /// <summary>Ha visto il bersaglio con i suoi occhi, non avvisato da un compagno: il branco si sveglia (D5).</summary>
        public event System.Action<EnemyAI> Spotted;

        /// <summary>È morto: chi lo ha legato dà l'esperienza al cavaliere (D2 della M8).</summary>
        public event System.Action<EnemyAI> Killed;

        /// <summary>Stordito da un'abilità del cavaliere (M20), con i secondi che prende davvero.</summary>
        public event System.Action<EnemyAI, float> Stunned;

        /// <summary>Lo stordimento è finito.</summary>
        public event System.Action<EnemyAI> StunEnded;

        /// <summary>Stordito: fermo, senza colpi né lanci (M20).</summary>
        public bool IsStunned => _stun.IsStunned;

        /// <summary>I secondi di stordimento che restano, con la regola: quelli presi davvero, appena stordito.</summary>
        public float StunRemaining => _stun.Remaining;

        /// <summary>I secondi in cui non si stordisce di nuovo, dopo uno stordimento.</summary>
        public float StunImmunity => _stun.ImmuneFor;

        /// <summary>Congelato da un'abilità del Mago (M21), con i secondi che prende davvero.</summary>
        public event System.Action<EnemyAI, float> Frozen;

        /// <summary>Il congelamento è finito.</summary>
        public event System.Action<EnemyAI> FreezeEnded;

        /// <summary>Rallentato (M21), o il rallentamento è cambiato o finito: chi lo mostra lo rilegge.</summary>
        public event System.Action<EnemyAI> SlowChanged;

        /// <summary>Congelato: fermo e senza colpi né lanci, come uno stordito, ma con la sua immunità.</summary>
        public bool IsFrozen => _freeze.IsStunned;

        public float FreezeRemaining => _freeze.Remaining;

        /// <summary>I secondi in cui non si congela di nuovo, dopo un congelamento.</summary>
        public float FreezeImmunity => _freeze.ImmuneFor;

        /// <summary>Fermo per uno stordimento o un congelamento: niente movimento, colpi né lanci.</summary>
        public bool IsImmobilized => _stun.IsStunned || _freeze.IsStunned;

        public bool IsSlowed => _slow.IsSlowed;

        /// <summary>La parte della velocità che perde ora, tra 0 e 1, con la regola del grado.</summary>
        public float SlowFraction => _slow.Fraction;

        public float SlowRemaining => _slow.Remaining;

        public EnemyState State => _brain.State;

        public EnemyArchetype Archetype => _archetype;

        bool IEnemyBody.IsTargetAlive => IsTargetAlive();

        bool IEnemyBody.IsTargetInRange => _attack.IsTargetInRange;

        bool IEnemyBody.IsSwinging => _attack.IsSwinging;

        bool IEnemyBody.IsTelegraphedSwing => _attack.IsSwinging && !_attack.IsQuickSwing;

        bool IEnemyBody.IsBeyondLeash
        {
            get
            {
                float leash = _archetype.LeashRange;
                Vector3 away = transform.position - _homePosition;
                away.y = 0f;
                return leash > 0f && away.sqrMagnitude > leash * leash;
            }
        }

        // un percorso che non arriva (casa irraggiungibile) finisce senza path: vale come arrivato,
        // invece di restare per sempre a metà strada
        bool IEnemyBody.IsHome => !_agent.enabled || !_agent.isOnNavMesh
            || (!_agent.pathPending && (!_agent.hasPath || _agent.remainingDistance <= _homeTolerance));

        float IRangedBody.TargetDistance => IsTargetAlive() ? FlatDistance(transform.position, _target.transform.position) : float.MaxValue;

        bool IRangedBody.IsCasting => _ranged != null && _ranged.IsCasting;

        bool IRangedBody.IsCastReady => _ranged != null && _ranged.IsReady;

        bool IRangedBody.IsAtDestination => !_agent.enabled || !_agent.isOnNavMesh
            || (!_agent.pathPending && (!_agent.hasPath || _agent.remainingDistance <= _homeTolerance));

        /// <summary>Dove l'ha messo il livello: per i test.</summary>
        public Vector3 HomePosition => _homePosition;

        /// <summary>Ha già scappato in questa vita: non lo rifà finché non torna a casa (D3 della M24).</summary>
        public bool HasFled => _hasFled;

        public bool IsFleeing => _brain != null && _brain.State == EnemyState.Flee;

        /// <summary>Il registro dei compagni morti e dei richiami del livello; senza, scappa solo per la vita.</summary>
        public void BindWatch(FleeWatch watch)
        {
            _watch = watch;
        }

        bool IFleeBody.WantsToFlee
        {
            get
            {
                if (!EnemyBehaviours.FleeEnabled || _hasFled || !_archetype.CanFlee || Time.time < _fleeRetryAt || _health.IsDead || !IsTargetAlive())
                {
                    return false;
                }

                bool low = _archetype.FleeBelow > 0f && _health.Current < _health.Max * _archetype.FleeBelow;
                bool grief = !low && _archetype.FleeOnLosses > 0 && _watch != null && _watch.RecentLosses(this, Time.time) >= _archetype.FleeOnLosses;
                if (!low && !grief)
                {
                    return false;
                }

                // solo i nemici normali (D2): un campione, un unico o un re che scappasse e tornasse cambierebbe la sfida
                bool special = TryGetComponent(out SpecialEnemy rank) && rank.Rank != EnemyRank.Normal;
                return !special && !TryGetComponent(out BossController _);
            }
        }

        // un punto lontano dal bersaglio, vicino a casa, raggiungibile in linea retta: dritto all'indietro, poi sempre più di lato.
        // Senza un posto che lo allontani non è una fuga: combatte, e per un secondo non riprova
        bool IFleeBody.BeginFlee()
        {
            if (!_agent.enabled || !_agent.isOnNavMesh || !IsTargetAlive())
            {
                _fleeRetryAt = Time.time + FleeRetryDelay;
                return false;
            }

            Vector3 away = transform.position - _target.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
            {
                away = -transform.forward;
            }

            away.Normalize();
            float wanted = _archetype.FleeDistance;
            for (int i = 0; i < FleeAngles.Length; i++)
            {
                for (int extra = 0; extra < FleeExtras.Length; extra++)
                {
                    Vector3 direction = Quaternion.Euler(0f, FleeAngles[i], 0f) * away;
                    Vector3 candidate = _target.transform.position + direction * (wanted + FleeExtras[extra]);
                    if (!NavMesh.SamplePosition(candidate, out var hit, 1.5f, NavMesh.AllAreas)
                        || FlatDistance(hit.position, _target.transform.position) < wanted
                        || FlatDistance(hit.position, _homePosition) > _archetype.FleeHomeLimit
                        || NavMesh.Raycast(transform.position, hit.position, out _, NavMesh.AllAreas))
                    {
                        continue;
                    }

                    _hasFled = true;
                    _fleeBoosted = true;
                    ScaleSpeed(_archetype.FleeSpeedFactor);
                    _agent.SetDestination(hit.position);
                    if (TryGetComponent(out CharacterAudio audio))
                    {
                        audio.PlayFleeCry();
                    }

                    return true;
                }
            }

            _fleeRetryAt = Time.time + FleeRetryDelay;
            return false;
        }

        bool IFleeBody.IsAtDestination => !_agent.enabled || !_agent.isOnNavMesh
            || (!_agent.pathPending && (!_agent.hasPath || _agent.remainingDistance <= _homeTolerance));

        void IFleeBody.EndFlee()
        {
            RestoreFleeSpeed();
            if (_archetype.FleeCallRadius > 0f && _watch != null)
            {
                _watch.Call(this, _archetype.FleeCallRadius);
            }
        }

        private void RestoreFleeSpeed()
        {
            if (!_fleeBoosted)
            {
                return;
            }

            _fleeBoosted = false;
            ScaleSpeed(1f / _archetype.FleeSpeedFactor);
        }

        // Fuga e ronda cambiano il passo: se è rallentato (M21) cambia anche la base da cui il rallentamento toglie la parte, e
        // la velocità che il rallentamento si aspetta di trovare, o Refresh la prenderebbe per una nuova base e lo rallenterebbe due volte
        private void ScaleSpeed(float factor)
        {
            _agent.speed *= factor;
            if (_slow.IsSlowed)
            {
                _slowBase *= factor;
                _slowSpeed *= factor;
            }
        }

        // la ronda (D7 della M24): i punti si decidono dal primo momento in cui servono, perché il grado (campione, unico) arriva dopo Awake
        private int _patrolDepth = -1;
        private bool _patrolResolved;
        private readonly System.Collections.Generic.List<Vector3> _patrolPoints = new System.Collections.Generic.List<Vector3>();
        private readonly System.Collections.Generic.List<float> _patrolPauses = new System.Collections.Generic.List<float>();
        private int _patrolIndex = -1;
        private bool _patrolBoosted;

        /// <summary>Il livello lega il nemico alla sua profondità: da lì la ronda. Un evocato non la riceve e resta fermo.</summary>
        public void BindPatrol(int depth)
        {
            _patrolDepth = depth;
            _patrolResolved = false;
        }

        /// <summary>I punti della ronda sul NavMesh, vuoti se non gira: per i test.</summary>
        public System.Collections.Generic.IReadOnlyList<Vector3> PatrolPoints
        {
            get
            {
                ResolvePatrol();
                return _patrolPoints;
            }
        }

        public bool IsPatrolling => _brain != null && _brain.State == EnemyState.Patrol;

        bool IPatrolBody.IsPatroller => EnemyBehaviours.PatrolEnabled && !_health.IsDead && PatrolPoints.Count >= PatrolPlan.MinPoints;

        float IPatrolBody.PatrolPause => _patrolPauses.Count == 0 ? 0f : _patrolPauses[Mathf.Max(0, _patrolIndex)];

        bool IPatrolBody.IsAtPatrolPoint => !_agent.enabled || !_agent.isOnNavMesh
            || (!_agent.pathPending && (!_agent.hasPath || _agent.remainingDistance <= _homeTolerance));

        void IPatrolBody.WalkToNextPatrolPoint()
        {
            if (!_agent.enabled || !_agent.isOnNavMesh || _patrolPoints.Count == 0)
            {
                return;
            }

            _patrolIndex = (_patrolIndex + 1) % _patrolPoints.Count;
            if (!_patrolBoosted)
            {
                _patrolBoosted = true;
                ScaleSpeed(_archetype.PatrolSpeedFactor);
            }

            _agent.SetDestination(_patrolPoints[_patrolIndex]);
        }

        void IPatrolBody.HoldAtPatrolPoint()
        {
            StopMoving();
        }

        void IPatrolBody.EndPatrol()
        {
            if (_patrolBoosted)
            {
                _patrolBoosted = false;
                ScaleSpeed(1f / _archetype.PatrolSpeedFactor);
            }

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh && _agent.hasPath)
            {
                _agent.ResetPath();
            }
        }

        // due o tre punti entro il raggio dall'hash della cella e della profondità: ognuno sul NavMesh e raggiungibile in linea retta da casa,
        // o a metà strada; con meno di due non gira. Solo i nemici normali del livello (D7): i campioni, gli unici e i re stanno dove sono
        private void ResolvePatrol()
        {
            if (_patrolResolved || _patrolDepth < 0 || _agent == null || !_agent.isOnNavMesh)
            {
                return;
            }

            _patrolResolved = true;
            _patrolPoints.Clear();
            _patrolPauses.Clear();
            _patrolIndex = -1;
            bool special = TryGetComponent(out SpecialEnemy rank) && rank.Rank != EnemyRank.Normal;
            if (!_archetype.CanPatrol || special || TryGetComponent(out BossController _))
            {
                return;
            }

            int cellX = Mathf.RoundToInt(_homePosition.x / Levels.LevelMap.CellSize);
            int cellZ = Mathf.RoundToInt(-_homePosition.z / Levels.LevelMap.CellSize);
            var plan = PatrolPlan.For(_patrolDepth, cellX, cellZ, _archetype.PatrolShare, _archetype.PatrolRadius);
            for (int i = 0; i < plan.Count; i++)
            {
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    Vector2 offset = plan.Offset(i, attempt == 0 ? 1f : 0.5f);
                    Vector3 candidate = _homePosition + new Vector3(offset.x, 0f, offset.y);
                    if (NavMesh.SamplePosition(candidate, out var hit, 1.5f, NavMesh.AllAreas)
                        && FlatDistance(hit.position, _homePosition) <= _archetype.PatrolRadius + 0.01f
                        && FlatDistance(hit.position, _homePosition) >= 1f
                        && NavMesh.SamplePosition(hit.position, out _, 0.2f, NavMesh.AllAreas)
                        && !NavMesh.Raycast(_homePosition, hit.position, out _, NavMesh.AllAreas))
                    {
                        _patrolPoints.Add(hit.position);
                        _patrolPauses.Add(plan.Pause(i));
                        break;
                    }
                }
            }

            // un punto solo non è una ronda
            if (_patrolPoints.Count < PatrolPlan.MinPoints)
            {
                _patrolPoints.Clear();
                _patrolPauses.Clear();
            }
        }

        private const float FleeRetryDelay = 1f;

        private static readonly float[] FleeAngles = { 0f, 30f, -30f, 60f, -60f, 90f, -90f };

        private static readonly float[] FleeExtras = { 0f, 2f, 4f };

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _attack = GetComponent<MeleeAttack>();
            _health = GetComponent<Health>();
            _collider = GetComponent<Collider>();
            // facoltativo: solo i nemici a distanza
            _ranged = GetComponent<RangedAttack>();
            _brain = new EnemyBrain(this, _archetype.CreateStates());
            _homePosition = transform.position;
            _homeRotation = transform.rotation;
            _stun.Ended += () => StunEnded?.Invoke(this);
            _freeze.Ended += () => FreezeEnded?.Invoke(this);
            _slow.Ended += EndSlow;

            // priorità diverse da un nemico all'altro: nello stesso corridoio uno cede il passo (trappola 5)
            int spread = _archetype.AvoidanceSpread;
            if (spread > 0)
            {
                int offset = (GetInstanceID() & 0x7fffffff) % (2 * spread + 1) - spread;
                _agent.avoidancePriority = Mathf.Clamp(_agent.avoidancePriority + offset, 0, 99);
            }
        }

        private void OnEnable()
        {
            _health.Died += HandleDied;
            _health.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            _health.Died -= HandleDied;
            _health.Damaged -= HandleDamaged;
        }

        // colpito da lontano (un incantesimo, M9) senza averlo visto: si sveglia come se un compagno lo avesse avvisato
        private void HandleDamaged(DamageInfo info, float applied)
        {
            if (!_health.IsDead)
            {
                _brain.Alert();
            }
        }

        /// <summary>Chi attaccare. Lo chiama il composition root; più avanti lo spawner.</summary>
        public void Bind(Health target)
        {
            _target = target;
        }

        /// <summary>
        /// Lo stordisce per tanti secondi, con la regola dello stordimento (M20): metà per il re, i
        /// campioni e gli unici, mai per un Inamovibile, e non di nuovo per 3 s dopo l'ultimo. Il colpo
        /// in corso si annulla, il lancio pure, e si ferma dov'è. False se non l'ha preso.
        /// </summary>
        public bool Stun(float seconds)
        {
            if (_health.IsDead)
            {
                return false;
            }

            var special = GetComponent<SpecialEnemy>();
            bool reduced = (special != null && special.Rank != EnemyRank.Normal) || GetComponent<BossController>() != null || GetComponent<GuardianController>() != null;
            bool unstoppable = special != null && special.IsUnstoppable;
            if (!_stun.TryStun(StunTimer.Scaled(seconds, reduced, unstoppable)))
            {
                return false;
            }

            _attack.Interrupt(_stun.Remaining);
            if (_ranged != null)
            {
                _ranged.Cancel();
            }

            StopMoving();
            _brain.Alert();
            Stunned?.Invoke(this, _stun.Remaining);
            return true;
        }

        /// <summary>
        /// Lo congela per tanti secondi (M21): la regola dello stordimento, con il suo tempo di pausa a parte,
        /// quindi un nemico stordito si può congelare e viceversa. Metà per il re, i campioni e gli unici, mai
        /// per un Inamovibile. Il colpo in corso si annulla, il lancio pure. False se non l'ha preso.
        /// </summary>
        public bool Freeze(float seconds)
        {
            if (_health.IsDead)
            {
                return false;
            }

            ReadRank(out bool reduced, out bool unstoppable);
            if (!_freeze.TryStun(StunTimer.Scaled(seconds, reduced, unstoppable)))
            {
                return false;
            }

            _attack.Interrupt(_freeze.Remaining);
            if (_ranged != null)
            {
                _ranged.Cancel();
            }

            StopMoving();
            _brain.Alert();
            ShowFrost();
            Frozen?.Invoke(this, _freeze.Remaining);
            return true;
        }

        /// <summary>
        /// Lo rallenta di <paramref name="fraction"/> (0,4: il 40%) per tanti secondi (M21): la metà per il
        /// re, i campioni e gli unici; chi è già rallentato tiene il più forte e il più lungo. Niente tempo di pausa.
        /// </summary>
        public bool Slow(float fraction, float seconds)
        {
            if (_health.IsDead)
            {
                return false;
            }

            ReadRank(out bool reduced, out _);
            bool wasSlowed = _slow.IsSlowed;
            if (!_slow.Apply(SlowTimer.Scaled(fraction, reduced), seconds))
            {
                return false;
            }

            if (!wasSlowed)
            {
                _slowBase = _agent.speed;
            }

            RefreshSlowSpeed();
            _brain.Alert();
            ShowFrost();
            SlowChanged?.Invoke(this);
            return true;
        }

        private void ReadRank(out bool reduced, out bool unstoppable)
        {
            var special = GetComponent<SpecialEnemy>();
            reduced = (special != null && special.Rank != EnemyRank.Normal) || GetComponent<BossController>() != null || GetComponent<GuardianController>() != null;
            unstoppable = special != null && special.IsUnstoppable;
        }

        // chi scrive la velocità dell'agent è anche il Rapido e il re furioso: se l'hanno cambiata
        // mentre era rallentato, la nuova è la base da cui togliere la parte
        private void RefreshSlowSpeed()
        {
            if (_agent == null || !_slow.IsSlowed)
            {
                return;
            }

            if (!Mathf.Approximately(_agent.speed, _slowSpeed) && _slowSpeed > 0f)
            {
                _slowBase = _agent.speed;
            }

            _slowSpeed = _slowBase * (1f - _slow.Fraction);
            _agent.speed = _slowSpeed;
        }

        private void EndSlow()
        {
            if (_agent != null && Mathf.Approximately(_agent.speed, _slowSpeed))
            {
                _agent.speed = _slowBase;
            }

            _slowSpeed = 0f;
            SlowChanged?.Invoke(this);
        }

        // la tinta fredda e il fermo immagine li fa un componente che nasce al primo gelo: niente da mettere nei prefab
        private void ShowFrost()
        {
            if (!TryGetComponent(out EnemyFrostView _))
            {
                gameObject.AddComponent<EnemyFrostView>();
            }
        }

        /// <summary>Un compagno l'ha visto: se è fermo, insegue anche lui. Non avvisa a sua volta.</summary>
        public void Alert()
        {
            _brain.Alert();
        }

        /// <summary>
        /// Torna dove l'ha messo il livello, fermo, senza bersaglio e a vita piena: il cavaliere è
        /// tornato in vita all'ingresso (D15 della M7). Un morto resta dov'è.
        /// </summary>
        public void ReturnHome()
        {
            if (!_brain.Rest())
            {
                return;
            }

            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.Warp(_homePosition);
            }
            else
            {
                transform.position = _homePosition;
            }

            transform.rotation = _homeRotation;
            _health.Heal(_health.Max);
            RestoreFleeSpeed();
            _hasFled = false;
            _stun.Clear();
            ClearFrost();
        }

        // senza gelo: tornato a casa o morto. La velocità torna quella di prima
        private void ClearFrost()
        {
            bool wasFrozen = _freeze.IsStunned;
            _freeze.Clear();
            if (wasFrozen)
            {
                FreezeEnded?.Invoke(this);
            }

            if (_slow.IsSlowed)
            {
                _slow.Clear();
                EndSlow();
            }
        }

        private void Update()
        {
            if (_slow.IsSlowed)
            {
                RefreshSlowSpeed();
                _slow.Advance(Time.deltaTime);
            }

            // stordito o congelato non pensa: l'attacco è già bloccato per lo stesso tempo
            if (_stun.IsStunned || _stun.ImmuneFor > 0f || _freeze.IsStunned || _freeze.ImmuneFor > 0f)
            {
                _stun.Advance(Time.deltaTime);
                _freeze.Advance(Time.deltaTime);
                if (_stun.IsStunned || _freeze.IsStunned)
                {
                    StopMoving();
                    return;
                }
            }

            EnemyState before = _brain.State;
            _brain.Tick(Time.deltaTime);
            if ((before == EnemyState.Idle || before == EnemyState.Patrol) && (_brain.State == EnemyState.Chase || _brain.State == EnemyState.Ranged))
            {
                Spotted?.Invoke(this);
            }
        }

        bool IEnemyBody.CanSeeTarget()
        {
            Vector3 eye = transform.position + Vector3.up * _archetype.EyeHeight;
            Vector3 targetEye = _target.transform.position + Vector3.up * _archetype.EyeHeight;
            Vector3 flat = targetEye - eye;
            flat.y = 0f;
            if (flat.sqrMagnitude > _archetype.AggroRange * _archetype.AggroRange)
            {
                return false;
            }

            return !Physics.Linecast(eye, targetEye, _sightBlockers, QueryTriggerInteraction.Ignore);
        }

        void IEnemyBody.Engage()
        {
            _attack.SetTarget(_target);
        }

        void IEnemyBody.Disengage()
        {
            _attack.ClearTarget();
            if (_ranged != null)
            {
                _ranged.Cancel();
                StopMoving();
            }
        }

        // i muri li dice il NavMesh, come per i proiettili: quelli alti spenti dalla camera non hanno collider
        bool IRangedBody.HasClearShot()
        {
            return IsTargetAlive() && !NavMesh.Raycast(transform.position, _target.transform.position, out _, NavMesh.AllAreas);
        }

        void IRangedBody.Cast()
        {
            StopMoving();
            _ranged.Begin(_target);
        }

        void IRangedBody.Approach()
        {
            if (!_agent.enabled || !_agent.isOnNavMesh || !IsTargetAlive())
            {
                return;
            }

            Vector3 point = _target.transform.position;
            if (!_agent.hasPath || (point - _approachPoint).sqrMagnitude > 0.25f)
            {
                _approachPoint = point;
                _agent.SetDestination(point);
            }
        }

        void IRangedBody.HoldPosition()
        {
            StopMoving();
            if (IsTargetAlive() && _ranged != null)
            {
                _ranged.Face(_target.transform.position, Time.deltaTime);
            }
        }

        // un punto lontano dal bersaglio, raggiungibile e senza allontanarsi troppo da casa: dritto
        // all'indietro, poi sempre più di lato (trappola 5)
        bool IRangedBody.TryRetreat()
        {
            if (!_agent.enabled || !_agent.isOnNavMesh || !IsTargetAlive())
            {
                return false;
            }

            Vector3 away = transform.position - _target.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
            {
                away = -transform.forward;
            }

            away.Normalize();
            float wanted = _archetype.RetreatDistance;
            float current = FlatDistance(transform.position, _target.transform.position);
            for (int i = 0; i < RetreatAngles.Length; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, RetreatAngles[i], 0f) * away;
                Vector3 candidate = _target.transform.position + direction * wanted;
                if (!NavMesh.SamplePosition(candidate, out var hit, 1f, NavMesh.AllAreas)
                    || FlatDistance(hit.position, _target.transform.position) <= current + 1f
                    || NavMesh.Raycast(transform.position, hit.position, out _, NavMesh.AllAreas))
                {
                    continue;
                }

                float leash = _archetype.LeashRange;
                if (leash > 0f && FlatDistance(hit.position, _homePosition) > leash)
                {
                    continue;
                }

                _agent.SetDestination(hit.position);
                return true;
            }

            return false;
        }

        private static readonly float[] RetreatAngles = { 0f, 30f, -30f, 60f, -60f, 90f, -90f };

        private void StopMoving()
        {
            if (_agent.enabled && _agent.isOnNavMesh && _agent.hasPath)
            {
                _agent.ResetPath();
            }
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        void IEnemyBody.GoHome()
        {
            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.SetDestination(_homePosition);
            }
        }

        void IEnemyBody.ArriveHome()
        {
            if (_agent.enabled && _agent.isOnNavMesh && _agent.hasPath)
            {
                _agent.ResetPath();
            }

            _health.Heal(_health.Max);
            RestoreFleeSpeed();
            _hasFled = false;
        }

        private void HandleDied()
        {
            RestoreFleeSpeed();
            _stun.Clear();
            ClearFrost();
            _brain.Die();
            Killed?.Invoke(this);

            // MeleeAttack spento annulla anche un colpo in volo, RangedAttack un lancio a metà
            _attack.enabled = false;
            if (_ranged != null)
            {
                _ranged.enabled = false;
            }

            if (_agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }

            // il corpo non deve più spingere gli altri agent né fermare i click diretti al pavimento
            _agent.enabled = false;
            if (_collider != null)
            {
                _collider.enabled = false;
            }

            // chi lo teneva come bersaglio lo controlla con il null di Unity: la distruzione è sicura
            Destroy(gameObject, _archetype.CorpseLifetime);
        }

        // _target è un tipo Unity: il confronto con null vede anche un player distrutto
        private bool IsTargetAlive()
        {
            return _target != null && !_target.IsDead;
        }
    }
}
