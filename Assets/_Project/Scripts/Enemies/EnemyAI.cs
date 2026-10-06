using DarkDescent.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Il corpo di un nemico (D8 della M7): le decisioni le prende un <see cref="EnemyBrain"/> con gli
    /// stati del suo <see cref="EnemyArchetype"/>, qui ci sono gli occhi (un raggio), le gambe e le
    /// braccia (MeleeAttack, che si avvicina e colpisce da solo) e quello che succede al corpo da morto.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(MeleeAttack), typeof(Health))]
    public class EnemyAI : MonoBehaviour, IEnemyBody
    {
        [Tooltip("Il tipo di nemico: numeri e stati.")]
        [SerializeField] private EnemyArchetype _archetype;

        [Tooltip("Layer che bloccano la vista: dietro un ostacolo il bersaglio non viene notato.")]
        [SerializeField] private LayerMask _sightBlockers;

        private NavMeshAgent _agent;
        private MeleeAttack _attack;
        private Health _health;
        private Collider _collider;
        private Health _target;
        private EnemyBrain _brain;

        /// <summary>Ha visto il bersaglio con i suoi occhi, non avvisato da un compagno: il branco si sveglia (D5).</summary>
        public event System.Action<EnemyAI> Spotted;

        public EnemyState State => _brain.State;

        public EnemyArchetype Archetype => _archetype;

        bool IEnemyBody.IsTargetAlive => IsTargetAlive();

        bool IEnemyBody.IsTargetInRange => _attack.IsTargetInRange;

        bool IEnemyBody.IsSwinging => _attack.IsSwinging;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _attack = GetComponent<MeleeAttack>();
            _health = GetComponent<Health>();
            _collider = GetComponent<Collider>();
            _brain = new EnemyBrain(this, _archetype.CreateStates());

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
        }

        private void OnDisable()
        {
            _health.Died -= HandleDied;
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

        private void Update()
        {
            EnemyState before = _brain.State;
            _brain.Tick(Time.deltaTime);
            if (before == EnemyState.Idle && _brain.State == EnemyState.Chase)
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
        }

        private void HandleDied()
        {
            _brain.Die();

            // MeleeAttack spento annulla anche un colpo in volo
            _attack.enabled = false;
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
