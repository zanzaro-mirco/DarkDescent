using DarkDescent.Player;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DarkDescent.Town
{
    /// <summary>
    /// Un abitante del borgo (D9 della M18), senza intelligenza né dialoghi: tiene una posa in loop,
    /// seduto attorno al fuoco o in piedi. Con il saluto, quando il cavaliere entra nel suo trigger
    /// saluta per qualche secondo e torna alla posa. Le due clip passano da un grafo di Playables suo,
    /// senza Animator Controller. Ognuno comincia da un punto diverso del giro, preso dalla posizione:
    /// seduti insieme non respirano all'unisono.
    /// </summary>
    [DisallowMultipleComponent]
    public class Villager : MonoBehaviour
    {
        [Tooltip("La posa che tiene sempre, in loop: seduto per terra, in piedi.")]
        [SerializeField] private AnimationClip _pose;

        [Tooltip("Il saluto quando arriva il cavaliere; vuoto, non saluta. Serve un collider trigger con un Rigidbody cinematico.")]
        [SerializeField] private AnimationClip _greeting;

        [SerializeField, Min(0.5f)] private float _greetingSeconds = 4.2f;
        [SerializeField, Min(0.05f)] private float _blendSeconds = 0.3f;

        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private Greeting _greetingState;

        /// <summary>Saluta in questo momento.</summary>
        public bool IsGreeting => _greetingState != null && _greetingState.IsWaving;

        public bool Greets => _greeting != null;

        public AnimationClip Pose => _pose;

        private void OnEnable()
        {
            var animator = GetComponentInChildren<Animator>();
            if (animator == null || _pose == null)
            {
                return;
            }

            _graph = PlayableGraph.Create(name);
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _mixer = AnimationMixerPlayable.Create(_graph, 2);
            var pose = AnimationClipPlayable.Create(_graph, _pose);
            pose.SetTime(Phase() * _pose.length);
            _graph.Connect(pose, 0, _mixer, 0);
            _mixer.SetInputWeight(0, 1f);
            if (_greeting != null)
            {
                _graph.Connect(AnimationClipPlayable.Create(_graph, _greeting), 0, _mixer, 1);
                _greetingState = new Greeting(_greetingSeconds, _blendSeconds);
            }

            AnimationPlayableOutput.Create(_graph, "Pose", animator).SetSourcePlayable(_mixer);
            _graph.Play();
        }

        private void OnDisable()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }

            _greetingState = null;
        }

        private void Update()
        {
            if (_greetingState == null)
            {
                return;
            }

            _greetingState.Tick(Time.deltaTime);
            float weight = _greetingState.Weight;
            _mixer.SetInputWeight(0, 1f - weight);
            _mixer.SetInputWeight(1, weight);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_greetingState == null || other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            // un saluto nuovo comincia dall'inizio del gesto, non da dove era rimasto
            if (!_greetingState.IsWaving)
            {
                _mixer.GetInput(1).SetTime(0);
            }

            _greetingState.Begin();
        }

        private float Phase()
        {
            Vector3 p = transform.position;
            uint hash = unchecked((uint)Mathf.RoundToInt(p.x * 10f) * 73856093u ^ (uint)Mathf.RoundToInt(p.z * 10f) * 19349663u);
            hash ^= hash >> 13;
            return (hash % 1000) / 1000f;
        }
    }
}
