using System;
using DarkDescent.Audio;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DarkDescent.Characters
{
    /// <summary>
    /// I passi del cavaliere (prova della M7): un passo ogni tanti metri percorsi, con i suoni del
    /// pavimento del livello, pietra nella cripta e terra nelle caverne. Si conta lo spazio e non il
    /// tempo, così fermo non suona e di corsa i passi si fittano. Ha una sorgente sua, su un figlio del
    /// cavaliere: l'intonazione dei passi non tocca i colpi che suonano insieme.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class Footsteps : MonoBehaviour
    {
        [Tooltip("Ogni quanti metri percorsi un passo: a 5 m/s, tre passi al secondo.")]
        [SerializeField, Min(0.2f)] private float _stride = 1.6f;

        [SerializeField, Range(0f, 1f)] private float _volume = 0.5f;

        [SerializeField, Range(0f, 0.3f)] private float _pitchVariation = 0.08f;

        [Tooltip("Uno spostamento più lungo in un fotogramma non è un passo: è il cambio di livello o la ripartenza.")]
        [SerializeField, Min(0.1f)] private float _jumpDistance = 1f;

        private AudioSource _source;
        private SfxLimiter _limiter;
        private AudioClip[] _clips;
        private Vector3 _lastPosition;
        private float _travelled;
        private int _lastClip = -1;

        /// <summary>Un passo è suonato: per i test.</summary>
        public event Action<AudioClip> StepPlayed;

        /// <summary>I suoni del pavimento del livello corrente; vuoto, i passi tacciono.</summary>
        public AudioClip[] Surface => _clips;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _lastPosition = transform.position;
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
            _travelled = 0f;
            _lastPosition = transform.position;
        }

        private void Update()
        {
            Vector3 position = transform.position;
            Vector3 step = position - _lastPosition;
            step.y = 0f;
            _lastPosition = position;

            float distance = step.magnitude;
            if (distance > _jumpDistance)
            {
                _travelled = 0f;
                return;
            }

            _travelled += distance;
            if (_travelled < _stride)
            {
                return;
            }

            _travelled -= _stride;
            Play();
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
