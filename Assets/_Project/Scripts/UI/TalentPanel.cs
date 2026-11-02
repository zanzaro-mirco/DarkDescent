using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DarkDescent.Localization;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Talents;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il pannello delle abilità (D10 della M20), sul tasto N: a sinistra l'albero della classe, a destra
    /// quello della specializzazione, con le linee, i nodi chiusi, aperti e presi, il tooltip e, sotto,
    /// *Applica modifiche* e *Annulla*. Disegna lo stato in attesa di <see cref="TalentBuild"/> e gli
    /// manda i click: le regole stanno tutte nel <c>Core</c>. Prima del livello 10 la specializzazione si
    /// vede ma è chiusa; dal 10 si sceglie con il pulsante sotto le due schede.
    /// </summary>
    [DisallowMultipleComponent]
    public class TalentPanel : MonoBehaviour
    {
        // la forma di un nodo: tondo il passivo, quadrato l'attivo, ottagonale la scelta
        private const int PassiveShape = 0;
        private const int ActiveShape = 1;
        private const int ChoiceShape = 2;

        private const string Gold = "#E6C473";
        private const string Dim = "#9A9488";
        private const string Warning = "#E08A6B";

        [Serializable]
        public sealed class TreeArea
        {
            public RectTransform Area;
            public RectTransform Lines;
            public RectTransform Nodes;
            public TMP_Text Name;
            public TMP_Text Points;
        }

        [Tooltip("La finestra da aprire e chiudere; il componente sta sul padre, sempre acceso.")]
        [SerializeField] private GameObject _window;

        [SerializeField] private TMP_Text _title;

        [SerializeField] private Button _close;

        [SerializeField] private TreeArea _classArea;

        [SerializeField] private TreeArea _specArea;

        [Tooltip("Il nodo modello, spento: se ne fanno copie.")]
        [SerializeField] private TalentNodeView _nodeTemplate;

        [Tooltip("Il segmento di una linea tra due nodi, spento.")]
        [SerializeField] private Image _lineTemplate;

        [Tooltip("La linea tratteggiata di una riga che chiede punti, spenta, con la scritta dentro.")]
        [SerializeField] private RectTransform _gateTemplate;

        [Tooltip("Le placche dei nodi: passivo, attivo, scelta.")]
        [SerializeField] private Sprite[] _plates = new Sprite[3];

        [Tooltip("Gli anelli dei nodi, nello stesso ordine.")]
        [SerializeField] private Sprite[] _rings = new Sprite[3];

        [Tooltip("Larghezza e altezza di una casella della griglia.")]
        [SerializeField] private Vector2 _cell = new Vector2(88f, 92f);

        [SerializeField, Min(1f)] private float _lineWidth = 5f;

        [Header("Specializzazione")]
        [SerializeField] private Button[] _specTabs = new Button[2];

        [SerializeField] private Button _chooseButton;

        [SerializeField] private TMP_Text _specNote;

        [Header("Sotto")]
        [SerializeField] private Button _apply;

        [SerializeField] private Button _cancel;

        [SerializeField] private TMP_Text _pendingNote;

        [SerializeField] private TMP_Text _hint;

        [Header("Tooltip e scelta")]
        [SerializeField] private RectTransform _tooltip;

        [SerializeField] private TMP_Text _tooltipText;

        [Tooltip("La finestrella con le due opzioni di un nodo a scelta.")]
        [SerializeField] private RectTransform _choicePopup;

        [SerializeField] private Button[] _choiceButtons = new Button[2];

        [Tooltip("Il velo trasparente sotto la finestrella: un click fuori la chiude.")]
        [SerializeField] private Button _choiceBlocker;

        [SerializeField] private Color _lineIdle = new Color(0.35f, 0.33f, 0.3f, 0.9f);
        [SerializeField] private Color _lineLit = new Color(0.9f, 0.77f, 0.45f, 1f);

        private readonly List<TalentNodeView> _classViews = new List<TalentNodeView>();
        private readonly List<TalentNodeView> _specViews = new List<TalentNodeView>();
        private readonly List<Image> _classLines = new List<Image>();
        private readonly List<Image> _specLines = new List<Image>();
        private readonly List<RectTransform> _classGates = new List<RectTransform>();
        private readonly List<RectTransform> _specGates = new List<RectTransform>();
        private readonly StringBuilder _text = new StringBuilder();
        private PlayerTalents _talents;
        private PlayerInputReader _reader;
        private Localizer _localizer;
        private TalentTree _previewed;
        private TalentNodeView _hovered;
        private TalentNodeView _choiceFor;
        private bool _subscribed;

        /// <summary>Il pannello si è aperto o chiuso.</summary>
        public event Action<bool> OpenChanged;

        public bool IsOpen => _window.activeSelf;

        public Button ApplyButton => _apply;

        public Button CancelButton => _cancel;

        public Button ChooseButton => _chooseButton;

        public Button CloseButton => _close;

        public bool IsTooltipShown => _tooltip.gameObject.activeSelf;

        public string TooltipText => _tooltipText.text;

        public bool IsChoiceOpen => _choicePopup.gameObject.activeSelf;

        public string PendingText => _pendingNote.gameObject.activeSelf ? _pendingNote.text : string.Empty;

        /// <summary>La scheda di una specializzazione da guardare, prima di sceglierla.</summary>
        public Button SpecTab(int index) => _specTabs[index];

        /// <summary>Una delle due opzioni della finestrella di un nodo a scelta.</summary>
        public Button ChoiceButton(int index) => _choiceButtons[index];

        public string AreaName(bool specialization) => (specialization ? _specArea : _classArea).Name.text;

        public string AreaPoints(bool specialization) => (specialization ? _specArea : _classArea).Points.text;

        public string SpecNote => _specNote.gameObject.activeSelf ? _specNote.text : string.Empty;

        /// <summary>La vista di un nodo di un albero, o null.</summary>
        public TalentNodeView View(TalentTree tree, string id)
        {
            foreach (var view in _classViews.Concat(_specViews))
            {
                if (view.gameObject.activeSelf && view.Tree == tree && view.Node.Id == id)
                {
                    return view;
                }
            }

            return null;
        }

        /// <summary>Quante linee e quante linee tratteggiate si vedono nell'albero: per i test.</summary>
        public (int lines, int gates) Drawn(bool specialization)
        {
            var lines = specialization ? _specLines : _classLines;
            var gates = specialization ? _specGates : _classGates;
            return (lines.Count(l => l.gameObject.activeSelf), gates.Count(g => g.gameObject.activeSelf));
        }

        public void Bind(PlayerTalents talents, PlayerInputReader reader, Localizer localizer)
        {
            Unsubscribe();
            _talents = talents;
            _reader = reader;
            _localizer = localizer;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
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

        public void Open()
        {
            if (IsOpen || _talents == null)
            {
                return;
            }

            _window.SetActive(true);
            Refresh();
            OpenChanged?.Invoke(true);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            CloseChoice();
            HideTooltip();
            _window.SetActive(false);
            OpenChanged?.Invoke(false);
        }

        private void Awake()
        {
            _window.SetActive(false);
            _nodeTemplate.gameObject.SetActive(false);
            _lineTemplate.gameObject.SetActive(false);
            _gateTemplate.gameObject.SetActive(false);
            _tooltip.gameObject.SetActive(false);
            _choicePopup.gameObject.SetActive(false);
            _choiceBlocker.gameObject.SetActive(false);
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
            if (_subscribed || _talents == null)
            {
                return;
            }

            _talents.Build.Changed += Refresh;
            if (_reader != null)
            {
                _reader.TalentsToggled += Toggle;
            }

            if (_localizer != null)
            {
                _localizer.LanguageChanged += Refresh;
            }

            _close.onClick.AddListener(Close);
            _apply.onClick.AddListener(HandleApply);
            _cancel.onClick.AddListener(HandleCancel);
            _chooseButton.onClick.AddListener(HandleChoose);
            _choiceBlocker.onClick.AddListener(CloseChoice);
            for (int i = 0; i < _specTabs.Length; i++)
            {
                int index = i;
                _specTabs[i].onClick.AddListener(() => Preview(index));
            }

            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                int option = i;
                _choiceButtons[i].onClick.AddListener(() => PickOption(option));
            }

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _talents.Build.Changed -= Refresh;
            if (_reader != null)
            {
                _reader.TalentsToggled -= Toggle;
            }

            if (_localizer != null)
            {
                _localizer.LanguageChanged -= Refresh;
            }

            _close.onClick.RemoveListener(Close);
            _apply.onClick.RemoveListener(HandleApply);
            _cancel.onClick.RemoveListener(HandleCancel);
            _chooseButton.onClick.RemoveListener(HandleChoose);
            _choiceBlocker.onClick.RemoveListener(CloseChoice);
            foreach (var tab in _specTabs)
            {
                tab.onClick.RemoveAllListeners();
            }

            foreach (var button in _choiceButtons)
            {
                button.onClick.RemoveAllListeners();
            }

            _subscribed = false;
        }

        private void HandleApply()
        {
            CloseChoice();
            _talents.Build.Apply();
        }

        private void HandleCancel()
        {
            CloseChoice();
            _talents.Build.Cancel();
        }

        private void HandleChoose()
        {
            _talents.Build.TryChoose(Viewed);
        }

        private void Preview(int index)
        {
            var specs = _talents.Build.Specializations;
            if (index < specs.Count && _talents.Build.Specialization == null)
            {
                _previewed = specs[index];
                CloseChoice();
                Refresh();
            }
        }

        // la specializzazione che si vede a destra: la scelta, o la scheda che si guarda
        private TalentTree Viewed
        {
            get
            {
                var build = _talents.Build;
                return build.Specialization != null ? build.Specialization : _previewed != null ? _previewed : build.Specializations[0];
            }
        }

        // si ridisegna solo da aperto: i nodi sono pochi, ma da chiuso non servono a niente
        private void Refresh()
        {
            if (!IsOpen || _talents == null)
            {
                return;
            }

            var build = _talents.Build;
            _title.text = Get("talents.title");
            _close.GetComponentInChildren<TMP_Text>(true).text = Get("talents.close");
            var spec = Viewed;
            bool chosen = build.Specialization != null;

            DrawTree(build.ClassTree, _classArea, _classViews, _classLines, _classGates);
            DrawTree(spec, _specArea, _specViews, _specLines, _specGates);

            _classArea.Name.text = Get(build.ClassTree.NameKey);
            _classArea.Points.text = _localizer.Format("talents.points", build.Available(build.ClassTree));
            _specArea.Name.text = chosen ? Get(spec.NameKey) : Get("talents.spec_title");
            _specArea.Points.text = chosen ? _localizer.Format("talents.points", build.Available(spec)) : string.Empty;

            // le schede e la scelta, finché non ce n'è una
            for (int i = 0; i < _specTabs.Length; i++)
            {
                bool used = !chosen && i < build.Specializations.Count;
                _specTabs[i].gameObject.SetActive(used);
                if (used)
                {
                    var tab = build.Specializations[i];
                    var label = _specTabs[i].GetComponentInChildren<TMP_Text>(true);
                    label.text = Get(tab.NameKey);
                    label.color = tab == spec ? ParseColor(Gold) : ParseColor(Dim);
                }
            }

            _chooseButton.gameObject.SetActive(!chosen);
            _specNote.gameObject.SetActive(!chosen);
            if (!chosen)
            {
                _chooseButton.GetComponentInChildren<TMP_Text>(true).text = Get("talents.choose_spec");
                _chooseButton.interactable = build.CanChoose(spec);
                _specNote.text = build.Level < TalentPoints.SpecializationLevel
                    ? _localizer.Format("talents.spec_locked", TalentPoints.SpecializationLevel)
                    : Get("talents.spec." + spec.Id);
            }

            _apply.GetComponentInChildren<TMP_Text>(true).text = Get("talents.apply");
            _cancel.GetComponentInChildren<TMP_Text>(true).text = Get("talents.cancel");
            _apply.interactable = build.HasChanges;
            _cancel.interactable = build.HasChanges;
            _pendingNote.gameObject.SetActive(build.HasChanges);
            _pendingNote.text = Get("talents.pending");
            _hint.text = Get("talents.hint");

            if (_hovered != null && _hovered.gameObject.activeSelf)
            {
                ShowTooltip(_hovered);
            }
            else
            {
                HideTooltip();
            }

            if (_choiceFor != null)
            {
                if (_choiceFor.gameObject.activeSelf && build.Rank(_choiceFor.Tree, _choiceFor.Node) == 0)
                {
                    FillChoice(_choiceFor);
                }
                else
                {
                    CloseChoice();
                }
            }
        }

        private void DrawTree(TalentTree tree, TreeArea area, List<TalentNodeView> views, List<Image> lines, List<RectTransform> gates)
        {
            var build = _talents.Build;
            int used = 0;
            int usedLines = 0;
            int usedGates = 0;
            bool active = tree != null && (tree == build.ClassTree || tree == build.Specialization);
            if (tree != null)
            {
                int columns = tree.Nodes.Max(n => n.Column) + 1;
                int rows = tree.Nodes.Max(n => n.Row) + 1;
                area.Area.sizeDelta = new Vector2(columns * _cell.x, rows * _cell.y);

                foreach (var node in tree.Nodes)
                {
                    foreach (string id in node.Links)
                    {
                        var parent = tree.Find(id);
                        if (parent == null)
                        {
                            continue;
                        }

                        var line = Take(lines, ref usedLines, _lineTemplate, area.Lines);
                        bool lit = active && build.Rank(tree, parent) >= parent.Ranks && (build.Rank(tree, node) > 0 || build.IsOpen(tree, node));
                        PlaceLine(line, Center(parent), Center(node), lit ? _lineLit : _lineIdle);
                    }
                }

                foreach (var gate in tree.Gates)
                {
                    var rect = Take(gates, ref usedGates, _gateTemplate, area.Lines);
                    rect.anchoredPosition = new Vector2(0f, -gate.Row * _cell.y);
                    rect.sizeDelta = new Vector2(area.Area.sizeDelta.x, rect.sizeDelta.y);
                    bool met = active && build.SpentAbove(tree, gate.Row) >= gate.Points;
                    var text = rect.GetComponentInChildren<TMP_Text>(true);
                    text.text = gate.Points.ToString();
                    text.color = met ? ParseColor(Gold) : ParseColor(Dim);
                    rect.GetComponent<Image>().color = met ? _lineLit : _lineIdle;
                }

                foreach (var node in tree.Nodes)
                {
                    var view = Take(views, ref used, _nodeTemplate, area.Nodes);
                    ((RectTransform)view.transform).anchoredPosition = Center(node);
                    int rank = build.Rank(tree, node);
                    int option = build.OptionOf(tree, node);
                    var shown = node.Options[Math.Max(0, option)];
                    bool choice = node.Kind == TalentNodeKind.Choice;
                    int shape = choice ? ChoiceShape : node.Kind == TalentNodeKind.Active ? ActiveShape : PassiveShape;
                    string label = choice && option < 0 ? Get("talent." + node.Id) : NameOf(shown);
                    var icon = choice && option < 0 ? null : shown.Icon != null ? shown.Icon : shown.Ability != null ? shown.Ability.Icon : null;
                    view.Show(tree, node, _plates[shape], _rings[shape], icon, label, rank, StateOf(tree, node, rank, active));
                }
            }

            Hide(views, used);
            Hide(lines, usedLines);
            Hide(gates, usedGates);
        }

        private TalentNodeState StateOf(TalentTree tree, TalentNode node, int rank, bool active)
        {
            if (rank >= node.Ranks)
            {
                return TalentNodeState.Full;
            }

            if (rank > 0)
            {
                return TalentNodeState.Taken;
            }

            return active && _talents.Build.IsOpen(tree, node) ? TalentNodeState.Available : TalentNodeState.Locked;
        }

        // il centro del nodo nella griglia, dall'angolo in alto a sinistra dell'area
        private Vector2 Center(TalentNode node)
        {
            return new Vector2((node.Column + 0.5f) * _cell.x, -(node.Row + 0.5f) * _cell.y);
        }

        private void PlaceLine(Image line, Vector2 from, Vector2 to, Color color)
        {
            var rect = (RectTransform)line.transform;
            Vector2 delta = to - from;
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, _lineWidth);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            line.color = color;
        }

        // la prossima copia del modello nella lista, accesa; ne fa una nuova solo se mancano
        private T Take<T>(List<T> pool, ref int used, T template, RectTransform parent) where T : Component
        {
            if (used == pool.Count)
            {
                var copy = Instantiate(template, parent);
                copy.name = $"{template.name}_{pool.Count + 1}";
                if (copy is TalentNodeView view)
                {
                    view.Hovered += HandleHovered;
                    view.Unhovered += HandleUnhovered;
                    view.Clicked += HandleClicked;
                }

                pool.Add(copy);
            }

            var item = pool[used++];
            item.gameObject.SetActive(true);
            return item;
        }

        private static void Hide<T>(List<T> pool, int used) where T : Component
        {
            for (int i = used; i < pool.Count; i++)
            {
                pool[i].gameObject.SetActive(false);
            }
        }

        private void HandleClicked(TalentNodeView view, bool right)
        {
            var build = _talents.Build;
            if (right)
            {
                CloseChoice();
                build.TryRemove(view.Tree, view.Node);
                return;
            }

            if (view.Node.Kind == TalentNodeKind.Choice)
            {
                if (build.Rank(view.Tree, view.Node) == 0)
                {
                    OpenChoice(view);
                }

                return;
            }

            CloseChoice();
            build.TryAdd(view.Tree, view.Node);
        }

        private void HandleHovered(TalentNodeView view)
        {
            _hovered = view;
            ShowTooltip(view);
        }

        private void HandleUnhovered(TalentNodeView view)
        {
            if (_hovered == view)
            {
                _hovered = null;
                HideTooltip();
            }
        }

        private void OpenChoice(TalentNodeView view)
        {
            if (_choiceFor == view)
            {
                CloseChoice();
                return;
            }

            _choiceFor = view;
            FillChoice(view);
            _choiceBlocker.gameObject.SetActive(true);
            _choicePopup.gameObject.SetActive(true);
            _choicePopup.SetAsLastSibling();
            HideTooltip();
            var rect = (RectTransform)view.transform;
            _choicePopup.pivot = new Vector2(0.5f, 1f);
            _choicePopup.position = rect.TransformPoint(new Vector3(0f, -rect.rect.height * 0.5f - 8f, 0f));
        }

        private void FillChoice(TalentNodeView view)
        {
            var build = _talents.Build;
            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                bool used = i < view.Node.Options.Length;
                _choiceButtons[i].gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }

                var option = view.Node.Options[i];
                var label = _choiceButtons[i].GetComponentInChildren<TMP_Text>(true);
                label.text = $"<color={Gold}>{NameOf(option)}</color>\n{TalentDescriber.Describe(option, 1, _localizer)}";
                _choiceButtons[i].interactable = build.CanAdd(view.Tree, view.Node, i);
            }
        }

        private void PickOption(int option)
        {
            var view = _choiceFor;
            CloseChoice();
            if (view != null)
            {
                _talents.Build.TryAdd(view.Tree, view.Node, option);
            }
        }

        private void CloseChoice()
        {
            _choiceFor = null;
            _choicePopup.gameObject.SetActive(false);
            _choiceBlocker.gameObject.SetActive(false);
        }

        private void ShowTooltip(TalentNodeView view)
        {
            // con la scelta aperta il tooltip coprirebbe le due opzioni
            if (_choiceFor != null)
            {
                return;
            }

            _tooltipText.text = Describe(view);
            _tooltip.gameObject.SetActive(true);
            _tooltip.SetAsLastSibling();

            // a destra del nodo se c'è posto, altrimenti a sinistra; verso l'alto nella metà bassa della finestra
            var node = (RectTransform)view.transform;
            var window = (RectTransform)_window.transform;
            Vector3 center = window.InverseTransformPoint(node.position);
            bool right = center.x < window.rect.center.x;
            bool low = center.y < window.rect.center.y;
            _tooltip.pivot = new Vector2(right ? 0f : 1f, low ? 0f : 1f);
            float half = node.rect.width * 0.5f + 10f;
            _tooltip.position = node.TransformPoint(new Vector3(right ? half : -half, low ? -node.rect.height * 0.5f : node.rect.height * 0.5f, 0f));
        }

        private void HideTooltip()
        {
            _tooltip.gameObject.SetActive(false);
        }

        // il testo del tooltip: nome, tipo, gradi, cosa fa, cosa serve per aprirlo e cosa si fa col mouse
        private string Describe(TalentNodeView view)
        {
            var build = _talents.Build;
            var tree = view.Tree;
            var node = view.Node;
            int rank = build.Rank(tree, node);
            bool choice = node.Kind == TalentNodeKind.Choice;
            _text.Clear();

            string title = choice ? Get("talent." + node.Id) : NameOf(node.Options[0]);
            _text.Append($"<size=130%><color={Gold}>{title}</color></size>\n");
            _text.Append($"<color={Dim}>{Get(choice ? "talents.choice" : node.Kind == TalentNodeKind.Active ? "talents.active" : "talents.passive")}</color>");
            if (node.Ranks > 1)
            {
                _text.Append('\n').Append(_localizer.Format("talents.rank", rank, node.Ranks));
            }

            if (choice)
            {
                int taken = build.OptionOf(tree, node);
                for (int i = 0; i < node.Options.Length; i++)
                {
                    var option = node.Options[i];
                    string mark = taken == i ? $" <color={Gold}>({Get("talents.chosen")})</color>" : string.Empty;
                    _text.Append($"\n\n<color={Gold}>{NameOf(option)}</color>{mark}\n").Append(TalentDescriber.Describe(option, 1, _localizer));
                }
            }
            else
            {
                var option = node.Options[0];
                _text.Append("\n\n");
                if (option.Ability != null)
                {
                    var ability = option.Ability;
                    _text.Append(Get(ability.NameKey + ".desc"));
                    _text.Append($"\n<color={Dim}>{_localizer.Format("talents.ability_stats", Math.Max(0f, build.AbilityValue(ability, AbilityParam.ManaCost)), Math.Max(0f, build.AbilityValue(ability, AbilityParam.Cooldown)))}</color>");
                }
                else
                {
                    _text.Append(TalentDescriber.Describe(option, rank, _localizer));
                    if (rank >= 1 && rank < node.Ranks)
                    {
                        _text.Append($"\n\n<color={Dim}>{Get("talents.next_rank")}</color>\n").Append(TalentDescriber.Describe(option, rank + 1, _localizer));
                    }
                }
            }

            string need = Requirement(tree, node, view.State);
            if (need != null)
            {
                _text.Append($"\n\n<color={Warning}>{need}</color>");
            }
            else if (view.State == TalentNodeState.Available || view.State == TalentNodeState.Taken)
            {
                _text.Append($"\n\n<color={Dim}>{Get(choice ? "talents.hint_choice" : "talents.hint")}</color>");
            }

            return _text.ToString();
        }

        private string Requirement(TalentTree tree, TalentNode node, TalentNodeState state)
        {
            var build = _talents.Build;
            if (tree != build.ClassTree && tree != build.Specialization)
            {
                return build.Level < TalentPoints.SpecializationLevel
                    ? _localizer.Format("talents.spec_locked", TalentPoints.SpecializationLevel)
                    : Get("talents.choose_spec");
            }

            if (state == TalentNodeState.Locked)
            {
                bool linked = node.Links.Any(id =>
                {
                    var parent = tree.Find(id);
                    return parent != null && build.Rank(tree, parent) >= parent.Ranks;
                });
                return linked ? _localizer.Format("talents.requires_points", tree.PointsFor(node.Row)) : Get("talents.requires_link");
            }

            return state == TalentNodeState.Available && build.Available(tree) == 0 ? Get("talents.no_points") : null;
        }

        private string NameOf(TalentOption option)
        {
            return Get(option.Ability != null ? option.Ability.NameKey : option.NameKey);
        }

        private string Get(string key) => _localizer != null ? _localizer.Get(key) : key;

        private static Color ParseColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
