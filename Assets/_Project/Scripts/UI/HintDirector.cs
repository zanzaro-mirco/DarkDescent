using System;
using System.Collections.Generic;
using DarkDescent.Localization;
using DarkDescent.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DarkDescent.UI
{
    /// <summary>
    /// I suggerimenti del primo gioco (D11–D13 della M23). Ogni suggerimento ha una condizione («c'è un nemico a
    /// portata di vista», «c'è un punto da spendere»), controllata quattro volte al secondo finché non è stato
    /// mostrato; quando scatta va in coda nel <see cref="HintBook"/>, e il riquadro li mostra uno alla volta, 8 secondi
    /// o finché si preme un tasto. Non ferma il gioco, non copre il click, e tace in pausa, nei filmati, da morti e
    /// nella sala del re (il chiamante lo dice con <c>gate</c>). Quali sono già stati visti sta nelle preferenze del
    /// computer, non nel salvataggio: sono del giocatore, non del cavaliere.
    /// </summary>
    [DisallowMultipleComponent]
    public class HintDirector : MonoBehaviour
    {
        public const string OnPreference = "hints.on";
        public const string SeenPreference = "hints.seen";

        [SerializeField] private HintBox _box;

        [Tooltip("Quanto resta il riquadro, in secondi.")]
        [SerializeField, Min(1f)] private float _duration = 8f;

        [Tooltip("Ogni quanto si controllano le condizioni, in secondi.")]
        [SerializeField, Min(0.05f)] private float _interval = 0.25f;

        [Tooltip("Per quanto un tasto non chiude il riquadro appena comparso, in secondi.")]
        [SerializeField, Min(0f)] private float _grace = 0.5f;

        private readonly HintBook _book = new HintBook();
        private readonly List<KeyValuePair<string, Func<bool>>> _senses = new List<KeyValuePair<string, Func<bool>>>();
        private Localizer _localizer;
        private PlayerInputReader _reader;
        private Func<bool> _gate;
        private string _current;
        private float _left;
        private float _shownFor;
        private float _nextCheck;
        private bool _loaded;

        /// <summary>Il suggerimento che si sta mostrando (o che si mostrerà alla ripresa), null se nessuno.</summary>
        public string Current => _current;

        public bool IsOn => _book.Enabled;

        /// <summary>Quanto resta il riquadro, in secondi (i test lo accorciano).</summary>
        public float Duration
        {
            get => _duration;
            set => _duration = Mathf.Max(0.1f, value);
        }

        public HintBook Book
        {
            get
            {
                Load();
                return _book;
            }
        }

        public HintBox Box => _box;

        /// <param name="gate">Falso quando il gioco non è da guardare: pausa, filmato, menu, morte, sala del re.</param>
        public void Bind(Localizer localizer, PlayerInputReader reader, Func<bool> gate)
        {
            if (_localizer != null)
            {
                _localizer.LanguageChanged -= RefreshText;
            }

            _localizer = localizer;
            _reader = reader;
            _gate = gate;
            if (_localizer != null)
            {
                _localizer.LanguageChanged += RefreshText;
            }
        }

        /// <summary>La condizione di un suggerimento: vera, entra in coda (una volta sola, finché non lo si è visto).</summary>
        public void AddSense(string id, Func<bool> sense)
        {
            _senses.Add(new KeyValuePair<string, Func<bool>>(id, sense));
        }

        /// <summary>L'opzione «Suggerimenti»: spenti, il riquadro si chiude e niente entra in coda.</summary>
        public void SetOn(bool on)
        {
            Load();
            _book.Enabled = on;
            if (!on)
            {
                _current = null;
                _box.Hide();
            }
        }

        /// <summary>«Rivedi i suggerimenti»: tutti tornano nuovi, anche quello in corso.</summary>
        public void ResetSeen()
        {
            Load();
            _book.Reset();
            _current = null;
            _box.Hide();
            PlayerPrefs.DeleteKey(SeenPreference);
            PlayerPrefs.Save();
        }

        private void Awake()
        {
            Load();
        }

        private void OnDestroy()
        {
            if (_localizer != null)
            {
                _localizer.LanguageChanged -= RefreshText;
            }
        }

        // le preferenze si leggono una volta, al primo uso: l'opzione le riscrive da sé
        private void Load()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            _book.Enabled = PlayerPrefs.GetInt(OnPreference, 1) == 1;
            _book.Load(PlayerPrefs.GetString(SeenPreference, string.Empty));
        }

        private void Update()
        {
            Load();
            if (!_book.Enabled || _localizer == null)
            {
                return;
            }

            bool allowed = _gate == null || _gate();
            float now = Time.unscaledTime;
            if (allowed && now >= _nextCheck)
            {
                _nextCheck = now + _interval;
                Poll();
            }

            if (_current == null)
            {
                if (allowed && _book.TryNext(out string next))
                {
                    Show(next);
                }

                return;
            }

            // il gioco non è da guardare: il riquadro si nasconde e il tempo si ferma dov'è
            if (!allowed)
            {
                if (_box.IsShown)
                {
                    _box.Hide();
                }

                return;
            }

            if (!_box.IsShown)
            {
                ShowText(_current);
            }

            float dt = Time.unscaledDeltaTime;
            _left -= dt;
            _shownFor += dt;
            _box.SetProgress(_left / _duration);

            var keyboard = Keyboard.current;
            bool pressed = keyboard != null && keyboard.anyKey.wasPressedThisFrame && _shownFor >= _grace;
            if (_left <= 0f || pressed)
            {
                _current = null;
                _box.Hide();
            }
        }

        private void Poll()
        {
            for (int i = 0; i < _senses.Count; i++)
            {
                string id = _senses[i].Key;
                if (!_book.HasSeen(id) && !_book.IsQueued(id) && id != _current && _senses[i].Value())
                {
                    _book.Raise(id);
                }
            }
        }

        private void Show(string id)
        {
            _current = id;
            _left = _duration;
            _shownFor = 0f;
            PlayerPrefs.SetString(SeenPreference, _book.Serialize());
            PlayerPrefs.Save();
            ShowText(id);
            _box.SetProgress(1f);
        }

        private void ShowText(string id)
        {
            _box.Show(_localizer.Get(Hints.TitleKey(id)), TextOf(id));
        }

        private void RefreshText()
        {
            if (_current != null && _box.IsShown)
            {
                ShowText(_current);
            }
        }

        // il testo con i tasti veri, letti dai binding: se un giorno cambiano, cambia anche il suggerimento
        private string TextOf(string id)
        {
            string a = string.Empty;
            string b = string.Empty;
            string c = string.Empty;
            if (_reader != null)
            {
                switch (id)
                {
                    case Hints.Move:
                        a = _reader.MoveKeysText();
                        break;
                    case Hints.Item:
                        a = _reader.KeyText("ShowLabels");
                        break;
                    case Hints.Potion:
                        a = _reader.KeyText("UseBelt");
                        b = _reader.KeyText("UseBelt", last: true);
                        break;
                    case Hints.AttributePoint:
                        a = _reader.KeyText("ToggleCharacter");
                        break;
                    case Hints.TalentPoint:
                        a = _reader.KeyText("ToggleTalents");
                        break;
                    case Hints.Portal:
                        a = _reader.KeyText("TownPortal");
                        break;
                    case Hints.Skills:
                        a = _reader.KeyText("UseSkill");
                        b = _reader.KeyText("UseSkill", last: true);
                        c = _reader.KeyText("ToggleSkills");
                        break;
                }
            }

            return _localizer.Format(Hints.TextKey(id), a, b, c);
        }
    }
}
