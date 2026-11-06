using System.Linq;
using DarkDescent.Core;
using DarkDescent.Editor;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Il loot dei segreti misurato (passo 25.7, D8 della M25): il baule maledetto dà di più, ma le fasce della M14 reggono ancora.</summary>
    public class SecretLootTests
    {
        private const int Seeds = 40;

        private static ChampionSettings Champions => AssetDatabase.LoadAssetAtPath<ChampionSettings>("Assets/_Project/Data/Enemies/ChampionSettings.asset");

        private static LootCensus.Tally Whole(bool secrets)
        {
            var crypt = AssetDatabase.LoadAssetAtPath<DungeonSettings>(LootCensus.CryptSettings);
            var caves = AssetDatabase.LoadAssetAtPath<DungeonSettings>(LootCensus.CaveSettings);
            var total = LootCensus.Range(crypt, AssetDatabase.LoadAssetAtPath<LevelTileset>(LootCensus.CryptTileset), Champions, 1, 4, Seeds, null, secrets);
            total.Merge(LootCensus.Range(caves, AssetDatabase.LoadAssetAtPath<LevelTileset>(LootCensus.CaveTileset), Champions, 5, 8, Seeds, null, secrets));
            return total;
        }

        [Test, Description("Con i bauli maledetti il loot a livello sale di meno di mezzo oggetto e rari e magici restano nelle fasce della M14; senza segreti la fascia degli oggetti regge")]
        public void WithSecrets_TheBandsStillHold()
        {
            var plain = Whole(false);
            var withSecrets = Whole(true);
            Assert.AreEqual(plain.Levels, withSecrets.Levels);
            Debug.Log($"[SecretLoot] senza segreti: {CensusChecks.DescribeLoot(plain)}\n[SecretLoot] con i segreti: {CensusChecks.DescribeLoot(withSecrets)}\n"
                + $"[SecretLoot] differenza: {withSecrets.PerLevel - plain.PerLevel:+0.000;-0.000} oggetti a livello; baule maledetto {(withSecrets.BySource.TryGetValue("baule maledetto", out double cursed) ? cursed / withSecrets.Levels : 0.0):0.000} a livello");

            // La misura del 25.7 ha trovato 4,99 senza segreti e 5,39 con il baule maledetto: sopra la fascia di 5,2. Il baule è un premio facoltativo
            // (si salta, e costa un'ondata), come Vorrak e il suo tesoro nella M24 (D17): la fascia degli oggetti resta misurata senza segreti
            // (LootCensusTests), e i segreti hanno un tetto loro, mezzo oggetto a livello; le parti di rari e magici restano nelle fasce della M14
            Assert.GreaterOrEqual(plain.PerLevel, 4.0);
            Assert.LessOrEqual(plain.PerLevel, 5.2);
            Assert.Greater(withSecrets.PerLevel, plain.PerLevel, "il baule maledetto dà qualcosa in più");
            Assert.Less(withSecrets.PerLevel - plain.PerLevel, 0.5, "ma non più di mezzo oggetto a livello");
            Assert.That(withSecrets.Rare / withSecrets.Items, Is.InRange(0.1, 0.25), "parte dei rari con i segreti");
            Assert.That(withSecrets.Magic / withSecrets.Items, Is.InRange(0.25, 0.35), "parte dei magici con i segreti");
        }

        [Test, Description("Il baule maledetto vale una cassa normale più un oggetto, e nessun livello ne ha più di uno")]
        public void CursedChest_IsOneItemAboveANormalOne()
        {
            var crypt = AssetDatabase.LoadAssetAtPath<DungeonSettings>(LootCensus.CryptSettings);
            var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>(LootCensus.CryptTileset);
            int levelsWithCursed = 0;
            int levels = 0;
            for (ulong seed = 1; seed <= 60; seed++)
            {
                for (int depth = 1; depth <= 4; depth++)
                {
                    levels++;
                    var sources = LootCensus.Sources(crypt, tileset, Champions, seed * 7919UL, depth, true);
                    int cursed = sources.Count(s => s.Name == "baule maledetto");
                    Assert.LessOrEqual(cursed, 1, $"seme {seed} profondità {depth}: più di un baule maledetto");
                    if (depth < SecretRules.CursedFirstDepth)
                    {
                        Assert.AreEqual(0, cursed, $"seme {seed} profondità {depth}: baule maledetto troppo presto");
                    }

                    if (cursed == 0)
                    {
                        continue;
                    }

                    levelsWithCursed++;
                    var source = sources.First(s => s.Name == "baule maledetto");
                    var plain = LootCensus.Count(new[] { new LootCensus.Source("casse", source.Table, null, 1f) }, depth);
                    var premium = LootCensus.Count(new[] { source }, depth);
                    Assert.AreEqual(plain.Items + 1.0, premium.Items, 1e-6, "una cassa normale più un oggetto sicuro");
                    Assert.Greater(premium.Rare, plain.Rare, "più rari");
                    Assert.AreEqual(1, source.DepthOffset, "il premio è della profondità dopo");
                }
            }

            Assert.Greater(levelsWithCursed, 0, "con 60 semi qualche livello della cripta ha il baule");
            Debug.Log($"[SecretLoot] cripta: baule maledetto in {levelsWithCursed} livelli su {levels}");
        }
    }
}
