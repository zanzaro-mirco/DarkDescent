using System.Collections.Generic;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.Player;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// L'inventario a schermo, nella metà destra (D8 della M4): griglia, slot di arma e scudo, e il
    /// fondo trasparente che con un oggetto sul cursore lo lascia a terra. Mostra e inoltra i click:
    /// la logica sta in <see cref="Inventory"/>. Si ridisegna sugli eventi, mai in Update.
    /// </summary>
    [DisallowMultipleComponent]
    public class InventoryPanel : MonoBehaviour
    {
        [Tooltip("La finestra da aprire e chiudere; il componente sta sul padre, sempre attivo.")]
        [SerializeField] private GameObject _window;

        [Tooltip("L'area della griglia, con il pivot in alto a sinistra.")]
        [SerializeField] private RectTransform _grid;

        [Tooltip("Immagine modello per gli oggetti nella griglia, spenta: se ne fanno copie.")]
        [SerializeField] private Image _itemTemplate;

        [SerializeField] private EquipmentSlotView _weaponSlot;
        [SerializeField] private EquipmentSlotView _offhandSlot;

        [Tooltip("Fondo trasparente a tutto schermo, dietro le finestre: acceso solo con un oggetto sul cursore.")]
        [SerializeField] private GameObject _dropCatcher;

        [Tooltip("La descrizione dell'oggetto sotto il cursore.")]
        [SerializeField] private ItemTooltip _tooltip;

        [SerializeField, Min(8f)] private float _cellSize = 56f;

        private readonly List<Image> _itemViews = new List<Image>();
        private readonly Dictionary<ItemInstance, Image> _viewByItem = new Dictionary<ItemInstance, Image>();
        private Vector2Int? _hoveredCell;
        private EquipSlot _hoveredSlot = EquipSlot.None;
        private PlayerInventory _inventory;
        private PlayerInputReader _reader;
        private Localizer _localizer;
        private bool _subscribed;

        public bool IsOpen => _window.activeSelf;

        public float CellSize => _cellSize;

        /// <summary>Le immagini degli oggetti nella griglia attualmente visibili.</summary>
        public int VisibleItemCount
        {
            get
            {
                int count = 0;
                foreach (var view in _itemViews)
                {
                    if (view.gameObject.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Come la sfera: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(PlayerInventory inventory, PlayerInputReader reader, Localizer localizer)
        {
            Unsubscribe();
            _inventory = inventory;
            _reader = reader;
            _localizer = localizer;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void Toggle()
        {
            SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            if (!open && _inventory != null)
            {
                // un oggetto rimasto sul cursore torna nella griglia, o a terra se non c'è posto
                _inventory.PutAwayHeld();
            }

            _window.SetActive(open);

            // una finestra spenta non riceve l'uscita del cursore: il tooltip si dimentica qui
            _hoveredCell = null;
            _hoveredSlot = EquipSlot.None;
            RefreshDropCatcher();
            RefreshTooltip();
        }

        public void ClickCell(Vector2Int cell)
        {
            _inventory.Inventory.ClickCell(cell);
        }

        public void ClickSlot(EquipSlot slot)
        {
            _inventory.Inventory.ClickSlot(slot);
        }

        /// <summary>Il cursore è entrato in una cella della griglia, o ne è uscito (null).</summary>
        public void HoverCell(Vector2Int? cell)
        {
            if (cell == _hoveredCell)
            {
                return;
            }

            _hoveredCell = cell;
            RefreshTooltip();
        }

        /// <summary>Il cursore è entrato in uno slot, o ne è uscito (<see cref="EquipSlot.None"/>).</summary>
        public void HoverSlot(EquipSlot slot)
        {
            if (slot == _hoveredSlot)
            {
                return;
            }

            _hoveredSlot = slot;
            RefreshTooltip();
        }

        /// <summary>Click fuori dalle finestre con un oggetto sul cursore: lo lascia a terra.</summary>
        public void ClickOutside()
        {
            _inventory.DropHeld();
        }

        private void Awake()
        {
            _window.SetActive(false);
            _dropCatcher.SetActive(false);
            _itemTemplate.gameObject.SetActive(false);
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

            _reader.InventoryToggled += Toggle;
            _inventory.Inventory.Grid.Changed += RefreshGrid;
            _inventory.Equipment.Changed += RefreshSlot;
            _inventory.Inventory.HeldChanged += HandleHeldChanged;
            _localizer.LanguageChanged += RefreshTooltip;
            _subscribed = true;

            RefreshGrid();
            RefreshSlot(EquipSlot.Weapon);
            RefreshSlot(EquipSlot.Offhand);
            RefreshDropCatcher();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _reader.InventoryToggled -= Toggle;
            _inventory.Inventory.Grid.Changed -= RefreshGrid;
            _inventory.Equipment.Changed -= RefreshSlot;
            _inventory.Inventory.HeldChanged -= HandleHeldChanged;
            _localizer.LanguageChanged -= RefreshTooltip;
            _subscribed = false;
        }

        private void RefreshGrid()
        {
            int index = 0;
            _viewByItem.Clear();
            foreach (var pair in _inventory.Inventory.Grid.Placements)
            {
                // le immagini si riusano: se ne crea una nuova solo quando gli oggetti aumentano
                if (index == _itemViews.Count)
                {
                    _itemViews.Add(Instantiate(_itemTemplate, _grid));
                }

                Image view = _itemViews[index++];
                RectInt area = pair.Value;
                var rect = view.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(area.x * _cellSize, -area.y * _cellSize);
                rect.sizeDelta = new Vector2(area.width * _cellSize, area.height * _cellSize);
                view.sprite = pair.Key.Definition.Icon;
                view.gameObject.SetActive(true);
                _viewByItem[pair.Key] = view;
            }

            for (int i = index; i < _itemViews.Count; i++)
            {
                _itemViews[i].gameObject.SetActive(false);
            }

            RefreshTooltip();
        }

        private void RefreshSlot(EquipSlot slot)
        {
            var view = SlotView(slot);
            if (view != null)
            {
                view.Show(_inventory.Equipment.Get(slot));
            }

            RefreshTooltip();
        }

        private EquipmentSlotView SlotView(EquipSlot slot)
        {
            return slot == EquipSlot.Weapon ? _weaponSlot : slot == EquipSlot.Offhand ? _offhandSlot : null;
        }

        private void HandleHeldChanged()
        {
            RefreshDropCatcher();
            RefreshTooltip();
        }

        private void RefreshDropCatcher()
        {
            _dropCatcher.SetActive(IsOpen && _inventory != null && _inventory.Inventory.Held != null);
        }

        // il tooltip descrive l'oggetto sotto il cursore, ma non mentre se ne tiene uno: coprirebbe
        // la cella dove posarlo
        private void RefreshTooltip()
        {
            ItemInstance item = null;
            RectTransform target = null;
            if (IsOpen && _inventory != null && _inventory.Inventory.Held == null)
            {
                if (_hoveredCell.HasValue)
                {
                    // l'immagine manca per un attimo se l'oggetto sul cursore cambia prima della griglia:
                    // il ridisegno della griglia richiama questo metodo
                    item = _inventory.Inventory.Grid.ItemAt(_hoveredCell.Value);
                    if (item != null && _viewByItem.TryGetValue(item, out Image view))
                    {
                        target = view.rectTransform;
                    }
                    else
                    {
                        item = null;
                    }
                }
                else if (_hoveredSlot != EquipSlot.None)
                {
                    item = _inventory.Equipment.Get(_hoveredSlot);
                    target = (RectTransform)SlotView(_hoveredSlot).transform;
                }
            }

            if (item == null)
            {
                _tooltip.Hide();
                return;
            }

            _tooltip.Show(item.Definition, _inventory.Equipment.MeetsRequirements(item.Definition), _localizer, target);
        }
    }
}
