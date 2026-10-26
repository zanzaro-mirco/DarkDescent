using System;
using DarkDescent.Items;
using DarkDescent.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// La finestra del mercante (D5 della M10), nella metà sinistra al posto del pannello del
    /// personaggio, con l'inventario aperto accanto: click su un posto del banco per comprare, click
    /// destro su un oggetto della borsa per vendere. La logica sta in <see cref="Shop"/>; la finestra
    /// si ridisegna sugli eventi, mai in Update.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShopPanel : MonoBehaviour
    {
        /// <summary>Il colore dei prezzi: l'oro dell'inventario.</summary>
        public static readonly Color GoldColor = new Color(0.88f, 0.75f, 0.38f);

        /// <summary>Il prezzo che l'oro non basta a pagare.</summary>
        public static readonly Color UnaffordableColor = new Color(0.9f, 0.36f, 0.33f);

        [Tooltip("La finestra da aprire e chiudere; il componente sta sul padre, sempre attivo.")]
        [SerializeField] private GameObject _window;

        [Tooltip("I posti del banco, in ordine: le provviste prima, poi gli oggetti.")]
        [SerializeField] private ShopSlotView[] _slots;

        [Tooltip("La riga che dice perché un acquisto non è riuscito.")]
        [SerializeField] private TMP_Text _message;

        [SerializeField] private Button _closeButton;

        [Tooltip("Il tooltip degli oggetti, lo stesso dell'inventario.")]
        [SerializeField] private ItemTooltip _tooltip;

        [SerializeField] private AudioSource _audio;

        [Tooltip("Le monete di un acquisto o di una vendita.")]
        [SerializeField] private AudioClip _coinClip;

        private Shop _shop;
        private PlayerInventory _inventory;
        private InventoryPanel _inventoryPanel;
        private Localizer _localizer;
        private bool _subscribed;
        private int _hovered = -1;

        // il tooltip è condiviso con l'inventario: il banco spegne solo quello che ha acceso lui
        private bool _showingTooltip;

        /// <summary>Aperta o chiusa.</summary>
        public event Action<bool> OpenChanged;

        public bool IsOpen => _window.activeSelf;

        public string MessageText => _message.text;

        public Button CloseButton => _closeButton;

        public int SlotCount => _slots.Length;

        public ShopSlotView Slot(int index) => _slots[index];

        /// <summary>La cosa in vendita al posto <paramref name="index"/>, o null.</summary>
        public ItemInstance ItemAt(int index) => _shop?.ItemAt(index);

        /// <summary>Come gli altri pannelli: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(Shop shop, PlayerInventory inventory, InventoryPanel inventoryPanel, Localizer localizer)
        {
            Unsubscribe();
            _shop = shop;
            _inventory = inventory;
            _inventoryPanel = inventoryPanel;
            _localizer = localizer;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        /// <summary>Apre il banco con l'inventario accanto, dove si vende.</summary>
        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            _window.SetActive(true);
            _message.text = string.Empty;
            _inventoryPanel.SetOpen(true);
            _inventoryPanel.SetTrade(this);
            Refresh();
            OpenChanged?.Invoke(true);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _window.SetActive(false);
            _hovered = -1;
            _tooltip.Hide();
            _inventoryPanel.HideComparison();
            _showingTooltip = false;
            _inventoryPanel.SetTrade(null);
            OpenChanged?.Invoke(false);
        }

        /// <summary>Compra la cosa al posto <paramref name="index"/>; se non riesce lo dice sotto il banco.</summary>
        public void Buy(int index)
        {
            if (_inventory.Inventory.Held != null)
            {
                return;
            }

            var result = _shop.Buy(_inventory.Inventory, index);
            switch (result)
            {
                case TradeResult.Done:
                    _message.text = string.Empty;
                    Play();
                    break;
                case TradeResult.NotEnoughGold:
                    _message.text = _localizer.Get(TextKeys.ShopNoGold);
                    break;
                case TradeResult.NoRoom:
                    _message.text = _localizer.Get(TextKeys.ShopNoRoom);
                    break;
            }

            RefreshTooltip();
        }

        /// <summary>Vende l'oggetto della borsa in quella cella: lo chiama l'inventario, al click destro.</summary>
        public bool SellAt(Vector2Int cell)
        {
            if (_shop.SellAt(_inventory.Inventory, cell) != TradeResult.Done)
            {
                return false;
            }

            _message.text = string.Empty;
            Play();
            return true;
        }

        /// <summary>Le righe del prezzo di vendita, per il tooltip di un oggetto della borsa.</summary>
        public string SellText(ItemInstance item)
        {
            return ItemDescription.TradeText(_localizer, TextKeys.ShopSellPrice, ItemPricing.SellPrice(item), true, TextKeys.ShopSellHint);
        }

        /// <summary>Il cursore è entrato nel posto <paramref name="index"/>, o ne è uscito (-1).</summary>
        public void Hover(int index)
        {
            _hovered = index;
            RefreshTooltip();
        }

        private void Awake()
        {
            _window.SetActive(false);
            _message.text = string.Empty;
        }

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(Close);
            Subscribe();
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(Close);
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _shop == null)
            {
                return;
            }

            _shop.Changed += Refresh;
            _inventory.Inventory.GoldChanged += Refresh;
            _localizer.LanguageChanged += Refresh;
            _subscribed = true;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _shop.Changed -= Refresh;
            _inventory.Inventory.GoldChanged -= Refresh;
            _localizer.LanguageChanged -= Refresh;
            _subscribed = false;
        }

        private void Play()
        {
            if (_audio != null && _coinClip != null)
            {
                _audio.PlayOneShot(_coinClip);
            }
        }

        private void Refresh()
        {
            if (!IsOpen)
            {
                return;
            }

            int gold = _inventory.Inventory.Gold;
            for (int i = 0; i < _slots.Length; i++)
            {
                var item = _shop.ItemAt(i);
                int price = item != null ? ItemPricing.Price(item) : 0;
                _slots[i].Show(item, price, price <= gold);
            }

            RefreshTooltip();
        }

        private void RefreshTooltip()
        {
            var item = IsOpen && _hovered >= 0 ? _shop.ItemAt(_hovered) : null;
            if (item == null)
            {
                if (_showingTooltip)
                {
                    _tooltip.Hide();
                    _inventoryPanel.HideComparison();
                    _showingTooltip = false;
                }

                return;
            }

            int price = ItemPricing.Price(item);
            string trade = ItemDescription.TradeText(_localizer, TextKeys.ShopPrice, price, price <= _inventory.Inventory.Gold, TextKeys.ShopBuyHint);
            _tooltip.Show(item, _inventory.Equipment.MeetsRequirements(item.Definition), _localizer, _slots[_hovered].Rect, trade: trade, equipment: _inventory.Equipment);

            // accanto, quello che si toglierebbe indossandolo, come dalla borsa
            _inventoryPanel.ShowComparison(item);
            _showingTooltip = true;
        }
    }
}
