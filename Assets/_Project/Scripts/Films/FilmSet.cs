using System.Collections.Generic;
using DarkDescent.Levels;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace DarkDescent.Films
{
    /// <summary>
    /// Il set di un filmato (D2 e D3 della M19): mette le comparse e gli oggetti nel livello in cui si
    /// gioca, li muove con il tempo della Timeline e alla fine rimette tutto com'era. Tutto quello che si
    /// vede dipende solo dal secondo del filmato, che la Timeline gli passa con <see cref="Show"/>: lo si
    /// può fermare, riavvolgere e fotografare in qualsiasi istante. La camera è una sola, una camera di
    /// Cinemachine senza movimenti suoi, messa a mano a ogni fotogramma: tra un'inquadratura e l'altra lo
    /// stacco è netto; alla fine la camera del gioco la riprende con la sua sfumatura. Il set dice anche
    /// la musica del filmato e fa i suoni delle sue scene, quando il filmato ne passa il secondo.
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
    public abstract class FilmSet : MonoBehaviour
    {
        // come la camera del gioco: abbastanza lontana da non tagliare niente
        private const float CameraDistance = 20f;
        private const int CameraPriority = 100;
        // un passo più lungo è un salto del tempo, come nelle foto: i suoni non partono
        private const float MaxCueStep = 0.25f;
        private const int Voices = 6;

        [Header("Suono")]
        [Tooltip("La musica del filmato; vuota, il filmato va in silenzio. La musica del livello tace comunque.")]
        [SerializeField] private AudioClip _music;

        [Tooltip("Da che secondo del brano parte la musica, se non sta già suonando.")]
        [SerializeField, Min(0f)] private float _musicFrom;

        [Tooltip("Acceso, alla fine del filmato il brano va fino in fondo; spento, sfuma.")]
        [SerializeField] private bool _musicRunsOut;

        [Tooltip("Il gruppo degli effetti del mixer: i suoni del set seguono il volume delle opzioni.")]
        [SerializeField] private AudioMixerGroup _soundGroup;

        private readonly List<GameObject> _props = new List<GameObject>();
        private readonly List<Renderer> _heroRenderers = new List<Renderer>();
        private readonly List<Light> _heroLights = new List<Light>();
        private CinemachineCamera _camera;
        private CinemachineBrain _brain;
        private CinemachineBlendDefinition _blend;
        private readonly List<AudioSource> _voices = new List<AudioSource>();
        private int _nextVoice;
        private float _lastTime;
        private float _step;

        /// <summary>Le inquadrature del filmato.</summary>
        public abstract FilmScript Script { get; }

        public PlayableDirector Director => GetComponent<PlayableDirector>();

        /// <summary>La musica del filmato, per il lettore.</summary>
        public FilmMusic Music => new FilmMusic(_music, _musicFrom, _musicRunsOut);

        /// <summary>Un suono del set è partito: per i test.</summary>
        public event System.Action<AudioClip> Cued;

        /// <summary>Il set è montato nel livello e non ancora smontato.</summary>
        public bool IsBuilt { get; private set; }

        /// <summary>
        /// Monta il set nel livello <paramref name="level"/>, con il cavaliere <paramref name="hero"/>
        /// e la camera del gioco <paramref name="gameplay"/>, che il filmato riprende alla fine.
        /// </summary>
        public void Build(LevelContext level, GameObject hero, CinemachineCamera gameplay)
        {
            var go = new GameObject("FilmCamera");
            go.transform.SetParent(transform, false);
            _camera = go.AddComponent<CinemachineCamera>();
            _camera.Priority = CameraPriority;

            // dentro il filmato si entra con uno stacco, non con la sfumatura del gioco
            _brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
            if (_brain != null)
            {
                _blend = _brain.DefaultBlend;
                _brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            }

            _lastTime = 0f;
            _step = 0f;
            Stage(level, hero, gameplay);
            IsBuilt = true;
        }

        /// <summary>Il set al secondo <paramref name="time"/> del filmato.</summary>
        public void Show(float time)
        {
            if (IsBuilt)
            {
                _step = time - _lastTime;
                _lastTime = time;
                Pose(time);
            }
        }

        /// <summary>Smonta il set: il livello torna com'era e la camera del gioco riprende, sfumando.</summary>
        public void Teardown()
        {
            if (!IsBuilt)
            {
                return;
            }

            IsBuilt = false;
            Strike();
            foreach (var prop in _props)
            {
                if (prop != null)
                {
                    Destroy(prop);
                }
            }

            _props.Clear();
            ShowHero();
            if (_brain != null)
            {
                _brain.DefaultBlend = _blend;
            }

            if (_camera != null)
            {
                Destroy(_camera.gameObject);
            }
        }

        private void OnDestroy()
        {
            Teardown();
        }

        protected abstract void Stage(LevelContext level, GameObject hero, CinemachineCamera gameplay);

        protected abstract void Pose(float time);

        protected abstract void Strike();

        /// <summary>
        /// La camera sopra <paramref name="target"/>, girata di <paramref name="yaw"/> gradi da nord e
        /// inclinata di <paramref name="pitch"/>, con la vista alta <paramref name="size"/> metri a metà.
        /// </summary>
        protected void Frame(Vector3 target, float yaw, float pitch, float size)
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            _camera.transform.SetPositionAndRotation(target - rotation * Vector3.forward * CameraDistance, rotation);
            _camera.Lens.OrthographicSize = size;
        }

        /// <summary>
        /// Il suono <paramref name="clip"/> al secondo <paramref name="at"/> di una scena, che adesso è al
        /// secondo <paramref name="t"/>: parte solo nel fotogramma che passa quel secondo, andando avanti. I
        /// suoni sono in due dimensioni: la camera dei filmati è lontana dal cavaliere che ascolta.
        /// </summary>
        protected void Cue(AudioClip clip, float at, float t, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || _step <= 0f || _step > MaxCueStep || at > t || at <= t - _step)
            {
                return;
            }

            // a turno: con tutte le voci occupate si interrompe la più vecchia
            if (_voices.Count < Voices)
            {
                var go = new GameObject($"Voice {_voices.Count}");
                go.transform.SetParent(transform, false);
                var made = go.AddComponent<AudioSource>();
                made.playOnAwake = false;
                made.spatialBlend = 0f;
                made.outputAudioMixerGroup = _soundGroup;
                _voices.Add(made);
            }

            var source = _voices[_nextVoice % _voices.Count];
            _nextVoice++;
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
            Cued?.Invoke(clip);
        }

        /// <summary>Un oggetto del set: si distrugge quando il set si smonta.</summary>
        protected GameObject Prop(GameObject prop)
        {
            _props.Add(prop);
            return prop;
        }

        /// <summary>Nasconde il cavaliere vero, che nel filmato è una copia: torna visibile allo smontaggio.</summary>
        protected void HideHero(GameObject hero)
        {
            foreach (var part in hero.GetComponentsInChildren<Renderer>())
            {
                if (part.enabled)
                {
                    part.enabled = false;
                    _heroRenderers.Add(part);
                }
            }

            foreach (var light in hero.GetComponentsInChildren<Light>())
            {
                if (light.enabled)
                {
                    light.enabled = false;
                    _heroLights.Add(light);
                }
            }
        }

        private void ShowHero()
        {
            foreach (var part in _heroRenderers)
            {
                if (part != null)
                {
                    part.enabled = true;
                }
            }

            foreach (var light in _heroLights)
            {
                if (light != null)
                {
                    light.enabled = true;
                }
            }

            _heroRenderers.Clear();
            _heroLights.Clear();
        }

        /// <summary>Il centro della cella del primo segno <paramref name="symbol"/> della mappa; zero se non c'è.</summary>
        protected static Vector3 Marker(LevelMap map, char symbol)
        {
            foreach (var marker in map.Markers)
            {
                if (marker.Symbol == symbol)
                {
                    return LevelMap.CellCenter(marker.X, marker.Y);
                }
            }

            return Vector3.zero;
        }

        /// <summary>Da 0 a 1 senza strappi, all'inizio e alla fine.</summary>
        protected static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
