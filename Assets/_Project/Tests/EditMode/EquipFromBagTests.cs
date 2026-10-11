using DarkDescent.Items;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Il click destro su un oggetto della borsa lo indossa (D1 della M10).</summary>
    public class EquipFromBagTests
    {
        private static ItemInstance Item(string name)
        {
            return new ItemInstance(AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset"));
        }

        private static Inventory NewInventory(float strength = 40f)
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.Strength, strength);
            return new Inventory(new InventoryGrid(10, 4), new Equipment(sheet));
        }

        [Test, Description("L'elmo nella borsa va in testa; quello di prima torna nel posto lasciato libero, con l'evento del lampo")]
        public void Helm_SwapsWithTheEquippedOne()
        {
            var inventory = NewInventory();
            var old = Item("KnightHelm");
            var hat = Item("BearHat");
            Assert.IsTrue(inventory.Equipment.TryEquip(old, out _));
            Assert.IsTrue(inventory.Grid.TryPlace(hat, new Vector2Int(3, 1)));
            int flashes = 0;
            inventory.Equipped += (slot, item) => flashes++;

            Assert.IsTrue(inventory.EquipFromGrid(new Vector2Int(4, 2)), "una cella qualsiasi dell'oggetto");
            Assert.AreSame(hat, inventory.Equipment.Get(EquipSlot.Helm));
            Assert.AreSame(old, inventory.Grid.ItemAt(new Vector2Int(3, 1)));
            Assert.AreEqual(1, flashes);
        }

        [Test, Description("Con la Forza che non basta l'oggetto resta nella borsa; una cella vuota non fa niente")]
        public void NotEnoughStrength_StaysInTheBag()
        {
            var inventory = NewInventory(30f);
            var plate = Item("DarkPlate");
            Assert.IsTrue(inventory.Grid.TryPlace(plate, new Vector2Int(0, 0)));

            Assert.IsFalse(inventory.EquipFromGrid(new Vector2Int(0, 0)));
            Assert.AreSame(plate, inventory.Grid.ItemAt(new Vector2Int(0, 0)));
            Assert.IsNull(inventory.Equipment.Get(EquipSlot.Body));
            Assert.IsFalse(inventory.EquipFromGrid(new Vector2Int(9, 3)));
        }

        [Test, Description("Un anello va nel posto libero; con tutti e due pieni sostituisce il primo")]
        public void Rings_FillTheFreeSlotThenReplaceTheFirst()
        {
            var inventory = NewInventory();
            var first = Item("Ring");
            var second = Item("Ring");
            var third = Item("Ring");
            Assert.IsTrue(inventory.Equipment.TryEquip(first, out _));
            Assert.IsTrue(inventory.Grid.TryPlace(second, new Vector2Int(0, 0)));
            Assert.IsTrue(inventory.Grid.TryPlace(third, new Vector2Int(1, 0)));

            Assert.IsTrue(inventory.EquipFromGrid(new Vector2Int(0, 0)));
            Assert.AreSame(second, inventory.Equipment.Get(EquipSlot.Ring2));
            Assert.IsNull(inventory.Grid.ItemAt(new Vector2Int(0, 0)));

            Assert.IsTrue(inventory.EquipFromGrid(new Vector2Int(1, 0)));
            Assert.AreSame(third, inventory.Equipment.Get(EquipSlot.Ring));
            Assert.AreSame(first, inventory.Grid.ItemAt(new Vector2Int(1, 0)));
        }

        [Test, Description("L'ascia indossata non entra dove stava il pugnale e la borsa è piena: non cambia niente")]
        public void NoRoomForTheOldItem_ChangesNothing()
        {
            var inventory = NewInventory();
            var axe = Item("Axe");
            var dagger = Item("Dagger");
            Assert.IsTrue(inventory.Equipment.TryEquip(axe, out _));
            Assert.IsTrue(inventory.Grid.TryPlace(dagger, new Vector2Int(0, 0)));
            while (inventory.Grid.TryAutoPlace(Item("Ring")))
            {
            }

            Assert.IsFalse(inventory.EquipFromGrid(new Vector2Int(0, 0)));
            Assert.AreSame(axe, inventory.Equipment.Get(EquipSlot.Weapon));
            Assert.AreSame(dagger, inventory.Grid.ItemAt(new Vector2Int(0, 1)));
        }

        [Test, Description("Nella borsa il tooltip di un oggetto da indossare dice come; quello di una pozione no")]
        public void Tooltip_SaysHowToEquip()
        {
            var localizer = new Localization.Localizer(Localization.StringTable.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv").text));
            var builder = new System.Text.StringBuilder();
            UI.ItemDescription.Write(builder, Item("BearHat"), true, localizer, equipHint: true);
            StringAssert.Contains("Right-click to equip", builder.ToString());
            UI.ItemDescription.Write(builder, Item("BearHat"), true, localizer);
            StringAssert.DoesNotContain("Right-click to equip", builder.ToString());
            UI.ItemDescription.Write(builder, Item("HealingPotion"), true, localizer, equipHint: true);
            StringAssert.DoesNotContain("Right-click to equip", builder.ToString());
        }
    }
}
