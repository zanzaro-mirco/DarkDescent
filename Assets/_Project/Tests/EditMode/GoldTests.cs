using System.Linq;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    /// <summary>L'oro e i prezzi (D4 della M10).</summary>
    public class GoldTests
    {
        private static LootTable Table(string name) => AssetDatabase.LoadAssetAtPath<LootTable>($"Assets/_Project/Data/Loot/{name}.asset");

        private static ItemDefinition Item(string name) => AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset");

        [Test, Description("Le casse lasciano sempre oro, i nemici a volte; l'intervallo alla profondità 1 cresce del 35% a livello")]
        public void GoldFor_FollowsChanceRangeAndDepth()
        {
            var chest = Table("ChestLoot");
            Assert.IsNotNull(chest.Gold);
            Assert.AreEqual(1f, chest.GoldChance);
            for (int i = 0; i < 200; i++)
            {
                var random = new SplitMix64Source((ulong)i);
                int gold = chest.GoldFor(random, 1);
                Assert.That(gold, Is.InRange(chest.GoldRange.x, chest.GoldRange.y));
            }

            // a profondità 5 lo stesso tiro vale 1 + 4 × 0,35 = 2,4 volte
            int atOne = chest.GoldFor(new SplitMix64Source(7UL), 1);
            int atFive = chest.GoldFor(new SplitMix64Source(7UL), 5);
            Assert.AreEqual(System.Math.Round(atOne * 2.4, System.MidpointRounding.AwayFromZero), atFive);

            int drops = Enumerable.Range(0, 1000).Count(i => Table("SkeletonLoot").GoldFor(new SplitMix64Source((ulong)i * 31UL), 1) > 0);
            Assert.That(drops, Is.InRange(420, 580), "uno scheletro su due lascia oro");
        }

        [Test, Description("L'oro ha un seme suo: lo stesso nemico dà sempre lo stesso oro, e oggetto e pozione non cambiano")]
        public void RollGold_IsSeededAndSeparate()
        {
            var generator = new ItemGenerator(AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset").Affixes, RarityTable.Default);
            var roller = new LootRoller(generator, 4711UL);
            var table = Table("BruteLoot");
            int gold = roller.RollGold(table, 3, 12, -40);
            Assert.AreEqual(gold, roller.RollGold(table, 3, 12, -40));
            Assert.AreNotEqual(SeedMixer.ForPotion(4711UL, 3, 12, -40), SeedMixer.ForGold(4711UL, 3, 12, -40));
            Assert.AreNotEqual(SeedMixer.ForEnemy(4711UL, 3, 12, -40), SeedMixer.ForGold(4711UL, 3, 12, -40));
        }

        [Test, Description("Prezzo: valore × rarità × (1 + livello / 10); il mercante compra a un quarto, almeno 1")]
        public void Price_FromValueRarityAndLevel()
        {
            var dagger = Item("Dagger");
            Assert.AreEqual(20, dagger.Value);
            Assert.AreEqual(22, ItemPricing.Price(new ItemInstance(dagger)), "20 × 1 × 1,1");
            Assert.AreEqual(5, ItemPricing.SellPrice(new ItemInstance(dagger)));
            var magic = new ItemInstance(dagger, Rarity.Magic, 4, 1UL, null);
            Assert.AreEqual(84, ItemPricing.Price(magic), "20 × 3 × 1,4");
            var rare = new ItemInstance(dagger, Rarity.Rare, 8, 1UL, null);
            Assert.AreEqual(216, ItemPricing.Price(rare), "20 × 6 × 1,8");
            Assert.AreEqual(54, ItemPricing.SellPrice(rare));
            Assert.AreEqual(4, ItemPricing.SellPrice(new ItemInstance(Item("HealingPotion"))), "15 × 1,1 = 17, un quarto 4");
        }

        [Test, Description("Ogni oggetto che si trova ha un valore")]
        public void EveryItem_HasAValue()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/_Project/Data/Items" }))
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.Greater(item.Value, 0, item.name);
            }
        }

        [Test, Description("Il portafoglio: si aggiunge, si spende se basta, si ricarica; ogni cambio lo annuncia")]
        public void Wallet_AddsSpendsRestores()
        {
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(new StatSheet()));
            int changes = 0;
            inventory.GoldChanged += () => changes++;
            inventory.AddGold(30);
            inventory.AddGold(0);
            Assert.AreEqual(30, inventory.Gold);
            Assert.IsFalse(inventory.TrySpendGold(31), "non basta");
            Assert.IsTrue(inventory.TrySpendGold(12));
            Assert.AreEqual(18, inventory.Gold);
            inventory.RestoreGold(500);
            Assert.AreEqual(500, inventory.Gold);
            Assert.AreEqual(3, changes);
        }
    }
}
