using System.Collections.Generic;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Localization;
using DarkDescent.Stats;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Il banco del mercante: assortimento, comprare e vendere (D5 della M10).</summary>
    public class ShopTests
    {
        private static MerchantTable Table => AssetDatabase.LoadAssetAtPath<MerchantTable>("Assets/_Project/Data/Loot/MerchantTable.asset");

        private static ItemDefinition Item(string name) => AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset");

        private static LootRoller Roller(ulong seed)
        {
            var generator = new ItemGenerator(AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset").Affixes, RarityTable.Default);
            return new LootRoller(generator, seed);
        }

        private static Inventory NewInventory(int gold)
        {
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(new StatSheet()));
            inventory.RestoreGold(gold);
            return inventory;
        }

        private static Shop Stocked(ulong seed = 99UL, int depth = 3)
        {
            var stock = new List<ItemInstance>();
            Roller(seed).RollStock(Table, depth, 0, stock);
            var shop = new Shop();
            shop.Restock(Table.Supplies, stock);
            return shop;
        }

        [Test, Description("La tabella: pozioni di cura e di mana sempre in vendita, otto oggetti dalle basi delle casse")]
        public void Table_HasPotionsAndEightItems()
        {
            CollectionAssert.AreEqual(new[] { "HealingPotion", "ManaPotion" }, Table.Supplies.Select(s => s.name).ToArray());
            Assert.AreEqual(8, Table.StockSize);
            Assert.IsTrue(Table.Stock.All(e => e.Item != null && e.Item.Slot != EquipSlot.None), "solo equipaggiamento");
        }

        [Test, Description("L'assortimento: otto oggetti del livello della profondità, uguali con lo stesso seme, diversi a un altro ritorno o più in basso")]
        public void RollStock_IsSeededByDepthAndVisit()
        {
            var first = new List<ItemInstance>();
            var again = new List<ItemInstance>();
            var otherVisit = new List<ItemInstance>();
            var deeper = new List<ItemInstance>();
            Roller(7UL).RollStock(Table, 3, 0, first);
            Roller(7UL).RollStock(Table, 3, 0, again);
            Roller(7UL).RollStock(Table, 3, 12, otherVisit);
            Roller(7UL).RollStock(Table, 6, 0, deeper);

            Assert.AreEqual(8, first.Count);
            Assert.IsTrue(first.All(i => i.ItemLevel == 3));
            Assert.IsTrue(deeper.All(i => i.ItemLevel == 6));
            CollectionAssert.AreEqual(first.Select(i => i.Seed).ToArray(), again.Select(i => i.Seed).ToArray());
            CollectionAssert.AreNotEqual(first.Select(i => i.Seed).ToArray(), otherVisit.Select(i => i.Seed).ToArray());
            Assert.AreNotEqual(SeedMixer.ForShop(7UL, 3, 0), SeedMixer.ForLevel(7UL, 3), "un flusso suo: non sposta i livelli");
        }

        [Test, Description("Una provvista si compra al prezzo pieno, va nella cintura e resta sul banco")]
        public void BuySupply_PaysAndKeepsTheSupply()
        {
            var shop = Stocked();
            var inventory = NewInventory(100);
            int price = ItemPricing.Price(shop.ItemAt(0));
            Assert.AreEqual(17, price, "15 × 1,1, arrotondato");

            Assert.AreEqual(TradeResult.Done, shop.Buy(inventory, 0));
            Assert.AreEqual(100 - price, inventory.Gold);
            Assert.AreEqual(1, inventory.Belt.Count);
            Assert.AreEqual("HealingPotion", shop.ItemAt(0).Definition.name, "le provviste non finiscono");
            Assert.AreEqual(10, shop.Count);
        }

        [Test, Description("Un oggetto comprato va nella borsa e sparisce dal banco")]
        public void BuyStock_MovesTheItemToTheBag()
        {
            var shop = Stocked();
            var inventory = NewInventory(100000);
            var item = shop.ItemAt(3);
            int price = ItemPricing.Price(item);
            int changes = 0;
            shop.Changed += () => changes++;

            Assert.AreEqual(TradeResult.Done, shop.Buy(inventory, 3));
            Assert.AreEqual(100000 - price, inventory.Gold);
            Assert.IsTrue(inventory.Grid.TryGetPlacement(item, out _));
            Assert.AreEqual(9, shop.Count);
            Assert.AreNotSame(item, shop.ItemAt(3));
            Assert.AreEqual(1, changes);
        }

        [Test, Description("Senza abbastanza oro, o senza posto, l'acquisto non tocca niente")]
        public void Buy_WithoutGoldOrRoom_ChangesNothing()
        {
            var shop = Stocked();
            var poor = NewInventory(5);
            Assert.AreEqual(TradeResult.NotEnoughGold, shop.Buy(poor, 3));
            Assert.AreEqual(5, poor.Gold);
            Assert.AreEqual(10, shop.Count);

            var full = NewInventory(100000);
            while (full.Grid.TryAutoPlace(new ItemInstance(Item("Ring"))))
            {
            }

            Assert.AreEqual(TradeResult.NoRoom, shop.Buy(full, 3));
            Assert.AreEqual(100000, full.Gold);
            Assert.AreEqual(10, shop.Count);
            Assert.AreEqual(TradeResult.Nothing, shop.Buy(full, 50));
        }

        [Test, Description("Vendere toglie l'oggetto dalla borsa e paga un quarto del prezzo, che non conta come oro raccolto")]
        public void Sell_PaysAQuarter()
        {
            var shop = Stocked();
            var inventory = NewInventory(0);
            var axe = new ItemInstance(Item("Axe"), Rarity.Magic, 4, 1UL, null);
            Assert.IsTrue(inventory.Grid.TryPlace(axe, new Vector2Int(3, 1)));
            int collected = 0;
            inventory.GoldAdded += amount => collected += amount;

            Assert.AreEqual(TradeResult.Done, shop.SellAt(inventory, new Vector2Int(3, 1)));
            Assert.AreEqual(ItemPricing.SellPrice(axe), inventory.Gold);
            Assert.IsFalse(inventory.Grid.TryGetPlacement(axe, out _));
            Assert.AreEqual(0, collected);
            Assert.AreEqual(TradeResult.Nothing, shop.SellAt(inventory, new Vector2Int(3, 1)));
        }

        [Test, Description("La profondità più bassa visitata decide il livello degli oggetti; la città non conta")]
        public void DeepestDepth_IgnoresTheTown()
        {
            var memory = new ExplorationMemory();
            Assert.AreEqual(0, memory.DeepestDepth);
            var map = LevelMap.Parse("###\n#<#\n###");
            memory.Open(0, map);
            memory.Open(4, map);
            memory.Open(2, map);
            Assert.AreEqual(4, memory.DeepestDepth);
        }

        [Test, Description("Le righe del prezzo: in oro se basta, in rosso se no, con il tasto sotto")]
        public void TradeText_ColorsThePrice()
        {
            var localizer = new Localizer(StringTable.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv").text));
            string affordable = ItemDescription.TradeText(localizer, TextKeys.ShopPrice, 120, true, TextKeys.ShopBuyHint);
            StringAssert.Contains("Price: 120 gold", affordable);
            StringAssert.Contains("Click to buy", affordable);
            StringAssert.Contains(ItemDescription.UnmetColor, ItemDescription.TradeText(localizer, TextKeys.ShopPrice, 120, false, TextKeys.ShopBuyHint));
            localizer.SetLanguage("it");
            StringAssert.Contains("Si vende a 30 oro", ItemDescription.TradeText(localizer, TextKeys.ShopSellPrice, 30, true, TextKeys.ShopSellHint));
        }
    }
}
