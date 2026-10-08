using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Tutto quello che il cavaliere porta: la griglia, l'equipaggiamento, la cintura e l'oggetto
    /// preso sul cursore. Qui sta il click-e-click di Diablo 1 (D5 della M4): un click prende, un
    /// click posa, e se sotto c'è un oggetto solo i due si scambiano. Logica pura: la UI la chiama e basta.
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

        /// <summary>Le pozioni a portata di tasto (D14 della M6).</summary>
        public Belt Belt { get; } = new Belt();

        /// <summary>L'oggetto preso con il cursore, o null.</summary>
        public ItemInstance Held { get; private set; }

        /// <summary>
        /// Raccoglie un oggetto da terra nel primo posto libero; false se non c'è spazio. Una pozione
        /// va nella cintura se c'è posto, altrimenti nella griglia (D14 della M6).
        /// </summary>
        public bool TryPickUp(ItemInstance item)
        {
            return Belt.TryAdd(item) || Grid.TryAutoPlace(item);
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

            if (!Equipment.TryEquip(Held, slot, out ItemInstance previous))
            {
                return false;
            }

            SetHeld(previous);
            return true;
        }

        /// <summary>
        /// Un click su un posto della cintura, come su una cella: a mani vuote prende la pozione,
        /// con una pozione in mano la posa e prende quella che c'era. Gli altri oggetti non ci vanno.
        /// </summary>
        public bool ClickBelt(int slot)
        {
            if (Held == null)
            {
                ItemInstance item = Belt.Take(slot);
                if (item == null)
                {
                    return false;
                }

                SetHeld(item);
                return true;
            }

            if (!Belt.TryPlaceOrSwap(slot, Held, out ItemInstance displaced))
            {
                return false;
            }

            SetHeld(displaced);
            return true;
        }

        /// <summary>Toglie la pozione di un posto della cintura per berla; null se lì non c'è una pozione.</summary>
        public PotionDefinition TakePotionFromBelt(int slot)
        {
            return Belt.Get(slot)?.Definition is PotionDefinition potion && Belt.Take(slot) != null ? potion : null;
        }

        /// <summary>Toglie la pozione che occupa una cella della griglia per berla; null se lì non c'è una pozione.</summary>
        public PotionDefinition TakePotionAt(Vector2Int cell)
        {
            ItemInstance item = Grid.ItemAt(cell);
            return item?.Definition is PotionDefinition potion && Grid.Remove(item) ? potion : null;
        }

        /// <summary>Toglie tutto, cursore compreso: per rimettere un'istantanea (D13 della M6).</summary>
        public void Clear()
        {
            if (Held != null)
            {
                SetHeld(null);
            }

            foreach (var item in new List<ItemInstance>(Grid.Placements.Keys))
            {
                Grid.Remove(item);
            }

            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
            {
                if (slot != EquipSlot.None)
                {
                    Equipment.Unequip(slot);
                }
            }

            for (int slot = 0; slot < Belt.Size; slot++)
            {
                Belt.Take(slot);
            }
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

        /// <summary>
        /// Rimette a posto l'oggetto preso, se c'è spazio (per esempio chiudendo l'inventario): come
        /// raccogliendolo, una pozione torna prima nella cintura.
        /// </summary>
        public bool TryStoreHeld()
        {
            if (Held == null || !TryPickUp(Held))
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
