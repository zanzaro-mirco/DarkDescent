using System;
using DarkDescent.Audio;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DarkDescent.Characters
{
    /// <summary>
    /// Suoni di un personaggio: fendente quando parte un colpo, impatto quando ne riceve uno, scudo
    /// quando lo blocca, vetro quando beve una pozione, morte.
    /// Ogni personaggio ha i suoi: le ossa dello scheletro non suonano come l'armatura del cavaliere.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class CharacterAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip[] _swingClips;
        [SerializeField] private AudioClip[] _hitClips;
        [SerializeField] private AudioClip[] _deathClips;

        [Tooltip("Colpo fermato dallo scudo: metallo, non carne.")]
        [SerializeField] private AudioClip[] _blockClips;

        [Tooltip("Una cura, cioè una pozione bevuta: la boccetta di vetro.")]
        [SerializeField] private AudioClip[] _healClips;

        [Tooltip("Variazione casuale dell'intonazione, per non sentire lo stesso suono identico a ogni colpo.")]
        [SerializeField, Range(0f, 0.3f)] private float _pitchVariation = 0.06f;

        private AudioSource _source;
        private SfxLimiter _limiter;
        private EnemyArchetype _archetype;
        private int _lastVoice = -1;
        private Health _health;
        private MeleeAttack _attack;
        private ShieldBlock _shieldBlock;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _health = GetComponent<Health>();
            _attack = GetComponent<MeleeAttack>();
            _shieldBlock = GetComponent<ShieldBlock>();

            // i versi del critico sono del tipo di nemico: il cavaliere non ne ha
            if (TryGetComponent(out EnemyAI enemy))
            {
                _archetype = enemy.Archetype;
            }
        }

        /// <summary>Un verso del critico è partito: per i test.</summary>
        public event Action<AudioClip> CriticalVoicePlayed;

        /// <summary>
        /// A chi chiedere il permesso di suonare (D9 della M7): lo passa il composition root. Senza,
        /// in una scena di prova, si suona sempre.
        /// </summary>
        public void Bind(SfxLimiter limiter)
        {
            _limiter = limiter;
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.Damaged += HandleDamaged;
                _health.Died += HandleDied;
                _health.Healed += HandleHealed;
            }

            if (_attack != null)
            {
                _attack.SwingStarted += HandleSwingStarted;
            }

            if (_shieldBlock != null)
            {
                _shieldBlock.Blocked += HandleBlocked;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= HandleDamaged;
                _health.Died -= HandleDied;
                _health.Healed -= HandleHealed;
            }

            if (_attack != null)
            {
                _attack.SwingStarted -= HandleSwingStarted;
            }

            if (_shieldBlock != null)
            {
                _shieldBlock.Blocked -= HandleBlocked;
            }
        }

        private void HandleSwingStarted()
        {
            PlayRandom(SfxKind.Swing, _swingClips);
        }

        private void HandleDamaged(DamageInfo info, float applied)
        {
            if (info.IsCritical && _archetype != null && _archetype.CriticalVoiceCount > 0)
            {
                PlayCriticalVoice();
                return;
            }

            PlayRandom(SfxKind.Hit, _hitClips);
        }

        // Al posto dell'impatto: il verso dice chi l'ha preso anche nel buio (D13). Mai lo stesso due
        // volte di fila, con l'intonazione del tipo di nemico.
        private void PlayCriticalVoice()
        {
            int count = _archetype.CriticalVoiceCount;
            int index = Random.Range(0, count);
            if (count > 1 && index == _lastVoice)
            {
                index = (index + 1) % count;
            }

            _lastVoice = index;
            var clip = _archetype.GetCriticalVoice(index);
            _source.pitch = Random.Range(_archetype.CriticalPitch.x, _archetype.CriticalPitch.y);

            // un tipo a sé: il verso passa anche nel fotogramma in cui un altro nemico fa un impatto
            if (Play(SfxKind.CriticalVoice, clip))
            {
                CriticalVoicePlayed?.Invoke(clip);
            }
        }

        private void HandleBlocked(DamageInfo info)
        {
            PlayRandom(SfxKind.Block, _blockClips);
        }

        private void HandleDied()
        {
            PlayRandom(SfxKind.Death, _deathClips);
        }

        private void HandleHealed(float amount)
        {
            PlayRandom(SfxKind.Heal, _healClips);
        }

        private void PlayRandom(SfxKind kind, AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            _source.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
            Play(kind, clips[Random.Range(0, clips.Length)]);
        }

        // PlayOneShot sovrappone i suoni sulla stessa sorgente: impatto e morte nello stesso frame si
        // sentono entrambi, se il limite di voci li lascia passare
        private bool Play(SfxKind kind, AudioClip clip)
        {
            if (_limiter != null && !_limiter.TryPlay(kind, _source, clip))
            {
                return false;
            }

            _source.PlayOneShot(clip);
            return true;
        }
    }
}
