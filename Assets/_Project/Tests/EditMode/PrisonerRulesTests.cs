using System.Collections.Generic;
using DarkDescent.Levels;
using DarkDescent.Localization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Il prigioniero (25.5 della M25, D6 e D16): quando un livello ne ha uno, dove sta tra i nemici che già c'erano, e i suoi consigli nelle due lingue.</summary>
    public class PrisonerRulesTests
    {
        private const int Seeds = 2000;
        private const string TablePath = "Assets/_Project/Data/Localization/Strings.csv";

        [Test, Description("Un livello su tre della cripta, dalla profondità 2 alla 4, vuole un prigioniero; fuori da lì mai (D6, D16)")]
        public void OneLevelInThree_InTheCrypt()
        {
            for (int depth = PrisonerRules.FirstDepth; depth <= PrisonerRules.LastDepth; depth++)
            {
                int with = 0;
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    with += PrisonerRules.LevelWantsOne(seed, depth) ? 1 : 0;
                }

                Assert.That(with, Is.InRange(Seeds * 0.29, Seeds * 0.38), $"profondità {depth}: {with} su {Seeds}");
            }

            foreach (int depth in new[] { 0, 1, 5, 8 })
            {
                for (ulong seed = 1; seed <= 100; seed++)
                {
                    Assert.IsFalse(PrisonerRules.LevelWantsOne(seed, depth));
                }
            }
        }

        [Test, Description("Il consiglio è uno dei quattro, dal seme, e vanno tutti e quattro; l'oro cresce con la profondità e resta poco")]
        public void AdviceAndGold()
        {
            var seen = new HashSet<int>();
            for (ulong seed = 1; seed <= 400; seed++)
            {
                int advice = PrisonerRules.AdviceFor(seed, 3);
                Assert.That(advice, Is.InRange(1, PrisonerRules.AdviceCount));
                seen.Add(advice);
            }

            Assert.AreEqual(PrisonerRules.AdviceCount, seen.Count);
            Assert.Greater(PrisonerRules.Gold(4), PrisonerRules.Gold(2));
            Assert.LessOrEqual(PrisonerRules.Gold(4), 100, "D8: poco oro");
        }

        [Test, Description("Titolo, etichetta, riga della scorta e i quattro consigli ci sono in tutte e due le lingue, diversi tra loro")]
        public void TheStringsExistInBothLanguages()
        {
            var table = StringTable.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(TablePath).text);
            int english = table.IndexOf("en");
            int italian = table.IndexOf("it");
            var keys = new List<string> { TextKeys.PrisonerTitle, TextKeys.PrisonerGuarded, "prisoner.label" };
            for (int advice = 1; advice <= PrisonerRules.AdviceCount; advice++)
            {
                keys.Add(PrisonerRules.AdviceKey(advice));
            }

            foreach (string key in keys)
            {
                Assert.IsTrue(table.TryGet(key, english, out string en) && !string.IsNullOrWhiteSpace(en), key + " in inglese");
                Assert.IsTrue(table.TryGet(key, italian, out string it) && !string.IsNullOrWhiteSpace(it), key + " in italiano");
                Assert.AreNotEqual(en, it, key);
            }
        }

        [Test, Description("Dove c'è un posto, sta contro un muro con almeno un nemico attorno, mai nel fondo sigillato, e lontano dalla leva e dalla porta (D6, D5)")]
        public void ThePlace_IsAgainstAWall_AmongEnemies()
        {
            var settings = ScriptableObject.CreateInstance<DungeonSettings>();
            try
            {
                int levels = 0;
                int placed = 0;
                int sealedChecked = 0;
                for (int depth = PrisonerRules.FirstDepth; depth <= PrisonerRules.LastDepth; depth++)
                {
                    for (ulong seed = 1; seed <= 100; seed++)
                    {
                        levels++;
                        var map = new DungeonGenerator(settings).Generate(seed, depth).ToMap(null);
                        var plan = SealedRoomPlanner.Find(map, seed, depth, requireChest: true);
                        var avoid = plan != null ? new[] { plan.Door, plan.Lever } : null;
                        var exclude = plan != null ? new HashSet<Vector2Int>(plan.Region) : null;
                        var spot = PrisonerRules.Place(map, seed, depth, avoid, exclude);
                        if (spot == null)
                        {
                            continue;
                        }

                        placed++;
                        string where = $"profondità {depth}, seme {seed}";
                        var cell = spot.Value.Cell;
                        var facing = spot.Value.Facing;
                        Assert.IsTrue(map.IsRock(cell.x - facing.x, cell.y - facing.y), where + ": il muro alle spalle");
                        Assert.GreaterOrEqual(PrisonerRules.Escort(map, cell, exclude), 1, where + ": nessuna scorta");
                        if (plan != null)
                        {
                            sealedChecked++;
                            Assert.IsFalse(exclude.Contains(cell), where + ": nel fondo sigillato");
                            Assert.GreaterOrEqual(Mathf.Abs(cell.x - plan.Lever.x) + Mathf.Abs(cell.y - plan.Lever.y), 6, where + ": troppo vicino alla leva");
                            Assert.GreaterOrEqual(Mathf.Abs(cell.x - plan.Door.x) + Mathf.Abs(cell.y - plan.Door.y), 6, where + ": troppo vicino alla porta");
                        }
                    }
                }

                Debug.Log($"[M25 prigioniero] un posto in {placed} livelli su {levels} ({sealedChecked} con la stanza sigillata)");
                Assert.GreaterOrEqual(placed, levels * 0.7, "il prigioniero trova un posto in almeno il 70% dei livelli");
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}
