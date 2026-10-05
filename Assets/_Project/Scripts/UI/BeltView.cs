using DarkDescent.Items;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkDescent.UI
{
    /// <summary>
    /// La cintura a schermo, sotto la sfera della vita (D14 della scheda M6): otto posti con il tasto
    /// scritto nell'angolo. Click destro su un posto beve; con l'inventario aperto il click sinistro
    /// prende e posa le pozioni, come nella griglia. Si ridisegna sugli eventi della cintura.
    /// </summary>
    [DisallowMultipleComponent]
    public class BeltView : MonoBehaviour
    {
        [Tooltip("Gli otto posti, da sinistra: il primo è il tasto 1.")]
        [SerializeField] private BeltSlotView[] _slots = new BeltSlotView[Belt.Size];

        [Tooltip("Il click sinistro sulla cintura vale solo con l'inventario aperto: altrimenti l'oggetto preso non avrebbe dove andare.")]
        [SerializeField] private InventoryPanel _inventoryPanel;

        private PlayerInventory _inventory;
        private bool _subscribed;

        /// <summary>Il posto a schermo, da 0: per i test.</summary>
        public BeltSlotView Slot(int index)
        {
            return _slots[index];
        }

        /// <summary>Come la sfera: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(PlayerInventory inventory)
        {
            Unsubscribe();
            _inventory = inventory;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        /// <summary>Un posto premuto con un tasto del mouse.</summary>
        public void ClickSlot(int slot, PointerEventData.InputButton button)
        {
            if (_inventory == null)
            {
                return;
            }

            if (button == PointerEventData.InputButton.Right)
            {
                _inventory.DrinkFromBelt(slot);
            }
            else if (button == PointerEventData.InputButton.Left && _inventoryPanel.IsOpen)
            {
                _inventory.Inventory.ClickBelt(slot);
            }
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

            _inventory.Belt.Changed += Refresh;
            _subscribed = true;
            for (int slot = 0; slot < _slots.Length; slot++)
            {
                Refresh(slot);
            }
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _inventory.Belt.Changed -= Refresh;
            _subscribed = false;
        }

        private void Refresh(int slot)
        {
            _slots[slot].Show(_inventory.Belt.Get(slot));
        }
    }
}
