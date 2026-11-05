using System;
using System.Collections.Generic;
using DarkDescent.Classes;
using DarkDescent.Localization;
using DarkDescent.Player;
using DarkDescent.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il pannello dei comandi (D14 della M23): <b>F1</b> lo apre e lo chiude, <b>Esc</b> lo chiude, e il menu di pausa ha il
    /// pulsante <i>Comandi</i>. Quattro gruppi in due colonne, dalla tabella <see cref="HelpTable"/>: i tasti si leggono dai
    /// binding quando il pannello si apre, i testi dalla tabella delle stringhe nella lingua del momento, e la riga del
    /// tasto sinistro dice «dardo arcano» al Mago. Non ferma il gioco da solo: lo ferma la pausa, se c'è.
    /// </summary>
    [DisallowMultipleComponent]
    public class HelpPanel : MonoBehaviour
    {
        [Tooltip("Il riquadro da mostrare; il componente sta sul padre, sempre acceso.")]
        [SerializeField] private GameObject _root;

        [SerializeField] private TMP_Text _title;

        [Tooltip("Le due colonne: sinistra Movimento e Vedere, destra Combattimento e Finestre.")]
        [SerializeField] private RectTransform _left;

        [SerializeField] private RectTransform _right;

        [SerializeField] private Button _close;

        [SerializeField] private TMP_Text _closeLabel;

        [SerializeField] private TMP_FontAsset _font;

        [SerializeField] private Color _gold = new Color(0.9f, 0.77f, 0.45f, 1f);

        [SerializeField] private Color _ink = new Color(0.91f, 0.89f, 0.83f, 1f);

        [Tooltip("Larghezza della colonna dei tasti.")]
        [SerializeField, Min(40f)] private float _keyWidth = 180f;

        private sealed class RowView
        {
            public HelpRow Row;
            public TMP_Text Key;
            public TMP_Text Text;
        }

        private sealed class GroupView
        {
            public HelpGroup Group;
            public TMP_Text Title;
        }

        private readonly List<RowView> _rows = new List<RowView>();
        private readonly List<GroupView> _groups = new List<GroupView>();
        private Localizer _localizer;
        private PlayerInputReader _reader;
        private PlayerClass _class;
        private bool _built;
        private bool _subscribed;

        /// <summary>Aperto o chiuso, per chi deve sapere se c'è un pannello davanti.</summary>
        public event Action<bool> OpenChanged;

        /// <summary>Chiuso: chi lo aveva aperto dal menu di pausa torna a mostrarsi.</summary>
        public event Action Closed;

        public bool IsOpen => _root.activeSelf;

        public Button CloseButton => _close;

        public int RowCount => _rows.Count;

        public string TitleText => _title.text;

        public string CloseText => _closeLabel != null ? _closeLabel.text : string.Empty;

        /// <summary>Il tasto scritto sulla riga, per i test: l'indice è quello della riga nella tabella, colonna sinistra poi destra.</summary>
        public string KeyAt(int index) => _rows[index].Key.text;

        public string TextAt(int index) => _rows[index].Text.text;

        public string GroupTitleAt(int index) => _groups[index].Title.text;

        public int GroupCount => _groups.Count;

        public void Bind(Localizer localizer, PlayerInputReader reader, PlayerClass playerClass)
        {
            Unsubscribe();
            _localizer = localizer;
            _reader = reader;
            _class = playerClass;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void Open()
        {
            if (IsOpen || _localizer == null)
            {
                return;
            }

            Build();
            Refresh();
            _root.SetActive(true);
            OpenChanged?.Invoke(true);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _root.SetActive(false);
            OpenChanged?.Invoke(false);
            Closed?.Invoke();
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private void Awake()
        {
            _root.SetActive(false);
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
            if (_subscribed || _localizer == null)
            {
                return;
            }

            _close.onClick.AddListener(Close);
            _localizer.LanguageChanged += HandleLanguage;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _close.onClick.RemoveListener(Close);
            _localizer.LanguageChanged -= HandleLanguage;
            _subscribed = false;
        }

        private void HandleLanguage()
        {
            if (IsOpen)
            {
                Refresh();
            }
        }

        // le righe si costruiscono una volta, alla prima apertura; poi si riscrivono solo i testi
        private void Build()
        {
            if (_built)
            {
                return;
            }

            _built = true;
            BuildColumn(_left, HelpTable.Left);
            BuildColumn(_right, HelpTable.Right);
        }

        private void BuildColumn(RectTransform column, IReadOnlyList<HelpGroup> groups)
        {
            foreach (var group in groups)
            {
                var title = NewText("Group", column, 24f, _gold, FontStyles.Normal);
                title.textWrappingMode = TextWrappingModes.NoWrap;
                title.characterSpacing = 2f;
                title.margin = new Vector4(0f, 14f, 0f, 4f);
                _groups.Add(new GroupView { Group = group, Title = title });

                foreach (var row in group.Rows)
                {
                    var line = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    line.transform.SetParent(column, false);
                    var layout = line.GetComponent<HorizontalLayoutGroup>();
                    layout.spacing = 14f;
                    layout.childControlWidth = true;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = false;
                    layout.childAlignment = TextAnchor.UpperLeft;

                    var key = NewText("Key", line.transform, 21f, _gold, FontStyles.Normal);
                    key.textWrappingMode = TextWrappingModes.NoWrap;
                    var keyLayout = key.gameObject.AddComponent<LayoutElement>();
                    keyLayout.minWidth = _keyWidth;
                    keyLayout.preferredWidth = _keyWidth;
                    keyLayout.flexibleWidth = 0f;

                    var text = NewText("Text", line.transform, 20f, _ink, FontStyles.Normal);
                    text.textWrappingMode = TextWrappingModes.Normal;
                    text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

                    _rows.Add(new RowView { Row = row, Key = key, Text = text });
                }
            }
        }

        private TMP_Text NewText(string name, Transform parent, float size, Color color, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            if (_font != null)
            {
                text.font = _font;
            }

            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
            return text;
        }

        private void Refresh()
        {
            bool mage = _class != null && _class.Current != null && _class.Current.Id == "mage";
            _title.text = _localizer.Get(HelpTable.TitleKey);
            if (_closeLabel != null)
            {
                _closeLabel.text = _localizer.Format(HelpTable.CloseKey, KeyOf(HelpKey.Help));
            }

            foreach (var group in _groups)
            {
                group.Title.text = _localizer.Get(group.Group.TitleKey);
            }

            foreach (var view in _rows)
            {
                view.Key.text = KeyOf(view.Row.Key);
                view.Text.text = _localizer.Get(view.Row.TextFor(mage));
            }
        }

        // il tasto come lo dice il binding: se un giorno cambiano, cambia anche il pannello
        private string KeyOf(HelpKey key)
        {
            switch (key)
            {
                case HelpKey.LeftClick: return _localizer.Get(HelpTable.LeftClickKey);
                case HelpKey.RightClick: return _localizer.Get(HelpTable.RightClickKey);
                case HelpKey.Wheel: return _localizer.Get(HelpTable.WheelKey);
                case HelpKey.Move: return _reader.MoveKeysText();
                case HelpKey.Rotate: return _reader.RotateKeysText();
                case HelpKey.Skills: return _reader.KeyText("UseSkill") + " " + _reader.KeyText("UseSkill", last: true);
                case HelpKey.Belt: return _localizer.Format(HelpTable.RangeKey, _reader.KeyText("UseBelt"), _reader.KeyText("UseBelt", last: true));
                case HelpKey.Portal: return _reader.KeyText("TownPortal");
                case HelpKey.Inventory: return _reader.KeyText("ToggleInventory");
                case HelpKey.Character: return _reader.KeyText("ToggleCharacter");
                case HelpKey.SkillMenu: return _reader.KeyText("ToggleSkills");
                case HelpKey.SkillTree: return _reader.KeyText("ToggleTalents");
                case HelpKey.Map: return _reader.KeyText("CycleMap");
                case HelpKey.Pause: return _reader.KeyText("Pause");
                case HelpKey.Labels: return _reader.KeyText("ShowLabels");
                case HelpKey.Help: return _reader.KeyText("ToggleHelp");
                default: return string.Empty;
            }
        }
    }
}
