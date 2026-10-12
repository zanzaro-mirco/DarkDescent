using System.Linq;
using System.Text;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Localization;
using DarkDescent.Town;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>La pergamena del ritorno e i portali (D6 della M10).</summary>
    public class ReturnScrollTests
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>("Assets/_Project/" + path);

        private static ReturnScrollDefinition Scroll => Asset<ReturnScrollDefinition>("Data/Items/ReturnScroll.asset");

        private static LootRoller Roller(ulong seed)
        {
            var generator = new ItemGenerator(Asset<AffixDatabase>("Data/AffixDatabase.asset").Affixes, RarityTable.Default);
            return new LootRoller(generator, seed);
        }

        [Test, Description("La pergamena: 1 × 2 come le altre, non si indossa, sta nel database, il mercante la vende a poco; il tooltip dice cosa fa e come leggerla")]
        public void Scroll_IsCheapAndReadable()
        {
            var scroll = Scroll;
            Assert.AreEqual(new Vector2Int(1, 2), scroll.Size);
            Assert.AreEqual(EquipSlot.None, scroll.Slot);
            Assert.IsNotNull(scroll.Icon);
            Assert.IsNotNull(scroll.Model);
            Assert.IsTrue(Asset<ItemDatabase>("Data/ItemDatabase.asset").TryGet(scroll.Id, out var found) && found == scroll, "si salva");
            CollectionAssert.Contains(Asset<MerchantTable>("Data/Loot/MerchantTable.asset").Supplies.ToArray(), scroll);
            Assert.Less(ItemPricing.Price(new ItemInstance(scroll)), 30, "a poco");

            var localizer = new Localizer(StringTable.Parse(Asset<TextAsset>("Data/Localization/Strings.csv").text));
            var builder = new StringBuilder();
            ItemDescription.Write(builder, new ItemInstance(scroll), true, localizer);
            StringAssert.Contains("Scroll of Return", builder.ToString());
            StringAssert.Contains("Opens a portal to the camp", builder.ToString());
            StringAssert.Contains("Right-click to read", builder.ToString());
            localizer.SetLanguage("it");
            builder.Clear();
            ItemDescription.Write(builder, new ItemInstance(scroll), true, localizer);
            StringAssert.Contains("Pergamena del ritorno", builder.ToString());
        }

        [Test, Description("Cade raramente, più dalle casse che dai nemici, con un tiro suo: stesso seme stessa pergamena, e il resto del loot non cambia")]
        public void ReturnScroll_DropsRarely()
        {
            var chest = Asset<LootTable>("Data/Loot/ChestLoot.asset");
            var skeleton = Asset<LootTable>("Data/Loot/SkeletonLoot.asset");
            Assert.AreEqual(Scroll, chest.ReturnScroll);
            Assert.Greater(chest.ReturnScrollChance, skeleton.ReturnScrollChance);

            var roller = Roller(11UL);
            int fromChests = 0;
            int fromSkeletons = 0;
            const int Rolls = 4000;
            for (int i = 0; i < Rolls; i++)
            {
                fromChests += roller.RollReturnScroll(chest, 2, i, -i) != null ? 1 : 0;
                fromSkeletons += roller.RollReturnScroll(skeleton, 2, i, -i) != null ? 1 : 0;
            }

            Assert.That(fromChests / (float)Rolls, Is.InRange(chest.ReturnScrollChance * 0.6f, chest.ReturnScrollChance * 1.4f));
            Assert.That(fromSkeletons / (float)Rolls, Is.InRange(skeleton.ReturnScrollChance * 0.4f, skeleton.ReturnScrollChance * 1.6f));

            int first = -1;
            for (int i = 0; i < Rolls && first < 0; i++)
            {
                first = roller.RollReturnScroll(chest, 2, i, -i) != null ? i : -1;
            }

            Assert.IsNotNull(Roller(11UL).RollReturnScroll(chest, 2, first, -first), "stesso seme, stessa pergamena");
            Assert.AreNotEqual(SeedMixer.ForReturnScroll(11UL, 2, 3, 4), SeedMixer.ForGold(11UL, 2, 3, 4));
            Assert.AreNotEqual(SeedMixer.ForReturnScroll(11UL, 2, 3, 4), SeedMixer.ForPotion(11UL, 2, 3, 4));
        }

        [Test, Description("Il portale della città riporta all'ingresso della profondità: la cripta fino alla 4, poi le caverne")]
        public void Depths_MapToTheirScenes()
        {
            var crypt = Asset<DungeonSettings>("Data/Levels/CryptSettings.asset");
            var caves = Asset<DungeonSettings>("Data/Levels/CaveSettings.asset");
            Assert.IsTrue(crypt.Covers(1) && crypt.Covers(4) && !crypt.Covers(5) && !crypt.Covers(0));
            Assert.IsTrue(caves.Covers(5) && caves.Covers(8) && !caves.Covers(4));
            Assert.AreEqual("Start", DungeonLevel.EntranceAt(1));
            Assert.AreEqual("FromAbove", DungeonLevel.EntranceAt(3));
            Assert.AreEqual("FromAbove", DungeonLevel.EntranceAt(5));
        }

        [Test, Description("La città ha l'arrivo dei portali accanto all'ingresso: il simbolo P è un ingresso Portal")]
        public void Town_HasThePortalArrival()
        {
            var map = LevelMap.Parse(Asset<TextAsset>("Data/Levels/Town.txt").text);
            var arrival = map.Markers.Single(m => m.Symbol == 'P');
            var start = map.Markers.Single(m => m.Symbol == '<');
            Assert.LessOrEqual(Mathf.Abs(arrival.X - start.X) + Mathf.Abs(arrival.Y - start.Y), 3, "accanto all'ingresso");
            Assert.IsTrue(map.IsFloor(arrival.X, arrival.Y - 1), "il portale sta a nord, sul pavimento");

            var prefab = Asset<LevelTileset>("Data/Levels/TownTileset.asset").GetMarkerPrefab('P');
            Assert.AreEqual(TownPortals.PortalEntrance, prefab.GetComponent<LevelEntrance>().Id);
        }
    }
}
