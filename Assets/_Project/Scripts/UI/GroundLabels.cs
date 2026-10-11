using System.Collections.Generic;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.Player;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// I nomi di tutti gli oggetti a terra sullo schermo mentre si tiene premuto `Alt`, come in
    /// Diablo 2 (D1 della M10). Piccoli, dal più vicino al cavaliere al più lontano: ognuno parte
    /// sopra il suo oggetto e, se tocca un nome già messo, sale di una riga finché trova posto; oltre
    /// qualche riga non si mostra. Un click sul nome raccoglie l'oggetto. Etichette da un pool e
    /// buffer fissi: mentre `Alt` è premuto la disposizione si rifà a ogni frame senza allocare,
    /// tranne il testo di un oggetto la prima volta che compare.
    /// </summary>
    [DisallowMultipleComponent]
    public class GroundLabels : MonoBehaviour
    {
        [Tooltip("Etichetta modello, spenta: il pool ne crea copie.")]
        [SerializeField] private GroundLabel _template;

        [Tooltip("I livelli degli oggetti a terra (Interactable).")]
        [SerializeField] private LayerMask _itemLayers;

        [Tooltip("Raggio attorno al cavaliere in cui cercare gli oggetti: un po' più dello schermo.")]
        [SerializeField, Min(1f)] private float _radius = 18f;

        [Tooltip("Altezza sopra l'oggetto da cui parte l'etichetta.")]
        [SerializeField, Min(0f)] private float _heightAboveItem = 0.35f;

        [Tooltip("Spazio tra due etichette, in pixel di riferimento.")]
        [SerializeField, Min(0f)] private float _gap = 2f;

        [Tooltip("Di quante righe un'etichetta può salire per non coprirne un'altra; oltre, non si mostra.")]
        [SerializeField, Min(0)] private int _maxLift = 6;

        [SerializeField, Min(1)] private int _maxLabels = 32;

        [Tooltip("Vuoto = Camera.main, risolta in Awake.")]
        [SerializeField] private Camera _camera;

        private readonly Dictionary<GroundItem, GroundLabel> _byItem = new Dictionary<GroundItem, GroundLabel>();
        private readonly Stack<GroundLabel> _pool = new Stack<GroundLabel>();
        private Collider[] _colliders;
        private GroundItem[] _found;
        private float[] _distances;
        private Rect[] _placed;
        private GroundLabel[] _stale;
        private RectTransform _container;
        private PlayerInputReader _reader;
        private PlayerController _controller;
        private Transform _player;
        private Localizer _localizer;
        private Canvas _canvas;
        private bool _alwaysVisible;
        private int _stamp;
        private bool _subscribed;

        /// <summary>Le etichette accese adesso, per i test.</summary>
        public IEnumerable<GroundLabel> Shown
        {
            get
            {
                foreach (var label in _byItem.Values)
                {
                    if (label.gameObject.activeSelf)
                    {
                        yield return label;
                    }
                }
            }
        }

        public bool IsActive => _alwaysVisible || (_reader != null && _reader.IsLabelsHeld);

        public void Bind(PlayerInputReader reader, PlayerController controller, Localizer localizer)
        {
            Unsubscribe();
            _reader = reader;
            _controller = controller;
            _player = controller.transform;
            _localizer = localizer;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        /// <summary>Nomi sempre visibili, senza tenere `Alt` (l'opzione della D10).</summary>
        public void SetAlwaysVisible(bool visible)
        {
            _alwaysVisible = visible;
            if (!IsActive)
            {
                ReleaseAll();
            }
        }

        private void Awake()
        {
            _container = (RectTransform)transform;
            _colliders = new Collider[_maxLabels * 2];
            _found = new GroundItem[_maxLabels * 2];
            _distances = new float[_maxLabels * 2];
            _placed = new Rect[_maxLabels];
            _stale = new GroundLabel[_maxLabels * 2];
            _template.gameObject.SetActive(false);
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            _canvas = GetComponentInParent<Canvas>().rootCanvas;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ReleaseAll();
        }

        private void Subscribe()
        {
            if (_subscribed || _reader == null)
            {
                return;
            }

            _reader.LabelsHeldChanged += HandleLabelsHeldChanged;
            _localizer.LanguageChanged += ReleaseAll;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _reader.LabelsHeldChanged -= HandleLabelsHeldChanged;
            _localizer.LanguageChanged -= ReleaseAll;
            _subscribed = false;
        }

        private void HandleLabelsHeldChanged(bool held)
        {
            if (!IsActive)
            {
                ReleaseAll();
            }
        }

        private void LateUpdate()
        {
            if (!_subscribed || !IsActive || _camera == null)
            {
                return;
            }

            _stamp++;
            // letta a ogni giro: in Overlay la conversione vuole null, in Screen Space Camera la sua camera
            Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            int found = Gather();
            int placed = 0;
            for (int i = 0; i < found; i++)
            {
                GroundItem item = _found[i];
                if (!_byItem.TryGetValue(item, out GroundLabel label))
                {
                    if (_byItem.Count >= _maxLabels)
                    {
                        continue;
                    }

                    label = Take();
                    label.Assign(item, item.GetLabel(_localizer));
                    _byItem.Add(item, label);
                }

                label.Stamp = _stamp;
                Vector3 screen = _camera.WorldToScreenPoint(item.transform.position + Vector3.up * _heightAboveItem);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_container, screen, uiCamera, out Vector2 anchor);
                if (TryPlace(label, anchor, placed, out Rect rect))
                {
                    _placed[placed++] = rect;
                    if (!label.gameObject.activeSelf)
                    {
                        label.gameObject.SetActive(true);
                    }

                    label.Place(new Vector2(rect.center.x, rect.yMin), rect.yMin - anchor.y);
                }
                else if (label.gameObject.activeSelf)
                {
                    label.gameObject.SetActive(false);
                }
            }

            ReleaseStale();
        }

        // gli oggetti a terra vicini e sullo schermo, dal più vicino al cavaliere
        private int Gather()
        {
            int hits = Physics.OverlapSphereNonAlloc(_player.position, _radius, _colliders, _itemLayers, QueryTriggerInteraction.Collide);
            int count = 0;
            for (int i = 0; i < hits && count < _found.Length; i++)
            {
                var item = _colliders[i].GetComponentInParent<GroundItem>();
                if (item == null || item.Item == null || Contains(item, count))
                {
                    continue;
                }

                Vector3 viewport = _camera.WorldToViewportPoint(item.transform.position);
                if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
                {
                    continue;
                }

                float distance = (item.transform.position - _player.position).sqrMagnitude;
                int at = count++;
                while (at > 0 && _distances[at - 1] > distance)
                {
                    _found[at] = _found[at - 1];
                    _distances[at] = _distances[at - 1];
                    at--;
                }

                _found[at] = item;
                _distances[at] = distance;
            }

            return count;
        }

        private bool Contains(GroundItem item, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_found[i] == item)
                {
                    return true;
                }
            }

            return false;
        }

        // sopra l'oggetto, o una riga più su alla volta finché non tocca nessun nome già messo
        private bool TryPlace(GroundLabel label, Vector2 anchor, int placed, out Rect rect)
        {
            float step = label.Height + _gap;
            for (int lift = 0; lift <= _maxLift; lift++)
            {
                rect = new Rect(anchor.x - label.Width / 2f, anchor.y + lift * step, label.Width, label.Height);
                bool free = true;
                for (int i = 0; i < placed && free; i++)
                {
                    free = !_placed[i].Overlaps(rect);
                }

                if (free)
                {
                    return true;
                }
            }

            rect = default;
            return false;
        }

        private GroundLabel Take()
        {
            GroundLabel label;
            if (_pool.Count > 0)
            {
                label = _pool.Pop();
            }
            else
            {
                label = Instantiate(_template, _container);
                label.Clicked += HandleLabelClicked;
            }

            label.gameObject.SetActive(true);
            return label;
        }

        private void ReleaseStale()
        {
            int stale = 0;
            foreach (var pair in _byItem)
            {
                if (pair.Value.Stamp != _stamp && stale < _stale.Length)
                {
                    _stale[stale++] = pair.Value;
                }
            }

            for (int i = 0; i < stale; i++)
            {
                Release(_stale[i]);
                _stale[i] = null;
            }
        }

        private void ReleaseAll()
        {
            int count = 0;
            foreach (var pair in _byItem)
            {
                if (count < _stale.Length)
                {
                    _stale[count++] = pair.Value;
                }
            }

            for (int i = 0; i < count; i++)
            {
                Release(_stale[i]);
                _stale[i] = null;
            }
        }

        private void Release(GroundLabel label)
        {
            // anche un oggetto già distrutto ha lo stesso id: la chiave si toglie lo stesso
            _byItem.Remove(label.Item);
            label.Release();
            _pool.Push(label);
        }

        private void HandleLabelClicked(GroundLabel label)
        {
            if (label.Item != null && label.Item.TryGetComponent(out Interaction.Interactable interactable))
            {
                _controller.Use(interactable);
            }
        }

        private void OnDestroy()
        {
            foreach (var label in _pool)
            {
                label.Clicked -= HandleLabelClicked;
            }

            foreach (var label in _byItem.Values)
            {
                label.Clicked -= HandleLabelClicked;
            }
        }
    }
}
