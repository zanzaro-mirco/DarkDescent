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
    public class EnemyAI : MonoBehaviour, IRangedBody
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

        // dove sta andando il mago: la destinazione si rinnova solo quando il bersaglio si è spostato
        private Vector3 _approachPoint;

        // dove l'ha messo il livello: ci torna quando il cavaliere torna in vita (D15 della M7) e
        // quando inseguendo si allontana troppo
        private Vector3 _homePosition;
        private Quaternion _homeRotation;

        /// <summary>Ha visto il bersaglio con i suoi occhi, non avvisato da un compagno: il branco si sveglia (D5).</summary>
        public event System.Action<EnemyAI> Spotted;

        /// <summary>È morto: chi lo ha legato dà l'esperienza al cavaliere (D2 della M8).</summary>
        public event System.Action<EnemyAI> Killed;

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
        }

        private void Update()
        {
            EnemyState before = _brain.State;
            _brain.Tick(Time.deltaTime);
            if (before == EnemyState.Idle && (_brain.State == EnemyState.Chase || _brain.State == EnemyState.Ranged))
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
        }

        private void HandleDied()
        {
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
