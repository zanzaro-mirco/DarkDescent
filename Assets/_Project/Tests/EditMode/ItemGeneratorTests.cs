using System.Collections.Generic;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class ItemGeneratorTests
    {
        private const string AffixesFolder = "Assets/_Project/Data/Affixes";

        private static AffixDatabase Affixes => AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");
        private static ItemDatabase Items => AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
        private static ItemDefinition Sword => AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/ShortSword.asset");
        private static ItemDefinition Shield => AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/BadgeShield.asset");

        private static ItemGenerator Generator => new ItemGenerator(Affixes.Affixes, RarityTable.Default);

        private static string Describe(ItemInstance item)
        {
            return $"{item.Rarity} [{string.Join(", ", item.Affixes.Select(a => $"{a.Definition.name} {a.Value}"))}]";
        }

        [Test, Description("SplitMix64 dà i valori di riferimento: lo stesso seme fa la stessa sequenza su ogni piattaforma")]
        public void SplitMix64_MatchesReferenceValues()
        {
            var random = new SplitMix64Source(0UL);

            Assert.AreEqual(0xE220A8397B1DCDAFUL, random.NextUInt64());
            Assert.AreEqual(0x6E789E6AA1B965F4UL, random.NextUInt64());
            Assert.AreEqual(0x06C45D188009454FUL, random.NextUInt64());

            var unit = new SplitMix64Source(4711UL);
            for (int i = 0; i < 1000; i++)
            {
                Assert.That(unit.NextDouble(), Is.InRange(0.0, 0.9999999999));
            }
        }

        [Test, Description("Stesso seme, stesso oggetto, sempre; semi diversi danno oggetti diversi")]
        public void SameSeed_SameItem()
        {
            var first = Generator;
            var second = Generator;
            var seen = new HashSet<string>();
            for (ulong seed = 1; seed <= 300; seed++)
            {
                var a = first.Generate(Sword, 2, seed);
                var b = second.Generate(Sword, 2, seed);
                Assert.AreEqual(Describe(a), Describe(b), $"seme {seed}");
                Assert.AreEqual(seed, a.Seed);
                Assert.AreEqual(2, a.ItemLevel);
                seen.Add(Describe(a));
            }

            Assert.Greater(seen.Count, 50, "trecento semi devono dare molti oggetti diversi");
        }

        [Test, Description("Su 100.000 estrazioni le rarità restano entro l'1% da 65/28/7: con 10.000 lo scarto normale sul 65% è già mezzo punto")]
        public void Rarities_FollowTheTable()
        {
            var random = new SplitMix64Source(12345UL);
            var counts = new int[3];
            const int draws = 100000;
            for (int i = 0; i < draws; i++)
            {
                counts[(int)RarityTable.Default.Roll(random)]++;
            }

            Assert.AreEqual(0.65, counts[0] / (double)draws, 0.01, "normali");
            Assert.AreEqual(0.28, counts[1] / (double)draws, 0.01, "magici");
            Assert.AreEqual(0.07, counts[2] / (double)draws, 0.01, "rari");
        }

        [Test, Description("Un oggetto di livello 1 non ha mai affissi di livello 2; a livello 2 compaiono")]
        public void Affixes_RespectItemLevel()
        {
            var generator = Generator;
            bool levelTwoSeen = false;
            for (ulong seed = 1; seed <= 1500; seed++)
            {
                foreach (var affix in generator.Generate(Sword, 1, seed, Rarity.Rare).Affixes)
                {
                    Assert.AreEqual(1, affix.Definition.MinItemLevel, $"seme {seed}: {affix.Definition.name} a livello 1");
                }

                foreach (var affix in generator.Generate(Shield, 2, seed, Rarity.Rare).Affixes)
                {
                    levelTwoSeen |= affix.Definition.MinItemLevel == 2;
                }
            }

            Assert.IsTrue(levelTwoSeen, "a livello 2 gli affissi di livello 2 devono uscire");
        }

        [Test, Description("Magico 1–2 affissi, raro 3–4; al più 1 o 2 prefissi e suffissi; mai due dello stesso gruppo; solo affissi ammessi")]
        public void Affixes_RespectRarityLimitsGroupsAndTargets()
        {
            var generator = Generator;
            for (ulong seed = 1; seed <= 1000; seed++)
            {
                foreach (var item in new[] { Sword, Shield })
                {
                    foreach (var rarity in new[] { Rarity.Normal, Rarity.Magic, Rarity.Rare })
                    {
                        var generated = generator.Generate(item, 2, seed, rarity);
                        var affixes = generated.Affixes;
                        int max = ItemGenerator.MaxPerKind(rarity);
                        string what = $"{item.name} {Describe(generated)}";

                        if (rarity == Rarity.Normal)
                        {
                            Assert.IsEmpty(affixes, what);
                        }
                        else if (rarity == Rarity.Magic)
                        {
                            Assert.That(affixes.Count, Is.InRange(1, 2), what);
                        }
                        else
                        {
                            Assert.That(affixes.Count, Is.InRange(3, 4), what);
                        }

                        Assert.LessOrEqual(affixes.Count(a => a.Definition.Kind == AffixKind.Prefix), max, what);
                        Assert.LessOrEqual(affixes.Count(a => a.Definition.Kind == AffixKind.Suffix), max, what);
                        Assert.AreEqual(affixes.Count, affixes.Select(a => a.Definition.Group).Distinct().Count(), $"gruppo ripetuto: {what}");
                        foreach (var affix in affixes)
                        {
                            Assert.IsTrue((affix.Definition.Targets & item.AffixTarget) != 0, $"affisso non ammesso: {what}");
                            Assert.That(affix.Value, Is.InRange(affix.Definition.Min, affix.Definition.Max), what);
                        }
                    }
                }
            }
        }

        [Test, Description("Un oggetto con affissi passa per JsonUtility e torna uguale: rarità, livello, seme, affissi e valori")]
        public void ItemWithAffixes_SurvivesJson()
        {
            var item = Generator.Generate(Shield, 2, 4711UL, Rarity.Rare);

            string json = JsonUtility.ToJson(item);
            var loaded = JsonUtility.FromJson<ItemInstance>(json);

            Assert.IsTrue(loaded.Resolve(Items, Affixes));
            Assert.AreEqual(Describe(item), Describe(loaded));
            Assert.AreEqual(Rarity.Rare, loaded.Rarity);
            Assert.AreEqual(2, loaded.ItemLevel);
            Assert.AreEqual(4711UL, loaded.Seed);
            StringAssert.DoesNotContain("Robust", json, "il nome non si salva");
        }

        [Test, Description("Il database degli affissi contiene esattamente Data/Affixes, con ID unici e intervalli sensati")]
        public void AffixDatabase_IsComplete()
        {
            var expected = AssetDatabase.FindAssets("t:AffixDefinition", new[] { AffixesFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<AffixDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();

            Assert.AreEqual(12, expected.Count, "gli undici affissi della M5 e il Letale dei gioielli (M8)");
            CollectionAssert.AreEquivalent(expected, Affixes.Affixes);
            Assert.AreEqual(expected.Count, expected.Select(a => a.Id).Distinct().Count(), "ID ripetuti");
            foreach (var affix in expected)
            {
                Assert.IsFalse(string.IsNullOrEmpty(affix.Id), affix.name);
                Assert.LessOrEqual(affix.Min, affix.Max, affix.name);
                Assert.AreNotEqual(AffixTargets.None, affix.Targets, affix.name);
                Assert.IsTrue(Affixes.TryGet(affix.Id, out var found) && found == affix);
            }
        }
    }
}
