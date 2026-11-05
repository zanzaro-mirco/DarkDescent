using System;
using System.Collections.Generic;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Le barre sopra i nemici feriti (D9 e D10 della M23), un'opzione spenta di default. Un solo canvas in
    /// world space, girato verso la camera, con un pool di barre (<see cref="EnemyBarRules.MaxBars"/> al più, le
    /// più vicine al cavaliere): il nemico che perde vita guadagna una barra, quello che muore, guarisce del
    /// tutto o va troppo lontano la perde. La vita si legge sugli eventi di <see cref="Health"/>; un solo
    /// <c>LateUpdate</c> per tutte, che muove le barre mostrate e nient'altro. Il boss non ha la barra (ha la sua,
    /// in alto), i campioni e gli unici la prendono nel colore del loro grado, l'unico anche il nome.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyHealthBars : MonoBehaviour
    {
        [Tooltip("Il font del nome sopra la barra degli unici (lo stesso della barra in alto).")]
        [SerializeField] private TMP_FontAsset _font;

        [Tooltip("Colore di un nemico normale.")]
        [SerializeField] private Color _normalColor = new Color(0.8f, 0.12f, 0.1f, 1f);

        [Tooltip("Il fondo scuro della barra, dietro il riempimento.")]
        [SerializeField] private Color _backColor = new Color(0.05f, 0.03f, 0.03f, 0.9f);

        [Tooltip("Larghezza e altezza della barra in metri.")]
        [SerializeField] private Vector2 _size = new Vector2(1.25f, 0.17f);

        [Tooltip("Spessore del bordo scuro in metri.")]
        [SerializeField, Min(0f)] private float _border = 0.02f;

        [Tooltip("Altezza della barra sopra i piedi, per un nemico alto una unità (si moltiplica per la sua scala).")]
        [SerializeField, Min(0f)] private float _height = 2.35f;

        [Tooltip("Altezza del nome sopra la barra, in metri.")]
        [SerializeField, Min(0f)] private float _nameLift = 0.08f;

        [SerializeField, Min(0.05f)] private float _nameSize = 0.3f;

        [Tooltip("Oltre questa distanza dal cavaliere un nemico non ha la barra, anche se ce ne sono meno di 24.")]
        [SerializeField, Min(1f)] private float _range = 24f;

        [Tooltip("Vuoto = Camera.main, risolta quando serve.")]
        [SerializeField] private Camera _camera;

        // una cella di quanto serve per ogni Health seguita: i delegati servono uguali per il -=
        private sealed class Entry
        {
            public Health Health;
            public Action<float, float> Changed;
            public Action Revived;
            public Action Died;
            public bool Excluded;
            public bool Wounded;
            public int PickedFrame = -1;
            public BarView View;
        }

        private sealed class BarView
        {
            public GameObject Root;
            public RectTransform Rect;
            public RectTransform Fill;
            public Image FillImage;
            public TextMeshProUGUI Name;
        }

        private readonly Dictionary<Health, Entry> _entries = new Dictionary<Health, Entry>();
        private readonly List<Entry> _wounded = new List<Entry>();
        private readonly List<Entry> _shown = new List<Entry>();
        private readonly List<Entry> _candidates = new List<Entry>();
        private readonly List<float> _distances = new List<float>();
        private readonly List<int> _picked = new List<int>();
        private readonly Stack<BarView> _free = new Stack<BarView>();
        private Canvas _canvas;
        private Transform _canvasRoot;
        private Transform _player;
        private Localizer _localizer;
        private bool _on;
        private bool _built;

        /// <summary>L'opzione: spenta, nessuna barra e nessun lavoro.</summary>
        public bool IsOn => _on;

        /// <summary>Le barre mostrate adesso, per i test.</summary>
        public int ShownCount => _shown.Count;

        /// <summary>I nemici feriti che ne avrebbero una, mostrata o no.</summary>
        public int WoundedCount => _wounded.Count;

        /// <summary>Le barre create finora (il pool, libere e in uso), per i test.</summary>
        public int PoolSize => _shown.Count + _free.Count;

        /// <summary>Il canvas delle barre, per i test e per la misura.</summary>
        public Canvas Canvas
        {
            get
            {
                Build();
                return _canvas;
            }
        }

        /// <summary>La barra di un nemico se è mostrata, altrimenti falso.</summary>
        public bool IsShowing(Health enemy)
        {
            return !ReferenceEquals(enemy, null) && _entries.TryGetValue(enemy, out var entry) && entry.View != null;
        }

        /// <summary>La parte piena della barra di un nemico (0 se non è mostrata).</summary>
        public float FillOf(Health enemy)
        {
            return IsShowing(enemy) ? _entries[enemy].View.Fill.anchorMax.x : 0f;
        }

        /// <summary>Dove sta la barra di un nemico nel mondo (zero se non è mostrata).</summary>
        public Vector3 PositionOf(Health enemy)
        {
            return IsShowing(enemy) ? _entries[enemy].View.Rect.position : default;
        }

        public Color ColorOf(Health enemy)
        {
            return IsShowing(enemy) ? _entries[enemy].View.FillImage.color : default;
        }

        /// <summary>Il nome sopra la barra, vuoto se il nemico non ne ha uno.</summary>
        public string NameOf(Health enemy)
        {
            return IsShowing(enemy) && _entries[enemy].View.Name.gameObject.activeSelf ? _entries[enemy].View.Name.text : string.Empty;
        }

        /// <summary>Il cavaliere, da cui si contano le distanze, e la lingua per il nome degli unici.</summary>
        public void Bind(Transform player, Localizer localizer)
        {
            if (_localizer != null)
            {
                _localizer.LanguageChanged -= RefreshNames;
            }

            _player = player;
            _localizer = localizer;
            if (_localizer != null)
            {
                _localizer.LanguageChanged += RefreshNames;
            }
        }

        /// <summary>Accende o spegne le barre (l'opzione delle preferenze).</summary>
        public void SetOn(bool on)
        {
            if (_on == on)
            {
                return;
            }

            _on = on;
            if (!on)
            {
                ReleaseAll();
            }
        }

        /// <summary>Segue la vita di un nemico del livello.</summary>
        public void Track(Health enemy)
        {
            if (enemy == null || _entries.ContainsKey(enemy))
            {
                return;
            }

            var entry = new Entry { Health = enemy, Excluded = enemy.GetComponent<BossController>() != null };
            entry.Changed = (current, max) => Refresh(entry);
            entry.Revived = () => Refresh(entry);
            entry.Died = () => Refresh(entry);
            enemy.HealthChanged += entry.Changed;
            enemy.Revived += entry.Revived;
            enemy.Died += entry.Died;
            _entries.Add(enemy, entry);
            Refresh(entry);
        }

        /// <summary>Lascia un nemico, anche già distrutto con il suo livello.</summary>
        public void Untrack(Health enemy)
        {
            // ReferenceEquals: una Health distrutta è "null" per Unity ma resta una chiave da togliere
            if (ReferenceEquals(enemy, null) || !_entries.TryGetValue(enemy, out var entry))
            {
                return;
            }

            enemy.HealthChanged -= entry.Changed;
            enemy.Revived -= entry.Revived;
            enemy.Died -= entry.Died;
            Release(entry);
            _wounded.Remove(entry);
            _entries.Remove(enemy);
        }

        private void OnDestroy()
        {
            if (_localizer != null)
            {
                _localizer.LanguageChanged -= RefreshNames;
            }
        }

        // ferito e vivo: nella lista di chi merita una barra; altrimenti fuori, e se la barra c'era, via
        private void Refresh(Entry entry)
        {
            bool wounded = !entry.Excluded && !entry.Health.IsDead && EnemyBarRules.IsWounded(entry.Health.Current, entry.Health.Max);
            if (wounded != entry.Wounded)
            {
                entry.Wounded = wounded;
                if (wounded)
                {
                    _wounded.Add(entry);
                }
                else
                {
                    _wounded.Remove(entry);
                }
            }

            if (entry.View != null)
            {
                if (wounded)
                {
                    SetFill(entry);
                }
                else
                {
                    Release(entry);
                }
            }
        }

        private void LateUpdate()
        {
            if (!_on)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null || _player == null)
            {
                return;
            }

            Build();
            _canvas.worldCamera = _camera;
            _canvasRoot.rotation = _camera.transform.rotation;

            // le più vicine, dentro il tetto e dentro il raggio
            _candidates.Clear();
            _distances.Clear();
            Vector3 center = _player.position;
            float rangeSquared = _range * _range;
            for (int i = 0; i < _wounded.Count; i++)
            {
                var entry = _wounded[i];
                if (entry.Health == null || !entry.Health.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector3 away = entry.Health.transform.position - center;
                away.y = 0f;
                float distance = away.sqrMagnitude;
                if (distance <= rangeSquared)
                {
                    _candidates.Add(entry);
                    _distances.Add(distance);
                }
            }

            EnemyBarRules.PickNearest(_distances, EnemyBarRules.MaxBars, _picked);
            int frame = Time.frameCount;
            for (int i = 0; i < _picked.Count; i++)
            {
                _candidates[_picked[i]].PickedFrame = frame;
            }

            // chi non è più tra le scelte lascia la barra; all'indietro, si toglie dalla lista mentre la si scorre
            for (int i = _shown.Count - 1; i >= 0; i--)
            {
                if (_shown[i].PickedFrame != frame)
                {
                    Release(_shown[i]);
                }
            }

            for (int i = 0; i < _picked.Count; i++)
            {
                var entry = _candidates[_picked[i]];
                if (entry.View == null)
                {
                    Assign(entry);
                }

                Place(entry);
            }
        }

        private void Assign(Entry entry)
        {
            var view = _free.Count > 0 ? _free.Pop() : CreateView();
            view.Root.SetActive(true);
            entry.View = view;
            _shown.Add(entry);
            view.FillImage.color = ColorFor(entry.Health, out var rank);
            bool unique = rank == EnemyRank.Unique;
            view.Name.gameObject.SetActive(unique);
            if (unique)
            {
                view.Name.text = NameFor(entry.Health);
                view.Name.color = view.FillImage.color;
            }

            SetFill(entry);
        }

        private void Release(Entry entry)
        {
            var view = entry.View;
            if (view == null)
            {
                return;
            }

            entry.View = null;
            _shown.Remove(entry);
            view.Root.SetActive(false);
            _free.Push(view);
        }

        private void ReleaseAll()
        {
            for (int i = _shown.Count - 1; i >= 0; i--)
            {
                Release(_shown[i]);
            }
        }

        private void SetFill(Entry entry)
        {
            entry.View.Fill.anchorMax = new Vector2(EnemyBarRules.Fill(entry.Health.Current, entry.Health.Max), 1f);
        }

        // sopra la testa, nel mondo; la posizione locale del canvas, che è fermo davanti alla camera
        private void Place(Entry entry)
        {
            var enemy = entry.Health.transform;
            Vector3 head = enemy.position + Vector3.up * (_height * enemy.lossyScale.y);
            entry.View.Rect.localPosition = _canvasRoot.InverseTransformPoint(head);
        }

        private Color ColorFor(Health enemy, out EnemyRank rank)
        {
            rank = EnemyRank.Normal;
            if (enemy.TryGetComponent(out SpecialEnemy special) && special.IsBound)
            {
                rank = special.Rank;
                if (rank == EnemyRank.Champion || rank == EnemyRank.Unique)
                {
                    return special.Numbers.Color;
                }
            }

            return _normalColor;
        }

        private string NameFor(Health enemy)
        {
            if (_localizer == null || !enemy.TryGetComponent(out EnemyAI ai) || ai.Archetype == null || string.IsNullOrEmpty(ai.Archetype.NameKey))
            {
                return string.Empty;
            }

            return _localizer.Get(ai.Archetype.NameKey);
        }

        private void RefreshNames()
        {
            for (int i = 0; i < _shown.Count; i++)
            {
                var view = _shown[i].View;
                if (view.Name.gameObject.activeSelf)
                {
                    view.Name.text = NameFor(_shown[i].Health);
                }
            }
        }

        // il canvas nasce al primo uso: con l'opzione spenta non costa niente
        private void Build()
        {
            if (_built)
            {
                return;
            }

            _built = true;
            var go = new GameObject("Bars", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(transform, false);
            _canvasRoot = go.transform;
            // un'unità del canvas è un centimetro: la barra misura 125 × 17 e sta a 1,25 m × 0,17 m nel mondo
            _canvasRoot.localScale = Vector3.one * 0.01f;
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)_canvasRoot).sizeDelta = Vector2.zero;
        }

        private BarView CreateView()
        {
            Build();
            float unit = 100f; // metri in unità del canvas
            Vector2 size = _size * unit;
            float border = _border * unit;

            var rootGo = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            rootGo.transform.SetParent(_canvasRoot, false);
            var rect = (RectTransform)rootGo.transform;
            rect.sizeDelta = size;
            var back = rootGo.GetComponent<Image>();
            back.color = _backColor;
            back.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(rect, false);
            var fill = (RectTransform)fillGo.transform;
            // il riempimento si accorcia da destra spostando l'ancora, come nella barra in alto
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(border, border);
            fill.offsetMax = new Vector2(-border, -border);
            var fillImage = fillGo.GetComponent<Image>();
            fillImage.raycastTarget = false;

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGo.transform.SetParent(rect, false);
            var nameRect = (RectTransform)nameGo.transform;
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.sizeDelta = new Vector2(size.x * 3f, _nameSize * unit * 1.4f);
            nameRect.anchoredPosition = new Vector2(0f, _nameLift * unit);
            var name = nameGo.GetComponent<TextMeshProUGUI>();
            if (_font != null)
            {
                name.font = _font;
            }

            name.fontSize = _nameSize * unit;
            name.alignment = TextAlignmentOptions.Center;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.raycastTarget = false;
            nameGo.SetActive(false);

            rootGo.SetActive(false);
            return new BarView { Root = rootGo, Rect = rect, Fill = fill, FillImage = fillImage, Name = name };
        }
    }
}
