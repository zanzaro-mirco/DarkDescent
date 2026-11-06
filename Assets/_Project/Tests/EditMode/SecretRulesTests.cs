using System.Collections.Generic;
using System.Linq;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Le regole dei segreti delle casse (25.3 della M25, D3, D4 e D8): chi è un mimic, chi è il baule maledetto, quanti nemici chiama.</summary>
    public class SecretRulesTests
    {
        private static List<Vector2Int> Cells(int count, int salt = 0)
        {
            var cells = new List<Vector2Int>();
            for (int i = 0; i < count; i++)
            {
                cells.Add(new Vector2Int(120 + i * 70 + salt, 40 + i * 30 - salt));
            }

            return cells;
        }

        [Test, Description("Alla profondità 1 nessuna cassa è un mimic, e nessun livello alle profondità 1 e 2 ha un baule maledetto (D3 e D4)")]
        public void TheFirstDepths_HaveNoSecrets()
        {
            for (ulong seed = 1; seed <= 200; seed++)
            {
                foreach (var cell in Cells(6))
                {
                    Assert.IsFalse(SecretRules.IsMimic(seed, 1, cell.x, cell.y), "nessun mimic alla 1");
                }

                Assert.IsFalse(SecretRules.LevelHasCursed(seed, 1));
                Assert.IsFalse(SecretRules.LevelHasCursed(seed, 2));
                Assert.AreEqual(-1, SecretRules.PickCursed(seed, 2, Cells(5)));
            }
        }

        [Test, Description("Lo stesso mondo, la stessa profondità e la stessa cella dànno sempre lo stesso segreto (D3)")]
        public void TheSameWorldGivesTheSameSecrets()
        {
            var cells = Cells(8);
            for (ulong seed = 1; seed <= 50; seed++)
            {
                for (int depth = 2; depth <= 8; depth++)
                {
                    Assert.AreEqual(SecretRules.PickCursed(seed, depth, cells), SecretRules.PickCursed(seed, depth, cells));
                    foreach (var cell in cells)
                    {
                        Assert.AreEqual(SecretRules.IsMimic(seed, depth, cell.x, cell.y), SecretRules.IsMimic(seed, depth, cell.x, cell.y));
                    }
                }
            }
        }

        [Test, Description("Dalla profondità 2 una cassa su otto è un mimic, con un margine: né quasi tutte né quasi nessuna (D3)")]
        public void AboutOneChestInEightIsAMimic()
        {
            int mimics = 0;
            int total = 0;
            for (ulong seed = 1; seed <= 400; seed++)
            {
                foreach (var cell in Cells(8, (int)seed))
                {
                    total++;
                    if (SecretRules.IsMimic(seed, 3, cell.x, cell.y))
                    {
                        mimics++;
                    }
                }
            }

            double share = (double)mimics / total;
            Assert.That(share, Is.InRange(0.10, 0.15), $"{mimics} mimic su {total} casse");
        }

        [Test, Description("Dalla profondità 3 un livello su due ha un baule maledetto (D4)")]
        public void AboutOneLevelInTwoHasACursedChest()
        {
            int levels = 0;
            for (ulong seed = 1; seed <= 600; seed++)
            {
                if (SecretRules.LevelHasCursed(seed, 3))
                {
                    levels++;
                }
            }

            Assert.That(levels / 600.0, Is.InRange(0.42, 0.58), $"{levels} livelli su 600");
        }

        [Test, Description("Il baule maledetto è una cassa del livello, mai un mimic, e la scelta non cambia con chi la chiama (D4)")]
        public void TheCursedChestIsOneOfTheChestsAndNeverAMimic()
        {
            int found = 0;
            for (ulong seed = 1; seed <= 300; seed++)
            {
                var cells = Cells(7, (int)seed);
                int pick = SecretRules.PickCursed(seed, 4, cells);
                if (!SecretRules.LevelHasCursed(seed, 4))
                {
                    Assert.AreEqual(-1, pick);
                    continue;
                }

                found++;
                Assert.That(pick, Is.InRange(0, cells.Count - 1));
                Assert.IsFalse(SecretRules.IsMimic(seed, 4, cells[pick].x, cells[pick].y), "un mimic non è il baule maledetto");

                // le stesse celle in un altro ordine, dallo stesso mondo: la stessa cella
                var shuffled = cells.AsEnumerable().Reverse().ToList();
                int other = SecretRules.PickCursed(seed, 4, shuffled);
                Assert.AreEqual(cells[pick], shuffled[other], "la scelta sta nella cella, non nell'ordine");
            }

            Assert.Greater(found, 100, "ci sono livelli con il baule");
        }

        [Test, Description("L'ondata è di tre, quattro o cinque nemici, e tutte e tre le taglie compaiono (D4)")]
        public void TheWaveIsThreeToFive()
        {
            var sizes = new HashSet<int>();
            for (ulong seed = 1; seed <= 200; seed++)
            {
                int size = SecretRules.WaveSize(seed, 3, 120, 40);
                Assert.That(size, Is.InRange(SecretRules.WaveMin, SecretRules.WaveMax));
                sizes.Add(size);
            }

            CollectionAssert.AreEquivalent(new[] { 3, 4, 5 }, sizes);
        }

        [Test, Description("Nella cripta i bruti dell'ondata sono nessuno o uno (solo con cinque nemici), alle caverne due, e non sono mai più dei nemici")]
        public void TheWaveBrutesGrowWithTheDepth()
        {
            Assert.AreEqual(0, SecretRules.WaveBrutes(3, 3));
            Assert.AreEqual(0, SecretRules.WaveBrutes(4, 4));
            Assert.AreEqual(1, SecretRules.WaveBrutes(4, 5));
            Assert.AreEqual(2, SecretRules.WaveBrutes(5, 3));
            Assert.AreEqual(2, SecretRules.WaveBrutes(8, 5));
            for (int depth = 3; depth <= 8; depth++)
            {
                for (int size = SecretRules.WaveMin; size <= SecretRules.WaveMax; size++)
                {
                    Assert.LessOrEqual(SecretRules.WaveBrutes(depth, size), size);
                }
            }
        }

        [Test, Description("Il premio del baule maledetto (D4 e D8): un oggetto in più, magici e rari più frequenti, almeno un magico, più oro, e gli unici solo un po' più frequenti")]
        public void TheCursedBonusIsAModestStepUp()
        {
            var bonus = ChestSecret.CursedBonus;
            Assert.AreEqual(1, bonus.ExtraRolls);
            Assert.AreEqual(Rarity.Magic, bonus.Guaranteed);
            Assert.AreEqual(2f, bonus.GoldFactor);
            Assert.IsFalse(bonus.ReplacesRoll, "il tiro di sempre resta");
            Assert.LessOrEqual(bonus.NamedFactor, 3f, "non più degli unici del forziere (D8)");
            Assert.AreEqual(0f, bonus.NamedGuarantee, "nessun unico garantito: è un'alternativa più rischiosa alle casse, non una strada facile");
        }

        [Test, Description("Il segreto di una cassa e il seme dei suoi nemici non toccano gli altri semi: la stessa cella ha segreti diversi in mondi diversi, ma non cambia il loot")]
        public void SecretsHaveTheirOwnSeedStream()
        {
            Assert.AreNotEqual(Core.SeedMixer.ForEnemy(5, 3, 120, 40), Core.SeedMixer.ForSecret(5, 3, 120, 40, 1UL));
            Assert.AreNotEqual(Core.SeedMixer.ForSecret(5, 3, 120, 40, 1UL), Core.SeedMixer.ForSecret(5, 3, 120, 40, 2UL));
            Assert.AreNotEqual(Core.SeedMixer.ForSecret(5, 3, 120, 40, 1UL), Core.SeedMixer.ForSecret(6, 3, 120, 40, 1UL));
        }
    }
}
