using DarkDescent.Items;
using DarkDescent.Player;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// L'oggetto preso, che segue il mouse. L'immagine non è bersaglio dei raggi (trappola 4 della
    /// M4): altrimenti prenderebbe lei la pressione destinata alla cella sotto.
    /// </summary>
    [DisallowMultipleComponent]
    public class ItemCursor : MonoBehaviour
    {
        [SerializeField] private Image _image;

        [Tooltip("Lato di una cella in pixel di riferimento: l'oggetto preso ha la stessa misura che nella griglia.")]
        [SerializeField, Min(8f)] private float _cellSize = 56f;

        private PlayerInventory _inventory;
        private PlayerInputReader _reader;
        private RectTransform _container;
        private Canvas _canvas;
        private bool _subscribed;

        public bool IsShowing => _image.gameObject.activeSelf;

        public void Bind(PlayerInventory inventory, PlayerInputReader reader)
        {
            Unsubscribe();
            _inventory = inventory;
            _reader = reader;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void Awake()
        {
            _container = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>().rootCanvas;
            _image.raycastTarget = false;
            _image.gameObject.SetActive(false);
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
            if (_subscribed || _inventory == null)
            {
                return;
            }

            _inventory.Inventory.HeldChanged += Refresh;
            _subscribed = true;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _inventory.Inventory.HeldChanged -= Refresh;
            _subscribed = false;
        }

        private void Refresh()
        {
            ItemInstance held = _inventory.Inventory.Held;
            if (held == null)
            {
                _image.gameObject.SetActive(false);
                return;
            }

            _image.sprite = held.Definition.Icon;
            _image.rectTransform.sizeDelta = (Vector2)held.Definition.Size * _cellSize;
            _image.gameObject.SetActive(true);
            Follow();
        }

        // seguire il mouse è posizione, non dati: l'unico lavoro per frame, e solo con un oggetto preso
        private void LateUpdate()
        {
            if (IsShowing)
            {
                Follow();
            }
        }

        private void Follow()
        {
            Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_container, _reader.PointerScreenPosition, uiCamera, out Vector2 local))
            {
                _image.rectTransform.anchoredPosition = local;
            }
        }
    }
}
