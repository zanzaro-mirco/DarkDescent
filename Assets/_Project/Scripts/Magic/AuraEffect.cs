using UnityEngine;

namespace DarkDescent.Magic
{
    /// <summary>
    /// L'effetto di un potenziamento (D8 della M9): un alone attorno al cavaliere finché dura, come
    /// la polvere grigia della Pelle di pietra. Lo accende e lo spegne <see cref="PlayerSpells"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class AuraEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _particles;

        [Tooltip("Una luce tenue, accesa finché dura: nel buio della cripta l'alone si vede.")]
        [SerializeField] private Light _light;

        [SerializeField] private AudioSource _audio;

        [Tooltip("Il suono all'accensione.")]
        [SerializeField] private AudioClip _startClip;

        public bool IsPlaying { get; private set; }

        public void Play()
        {
            IsPlaying = true;
            gameObject.SetActive(true);
            _particles.Clear();
            _particles.Play();
            if (_light != null)
            {
                _light.enabled = true;
            }

            if (_audio != null && _startClip != null)
            {
                _audio.PlayOneShot(_startClip);
            }
        }

        /// <summary>Smette di emettere: le particelle già nate si spengono da sole.</summary>
        public void Stop()
        {
            IsPlaying = false;
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_light != null)
            {
                _light.enabled = false;
            }
        }
    }
}
