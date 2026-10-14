using System.Collections.Generic;
using System.Linq;
using DarkDescent.Combat;
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
            var king = map.Markers.Single(m => m.Symbol == CavePopulator.BossSymbol);
            var gate = map.Markers.Single(m => m.Symbol == CavePopulator.GateSymbol);
            Assert.IsFalse(map.Markers.Any(m => m.Symbol == '>'), "l'ultima profondità non ha scala");

            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    Assert.IsTrue(map.IsFloor(king.X + dx, king.Y + dy), $"la sala è piena in ({dx}, {dy})");
                }
            }

            Assert.IsFalse(map.Markers.Any(m => CavePopulator.IsEnemy(m.Symbol) && m.Symbol != CavePopulator.BossSymbol
                && Mathf.Max(Mathf.Abs(m.X - king.X), Mathf.Abs(m.Y - king.Y)) <= 3), "nella sala c'è solo il re");
            Assert.AreEqual(3, Mathf.Max(Mathf.Abs(gate.X - king.X), Mathf.Abs(gate.Y - king.Y)), "la porta sta nell'anello attorno alla sala");

            var entrance = map.Markers.Single(m => m.Symbol == '<');
            Assert.IsTrue(Reaches(map, entrance, king, blockGate: false), "dalla porta si entra");
            Assert.IsFalse(Reaches(map, entrance, king, blockGate: true), "chiusa la porta, non si entra da nessun'altra parte");

            Assert.IsFalse(DungeonLevel.CreateMap(Caves, seed, 7, out _).Markers.Any(m => m.Symbol == CavePopulator.BossSymbol));
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

        // a piedi in quattro direzioni dall'ingresso; con la porta chiusa la cella della porta è roccia
        private static bool Reaches(LevelMap map, MapMarker from, MapMarker to, bool blockGate)
        {
            var seen = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(from.X, from.Y));
            seen.Add(new Vector2Int(from.X, from.Y));
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (cell.x == to.X && cell.y == to.Y)
                {
                    return true;
                }

                foreach (var step in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
                {
                    var next = cell + step;
                    bool gate = map.Markers.Any(m => m.Symbol == CavePopulator.GateSymbol && m.X == next.x && m.Y == next.y);
                    if (map.IsFloor(next.x, next.y) && !(blockGate && gate) && seen.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return false;
        }
    }
}
