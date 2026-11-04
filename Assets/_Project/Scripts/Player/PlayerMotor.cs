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

        [Tooltip("Con i tasti di movimento, quanti metri avanti sta la destinazione (D4 della M23).")]
        [SerializeField, Min(0.1f)] private float _keyStep = 1.5f;

        [Tooltip("Contro un bordo, sotto questa parte della direzione lungo il bordo ci si ferma invece di scivolare.")]
        [SerializeField, Range(0f, 1f)] private float _slideLeast = 0.2f;

        [Tooltip("Sotto questa distanza dal bordo non si riparte: il cavaliere è già contro il muro.")]
        [SerializeField, Min(0f)] private float _keyMinimum = 0.05f;

        private NavMeshAgent _agent;

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

        /// <summary>
        /// Spinge il cavaliere verso una direzione in piano (WASD, D4 della M23): una destinazione poco avanti, ritagliata sul
        /// bordo del NavMesh, che un muro o una balaustra fermano. Contro un bordo storto si scivola lungo il bordo; contro un
        /// bordo dritto ci si ferma, senza aggirare niente (un punto oltre l'ostacolo, preso con <see cref="MoveTo"/>,
        /// manderebbe l'agent a fare il giro). Vero se il cavaliere ha dove andare.
        /// </summary>
        public bool Steer(Vector3 direction)
        {
            direction.y = 0f;
            if (!_agent.isOnNavMesh || direction.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            direction.Normalize();
            Vector3 from = transform.position;
            Vector3 destination = from + direction * _keyStep;
            if (NavMesh.Raycast(from, destination, out NavMeshHit edge, NavMesh.AllAreas))
            {
                destination = edge.position;
                Vector3 along = Vector3.ProjectOnPlane(direction, edge.normal);
                along.y = 0f;
                if (along.magnitude > _slideLeast)
                {
                    Vector3 slideTo = edge.position + along.normalized * _keyStep;
                    destination = NavMesh.Raycast(edge.position, slideTo, out NavMeshHit second, NavMesh.AllAreas) ? second.position : slideTo;
                }
            }

            Vector3 offset = destination - from;
            offset.y = 0f;
            if (offset.sqrMagnitude < _keyMinimum * _keyMinimum)
            {
                Stop();
                return false;
            }

            if (_agent.hasPath && (destination - _agent.destination).sqrMagnitude < _repathThreshold * _repathThreshold)
            {
                return true;
            }

            _agent.SetDestination(destination);
            return true;
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
