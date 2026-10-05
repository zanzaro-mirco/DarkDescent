using DarkDescent.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>Un posto della cintura: mostra l'icona della pozione e inoltra alla cintura la pressione, con il tasto del mouse.</summary>
    [DisallowMultipleComponent]
    public class BeltSlotView : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private BeltView _belt;

        [SerializeField, Range(0, Belt.Size - 1)] private int _slot;

        [Tooltip("L'icona della pozione, senza bersaglio dei raggi: la pressione la prende lo sfondo del posto.")]
        [SerializeField] private Image _icon;

        public int Index => _slot;

        public bool HasIcon => _icon.enabled;

        public void Show(ItemInstance item)
        {
            _icon.sprite = item?.Definition.Icon;
            _icon.enabled = item != null;
        }

        // alla pressione, come la griglia: bere deve essere immediato
        public void OnPointerDown(PointerEventData eventData)
        {
            _belt.ClickSlot(_slot, eventData.button);
        }
    }
}
