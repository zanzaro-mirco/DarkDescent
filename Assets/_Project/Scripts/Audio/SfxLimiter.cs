using System;
using UnityEngine;

namespace DarkDescent.Audio
{
    /// <summary>
    /// Il limite di voci dei personaggi (D9 della M7), in Core: chi vuole suonare chiede qui. Misura la
    /// distanza dalle orecchie del cavaliere, decide con <see cref="SfxBudget"/> e dà alla sorgente la
    /// priorità della sua distanza. Musica e versi nel buio non passano di qui: hanno sorgenti loro.
    /// </summary>
    [DisallowMultipleComponent]
    public class SfxLimiter : MonoBehaviour
    {
        [Tooltip("Le orecchie: l'AudioListener sulla testa del cavaliere.")]
        [SerializeField] private Transform _listener;

        [Tooltip("Quanti suoni dei personaggi insieme, al massimo.")]
        [SerializeField, Range(1, 32)] private int _maxVoices = 12;

        private SfxBudget _budget;

        /// <summary>Un suono è passato, o è stato tenuto muto: per i test.</summary>
        public event Action<SfxKind, bool> Requested;

        public int ActiveVoices => _budget.ActiveVoices;

        private void Awake()
        {
            _budget = new SfxBudget(_maxVoices);
        }

        /// <summary>
        /// Vero se il suono di questo tipo, da questa sorgente, può partire adesso. Se sì, la sorgente
        /// ha già la priorità giusta.
        /// </summary>
        public bool TryPlay(SfxKind kind, AudioSource source, AudioClip clip)
        {
            float distance = _listener != null ? Vector3.Distance(_listener.position, source.transform.position) : 0f;
            float duration = clip.length / Mathf.Max(0.01f, Mathf.Abs(source.pitch));

            // in tempo reale: durante l'hit stop il tempo di gioco è fermo, i suoni no
            bool allowed = _budget.Request(kind, Time.frameCount, Time.unscaledTime, distance, duration);
            if (allowed)
            {
                source.priority = SfxBudget.Priority(distance);
            }

            Requested?.Invoke(kind, allowed);
            return allowed;
        }
    }
}
