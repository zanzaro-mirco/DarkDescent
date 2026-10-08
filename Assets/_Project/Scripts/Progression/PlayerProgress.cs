using System;
using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Stats;
using UnityEngine;

namespace DarkDescent.Progression
{
    /// <summary>
    /// La crescita del cavaliere nel gioco (passo 8.2 della M8): riceve l'esperienza dei nemici
    /// uccisi, e salendo di livello riempie la vita (D3), suona e accende per un momento una luce
    /// dorata attorno a lui. Il livello, l'esperienza e i punti li tiene <see cref="CharacterProgress"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats), typeof(Health))]
    public class PlayerProgress : MonoBehaviour
    {
        [SerializeField] private ProgressionSettings _settings;

        [Tooltip("Una sorgente sua, su un figlio: l'intonazione dei colpi non tocca il suono del livello.")]
        [SerializeField] private AudioSource _audio;

        [SerializeField] private AudioClip _levelUpClip;

        [SerializeField, Range(0f, 1f)] private float _volume = 0.8f;

        [Tooltip("La luce dorata attorno al cavaliere salendo di livello.")]
        [SerializeField] private Light _glow;

        [SerializeField, Min(0f)] private float _glowIntensity = 6f;

        [SerializeField, Min(0.05f)] private float _glowDuration = 1.4f;

        private CharacterProgress _progress;
        private Health _health;
        private Coroutine _glowing;
        private int _lastSoundFrame = -1;

        /// <summary>Salito di livello: il suono e la luce sono partiti. Per i test.</summary>
        public event Action<int> LevelUpShown;

        // creato al primo accesso: HUD e pannello si collegano dal composition root, in un ordine
        // che rispetto all'Awake di questo componente non è garantito
        public CharacterProgress Progress => _progress ??= new CharacterProgress(_settings, GetComponent<CharacterStats>().Sheet);

        public ProgressionSettings Settings => _settings;

        public bool IsGlowing => _glow != null && _glow.enabled;

        private void Awake()
        {
            _health = GetComponent<Health>();
            if (_glow != null)
            {
                _glow.enabled = false;
            }
        }

        private void OnEnable()
        {
            Progress.LeveledUp += HandleLeveledUp;
        }

        private void OnDisable()
        {
            Progress.LeveledUp -= HandleLeveledUp;
            _glowing = null;
            if (_glow != null)
            {
                _glow.enabled = false;
            }
        }

        /// <summary>
        /// Un nemico che vale <paramref name="baseExperience"/> è morto alla profondità data:
        /// l'esperienza, già ridotta se il cavaliere è più forte della zona. Da morto niente.
        /// </summary>
        public int AwardKill(int baseExperience, int depth)
        {
            if (_health.IsDead)
            {
                return 0;
            }

            int experience = _settings.ExperienceForKill(baseExperience, depth, Progress.Level);
            Progress.Add(experience);
            return experience;
        }

        // la vita massima è già cresciuta: CharacterProgress scrive la vita dei livelli prima di
        // annunciarli, e Health la rilegge dalla scheda
        private void HandleLeveledUp(int level)
        {
            _health.Heal(_health.Max);

            // più livelli in un colpo solo: un suono e una luce, non uno per livello
            if (_lastSoundFrame == Time.frameCount)
            {
                return;
            }

            _lastSoundFrame = Time.frameCount;
            if (_audio != null && _levelUpClip != null)
            {
                _audio.PlayOneShot(_levelUpClip, _volume);
            }

            if (_glow != null && isActiveAndEnabled)
            {
                if (_glowing != null)
                {
                    StopCoroutine(_glowing);
                }

                _glowing = StartCoroutine(Glow());
            }

            LevelUpShown?.Invoke(level);
        }

        // sale in fretta e si spegne piano; tempo reale, come le scritte: l'hit stop non la ferma
        private IEnumerator Glow()
        {
            _glow.enabled = true;
            for (float time = 0f; time < _glowDuration; time += Time.unscaledDeltaTime)
            {
                float t = time / _glowDuration;
                _glow.intensity = _glowIntensity * (t < 0.15f ? t / 0.15f : 1f - (t - 0.15f) / 0.85f);
                yield return null;
            }

            _glow.enabled = false;
            _glowing = null;
        }
    }
}
