using System.Collections.Generic;
using System.Linq;
using DarkDescent.Levels;
using DarkDescent.Localization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>La lore (25.5 della M25, D9 e D16): quale testo sta a quale profondità, dove si mette il leggio, e i dodici testi nelle due lingue.</summary>
    public class LoreRulesTests
    {
        private const int Seeds = 2000;
        private const string TablePath = "Assets/_Project/Data/Localization/Strings.csv";

        private static StringTable Table => StringTable.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(TablePath).text);

        [Test, Description("Nello stesso mondo un testo non si ripete mai tra le profondità, un testo della cripta non compare nelle caverne e uno delle caverne non sta nella cripta (D9)")]
        public void ATextNeverRepeatsInTheSameWorld()
        {
            var crypt = new HashSet<int> { 1, 2, 3, 4, 5, 6, 11, 12 };
            var caves = new HashSet<int> { 7, 8, 9, 10, 11, 12 };
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var seen = new HashSet<int>();
                for (int depth = 1; depth <= LoreRules.LastDepth; depth++)
                {
                    int text = LoreRules.TextFor(seed, depth);
                    Assert.AreEqual(LoreRules.Has(seed, depth), text > 0, $"seme {seed}, profondità {depth}");
                    if (text == 0)
                    {
                        continue;
                    }

                    Assert.IsTrue(seen.Add(text), $"seme {seed}: il testo {text} si ripete (profondità {depth})");
                    Assert.IsTrue(depth <= LoreRules.LastCryptDepth ? crypt.Contains(text) : caves.Contains(text), $"seme {seed}, profondità {depth}: il testo {text} è del posto sbagliato");
                }
            }
        }

        [Test, Description("Due livelli su tre hanno un testo, a ogni profondità, e dalla 9 in giù non c'è niente (D9)")]
        public void TwoLevelsInThreeHaveAText()
        {
            for (int depth = 1; depth <= LoreRules.LastDepth; depth++)
            {
                int with = 0;
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    with += LoreRules.Has(seed, depth) ? 1 : 0;
                }

                Assert.That(with, Is.InRange(Seeds * 0.62, Seeds * 0.71), $"profondità {depth}: {with} su {Seeds}");
            }

            Assert.IsFalse(LoreRules.Has(1UL, 0));
            Assert.IsFalse(LoreRules.Has(1UL, LoreRules.LastDepth + 1));
        }

        [Test, Description("Il leggio regge i testi 1, 5 e 10, la lapide 3, 6, 7 e 12, le carte gli altri (scheda 25.2)")]
        public void TheFurnitureHoldsItsTexts()
        {
            foreach (int text in new[] { 1, 5, 10 })
            {
                Assert.AreEqual(LoreKind.Lectern, LoreRules.KindOf(text));
            }

            foreach (int text in new[] { 3, 6, 7, 12 })
            {
                Assert.AreEqual(LoreKind.Tablet, LoreRules.KindOf(text));
            }

            foreach (int text in new[] { 2, 4, 8, 9, 11 })
            {
                Assert.AreEqual(LoreKind.Book, LoreRules.KindOf(text));
            }
        }

        [Test, Description("I dodici testi hanno titolo e testo in italiano e in inglese, diversi tra loro, e le righe degli arredi e della leva ci sono (D9, D12)")]
        public void TheTwelveTexts_ExistInBothLanguages()
        {
            var table = Table;
            int english = table.IndexOf("en");
            int italian = table.IndexOf("it");
            for (int text = 1; text <= LoreRules.TextCount; text++)
            {
                foreach (string key in new[] { LoreRules.TitleKey(text), LoreRules.BodyKey(text) })
                {
                    Assert.IsTrue(table.TryGet(key, english, out string en) && !string.IsNullOrWhiteSpace(en), key + " in inglese");
                    Assert.IsTrue(table.TryGet(key, italian, out string it) && !string.IsNullOrWhiteSpace(it), key + " in italiano");
                    Assert.AreNotEqual(en, it, key + ": le due lingue sono uguali");
                }
            }

            foreach (string key in new[] { "lore.close", "lore.lectern", "lore.tablet", "lore.book", TextKeys.SecretHeard })
            {
                Assert.IsTrue(table.Contains(key), key);
            }
        }

        [Test, Description("Un virgolettato dentro un testo di inglese non rompe la tabella: il testo 8 ha le sue virgolette")]
        public void AQuoteInsideAText_SurvivesTheTable()
        {
            var table = Table;
            Assert.IsTrue(table.TryGet(LoreRules.BodyKey(8), table.IndexOf("en"), out string text));
            StringAssert.Contains("\"I think\"", text);
        }

        private static IEnumerable<(string name, LevelMap map, int depth)> Levels(int first, int last, int seeds, bool caves)
        {
            var crypt = ScriptableObject.CreateInstance<DungeonSettings>();
            var cave = ScriptableObject.CreateInstance<CaveSettings>();
            try
            {
                for (int depth = first; depth <= last; depth++)
                {
                    for (ulong seed = 1; seed <= (ulong)seeds; seed++)
                    {
                        var layout = caves ? cave.CreateGenerator().Generate(seed, depth) : new DungeonGenerator(crypt).Generate(seed, depth);
                        yield return ($"{(caves ? "caverne" : "cripta")}, profondità {depth}, seme {seed}", layout.ToMap(null), depth);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(crypt);
                Object.DestroyImmediate(cave);
            }
        }

        [Test, Description("Il leggio ha un posto nella cripta e nelle caverne in almeno il 95% dei livelli: contro un muro, davanti libero, lontano da ingresso e scala, senza ostacoli attorno (D16)")]
        public void TheLecternFindsAPlace_InCryptAndCaves()
        {
            foreach (bool caves in new[] { false, true })
            {
                int levels = 0;
                int found = 0;
                int first = caves ? 5 : 1;
                int last = caves ? 8 : 4;
                foreach (var (name, map, depth) in Levels(first, last, 60, caves))
                {
                    levels++;
                    var walkable = SealedRoomPlanner.Walkability(map);
                    var spot = SecretSpots.Pick(map, walkable, 7UL, depth, LoreRules.SpotKind, null, null);
                    if (spot == null)
                    {
                        continue;
                    }

                    found++;
                    var cell = spot.Value.Cell;
                    var facing = spot.Value.Facing;
                    Assert.AreEqual(LevelMap.Floor, map.GetSymbol(cell.x, cell.y), name);
                    Assert.IsTrue(walkable[cell.x, cell.y], name);
                    Assert.IsTrue(map.IsRock(cell.x - facing.x, cell.y - facing.y), name + ": il muro alle spalle");
                    Assert.IsTrue(walkable[cell.x + facing.x, cell.y + facing.y], name + ": davanti libero");
                }

                Debug.Log($"[M25 lore] {(caves ? "caverne" : "cripta")}: il leggio trova un posto in {found} livelli su {levels}");
                Assert.GreaterOrEqual(found, levels * 0.95, (caves ? "caverne" : "cripta") + $": solo {found} su {levels}");
            }
        }

        [Test, Description("Il posto della lore non entra nel fondo della stanza sigillata e sta lontano dalla porta e dalla leva (D5)")]
        public void TheLecternStaysOutOfTheSealedRoom()
        {
            int checkedRooms = 0;
            foreach (var (name, map, depth) in Levels(2, 4, 80, false))
            {
                var plan = SealedRoomPlanner.Find(map, 7UL, depth, requireChest: true);
                if (plan == null)
                {
                    continue;
                }

                var region = new HashSet<Vector2Int>(plan.Region);
                var spot = SecretSpots.Pick(map, 7UL, depth, LoreRules.SpotKind, new[] { plan.Door, plan.Lever }, region);
                if (spot == null)
                {
                    continue;
                }

                checkedRooms++;
                Assert.IsFalse(region.Contains(spot.Value.Cell), name);
                Assert.IsFalse(region.Contains(spot.Value.Cell + spot.Value.Facing), name);
                Assert.GreaterOrEqual(Mathf.Abs(spot.Value.Cell.x - plan.Lever.x) + Mathf.Abs(spot.Value.Cell.y - plan.Lever.y), 6, name + ": troppo vicino alla leva");
            }

            Assert.Greater(checkedRooms, 20, "troppo poche stanze sigillate per provare");
        }
    }
}
