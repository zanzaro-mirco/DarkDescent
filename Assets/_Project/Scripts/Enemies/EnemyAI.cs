using DarkDescent.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// IA da mischia: una enum e uno switch. Fermo finché non vede il bersaglio, poi lo insegue e lo
    /// colpisce fino alla morte di uno dei due. Avvicinamento e colpi li fa MeleeAttack: l'IA sceglie
    /// solo chi colpire. Una state machine a classi arriva alla M7, con più tipi di nemico.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(MeleeAttack), typeof(Health))]
    public class EnemyAI : MonoBehaviour
    {
        [Tooltip("Distanza entro cui il bersaglio viene notato, se non c'è un ostacolo in mezzo.")]
        [SerializeField, Min(0f)] private float _aggroRange = 8f;

        [Tooltip("Layer che bloccano la vista: dietro un ostacolo il bersaglio non viene notato.")]
        [SerializeField] private LayerMask _sightBlockers;

        [Tooltip("Altezza degli occhi per il controllo della vista, dal pivot ai piedi.")]
        [SerializeField, Min(0f)] private float _eyeHeight = 1.5f;

        [Tooltip("Ogni quanti secondi, da fermo, si controlla se il bersaglio è visibile.")]
        [SerializeField, Min(0.02f)] private float _perceptionInterval = 0.2f;

        [Tooltip("Secondi in cui il corpo resta a terra prima di sparire.")]
        [SerializeField, Min(0f)] private float _corpseLifetime = 5f;

        private NavMeshAgent _agent;
        private MeleeAttack _attack;
        private Health _health;
        private Collider _collider;
        private Health _target;
        private float _perceptionTimer;

        public EnemyState State { get; private set; } = EnemyState.Idle;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _attack = GetComponent<MeleeAttack>();
            _health = GetComponent<Health>();
            _collider = GetComponent<Collider>();
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

        private void Update()
        {
            switch (State)
            {
                case EnemyState.Idle:
                    UpdateIdle();
                    break;
                case EnemyState.Chase:
                    UpdateChase();
                    break;
                case EnemyState.Attack:
                    UpdateAttack();
                    break;
                case EnemyState.Dead:
                    break;
            }
        }

        private void UpdateIdle()
        {
            _perceptionTimer -= Time.deltaTime;
            if (_perceptionTimer > 0f)
            {
                return;
            }

            _perceptionTimer = _perceptionInterval;
            if (IsTargetAlive() && CanSeeTarget())
            {
                State = EnemyState.Chase;
            }
        }

        // Chase e Attack chiedono entrambi il colpo a MeleeAttack, che si avvicina da solo se serve.
        // Restano due stati perché dicono cose diverse a chi guarda (test, animazioni, suoni alla 2.8).
        private void UpdateChase()
        {
            if (!IsTargetAlive())
            {
                ReturnToIdle();
                return;
            }

            _attack.SetTarget(_target);
            if (_attack.IsTargetInRange || _attack.IsSwinging)
            {
                State = EnemyState.Attack;
            }
        }

        private void UpdateAttack()
        {
            if (!IsTargetAlive())
            {
                ReturnToIdle();
                return;
            }

            // richiesto a ogni frame: finito un colpo ne parte un altro appena l'arma è pronta
            _attack.SetTarget(_target);
            if (!_attack.IsTargetInRange && !_attack.IsSwinging)
            {
                State = EnemyState.Chase;
            }
        }

        private void ReturnToIdle()
        {
            _attack.ClearTarget();
            State = EnemyState.Idle;
        }

        private void HandleDied()
        {
            State = EnemyState.Dead;

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
            Destroy(gameObject, _corpseLifetime);
        }

        // _target è un tipo Unity: il confronto con null vede anche un player distrutto
        private bool IsTargetAlive()
        {
            return _target != null && !_target.IsDead;
        }

        private bool CanSeeTarget()
        {
            Vector3 eye = transform.position + Vector3.up * _eyeHeight;
            Vector3 targetEye = _target.transform.position + Vector3.up * _eyeHeight;
            Vector3 flat = targetEye - eye;
            flat.y = 0f;
            if (flat.sqrMagnitude > _aggroRange * _aggroRange)
            {
                return false;
            }

            return !Physics.Linecast(eye, targetEye, _sightBlockers, QueryTriggerInteraction.Ignore);
        }
    }
}
