using System;
using System.Collections;
using DarkDescent.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace DarkDescent.Audio
{
    /// <summary>
    /// Il suono del livello (passo 7.0 della M7): musica e fondo in loop, e i versi nel buio. Sta in
    /// Core e sopravvive ai cambi di livello. Il composition root gli passa il profilo del livello
    /// appena entrato: se è lo stesso di prima la musica continua, altrimenti sfuma nell'altra.
    /// </summary>
    [DisallowMultipleComponent]
    public class AmbiencePlayer : MonoBehaviour
    {
        [Tooltip("Per i livelli senza un profilo: quelli fatti a mano e la sandbox.")]
        [SerializeField] private AmbienceProfile _defaultProfile;

        [Tooltip("Musica e fondo: il gruppo che il menu delle opzioni (M10) abbasserà con la musica.")]
        [SerializeField] private AudioMixerGroup _musicGroup;

        [Tooltip("I versi nel buio: sono rumori del mondo, vanno con gli effetti.")]
        [SerializeField] private AudioMixerGroup _stingerGroup;

        [Tooltip("Secondi della dissolvenza tra due profili, e dell'entrata del primo.")]
        [SerializeField, Min(0.01f)] private float _fadeTime = 2.5f;

        [Tooltip("Quanti versi possono suonare insieme.")]
        [SerializeField, Min(1)] private int _stingerVoices = 2;

        [Tooltip("Sotto questa frequenza passa il verso: tolti gli acuti, suona lontano, dietro la roccia.")]
        [SerializeField, Range(500f, 22000f)] private float _stingerCutoff = 2200f;

        [Tooltip("Il verso si sente pieno fino a qui, poi cala con la distanza.")]
        [SerializeField, Min(0.1f)] private float _stingerMinDistance = 5f;

        [SerializeField, Min(1f)] private float _stingerMaxDistance = 40f;

        // due coppie musica + fondo: una suona, l'altra entra alla prossima dissolvenza
        private readonly AudioSource[] _music = new AudioSource[2];
        private readonly AudioSource[] _beds = new AudioSource[2];
        private AudioSource[] _stingers;
        private int _active;
        private int _nextVoice;

        private Transform _around;
        private StingerSchedule _schedule;
        private float _nextStinger = float.PositiveInfinity;
        private Coroutine _fade;

        /// <summary>Un verso è partito, dalla sorgente data: per i test.</summary>
        public event Action<AudioSource> StingerPlayed;

        public AmbienceProfile Current { get; private set; }

        /// <summary>La sorgente della musica che si sente, o che sta entrando.</summary>
        public AudioSource Music => _music[_active];

        public AudioSource Bed => _beds[_active];

        private void Awake()
        {
            for (int i = 0; i < 2; i++)
            {
                _music[i] = CreateLoop($"Music {i}");
                _beds[i] = CreateLoop($"Bed {i}");
            }

            _stingers = new AudioSource[_stingerVoices];
            for (int i = 0; i < _stingers.Length; i++)
            {
                var voice = new GameObject($"Stinger {i}");
                voice.transform.SetParent(transform, false);
                var source = voice.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.outputAudioMixerGroup = _stingerGroup;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = _stingerMinDistance;
                source.maxDistance = _stingerMaxDistance;
                source.dopplerLevel = 0f;
                voice.AddComponent<AudioLowPassFilter>().cutoffFrequency = _stingerCutoff;
                _stingers[i] = source;
            }
        }

        /// <summary>Attorno a chi nascono i versi, e da dove vengono i numeri per sceglierli.</summary>
        public void Bind(Transform around, IRandomSource random)
        {
            _around = around;
            _schedule = new StingerSchedule(random);
        }

        /// <summary>Il suono di un livello appena entrato; null vuol dire quello predefinito.</summary>
        public void Play(AmbienceProfile profile)
        {
            if (profile == null)
            {
                profile = _defaultProfile;
            }

            if (profile == Current)
            {
                return;
            }

            Current = profile;
            if (_fade != null)
            {
                StopCoroutine(_fade);
            }

            int previous = _active;
            _active = 1 - _active;
            StartLoop(_music[_active], profile != null ? profile.Music : null);
            StartLoop(_beds[_active], profile != null ? profile.Bed : null);
            _fade = StartCoroutine(Crossfade(previous, profile));
            ScheduleStinger();
        }

        private void Update()
        {
            if (Time.time < _nextStinger)
            {
                return;
            }

            PlayStinger();
            ScheduleStinger();
        }

        private void ScheduleStinger()
        {
            bool canPlay = Current != null && Current.StingerCount > 0 && _schedule != null && _around != null;
            _nextStinger = canPlay ? Time.time + _schedule.NextDelay(Current.StingerInterval) : float.PositiveInfinity;
        }

        private void PlayStinger()
        {
            // a turno: con tutte le voci occupate si interrompe la più vecchia
            var source = _stingers[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _stingers.Length;

            source.transform.position = _around.position + _schedule.NextOffset(Current.StingerDistance);
            source.clip = Current.GetStinger(_schedule.NextIndex(Current.StingerCount));
            source.pitch = _schedule.NextPitch(Current.StingerPitch);
            source.volume = Current.StingerVolume;
            source.Play();
            StingerPlayed?.Invoke(source);
        }

        // Il tempo senza scala: l'hit stop non ferma la dissolvenza.
        private IEnumerator Crossfade(int previous, AmbienceProfile profile)
        {
            AudioSource oldMusic = _music[previous], oldBed = _beds[previous];
            AudioSource newMusic = _music[_active], newBed = _beds[_active];
            float oldMusicVolume = oldMusic.volume, oldBedVolume = oldBed.volume;
            float musicVolume = profile != null ? profile.MusicVolume : 0f;
            float bedVolume = profile != null ? profile.BedVolume : 0f;

            for (float time = 0f; time < _fadeTime; time += Time.unscaledDeltaTime)
            {
                float t = time / _fadeTime;
                oldMusic.volume = oldMusicVolume * (1f - t);
                oldBed.volume = oldBedVolume * (1f - t);
                newMusic.volume = musicVolume * t;
                newBed.volume = bedVolume * t;
                yield return null;
            }

            oldMusic.Stop();
            oldBed.Stop();
            newMusic.volume = musicVolume;
            newBed.volume = bedVolume;
            _fade = null;
        }

        private AudioSource CreateLoop(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.priority = 0;
            source.volume = 0f;
            source.outputAudioMixerGroup = _musicGroup;
            return source;
        }

        private static void StartLoop(AudioSource source, AudioClip clip)
        {
            source.Stop();
            source.clip = clip;
            source.volume = 0f;
            if (clip != null)
            {
                source.Play();
            }
        }
    }
}
