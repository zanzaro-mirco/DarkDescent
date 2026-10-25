using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Il Re delle ossa e la sua sala (D8 della M10).</summary>
    public class BossTests
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>("Assets/_Project/" + path);

        private static CaveSettings Caves => Asset<CaveSettings>("Data/Levels/CaveSettings.asset");

        [Test, Description("Le fasi per vita: mischia sopra il 60%, evocazione fino al 30%, furia sotto")]
        public void Phases_FollowTheLife()
        {
            Assert.AreEqual(BossPhase.Melee, BossPhases.For(600f, 600f));
            Assert.AreEqual(BossPhase.Melee, BossPhases.For(360f, 600f));
            Assert.AreEqual(BossPhase.Summon, BossPhases.For(359f, 600f));
            Assert.AreEqual(BossPhase.Summon, BossPhases.For(180f, 600f));
            Assert.AreEqual(BossPhase.Fury, BossPhases.For(179f, 600f));
            Assert.AreEqual(BossPhase.Fury, BossPhases.For(0f, 600f));
        }

        [Test, Description("Alla profondità 8 la caverna ha la sala del boss: il re al centro di 5 × 5 celle libere, una porta sola, nessuna scala; alla 7 no")]
        public void Depth8_HasTheBossRoom([Values(1UL, 2UL, 3UL, 7UL, 42UL, 99UL, 1234UL, 777777UL)] ulong seed)
        {
            Assert.AreEqual(8, Caves.BossDepth);
            var map = DungeonLevel.CreateMap(Caves, seed, 8, out _);
            MapChecks.Pass($"seme {seed}", CaveChecks.BossRoom(map));
            Assert.AreEqual(1, map.Markers.Count(m => m.Symbol == CavePopulator.GateSymbol), "una porta sola");

            Assert.IsFalse(DungeonLevel.CreateMap(Caves, seed, 7, out _).Markers.Any(m => m.Symbol == CavePopulator.BossSymbol));
        }

        [Test, Description("Alla profondità 8 c'è sempre il re, con la sala libera, anche quando la zona aperta più lontana a piedi è vicina all'ingresso in linea d'aria o la sala resta senza porta (ronde notturne del 23 ott 2026)")]
        public void Depth8_AlwaysHasTheKing([Values(455840980255010401UL, 8230148923540967550UL)] ulong seed)
        {
            MapChecks.Pass($"seme {seed}", CaveChecks.BossRoom(DungeonLevel.CreateMap(Caves, seed, 8, out _)));
        }

        [Test, Description("Nella sala senza porta il tesoro del re non chiude il passaggio verso la caverna dietro la parete sud (ronda notturna del 25 ott 2026)")]
        public void KingsTreasure_LeavesThePassageOpen([Values(6764078822790321387UL)] ulong seed)
        {
            DungeonLevel.CreateMap(Caves, seed, 8, out var built);
            MapChecks.Pass($"seme {seed}", MapChecks.Reachable(built, stairs: false) ?? MapChecks.ObstaclesLeavePath(built, stairs: false));
        }

        [Test, Description("L'anello della sala del re non lascia pavimenti uniti solo in diagonale attorno ai suoi angoli, nemmeno quando la sala resta senza porta (ronde notturne del 23 ott 2026)")]
        public void BossRing_LeavesNoDiagonals([Values(10720979491319261754UL, 9929082385448978859UL, 16557588204245213777UL, 8230148923540967550UL, 7000665500740811585UL, 9793415644043241166UL)] ulong seed)
        {
            var layout = Caves.CreateGenerator().Generate(SeedMixer.ForLevel(seed, 8), 8);
            MapChecks.Pass($"seme {seed}", CaveChecks.PillarsAndDiagonals(layout) ?? MapChecks.Reachable(layout, stairs: false) ?? CaveChecks.Shape(layout));
        }

        [Test, Description("Il bottino del re: sempre un oggetto almeno raro del livello, una pozione e molto oro")]
        public void BossLoot_IsGenerous()
        {
            var table = Asset<LootTable>("Data/Loot/BossLoot.asset");
            var brute = Asset<LootTable>("Data/Loot/BruteLoot.asset");
            Assert.AreEqual(Rarity.Normal, brute.MinRarity, "gli altri non cambiano");
            var generator = new ItemGenerator(Asset<AffixDatabase>("Data/AffixDatabase.asset").Affixes, RarityTable.Default);
            var roller = new LootRoller(generator, 5UL);
            for (int i = 0; i < 40; i++)
            {
                var item = roller.Roll(table, 8, i, 3 * i);
                Assert.IsNotNull(item);
                Assert.GreaterOrEqual(item.Rarity, Rarity.Rare);
                Assert.AreEqual(8, item.ItemLevel);
                Assert.IsNotNull(roller.RollPotion(table, 8, i, 3 * i));
                Assert.Greater(roller.RollGold(table, 8, i, 3 * i), 150);
            }
        }

        [Test, Description("Il prefab del re: il bruto più grande, con vita, nome, armi e poteri suoi; il tileset delle caverne lega K e G")]
        public void BossPrefab_IsWired()
        {
            var king = Asset<GameObject>("Prefabs/BoneKing.prefab");
            Assert.AreEqual("enemy.bone_king", king.GetComponent<EnemyAI>().Archetype.NameKey);
            Assert.IsTrue(king.GetComponent<EnemyAI>().Archetype.IsTelegraphed, "il colpo caricato del bruto");
            Assert.AreEqual(600f, king.GetComponent<Health>().Max);
            Assert.Greater(king.GetComponent<MeleeAttack>().Weapon.MaxDamage, Asset<GameObject>("Prefabs/Brute.prefab").GetComponent<MeleeAttack>().Weapon.MaxDamage);
            Assert.IsNotNull(king.GetComponent<BossController>());
            Assert.IsNotNull(king.GetComponent<TelegraphCircle>());
            Assert.AreEqual(Vector3.one * 1.8f, king.transform.Find("Model").localScale);

            var tileset = Asset<LevelTileset>("Data/Levels/CaveTileset.asset");
            Assert.AreEqual(king, tileset.GetMarkerPrefab(CavePopulator.BossSymbol));
            Assert.IsNotNull(tileset.GetMarkerPrefab(CavePopulator.GateSymbol).GetComponent<BossGate>());
        }
    }
}
