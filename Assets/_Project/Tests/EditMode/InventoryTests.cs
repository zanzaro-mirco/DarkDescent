using DarkDescent.Items;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class InventoryTests
    {
        private StatSheet _stats;
        private Inventory _inventory;

        private static ItemInstance Load(string name) => new ItemInstance(AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset"));

        [SetUp]
        public void SetUp()
        {
            _stats = new StatSheet();
            _stats.SetBase(StatType.Strength, 30f);
            _inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(_stats));
        }

        [Test, Description("Click su un oggetto: va sul cursore e lascia la griglia; click su una cella libera: torna giù lì")]
        public void ClickCell_PicksThenPlaces()
        {
            var shield = Load("BadgeShield");
            _inventory.Grid.TryPlace(shield, new Vector2Int(0, 0));

            Assert.IsTrue(_inventory.ClickCell(new Vector2Int(1, 1)));
            Assert.AreSame(shield, _inventory.Held);
            Assert.IsNull(_inventory.Grid.ItemAt(new Vector2Int(0, 0)));

            Assert.IsTrue(_inventory.ClickCell(new Vector2Int(5, 2)));
            Assert.IsNull(_inventory.Held);
            // 2×2 centrato sulla cella cliccata: angolo in alto a sinistra sulla stessa cella
            Assert.AreEqual(new Vector2Int(5, 2), _inventory.Grid.Placements[shield].position);
        }

        [Test, Description("L'oggetto posato vicino al bordo si sposta dentro la griglia")]
        public void ClickCell_ClampsInsideGrid()
        {
            var sword = Load("ShortSword");
            _inventory.Grid.TryPlace(sword, new Vector2Int(0, 0));
            _inventory.ClickCell(new Vector2Int(0, 0));

            Assert.IsTrue(_inventory.ClickCell(new Vector2Int(9, 3)));

            Assert.AreEqual(new Vector2Int(9, 1), _inventory.Grid.Placements[sword].position);
        }

        [Test, Description("Posato sopra un altro oggetto, i due si scambiano: quello di sotto va sul cursore")]
        public void ClickCell_Swaps()
        {
            var sword = Load("ShortSword");
            var shield = Load("BadgeShield");
            _inventory.Grid.TryPlace(sword, new Vector2Int(0, 0));
            _inventory.Grid.TryPlace(shield, new Vector2Int(4, 0));
            _inventory.ClickCell(new Vector2Int(0, 1));

            Assert.IsTrue(_inventory.ClickCell(new Vector2Int(4, 1)));

            Assert.AreSame(shield, _inventory.Held);
            Assert.AreSame(sword, _inventory.Grid.ItemAt(new Vector2Int(4, 1)));
        }

        [Test, Description("Dal cursore allo slot giusto si equipaggia, e quello di prima sale sul cursore")]
        public void ClickSlot_EquipsAndReturnsPrevious()
        {
            var sword = Load("ShortSword");
            var blade = Load("SkeletonBlade");
            _inventory.Equipment.TryEquip(sword, out _);
            _inventory.Grid.TryPlace(blade, new Vector2Int(0, 0));
            _inventory.ClickCell(new Vector2Int(0, 0));

            Assert.IsTrue(_inventory.ClickSlot(EquipSlot.Weapon));

            Assert.AreSame(blade, _inventory.Equipment.Get(EquipSlot.Weapon));
            Assert.AreSame(sword, _inventory.Held);
        }

        [Test, Description("Uno scudo non va nello slot dell'arma, e un'arma troppo pesante non si equipaggia: il cursore resta com'era")]
        public void ClickSlot_RefusesWrongSlotOrRequirements()
        {
            var shield = Load("BadgeShield");
            _inventory.Grid.TryPlace(shield, new Vector2Int(0, 0));
            _inventory.ClickCell(new Vector2Int(0, 0));
            Assert.IsFalse(_inventory.ClickSlot(EquipSlot.Weapon));
            Assert.AreSame(shield, _inventory.Held);

            _stats.SetBase(StatType.Strength, 10f);
            _inventory.ClickCell(new Vector2Int(0, 0));
            var blade = Load("SkeletonBlade");
            _inventory.Grid.TryPlace(blade, new Vector2Int(5, 0));
            _inventory.ClickCell(new Vector2Int(5, 0));
            Assert.IsFalse(_inventory.ClickSlot(EquipSlot.Weapon), "Forza 10 contro 25");
            Assert.AreSame(blade, _inventory.Held);
        }

        [Test, Description("A mani vuote, un click sullo slot toglie l'oggetto e lo mette sul cursore")]
        public void ClickSlot_EmptyHanded_Unequips()
        {
            var shield = Load("BadgeShield");
            _inventory.Equipment.TryEquip(shield, out _);

            Assert.IsTrue(_inventory.ClickSlot(EquipSlot.Offhand));

            Assert.AreSame(shield, _inventory.Held);
            Assert.IsNull(_inventory.Equipment.Get(EquipSlot.Offhand));
            Assert.AreEqual(0f, _stats.Get(StatType.Armor));
        }

        [Test, Description("HeldChanged scatta quando il cursore prende o lascia")]
        public void HeldChanged_Raised()
        {
            int changes = 0;
            _inventory.HeldChanged += () => changes++;
            _inventory.Grid.TryPlace(Load("ShortSword"), new Vector2Int(0, 0));

            _inventory.ClickCell(new Vector2Int(0, 0));
            _inventory.ReleaseHeld();
            _inventory.ReleaseHeld();

            Assert.AreEqual(2, changes);
        }
    }
}
