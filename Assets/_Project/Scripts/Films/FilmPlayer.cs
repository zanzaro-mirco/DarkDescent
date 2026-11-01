using System;
using System.Collections.Generic;
using DarkDescent.Localization;
using DarkDescent.Player;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace DarkDescent.Films
{
    /// <summary>
    /// Il lettore dei filmati (D9 della M19): fa partire la Timeline di un filmato e ci mette sopra le
    /// bande nere a 2,35:1, la didascalia dell'inquadratura, il titolo e l'invito a saltare. Mentre il filmato
    /// gira il gioco non legge comandi e le finestre del gioco sono spente; Esc o un click mostrano
    /// l'invito, il secondo salta. Alla fine, o al salto, tutto torna com'era e il tempo a 1. Le bande
    /// e le scritte le costruisce lui, in un canvas suo sopra tutti gli altri. Suona anche la musica del
    /// filmato (D10), nel gruppo della musica delle opzioni: alla fine sfuma, o finisce da sola se il
    /// filmato lo chiede; saltando sfuma in fretta.
    /// </summary>
    [DisallowMultipleComponent]
    public class FilmPlayer : MonoBehaviour
    {
        /// <summary>La chiave dell'invito a saltare.</summary>
        public const string SkipKey = "film.skip";

        [Tooltip("Il carattere delle didascalie e dell'invito: il Cinzel dell'interfaccia.")]
        [SerializeField] private TMP_FontAsset _font;

        [Tooltip("Il formato dell'immagine tra le bande: 2,35 come al cinema.")]
        [SerializeField, Min(1f)] private float _aspect = 2.35f;

        [Tooltip("Secondi in cui una didascalia appare o sparisce.")]
        [SerializeField, Min(0.05f)] private float _captionFade = 0.6f;

        [Tooltip("Secondi in cui l'invito a saltare resta dopo il primo Esc o click.")]
        [SerializeField, Min(0.5f)] private float _promptSeconds = 3f;

        [Tooltip("L'ordine del canvas dei filmati: sopra l'HUD, i menu e la dissolvenza.")]
        [SerializeField] private int _sortingOrder = 900;

        [Header("Musica")]
        [Tooltip("Il gruppo della musica del mixer: il volume delle opzioni vale anche per i filmati.")]
        [SerializeField] private AudioMixerGroup _musicGroup;

        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.8f;

        [Tooltip("Secondi in cui entra un brano preso a metà, dopo un filmato saltato.")]
        [SerializeField, Min(0.05f)] private float _musicFadeIn = 1f;

        [Tooltip("Secondi in cui il brano sfuma alla fine del filmato, se non finisce da solo.")]
        [SerializeField, Min(0.05f)] private float _musicFadeOut = 2.5f;

        [Tooltip("Secondi in cui il brano sfuma quando il filmato si salta.")]
        [SerializeField, Min(0.05f)] private float _skipFadeOut = 0.6f;

        private readonly List<Canvas> _hidden = new List<Canvas>();
        private PlayerInputReader _input;
        private Localizer _localizer;
        private Canvas _canvas;
        private RectTransform _top;
        private RectTransform _bottom;
        private Fade _caption;
        private Fade _title;
        private TMP_Text _prompt;
        private PlayableDirector _director;
        private FilmScript _script;
        private SkipPrompt _skip;
        private bool _started;
        private AudioSource _music;
        private bool _runsOut;
        private float _musicTarget;
        private float _musicRate;

        /// <summary>Il filmato è finito o è stato saltato: il gioco riprende.</summary>
        public event Action<FilmScript> Finished;

        public bool IsPlaying => _script != null;

        /// <summary>Il filmato che gira, o null.</summary>
        public FilmScript Current => _script;

        /// <summary>La didascalia a schermo, anche mentre appare o sparisce; vuota se non ce n'è.</summary>
        public string CaptionText => _caption != null ? _caption.Text : string.Empty;

        /// <summary>Il titolo in mezzo allo schermo, anche mentre appare o sparisce; vuoto se non ce n'è.</summary>
        public string TitleText => _title != null ? _title.Text : string.Empty;

        public bool IsPromptShowing => _prompt != null && _prompt.gameObject.activeSelf;

        /// <summary>L'altezza di una banda nera, in frazione dello schermo.</summary>
        public float BandHeight => _top != null ? _top.anchorMax.y - _top.anchorMin.y : 0f;

        /// <summary>La musica di un filmato suona ancora, anche finito il filmato, mentre sfuma o va in fondo.</summary>
        public bool IsMusicPlaying => _music != null && _music.isPlaying;

        /// <summary>La sorgente della musica dei filmati: per i test.</summary>
        public AudioSource Music => _music;

        public void Bind(PlayerInputReader input, Localizer localizer)
        {
            _input = input;
            _localizer = localizer;
        }

        /// <summary>
        /// Fa partire il filmato <paramref name="script"/> sulla Timeline di <paramref name="director"/>.
        /// Falso, e non succede niente, se un altro filmato sta già girando.
        /// </summary>
        /// <param name="music">La musica del filmato; senza, il filmato va in silenzio e quella che suona sfuma.</param>
        public bool Play(FilmScript script, PlayableDirector director, FilmMusic music = default)
        {
            if (IsPlaying || script == null || director == null)
            {
                return false;
            }

            EnsureOverlay();
            _script = script;
            _director = director;
            _skip = new SkipPrompt(_promptSeconds);
            _caption.Reset();
            _title.Reset();
            _started = false;

            HideOtherCanvases();
            if (_input != null)
            {
                _input.enabled = false;
            }

            Time.timeScale = 1f;
            _canvas.gameObject.SetActive(true);
            _prompt.gameObject.SetActive(false);
            Letterbox();
            _director.extrapolationMode = DirectorWrapMode.None;
            _director.time = 0d;
            _director.Play();
            StartMusic(music);
            return true;
        }

        /// <summary>Come Esc o un click: la prima volta l'invito, la seconda il salto.</summary>
        public void Press()
        {
            if (!IsPlaying)
            {
                return;
            }

            _skip.Press(Time.unscaledTime);
            if (_skip.Skipped)
            {
                Stop(skipped: true);
                return;
            }

            _prompt.text = _localizer != null ? _localizer.Get(SkipKey) : SkipKey;
            _prompt.gameObject.SetActive(true);
        }

        private void Update()
        {
            UpdateMusic();
            if (!IsPlaying)
            {
                return;
            }

            // i comandi del gioco sono spenti: Esc e il click si leggono qui, dai dispositivi
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame))
            {
                Press();
                if (!IsPlaying)
                {
                    return;
                }
            }

            if (_director == null)
            {
                Stop(skipped: false);
                return;
            }

            // la Timeline parte al frame dopo Play: finisce quando non gira più, o è arrivata in fondo
            float time = (float)_director.time;
            bool playing = _director.state == PlayState.Playing;
            _started |= playing;
            if ((_started && !playing) || time >= _script.Duration)
            {
                Stop(skipped: false);
                return;
            }

            _prompt.gameObject.SetActive(_skip.IsShowing(Time.unscaledTime));
            float step = Time.unscaledDeltaTime / _captionFade;
            _caption.Show(_script.CaptionAt(time), step, _localizer);
            _title.Show(_script.TitleAt(time), step, _localizer);
        }

        private void Stop(bool skipped)
        {
            var finished = _script;
            if (_director != null)
            {
                _director.Stop();
            }

            if (skipped)
            {
                FadeMusic(_skipFadeOut);
            }
            else if (!_runsOut)
            {
                FadeMusic(_musicFadeOut);
            }

            _script = null;
            _director = null;
            _canvas.gameObject.SetActive(false);
            foreach (var canvas in _hidden)
            {
                if (canvas != null)
                {
                    canvas.enabled = true;
                }
            }

            _hidden.Clear();
            if (_input != null)
            {
                _input.enabled = true;
            }

            Time.timeScale = 1f;
            Finished?.Invoke(finished);
        }

        private void OnDisable()
        {
            if (IsPlaying)
            {
                Stop(skipped: true);
            }
        }

        // lo stesso brano che suona già continua; un altro parte dal suo secondo, con un'entrata se è a metà;
        // senza brano, quello che suona sfuma
        private void StartMusic(FilmMusic music)
        {
            if (music.Clip == null)
            {
                FadeMusic(_skipFadeOut);
                return;
            }

            EnsureMusic();
            _runsOut = music.RunsOut;
            _musicTarget = _musicVolume;
            if (_music.isPlaying && _music.clip == music.Clip)
            {
                _musicRate = _musicVolume / _musicFadeIn;
                return;
            }

            _music.Stop();
            _music.clip = music.Clip;
            _music.time = Mathf.Clamp(music.From, 0f, Mathf.Max(0f, music.Clip.length - 0.1f));
            _music.volume = music.From > 0f ? 0f : _musicVolume;
            _musicRate = _musicVolume / _musicFadeIn;
            _music.Play();
        }

        private void FadeMusic(float seconds)
        {
            if (_music == null)
            {
                return;
            }

            _musicTarget = 0f;
            _musicRate = _musicVolume / seconds;

            // un brano chiesto che non suona ancora partirebbe dopo, a filmato finito: senza la clip non
            // parte più
            if (!_music.isPlaying)
            {
                _music.Stop();
                _music.clip = null;
            }
        }

        // in tempo vero: la finestra della vittoria ferma il gioco, non la musica
        private void UpdateMusic()
        {
            if (!IsMusicPlaying)
            {
                return;
            }

            _music.volume = Mathf.MoveTowards(_music.volume, _musicTarget, _musicRate * Time.unscaledDeltaTime);
            if (_musicTarget <= 0f && _music.volume <= 0f)
            {
                _music.Stop();
            }
        }

        private void EnsureMusic()
        {
            if (_music != null)
            {
                return;
            }

            var go = new GameObject("FilmMusic");
            go.transform.SetParent(transform, false);
            _music = go.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = false;
            _music.spatialBlend = 0f;
            _music.priority = 0;
            _music.outputAudioMixerGroup = _musicGroup;
        }

        // le finestre del gioco si spengono per il filmato: si riaccendono solo quelle spente qui
        private void HideOtherCanvases()
        {
            _hidden.Clear();
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas != _canvas && canvas.isRootCanvas && canvas.enabled && canvas.renderMode != RenderMode.WorldSpace)
                {
                    canvas.enabled = false;
                    _hidden.Add(canvas);
                }
            }
        }

        // le bande prendono quello che avanza sopra e sotto un'immagine larga _aspect volte l'altezza
        private void Letterbox()
        {
            float screen = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            float band = Mathf.Max(0f, (1f - screen / _aspect) * 0.5f);
            _top.anchorMin = new Vector2(0f, 1f - band);
            _top.anchorMax = Vector2.one;
            _bottom.anchorMin = Vector2.zero;
            _bottom.anchorMax = new Vector2(1f, band);
        }

        private void EnsureOverlay()
        {
            if (_canvas != null)
            {
                return;
            }

            var root = new GameObject("FilmOverlay", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = _sortingOrder;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            _top = Band(root.transform, "BandTop");
            _bottom = Band(root.transform, "BandBottom");
            _caption = new Fade(Label(_bottom, "Caption", 34f, new Color(0.92f, 0.86f, 0.72f)));
            _prompt = Label(_top, "SkipPrompt", 24f, new Color(0.7f, 0.66f, 0.58f));
            var title = Label((RectTransform)root.transform, "Title", 104f, new Color(0.96f, 0.86f, 0.62f));
            // il titolo sta sopra il mondo, non sulle bande: un bordo scuro lo stacca dai sassi
            title.outlineWidth = 0.18f;
            title.outlineColor = new Color32(20, 14, 10, 255);
            _title = new Fade(title);
            root.SetActive(false);
        }

        private static RectTransform Band(Transform parent, string name)
        {
            var band = new GameObject(name, typeof(RectTransform), typeof(Image));
            band.transform.SetParent(parent, false);
            band.GetComponent<Image>().color = Color.black;
            var rect = (RectTransform)band.transform;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private TMP_Text Label(RectTransform parent, string name, float size, Color color)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(parent, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(120f, 0f);
            rect.offsetMax = new Vector2(-120f, 0f);
            var text = label.GetComponent<TextMeshProUGUI>();
            if (_font != null)
            {
                text.font = _font;
            }

            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        // una scritta che appare e sparisce: la nuova appare quando la vecchia è sparita, con un
        // attimo di vuoto in mezzo
        private sealed class Fade
        {
            private readonly TMP_Text _text;
            private string _key;
            private float _alpha;

            public Fade(TMP_Text text)
            {
                _text = text;
                Reset();
            }

            public string Text => _text.gameObject.activeSelf ? _text.text : string.Empty;

            public void Reset()
            {
                _key = null;
                _alpha = 0f;
                _text.gameObject.SetActive(false);
            }

            public void Show(string key, float step, Localizer localizer)
            {
                if (key != _key)
                {
                    _alpha = Mathf.Max(0f, _alpha - step);
                    if (_alpha <= 0f)
                    {
                        _key = key;
                        _text.text = key != null && localizer != null ? localizer.Get(key) : key ?? string.Empty;
                    }
                }
                else if (key != null)
                {
                    _alpha = Mathf.Min(1f, _alpha + step);
                }

                _text.alpha = _alpha;
                _text.gameObject.SetActive(_key != null || _alpha > 0f);
            }
        }
    }
}
