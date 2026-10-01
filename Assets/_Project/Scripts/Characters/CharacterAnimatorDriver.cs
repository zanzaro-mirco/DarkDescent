using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Characters
{
    /// <summary>
    /// Traduce lo stato di un personaggio, player o nemico, in animazioni. Nessuna logica di gioco:
    /// la locomozione la legge dall'agent, gli one-shot (attacco, colpo subito, morte) li riceve
    /// da chi decide. Sta sul modello, figlio della radice che porta l'agent.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimatorDriver : MonoBehaviour
    {
        // hash calcolati una volta: le chiamate con la stringa fanno un lookup ogni volta
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackStateHash = Animator.StringToHash("Attack");
        private static readonly int HitStateHash = Animator.StringToHash("Hit");
        private static readonly int DeathStateHash = Animator.StringToHash("Death");

        private const int BaseLayer = 0;

        [Tooltip("Vuoto = NavMeshAgent del parent, risolto in Awake.")]
        [SerializeField] private NavMeshAgent _agent;

        [Tooltip("Smorzamento del parametro Speed, in secondi. Evita gli scatti tra idle e movimento.")]
        [SerializeField, Min(0f)] private float _speedDampTime = 0.1f;

        [Tooltip("Dissolvenza verso attacco, colpo subito e morte, in secondi.")]
        [SerializeField, Min(0f)] private float _oneShotFadeTime = 0.05f;

        private Animator _animator;

        /// <summary>Dopo la morte il driver ignora ogni altro comando: l'animazione resta sull'ultimo fotogramma.</summary>
        public bool IsDead { get; private set; }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_agent == null)
            {
                _agent = GetComponentInParent<NavMeshAgent>();
            }
        }

        private void Update()
        {
            _animator.SetFloat(SpeedHash, NormalizedSpeed(), _speedDampTime, Time.deltaTime);
        }

        public void PlayAttack()
        {
            PlayOneShot(AttackStateHash);
        }

        public void PlayHit()
        {
            PlayOneShot(HitStateHash);
        }

        public void PlayDeath()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            _animator.CrossFadeInFixedTime(DeathStateHash, _oneShotFadeTime, BaseLayer, 0f);
        }

        // CrossFade sullo stato e non trigger: un trigger arrivato durante una transizione resta
        // armato e fa partire un secondo attacco (trappola 4 della scheda M2). Con offset 0 lo stesso
        // stato riparte dall'inizio, quindi due attacchi di fila sono due fendenti.
        private void PlayOneShot(int stateHash)
        {
            if (IsDead)
            {
                return;
            }

            _animator.CrossFadeInFixedTime(stateHash, _oneShotFadeTime, BaseLayer, 0f);
        }

        private float NormalizedSpeed()
        {
            // l'agent può mancare (modello in una scena di prova) o essere spento dopo la morte
            if (_agent == null || !_agent.enabled || _agent.speed <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(_agent.velocity.magnitude / _agent.speed);
        }
    }
}
