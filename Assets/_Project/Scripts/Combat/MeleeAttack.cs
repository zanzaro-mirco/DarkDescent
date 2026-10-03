using System;
using DarkDescent.Core;
using DarkDescent.Stats;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Combat
{
    /// <summary>
    /// Attacco in mischia, uguale per player e nemici: chi lo usa sceglie solo il bersaglio.
    /// Se il bersaglio è lontano lo raggiunge con l'agent, a portata si ferma, si gira e colpisce.
    /// Il danno arriva dopo il ritardo dell'arma, ricontrollando che il bersaglio sia ancora lì.
    /// A quel punto si tira: colpito o mancato (Destrezza contro Armatura), poi il danno (arma e
    /// Forza), con la formula di CombatFormulas.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public class MeleeAttack : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition _weapon;

        [Tooltip("Ogni quanti secondi si ricalcola il percorso verso un bersaglio che si muove.")]
        [SerializeField, Min(0.02f)] private float _chaseRepathInterval = 0.1f;

        [Tooltip("Entro quanti gradi dalla direzione del bersaglio il colpo può partire.")]
        [SerializeField, Range(1f, 180f)] private float _facingTolerance = 30f;

        private NavMeshAgent _agent;
        private CharacterStats _stats;
        private IRandomSource _random;

        // il bersaglio scelto e quello del colpo in corso sono distinti: cambiare bersaglio
        // a metà fendente non deve spostare il danno su un altro nemico
        private IDamageable _target;
        private Transform _targetTransform;
        private float _targetRadius;
        private CharacterStats _targetStats;
        private bool _attackRequested;

        private IDamageable _swingTarget;
        private Transform _swingTransform;
        private float _swingTargetRadius;
        private CharacterStats _swingTargetStats;
        private bool _hitPending;
        private float _hitTimer;

        private float _cooldown;
        private float _repathTimer;
        private float _lockTimer;
        private bool _inRange;

        /// <summary>Partito un colpo: l'animazione d'attacco si aggancia qui.</summary>
        public event Action SwingStarted;

        /// <summary>Il colpo è arrivato e ha fatto danno: hit stop e simili si agganciano qui.</summary>
        public event Action<DamageInfo> HitLanded;

        /// <summary>Il colpo è arrivato a portata ma il tiro l'ha mancato: niente danno né hit stop.</summary>
        public event Action<DamageInfo> Missed;

        public WeaponDefinition Weapon => _weapon;

        public bool HasTarget => _targetTransform != null;

        /// <summary>Vero dall'inizio del colpo al danno: in questo intervallo il colpo non si annulla.</summary>
        public bool IsSwinging => _hitPending;

        /// <summary>Il bersaglio è a portata: fermo, girato o in attesa del prossimo colpo.</summary>
        public bool IsTargetInRange => _inRange;

        /// <summary>Vero durante il blocco dopo un'interruzione: niente inseguimento né colpi.</summary>
        public bool IsInterrupted => _lockTimer > 0f;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            // senza statistiche (un bersaglio di prova) si tira con Forza e Destrezza a zero
            TryGetComponent(out _stats);
        }

        /// <summary>Da dove vengono i tiri: lo passa il CompositionRoot (D3 della M4).</summary>
        public void SetRandomSource(IRandomSource random)
        {
            _random = random;
        }

        private void OnDisable()
        {
            // spento a metà colpo (per esempio alla morte): il danno non deve arrivare più tardi
            _hitPending = false;
            _lockTimer = 0f;
            ForgetTarget();
            ForgetSwingTarget();
        }

        /// <summary>
        /// Chiede un colpo al bersaglio: lo raggiunge se serve, colpisce una volta e poi lo lascia.
        /// Per colpire di continuo si richiama a ogni tick (tasto tenuto premuto, IA in attacco).
        /// </summary>
        public void SetTarget(IDamageable target)
        {
            if (!(target is Component component) || !target.IsAlive())
            {
                return;
            }

            if (component.transform != _targetTransform)
            {
                _target = target;
                _targetTransform = component.transform;
                _targetRadius = RadiusOf(component);
                _targetStats = component.GetComponent<CharacterStats>();
                _repathTimer = 0f;
            }

            _attackRequested = true;
        }

        /// <summary>Lascia il bersaglio. Un colpo già partito arriva comunque.</summary>
        public void ClearTarget()
        {
            if (_targetTransform != null)
            {
                StopAgent();
            }

            ForgetTarget();
        }

        /// <summary>
        /// Annulla il colpo in corso (il danno non arriva) e blocca il personaggio per la durata data.
        /// Il bersaglio resta: finito il blocco, se qualcuno lo richiede ancora, si riprende.
        /// </summary>
        public void Interrupt(float lockDuration)
        {
            _hitPending = false;
            ForgetSwingTarget();
            _lockTimer = Mathf.Max(_lockTimer, lockDuration);
            StopAgent();
        }

        private void Update()
        {
            if (_cooldown > 0f)
            {
                _cooldown -= Time.deltaTime;
            }

            if (_lockTimer > 0f)
            {
                _lockTimer -= Time.deltaTime;
                return;
            }

            if (_hitPending)
            {
                _hitTimer -= Time.deltaTime;
                if (_hitTimer <= 0f)
                {
                    ResolveHit();
                }

                // il colpo partito impegna il personaggio: niente inseguimento né rotazione
                return;
            }

            if (_targetTransform == null)
            {
                return;
            }

            // colpo già dato e nessuna nuova richiesta, oppure bersaglio morto o distrutto
            if (!_attackRequested || !_target.IsAlive())
            {
                ClearTarget();
                return;
            }

            Vector3 toTarget = Flat(_targetTransform.position - transform.position);
            _inRange = EdgeDistance(toTarget, _targetRadius) <= _weapon.Range;
            if (!_inRange)
            {
                Chase();
                return;
            }

            // a portata ci si ferma qui, non con stoppingDistance: l'agent si fermerebbe a distanze
            // diverse secondo l'angolo d'arrivo (trappola 2 della scheda M2)
            StopAgent();

            if (!FaceTowards(toTarget) || _cooldown > 0f)
            {
                return;
            }

            StartSwing();
        }

        private void Chase()
        {
            _repathTimer -= Time.deltaTime;
            if (_repathTimer > 0f || !_agent.isOnNavMesh)
            {
                return;
            }

            _repathTimer = _chaseRepathInterval;
            _agent.SetDestination(_targetTransform.position);
        }

        private bool FaceTowards(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            Quaternion desired = Quaternion.LookRotation(toTarget);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, _agent.angularSpeed * Time.deltaTime);
            return Quaternion.Angle(transform.rotation, desired) <= _facingTolerance;
        }

        private void StartSwing()
        {
            _attackRequested = false;
            _hitPending = true;
            _hitTimer = _weapon.HitDelay;
            _cooldown = _weapon.AttackInterval;

            _swingTarget = _target;
            _swingTransform = _targetTransform;
            _swingTargetRadius = _targetRadius;
            _swingTargetStats = _targetStats;

            SwingStarted?.Invoke();
        }

        // Nel frattempo il bersaglio può essere morto, scappato o distrutto (trappola 10):
        // si ricontrolla tutto, con il confronto di Unity sul Transform.
        private void ResolveHit()
        {
            _hitPending = false;
            bool inReach = _swingTransform != null
                && _swingTarget.IsAlive()
                && EdgeDistance(Flat(_swingTransform.position - transform.position), _swingTargetRadius) <= _weapon.Range + _weapon.RangeTolerance;

            if (inReach)
            {
                if (_random == null)
                {
                    throw new InvalidOperationException($"{name}: MeleeAttack senza IRandomSource, lo collega il CompositionRoot");
                }

                float hitChance = CombatFormulas.HitChance(
                    CharacterStats.ValueOf(_stats, StatType.Dexterity),
                    CharacterStats.ValueOf(_swingTargetStats, StatType.Armor));

                if (CombatFormulas.RollHit(hitChance, _random))
                {
                    float amount = CombatFormulas.RollDamage(_weapon.MinDamage, _weapon.MaxDamage,
                        CharacterStats.ValueOf(_stats, StatType.Strength), _random);
                    var info = new DamageInfo(amount, _weapon.DamageType, gameObject);
                    _swingTarget.TakeDamage(info);
                    HitLanded?.Invoke(info);
                }
                else
                {
                    var info = new DamageInfo(0f, _weapon.DamageType, gameObject);
                    _swingTarget.Evade(info);
                    Missed?.Invoke(info);
                }
            }

            ForgetSwingTarget();
        }

        private float EdgeDistance(Vector3 flatOffset, float targetRadius)
        {
            return flatOffset.magnitude - _agent.radius - targetRadius;
        }

        private void StopAgent()
        {
            if (_agent.enabled && _agent.isOnNavMesh && _agent.hasPath)
            {
                _agent.ResetPath();
            }
        }

        private void ForgetTarget()
        {
            _target = null;
            _targetTransform = null;
            _targetStats = null;
            _attackRequested = false;
            _inRange = false;
        }

        private void ForgetSwingTarget()
        {
            _swingTarget = null;
            _swingTransform = null;
            _swingTargetStats = null;
        }

        private static float RadiusOf(Component target)
        {
            return target.TryGetComponent(out NavMeshAgent agent) ? agent.radius : 0f;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
