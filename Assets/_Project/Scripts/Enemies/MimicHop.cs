using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Il mimic (D3 della M25) non ha uno scheletro né animazioni: il forziere salta a ogni passo e il coperchio sbatte come una bocca
    /// mentre insegue. Solo il modello si muove: l'agente, il collider e i colpi restano fermi sulla radice. Il coperchio sta fermo
    /// quando il mimic non cammina.
    /// </summary>
    [DisallowMultipleComponent]
    public class MimicHop : MonoBehaviour
    {
        [Tooltip("Il modello del forziere: salta sull'asse Y.")]
        [SerializeField] private Transform _visual;

        [Tooltip("Il coperchio: ruota sul suo perno, che nel modello dei KayKit è la cerniera dietro.")]
        [SerializeField] private Transform _lid;

        [SerializeField] private NavMeshAgent _agent;

        [Tooltip("L'altezza dei salti, in metri.")]
        [SerializeField, Min(0f)] private float _hopHeight = 0.22f;

        [Tooltip("Quanti salti al secondo mentre cammina.")]
        [SerializeField, Min(0.1f)] private float _hopsPerSecond = 2.5f;

        [Tooltip("Di quanto si apre la bocca al massimo, in gradi.")]
        [SerializeField] private float _mouthAngle = 38f;

        private Vector3 _visualHome;
        private Quaternion _lidHome;
        private float _phase;

        /// <summary>Il modello è sollevato da terra in questo momento: per i test.</summary>
        public float Lift => _visual != null ? _visual.localPosition.y - _visualHome.y : 0f;

        public void Configure(Transform visual, Transform lid, NavMeshAgent agent)
        {
            _visual = visual;
            _lid = lid;
            _agent = agent;
        }

        private void Awake()
        {
            if (_agent == null)
            {
                _agent = GetComponent<NavMeshAgent>();
            }

            if (_visual != null)
            {
                _visualHome = _visual.localPosition;
            }

            if (_lid != null)
            {
                _lidHome = _lid.localRotation;
            }
        }

        private void Update()
        {
            bool moving = _agent != null && _agent.enabled && _agent.isOnNavMesh && _agent.velocity.sqrMagnitude > 0.04f;
            _phase = moving ? _phase + Time.deltaTime * _hopsPerSecond : 0f;
            float arc = moving ? Mathf.Abs(Mathf.Sin(_phase * Mathf.PI)) : 0f;
            if (_visual != null)
            {
                _visual.localPosition = _visualHome + Vector3.up * (arc * _hopHeight);
            }

            if (_lid != null)
            {
                // la bocca si apre a ogni salto, e si richiude all'atterraggio
                _lid.localRotation = _lidHome * Quaternion.Euler(-_mouthAngle * arc, 0f, 0f);
            }
        }
    }
}
