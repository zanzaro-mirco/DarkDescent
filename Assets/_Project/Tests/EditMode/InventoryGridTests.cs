using DarkDescent.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class InventoryGridTests
    {
        private InventoryGrid _grid;

        private static ItemInstance Sword => new ItemInstance(AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/ShortSword.asset"));
        private static ItemInstance Shield => new ItemInstance(AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/BadgeShield.asset"));

        [SetUp]
        public void SetUp()
        {
            _grid = new InventoryGrid(10, 4);
        }

        [Test, Description("Un oggetto posato occupa tutte le sue celle: la spada 1×3, lo scudo 2×2")]
        public void Place_OccupiesAllCells()
        {
            var sword = Sword;
            var shield = Shield;

            Assert.IsTrue(_grid.TryPlace(sword, new Vector2Int(0, 0)));
            Assert.IsTrue(_grid.TryPlace(shield, new Vector2Int(3, 1)));

            for (int y = 0; y < 3; y++)
            {
                Assert.AreSame(sword, _grid.ItemAt(new Vector2Int(0, y)));
            }

            Assert.IsNull(_grid.ItemAt(new Vector2Int(0, 3)));
            Assert.AreSame(shield, _grid.ItemAt(new Vector2Int(4, 2)));
            Assert.IsNull(_grid.ItemAt(new Vector2Int(5, 2)));
        }

        [Test, Description("Niente sovrapposizioni e niente fuori dai bordi")]
        public void Place_RejectsOverlapAndOutOfBounds()
        {
            _grid.TryPlace(Shield, new Vector2Int(0, 0));

            Assert.IsFalse(_grid.TryPlace(Sword, new Vector2Int(1, 1)), "sopra lo scudo");
            Assert.IsFalse(_grid.TryPlace(Sword, new Vector2Int(9, 2)), "esce dal fondo");
            Assert.IsFalse(_grid.TryPlace(Shield, new Vector2Int(9, 0)), "esce a destra");
            Assert.IsFalse(_grid.TryPlace(Sword, new Vector2Int(-1, 0)));
            Assert.IsTrue(_grid.TryPlace(Sword, new Vector2Int(9, 1)));
        }

        [Test, Description("Tolto un oggetto, le sue celle tornano libere")]
        public void Remove_FreesCells()
        {
            var shield = Shield;
            _grid.TryPlace(shield, new Vector2Int(2, 2));

            Assert.IsTrue(_grid.Remove(shield));

            Assert.IsNull(_grid.ItemAt(new Vector2Int(3, 3)));
            Assert.IsTrue(_grid.CanPlace(Shield, new Vector2Int(2, 2)));
            Assert.IsFalse(_grid.Remove(shield), "non c'è più");
        }

        [Test, Description("Il primo posto libero si cerca colonna per colonna, da sinistra e dall'alto")]
        public void AutoPlace_FillsColumnsFirst()
        {
            var first = Shield;
            var second = Sword;
            var third = Sword;

            Assert.IsTrue(_grid.TryAutoPlace(first));
            Assert.IsTrue(_grid.TryAutoPlace(second));
            Assert.IsTrue(_grid.TryAutoPlace(third));

            Assert.AreEqual(new RectInt(0, 0, 2, 2), _grid.Placements[first]);
            // sotto lo scudo una spada da 3 non ci sta: va nella colonna 2
            Assert.AreEqual(new Vector2Int(2, 0), _grid.Placements[second].position);
            Assert.AreEqual(new Vector2Int(3, 0), _grid.Placements[third].position);
        }

        [Test, Description("A griglia piena il posto automatico non si trova")]
        public void AutoPlace_FailsWhenFull()
        {
            // dieci spade riempiono le prime tre righe, la quarta resta libera ma è alta solo una cella
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(_grid.TryAutoPlace(Sword));
            }

            Assert.IsFalse(_grid.TryAutoPlace(Sword));
            Assert.IsFalse(_grid.TryAutoPlace(Shield));
        }

        [Test, Description("Posato su un oggetto solo, lo scambia; su due, non fa niente")]
        public void PlaceOrSwap_SwapsOnlyWithOneItem()
        {
            var shield = Shield;
            var left = Sword;
            var right = Sword;
            _grid.TryPlace(shield, new Vector2Int(0, 0));

            Assert.IsTrue(_grid.TryPlaceOrSwap(left, new Vector2Int(1, 0), out var displaced));
            Assert.AreSame(shield, displaced);
            Assert.AreSame(left, _grid.ItemAt(new Vector2Int(1, 2)));
            Assert.IsNull(_grid.ItemAt(new Vector2Int(0, 0)), "lo scudo è uscito tutto");

            _grid.TryPlace(right, new Vector2Int(2, 0));
            Assert.IsFalse(_grid.TryPlaceOrSwap(shield, new Vector2Int(1, 0), out displaced), "sotto ci sono due spade");
            Assert.IsNull(displaced);
        }
    }
}
