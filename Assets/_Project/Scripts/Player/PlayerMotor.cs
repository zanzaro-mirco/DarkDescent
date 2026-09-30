using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Player
{
    /// <summary>
    /// Esegue il movimento verso un punto del mondo tramite il NavMeshAgent.
    /// Non conosce l'input: riceve solo destinazioni.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerMotor : MonoBehaviour
    {
        [Tooltip("Sotto questa distanza dalla destinazione corrente il nuovo punto viene ignorato, per non ricalcolare il path a vuoto.")]
        [SerializeField, Min(0f)] private float _repathThreshold = 0.2f;

        [Tooltip("Raggio entro cui cercare il punto di NavMesh più vicino a quello richiesto.")]
        [SerializeField, Min(0.01f)] private float _sampleRadius = 1f;

        private NavMeshAgent _agent;

        /// <summary>0 = fermo, 1 = velocità massima. Grezza, senza smoothing: lo smorzamento spetta a chi la legge.</summary>
        public float NormalizedSpeed => _agent.speed > 0f ? Mathf.Clamp01(_agent.velocity.magnitude / _agent.speed) : 0f;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        public void MoveTo(Vector3 worldPoint)
        {
            // SetDestination su un agent fuori dal NavMesh logga un errore invece di fallire in silenzio
            if (!_agent.isOnNavMesh)
            {
                return;
            }

            // il punto cliccato può cadere appena fuori dal NavMesh (bordo ritagliato attorno agli ostacoli
            // dal raggio dell'agent): lo si riporta sul punto navigabile più vicino
            if (!NavMesh.SamplePosition(worldPoint, out NavMeshHit hit, _sampleRadius, NavMesh.AllAreas))
            {
                return;
            }

            if (_agent.hasPath && (hit.position - _agent.destination).sqrMagnitude < _repathThreshold * _repathThreshold)
            {
                return;
            }

            _agent.SetDestination(hit.position);
        }

        public void Stop()
        {
            if (_agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }
        }
    }
}
