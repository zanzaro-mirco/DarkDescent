using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Quello che il cavaliere portava entrando in un livello (D13 della scheda M6): griglia con le
    /// posizioni, equipaggiamento e cintura. Si scrive in JSON con le istanze così come sono (ADR-022),
    /// quindi è una copia che nessun oggetto in gioco può cambiare, e alla M8 è già il formato del
    /// salvataggio. L'oggetto sul cursore va nella griglia. Logica pura.
    /// </summary>
    [Serializable]
    public sealed class InventorySnapshot
    {
        [SerializeField] private List<ItemInstance> _gridItems = new List<ItemInstance>();
        [SerializeField] private List<Vector2Int> _gridPositions = new List<Vector2Int>();
        [SerializeField] private List<ItemInstance> _equipped = new List<ItemInstance>();

        // lo slot di ogni oggetto indossato: con due anelli conta quale (M8). Vuota nelle istantanee
        // di prima, e allora decide l'equipaggiamento
        [SerializeField] private List<EquipSlot> _equippedSlots = new List<EquipSlot>();
        [SerializeField] private List<ItemInstance> _beltItems = new List<ItemInstance>();
        [SerializeField] private List<int> _beltSlots = new List<int>();
        [SerializeField] private List<ItemInstance> _loose = new List<ItemInstance>();

        public static InventorySnapshot Capture(Inventory inventory)
        {
            var snapshot = new InventorySnapshot();
            foreach (var pair in inventory.Grid.Placements)
            {
                snapshot._gridItems.Add(pair.Key);
                snapshot._gridPositions.Add(pair.Value.position);
            }

            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
            {
                var item = inventory.Equipment.Get(slot);
                if (item != null)
                {
                    snapshot._equipped.Add(item);
                    snapshot._equippedSlots.Add(slot);
                }
            }

            for (int slot = 0; slot < Belt.Size; slot++)
            {
                var item = inventory.Belt.Get(slot);
                if (item != null)
                {
                    snapshot._beltItems.Add(item);
                    snapshot._beltSlots.Add(slot);
                }
            }

            if (inventory.Held != null)
            {
                snapshot._loose.Add(inventory.Held);
            }

            return snapshot;
        }

        public static InventorySnapshot FromJson(string json)
        {
            return JsonUtility.FromJson<InventorySnapshot>(json);
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// Svuota l'inventario e ci rimette quello dell'istantanea, ritrovando definizioni e affissi
        /// dagli ID. Un oggetto che i database non conoscono più si perde; un'arma che non si può più
        /// equipaggiare va nella griglia. False se qualcosa non è tornato al suo posto.
        /// </summary>
        public bool Restore(Inventory inventory, ItemDatabase items, AffixDatabase affixes)
        {
            inventory.Clear();
            bool complete = true;

            // prima l'equipaggiamento: i suoi bonus possono servire ai requisiti degli altri oggetti
            for (int i = 0; i < _equipped.Count; i++)
            {
                var item = _equipped[i];
                if (!item.Resolve(items, affixes))
                {
                    complete = false;
                    continue;
                }

                bool equipped = i < _equippedSlots.Count
                    ? inventory.Equipment.TryEquip(item, _equippedSlots[i], out _)
                    : inventory.Equipment.TryEquip(item, out _);
                complete &= equipped || inventory.Grid.TryAutoPlace(item);
            }

            for (int i = 0; i < _gridItems.Count; i++)
            {
                var item = _gridItems[i];
                complete &= item.Resolve(items, affixes) && (inventory.Grid.TryPlace(item, _gridPositions[i]) || inventory.Grid.TryAutoPlace(item));
            }

            for (int i = 0; i < _beltItems.Count; i++)
            {
                var item = _beltItems[i];
                complete &= item.Resolve(items, affixes) && (inventory.Belt.TryPlaceOrSwap(_beltSlots[i], item, out _) || inventory.TryPickUp(item));
            }

            foreach (var item in _loose)
            {
                complete &= item.Resolve(items, affixes) && inventory.TryPickUp(item);
            }

            return complete;
        }
    }
}
