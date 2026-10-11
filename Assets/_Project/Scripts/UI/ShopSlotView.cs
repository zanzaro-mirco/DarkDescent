using DarkDescent.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Un posto del banco del mercante: sfondo del colore della rarità, icona e prezzo sotto. Inoltra
    /// click e cursore al <see cref="ShopPanel"/>; da solo non decide niente.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShopSlotView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private ShopPanel _panel;

        [Tooltip("Il posto sul banco: le provviste prima, poi gli oggetti.")]
        [SerializeField, Min(0)] private int _index;

        [Tooltip("Lo sfondo, nel colore della rarità: prende click e cursore.")]
        [SerializeField] private Image _background;

        [Tooltip("L'icona, senza bersaglio dei raggi.")]
        [SerializeField] private Image _icon;

        [SerializeField] private TMP_Text _price;

        private Color _emptyColor;

        public int Index => _index;

        public bool HasItem => _icon.enabled;

        public string PriceText => _price.text;

        public RectTransform Rect => (RectTransform)transform;

        private void Awake()
        {
            _emptyColor = _background.color;
        }

        /// <summary>L'oggetto in vendita e il suo prezzo, rosso se l'oro non basta; null spegne il posto.</summary>
        public void Show(ItemInstance item, int price, bool affordable)
        {
            _icon.enabled = item != null;
            _price.enabled = item != null;
            if (item == null)
            {
                _background.color = _emptyColor;
                return;
            }

            _icon.sprite = item.Definition.Icon;
            _background.color = RarityColors.Cell(item.Rarity);
            _price.color = affordable ? ShopPanel.GoldColor : ShopPanel.UnaffordableColor;
            _price.SetText("{0}", price);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _panel.Buy(_index);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _panel.Hover(_index);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _panel.Hover(-1);
        }
    }
}
