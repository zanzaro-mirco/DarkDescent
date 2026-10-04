using DarkDescent.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>Uno slot dell'equipaggiamento: mostra l'icona di quello che c'è e inoltra al pannello pressione e passaggio del cursore.</summary>
    [DisallowMultipleComponent]
    public class EquipmentSlotView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private InventoryPanel _panel;

        [SerializeField] private EquipSlot _slot;

        [Tooltip("L'icona dell'oggetto, senza bersaglio dei raggi: la pressione la prende lo sfondo dello slot.")]
        [SerializeField] private Image _icon;

        public EquipSlot Slot => _slot;

        public bool HasIcon => _icon.enabled;

        public void Show(ItemInstance item)
        {
            _icon.sprite = item?.Definition.Icon;
            _icon.enabled = item != null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _panel.ClickSlot(_slot);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _panel.HoverSlot(_slot);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _panel.HoverSlot(EquipSlot.None);
        }
    }
}
