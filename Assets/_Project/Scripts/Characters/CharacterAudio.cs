using DarkDescent.Combat;
using UnityEngine;

namespace DarkDescent.Characters
{
    /// <summary>
    /// Suoni di un personaggio: fendente quando parte un colpo, impatto quando ne riceve uno, morte.
    /// Ogni personaggio ha i suoi: le ossa dello scheletro non suonano come l'armatura del cavaliere.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class CharacterAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip[] _swingClips;
        [SerializeField] private AudioClip[] _hitClips;
        [SerializeField] private AudioClip[] _deathClips;

        [Tooltip("Variazione casuale dell'intonazione, per non sentire lo stesso suono identico a ogni colpo.")]
        [SerializeField, Range(0f, 0.3f)] private float _pitchVariation = 0.06f;

        private AudioSource _source;
        private Health _health;
        private MeleeAttack _attack;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _health = GetComponent<Health>();
            _attack = GetComponent<MeleeAttack>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.Damaged += HandleDamaged;
                _health.Died += HandleDied;
            }

            if (_attack != null)
            {
                _attack.SwingStarted += HandleSwingStarted;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= HandleDamaged;
                _health.Died -= HandleDied;
            }

            if (_attack != null)
            {
                _attack.SwingStarted -= HandleSwingStarted;
            }
        }

        private void HandleSwingStarted()
        {
            PlayRandom(_swingClips);
        }

        private void HandleDamaged(DamageInfo info, float applied)
        {
            PlayRandom(_hitClips);
        }

        private void HandleDied()
        {
            PlayRandom(_deathClips);
        }

        private void PlayRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            // PlayOneShot sovrappone i suoni sulla stessa sorgente: impatto e morte nello stesso frame si sentono entrambi
            _source.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
            _source.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        }
    }
}
