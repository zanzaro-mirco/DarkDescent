using System.Linq;
using DarkDescent.Items;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class InventorySnapshotTests
    {
        private static ItemDatabase Items => AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
        private static AffixDatabase Affixes => AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");
        private static ItemDefinition Load(string name) => AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset");

        private static Inventory NewInventory()
        {
            var stats = new StatSheet();
            stats.SetBase(StatType.Strength, 30f);
            return new Inventory(new InventoryGrid(10, 4), new Equipment(stats));
        }

        private static string Describe(Inventory inventory)
        {
            var grid = inventory.Grid.Placements
                .Select(p => $"{p.Key.Definition.name}@{p.Value.position}:{p.Key.Rarity}[{string.Join(",", p.Key.Affixes.Select(a => a.AffixId + "=" + a.Value))}]")
                .OrderBy(s => s);
            string equipment = $"{inventory.Equipment.Get(EquipSlot.Weapon)?.Definition.name}/{inventory.Equipment.Get(EquipSlot.Offhand)?.Definition.name}";
            string belt = string.Join(",", Enumerable.Range(0, Belt.Size).Select(i => inventory.Belt.Get(i)?.Definition.name ?? "-"));
            return string.Join(" ", grid) + " | " + equipment + " | " + belt;
        }

        [Test, Description("Un'istantanea passata per JSON rimette griglia con le posizioni, equipaggiamento, cintura e affissi; quello raccolto dopo sparisce")]
        public void Snapshot_RoundTripsThroughJson()
        {
            var inventory = NewInventory();
            inventory.Equipment.TryEquip(new ItemInstance(Load("ShortSword")), out _);
            inventory.Equipment.TryEquip(new ItemInstance(Load("BadgeShield")), out _);
            var magic = new ItemInstance(Load("Axe"), Rarity.Magic, 2, 99UL, new[] { new ItemAffix(Affixes.Affixes[0], 7) });
            inventory.Grid.TryPlace(magic, new Vector2Int(4, 1));
            inventory.Grid.TryPlace(new ItemInstance(Load("Dagger")), new Vector2Int(0, 0));
            inventory.Belt.TryPlaceOrSwap(3, new ItemInstance(Load("HealingPotion")), out _);
            string before = Describe(inventory);

            string json = InventorySnapshot.Capture(inventory).ToJson();

            // dopo l'ingresso: si beve, si raccoglie, si cambia arma
            inventory.Belt.Take(3);
            inventory.TryPickUp(new ItemInstance(Load("RoundShield")));
            inventory.Equipment.Unequip(EquipSlot.Weapon);
            Assert.AreNotEqual(before, Describe(inventory));

            Assert.IsTrue(InventorySnapshot.FromJson(json).Restore(inventory, Items, Affixes));

            Assert.AreEqual(before, Describe(inventory));
            Assert.AreEqual(Load("ShortSword"), inventory.Equipment.Get(EquipSlot.Weapon).Definition);
            StringAssert.Contains(magic.Affixes[0].AffixId, json, "il JSON porta gli ID, non i riferimenti");
            Assert.IsNotNull(inventory.Grid.ItemAt(new Vector2Int(4, 1)).Affixes[0].Definition, "gli affissi ritrovano la definizione");
        }

        [Test, Description("L'oggetto sul cursore all'ingresso torna nell'inventario; quello sul cursore al momento di ricominciare sparisce")]
        public void Snapshot_HeldItems()
        {
            var inventory = NewInventory();
            inventory.Grid.TryPlace(new ItemInstance(Load("Dagger")), new Vector2Int(2, 2));
            inventory.ClickCell(new Vector2Int(2, 2));
            Assert.IsNotNull(inventory.Held);

            string json = InventorySnapshot.Capture(inventory).ToJson();
            inventory.ReleaseHeld();
            inventory.Grid.TryPlace(new ItemInstance(Load("Axe")), new Vector2Int(0, 0));
            inventory.ClickCell(new Vector2Int(0, 0));

            InventorySnapshot.FromJson(json).Restore(inventory, Items, Affixes);

            Assert.IsNull(inventory.Held);
            Assert.AreEqual(1, inventory.Grid.Placements.Count);
            Assert.AreEqual(Load("Dagger"), inventory.Grid.Placements.Keys.First().Definition);
        }
    }
}
