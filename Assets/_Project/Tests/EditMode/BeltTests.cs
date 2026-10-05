using System.Collections.Generic;
using DarkDescent.Items;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class BeltTests
    {
        private Inventory _inventory;

        private static ItemDefinition Load(string name) => AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset");

        private static ItemInstance Potion() => new ItemInstance(Load("HealingPotion"));

        private Belt Belt => _inventory.Belt;

        [SetUp]
        public void SetUp()
        {
            _inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(new StatSheet()));
        }

        [Test, Description("Nella cintura vanno solo pozioni, nel primo posto libero, fino a 8; ogni posto cambiato avvisa")]
        public void TryAdd_FillsFirstFreeSlot()
        {
            var changed = new List<int>();
            Belt.Changed += changed.Add;

            Assert.IsFalse(Belt.TryAdd(new ItemInstance(Load("Dagger"))), "un pugnale non è una pozione");
            Assert.IsFalse(Belt.TryAdd(null));
            for (int i = 0; i < Belt.Size; i++)
            {
                Assert.IsTrue(Belt.TryAdd(Potion()));
            }

            Assert.IsFalse(Belt.TryAdd(Potion()), "cintura piena");
            Assert.AreEqual(8, Belt.Count);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, changed);

            var taken = Belt.Take(2);
            Assert.IsNotNull(taken);
            Assert.IsNull(Belt.Get(2));
            Assert.IsNull(Belt.Take(2), "il posto è vuoto");
            Assert.IsNull(Belt.Get(-1));
            Assert.IsNull(Belt.Get(Belt.Size));

            Assert.IsTrue(Belt.TryAdd(taken));
            Assert.AreSame(taken, Belt.Get(2), "il primo posto libero è quello appena vuotato");
        }

        [Test, Description("Raccolta: una pozione va nella cintura; a cintura piena nella griglia; gli altri oggetti sempre nella griglia")]
        public void PickUp_PrefersTheBelt()
        {
            var first = Potion();
            Assert.IsTrue(_inventory.TryPickUp(first));
            Assert.AreSame(first, Belt.Get(0));
            Assert.AreEqual(0, _inventory.Grid.Placements.Count);

            for (int i = 1; i < Belt.Size; i++)
            {
                _inventory.TryPickUp(Potion());
            }

            var ninth = Potion();
            Assert.IsTrue(_inventory.TryPickUp(ninth));
            Assert.IsTrue(_inventory.Grid.TryGetPlacement(ninth, out _), "a cintura piena va nella griglia");

            var dagger = new ItemInstance(Load("Dagger"));
            Assert.IsTrue(_inventory.TryPickUp(dagger));
            Assert.IsTrue(_inventory.Grid.TryGetPlacement(dagger, out _));
        }

        [Test, Description("Click sulla cintura come sulla griglia: prende, posa, scambia; un oggetto che non è una pozione resta sul cursore")]
        public void ClickBelt_PicksPlacesAndSwaps()
        {
            var a = Potion();
            var b = Potion();
            Belt.TryAdd(a);
            Belt.TryAdd(b);

            Assert.IsFalse(_inventory.ClickBelt(5), "posto vuoto e cursore libero");
            Assert.IsTrue(_inventory.ClickBelt(0));
            Assert.AreSame(a, _inventory.Held);
            Assert.IsNull(Belt.Get(0));

            Assert.IsTrue(_inventory.ClickBelt(1), "posata su un'altra pozione, le due si scambiano");
            Assert.AreSame(a, Belt.Get(1));
            Assert.AreSame(b, _inventory.Held);

            Assert.IsTrue(_inventory.ClickBelt(7));
            Assert.AreSame(b, Belt.Get(7));
            Assert.IsNull(_inventory.Held);

            var dagger = new ItemInstance(Load("Dagger"));
            _inventory.Grid.TryPlace(dagger, Vector2Int.zero);
            _inventory.ClickCell(Vector2Int.zero);
            Assert.IsFalse(_inventory.ClickBelt(3));
            Assert.AreSame(dagger, _inventory.Held);
            Assert.IsNull(Belt.Get(3));
        }

        [Test, Description("Chiudendo l'inventario una pozione sul cursore torna prima nella cintura")]
        public void StoreHeld_PotionGoesBackToTheBelt()
        {
            var potion = Potion();
            _inventory.Grid.TryPlace(potion, new Vector2Int(4, 2));
            _inventory.ClickCell(new Vector2Int(4, 2));

            Assert.IsTrue(_inventory.TryStoreHeld());
            Assert.AreSame(potion, Belt.Get(0));
            Assert.IsFalse(_inventory.Grid.TryGetPlacement(potion, out _));
        }

        [Test, Description("Bere toglie la pozione dal suo posto, nella cintura o nella griglia; dove non c'è una pozione non toglie niente")]
        public void TakePotion_RemovesOnlyPotions()
        {
            var potion = Potion();
            Belt.TryAdd(potion);
            var inGrid = Potion();
            _inventory.Grid.TryPlace(inGrid, new Vector2Int(3, 1));
            var dagger = new ItemInstance(Load("Dagger"));
            _inventory.Grid.TryPlace(dagger, new Vector2Int(0, 0));

            Assert.AreSame(potion.Definition, _inventory.TakePotionFromBelt(0));
            Assert.IsNull(Belt.Get(0));
            Assert.IsNull(_inventory.TakePotionFromBelt(0), "il posto ora è vuoto");

            Assert.AreSame(inGrid.Definition, _inventory.TakePotionAt(new Vector2Int(3, 1)));
            Assert.IsNull(_inventory.Grid.ItemAt(new Vector2Int(3, 1)));
            Assert.IsNull(_inventory.TakePotionAt(new Vector2Int(0, 1)), "il pugnale non si beve");
            Assert.AreSame(dagger, _inventory.Grid.ItemAt(new Vector2Int(0, 1)));
        }
    }
}
