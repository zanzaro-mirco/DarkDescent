using System;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Tutto quello che il cavaliere porta: la griglia, l'equipaggiamento e l'oggetto preso sul
    /// cursore. Qui sta il click-e-click di Diablo 1 (D5 della M4): un click prende, un click posa,
    /// e se sotto c'è un oggetto solo i due si scambiano. Logica pura: la UI la chiama e basta.
    /// </summary>
    public sealed class Inventory
    {
        public Inventory(InventoryGrid grid, Equipment equipment)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        /// <summary>L'oggetto sul cursore è cambiato (preso, posato, scambiato, lasciato a terra).</summary>
        public event Action HeldChanged;

        public InventoryGrid Grid { get; }

        public Equipment Equipment { get; }

        /// <summary>L'oggetto preso con il cursore, o null.</summary>
        public ItemInstance Held { get; private set; }

        /// <summary>Raccoglie un oggetto da terra nel primo posto libero; false se non c'è spazio.</summary>
        public bool TryPickUp(ItemInstance item)
        {
            return Grid.TryAutoPlace(item);
        }

        /// <summary>
        /// Un click su una cella. A mani vuote prende l'oggetto che c'è; con un oggetto in mano lo
        /// posa centrato sulla cella, spostandolo dentro i bordi, e prende quello che c'era sotto.
        /// </summary>
        public bool ClickCell(Vector2Int cell)
        {
            if (Held == null)
            {
                ItemInstance item = Grid.ItemAt(cell);
                if (item == null)
                {
                    return false;
                }

                Grid.Remove(item);
                SetHeld(item);
                return true;
            }

            if (!Grid.TryPlaceOrSwap(Held, TopLeftFor(Held, cell), out ItemInstance displaced))
            {
                return false;
            }

            SetHeld(displaced);
            return true;
        }

        /// <summary>
        /// Un click su uno slot. A mani vuote toglie l'oggetto equipaggiato e lo prende; con un
        /// oggetto in mano lo equipaggia, se è dello slot giusto e i requisiti bastano.
        /// </summary>
        public bool ClickSlot(EquipSlot slot)
        {
            if (Held == null)
            {
                ItemInstance item = Equipment.Unequip(slot);
                if (item == null)
                {
                    return false;
                }

                SetHeld(item);
                return true;
            }

            if (Held.Definition.Slot != slot || !Equipment.TryEquip(Held, out ItemInstance previous))
            {
                return false;
            }

            SetHeld(previous);
            return true;
        }

        /// <summary>Lascia l'oggetto preso: chi lo chiama lo mette a terra.</summary>
        public ItemInstance ReleaseHeld()
        {
            ItemInstance item = Held;
            if (item != null)
            {
                SetHeld(null);
            }

            return item;
        }

        /// <summary>Rimette nella griglia l'oggetto preso, se c'è posto (per esempio chiudendo l'inventario).</summary>
        public bool TryStoreHeld()
        {
            if (Held == null || !Grid.TryAutoPlace(Held))
            {
                return false;
            }

            SetHeld(null);
            return true;
        }

        /// <summary>L'angolo in alto a sinistra per posare l'oggetto centrato sulla cella, dentro la griglia.</summary>
        public Vector2Int TopLeftFor(ItemInstance item, Vector2Int cell)
        {
            Vector2Int size = item.Definition.Size;
            int x = Mathf.Clamp(cell.x - (size.x - 1) / 2, 0, Mathf.Max(0, Grid.Width - size.x));
            int y = Mathf.Clamp(cell.y - (size.y - 1) / 2, 0, Mathf.Max(0, Grid.Height - size.y));
            return new Vector2Int(x, y);
        }

        private void SetHeld(ItemInstance item)
        {
            Held = item;
            HeldChanged?.Invoke();
        }
    }
}
