using System;
using System.Collections.Generic;
using DarkDescent.Localization;
using DarkDescent.Magic;
using DarkDescent.Player;
using TMPro;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il menu delle abilità (D18 della M20): per ogni casella, le abilità note che ci possono andare,
    /// e quella che c'è già accesa. Un click su una casella della barra lo apre solo su di lei, e una
    /// scelta lo chiude; K lo apre con tutte e quattro, e resta aperto finché non si ripreme K, Esc o
    /// non si clicca di nuovo. Nella casella del sinistro va solo l'Attacco, per ora.
    /// </summary>
    [DisallowMultipleComponent]
    public class SkillMenu : MonoBehaviour
    {
        /// <summary>Le chiavi dei titoli delle righe, nell'ordine delle caselle.</summary>
        public static readonly string[] RowKeys = { "skills.slot.primary", "skills.slot.secondary", "skills.slot.q", "skills.slot.e" };

        /// <summary>Tutte le righe: il menu aperto con K.</summary>
        public const int AllSlots = -1;

        [Tooltip("La finestra da aprire e chiudere; il componente sta sul padre, sempre attivo.")]
        [SerializeField] private GameObject _window;

        [Tooltip("Le quattro righe, nell'ordine delle caselle.")]
        [SerializeField] private GameObject[] _rows = new GameObject[SpellBook.Slots];

        [Tooltip("Il titolo di ogni riga.")]
        [SerializeField] private TMP_Text[] _rowTitles = new TMP_Text[SpellBook.Slots];

        [Tooltip("Il contenitore delle scelte di ogni riga.")]
        [SerializeField] private RectTransform[] _rowChoices = new RectTransform[SpellBook.Slots];

        [Tooltip("La scritta di una riga senza abilità da scegliere.")]
        [SerializeField] private TMP_Text[] _rowEmpty = new TMP_Text[SpellBook.Slots];

        [SerializeField] private TMP_Text _title;

        [Tooltip("La scelta modello, spenta: se ne fanno copie.")]
        [SerializeField] private SkillChoiceView _choiceTemplate;

        private readonly List<SkillChoiceView>[] _choices = new List<SkillChoiceView>[SpellBook.Slots];
        private PlayerMagic _magic;
        private PlayerInputReader _reader;
        private Localizer _localizer;
        private bool _subscribed;

        /// <summary>Il menu si è aperto, chiuso o è passato ad altre righe: la barra accende la sua casella.</summary>
        public event Action Changed;

        public bool IsOpen => _window.activeSelf;

        /// <summary>La casella su cui è aperto, o <see cref="AllSlots"/> se è aperto con K.</summary>
        public int OpenSlot { get; private set; } = AllSlots;

        /// <summary>Il nome di un'abilità nella lingua scelta; senza traduttore, la chiave.</summary>
        public static string NameOf(SpellDefinition spell, Localizer localizer)
        {
            if (spell == null)
            {
                return "";
            }

            return localizer != null ? localizer.Get(spell.NameKey) : spell.NameKey;
        }

        public void Bind(PlayerMagic magic, PlayerInputReader reader, Localizer localizer)
        {
            Unsubscribe();
            _magic = magic;
            _reader = reader;
            _localizer = localizer;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        /// <summary>Se la riga di una casella è a schermo.</summary>
        public bool ShowsSlot(int slot) => IsOpen && (OpenSlot == AllSlots || OpenSlot == slot);

        /// <summary>Le abilità tra cui scegliere in una riga, nell'ordine a schermo: per i test.</summary>
        public IReadOnlyList<SkillChoiceView> Choices(int slot) => _choices[slot] ??= new List<SkillChoiceView>();

        /// <summary>K: apre tutte le righe, o chiude il menu se era aperto così.</summary>
        public void Toggle()
        {
            if (IsOpen && OpenSlot == AllSlots)
            {
                Close();
            }
            else
            {
                Open(AllSlots);
            }
        }

        /// <summary>Il click su una casella della barra: apre la sua riga, o chiude se era già aperta su di lei.</summary>
        public void ToggleSlot(int slot)
        {
            if (IsOpen && OpenSlot == slot)
            {
                Close();
            }
            else
            {
                Open(slot);
            }
        }

        public void Open(int slot)
        {
            OpenSlot = slot;
            _window.SetActive(true);
            Refresh();
            Changed?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _window.SetActive(false);
            Changed?.Invoke();
        }

        /// <summary>
        /// Mette un'abilità in una casella: il click su una scelta. Aperto su una casella sola, il menu
        /// si chiude; aperto con K, resta e si ridisegna.
        /// </summary>
        public bool Choose(int slot, SpellDefinition spell)
        {
            if (_magic == null || !_magic.Assign(slot, spell))
            {
                return false;
            }

            if (OpenSlot == AllSlots)
            {
                Refresh();
            }
            else
            {
                Close();
            }

            return true;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _magic == null)
            {
                return;
            }

            if (_reader != null)
            {
                _reader.SkillsToggled += Toggle;
            }

            _magic.Book.Learned += HandleLearned;
            _magic.Book.Rearranged += Refresh;
            if (_localizer != null)
            {
                _localizer.LanguageChanged += Refresh;
            }

            _subscribed = true;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (_reader != null)
            {
                _reader.SkillsToggled -= Toggle;
            }

            _magic.Book.Learned -= HandleLearned;
            _magic.Book.Rearranged -= Refresh;
            if (_localizer != null)
            {
                _localizer.LanguageChanged -= Refresh;
            }

            _subscribed = false;
        }

        private void HandleLearned(int slot)
        {
            Refresh();
        }

        // si ridisegna solo da aperto: le copie delle scelte costano poco, ma non servono a niente da chiuso
        private void Refresh()
        {
            if (!IsOpen || _magic == null)
            {
                return;
            }

            if (_title != null && _localizer != null)
            {
                _title.text = _localizer.Get("skills.title");
            }

            for (int slot = 0; slot < SpellBook.Slots; slot++)
            {
                bool shown = OpenSlot == AllSlots || OpenSlot == slot;
                _rows[slot].SetActive(shown);
                if (!shown)
                {
                    continue;
                }

                if (_localizer != null)
                {
                    _rowTitles[slot].text = _localizer.Get(RowKeys[slot]);
                }

                var spells = _magic.Book.Choices(slot);
                var views = _choices[slot] ??= new List<SkillChoiceView>();
                while (views.Count < spells.Count)
                {
                    var copy = Instantiate(_choiceTemplate, _rowChoices[slot]);
                    copy.name = $"Choice_{views.Count + 1}";
                    views.Add(copy);
                }

                for (int i = 0; i < views.Count; i++)
                {
                    bool used = i < spells.Count;
                    views[i].gameObject.SetActive(used);
                    if (used)
                    {
                        int row = slot;
                        var spell = spells[i];
                        views[i].Show(spell, NameOf(spell, _localizer), _magic.Book[slot] == spell, () => Choose(row, spell));
                    }
                }

                if (_rowEmpty[slot] != null)
                {
                    _rowEmpty[slot].gameObject.SetActive(spells.Count == 0);
                    if (_localizer != null)
                    {
                        _rowEmpty[slot].text = _localizer.Get("skills.none");
                    }
                }
            }
        }
    }
}
