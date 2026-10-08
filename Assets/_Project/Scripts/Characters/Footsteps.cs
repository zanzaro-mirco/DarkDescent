using System;
using DarkDescent.Audio;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DarkDescent.Characters
{
    /// <summary>
    /// I passi del cavaliere (prova della M7): un passo quando un piede tocca terra nell'animazione di
    /// corsa, con i suoni del pavimento del livello, pietra nella cripta e terra nelle caverne. Si segue
    /// il ciclo dell'animazione e non lo spazio percorso: contando i metri i passi andavano più svelti
    /// dei piedi (seconda prova della build M7). Ha una sorgente sua, su un figlio del cavaliere:
    /// l'intonazione dei passi non tocca i colpi che suonano insieme.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class Footsteps : MonoBehaviour
    {
        private static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [Tooltip("L'animator del modello del cavaliere. Vuoto: quello tra i figli del suo padre.")]
        [SerializeField] private Animator _animator;

        [Tooltip("Dove i piedi toccano terra nel ciclo della corsa, da 0 a 1 (Running_A di KayKit: il sinistro al 12%, il destro al 62%).")]
        [SerializeField] private float[] _footfalls = { 0.12f, 0.62f };

        [Tooltip("Sotto questa velocità (il parametro Speed, da 0 a 1) i piedi quasi non si staccano: niente passi.")]
        [SerializeField, Range(0f, 1f)] private float _minSpeed = 0.3f;

        [SerializeField, Range(0f, 1f)] private float _volume = 0.5f;

        [SerializeField, Range(0f, 0.3f)] private float _pitchVariation = 0.08f;

        private AudioSource _source;
        private SfxLimiter _limiter;
        private AudioClip[] _clips;
        private int _lastClip = -1;

        // dove era il ciclo della corsa al frame prima; negativo fuori dalla corsa
        private float _lastCycle = -1f;

        /// <summary>Un passo è suonato: per i test.</summary>
        public event Action<AudioClip> StepPlayed;

        /// <summary>I suoni del pavimento del livello corrente; vuoto, i passi tacciono.</summary>
        public AudioClip[] Surface => _clips;

        /// <summary>Dove i piedi toccano terra nel ciclo della corsa: per i test.</summary>
        public float[] Footfalls => _footfalls;

        /// <summary>L'animator che si segue: per i test.</summary>
        public Animator Animator => _animator;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            if (_animator == null && transform.parent != null)
            {
                _animator = transform.parent.GetComponentInChildren<Animator>();
            }
        }

        /// <summary>A chi chiedere il permesso di suonare (D9 della M7): lo passa il composition root.</summary>
        public void Bind(SfxLimiter limiter)
        {
            _limiter = limiter;
        }

        /// <summary>Il pavimento del livello in cui si è appena entrati, dal suo tileset.</summary>
        public void SetSurface(AudioClip[] clips)
        {
            _clips = clips;
            _lastCycle = -1f;
        }

        private void Update()
        {
            if (_animator == null)
            {
                return;
            }

            // durante una transizione lo stato corrente è quello da cui si esce: dall'attacco alla
            // corsa i passi ripartono a transizione finita
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash != LocomotionHash)
            {
                _lastCycle = -1f;
                return;
            }

            float before = _lastCycle;
            float now = state.normalizedTime;
            _lastCycle = now;
            if (before < 0f || now <= before || _animator.GetFloat(SpeedHash) < _minSpeed)
            {
                return;
            }

            // un piede tocca terra quando il ciclo passa il suo punto: con la parte intera si contano
            // anche i giri completi, e un frame lungo non perde il passo
            foreach (float footfall in _footfalls)
            {
                if (Mathf.Floor(now - footfall) > Mathf.Floor(before - footfall))
                {
                    Play();
                    return;
                }
            }
        }

        private void Play()
        {
            if (_clips == null || _clips.Length == 0)
            {
                return;
            }

            // mai lo stesso due volte di fila: cinque passi uguali in fila si sentono
            int index = Random.Range(0, _clips.Length);
            if (_clips.Length > 1 && index == _lastClip)
            {
                index = (index + 1) % _clips.Length;
            }

            _lastClip = index;
            var clip = _clips[index];
            if (_limiter != null && !_limiter.TryPlay(SfxKind.Footstep, _source, clip))
            {
                return;
            }

            _source.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
            _source.PlayOneShot(clip, _volume);
            StepPlayed?.Invoke(clip);
        }
    }
}
