using System.Collections.Generic;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Tests
{
    /// <summary>I barili da rompere (D5 della M13): semi, loot, ricordi del livello e prefab.</summary>
    public class BarrelDataTests
    {
        private const string Prefabs = "Assets/_Project/Prefabs/Dungeon/";

        private static LootTable Table(string name) => AssetDatabase.LoadAssetAtPath<LootTable>($"Assets/_Project/Data/Loot/{name}.asset");

        private static LootRoller Roller(ulong seed)
        {
            var affixes = AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset").Affixes;
            return new LootRoller(new ItemGenerator(affixes, RarityTable.Default), seed);
        }

        private static string Describe(ItemInstance item)
        {
            return item == null ? "niente" : $"{item.Definition.name} {item.Rarity} L{item.ItemLevel} {item.Seed}";
        }

        [Test, Description("I semi del barile sono suoi: non coincidono con quelli di nemici, pozioni, oro e tiri in più della stessa cella")]
        public void BarrelSeeds_AreSeparateFromEnemiesAndChests()
        {
            for (int x = -3; x <= 3; x++)
            {
                ulong barrel = SeedMixer.ForBarrel(4711UL, 2, x * 40, 17);
                var others = new[]
                {
                    SeedMixer.ForEnemy(4711UL, 2, x * 40, 17), SeedMixer.ForPotion(4711UL, 2, x * 40, 17),
                    SeedMixer.ForGold(4711UL, 2, x * 40, 17), SeedMixer.ForExtraLoot(4711UL, 2, x * 40, 17, 0),
                };
                CollectionAssert.DoesNotContain(others, barrel);
                CollectionAssert.DoesNotContain(others, SeedMixer.ForBarrelPotion(4711UL, 2, x * 40, 17));
                CollectionAssert.DoesNotContain(others, SeedMixer.ForBarrelGold(4711UL, 2, x * 40, 17));
                Assert.AreNotEqual(barrel, SeedMixer.ForBarrelPotion(4711UL, 2, x * 40, 17));
                Assert.AreNotEqual(barrel, SeedMixer.ForBarrelGold(4711UL, 2, x * 40, 17));
            }
        }

        [Test, Description("Lo stesso barile lascia sempre le stesse cose, e non quelle del nemico partito dalla stessa cella")]
        public void RollBarrel_IsSeededAndNotTheEnemyRoll()
        {
            var roller = Roller(4711UL);
            var table = Table("SkeletonLoot");
            int same = 0;
            int drops = 0;
            for (int x = 0; x < 400; x++)
            {
                var item = roller.RollBarrel(table, 3, x, -12);
                Assert.AreEqual(Describe(item), Describe(roller.RollBarrel(table, 3, x, -12)));
                Assert.AreEqual(Describe(roller.RollBarrelPotion(table, 3, x, -12)), Describe(roller.RollBarrelPotion(table, 3, x, -12)));
                Assert.AreEqual(roller.RollBarrelGold(table, 3, x, -12), roller.RollBarrelGold(table, 3, x, -12));
                var enemy = roller.Roll(table, 3, x, -12);
                if (item != null)
                {
                    drops++;
                    same += Describe(item) == Describe(enemy) ? 1 : 0;
                }
            }

            Assert.Greater(drops, 0);
            Assert.AreEqual(0, same, "il barile non ripete il tiro del nemico");
        }

        [Test, Description("Il loot dei barili è raro: oggetto 5%, pozione 5% (3 di vita e 2 di mana), oro 15% tra 2 e 5 alla profondità 1")]
        public void BarrelLoot_IsRare()
        {
            var roller = Roller(99UL);
            var table = Table("BarrelLoot");
            int items = 0;
            int potions = 0;
            int golds = 0;
            for (int x = 0; x < 4000; x++)
            {
                items += roller.RollBarrel(table, 1, x, 5) != null ? 1 : 0;
                potions += roller.RollBarrelPotion(table, 1, x, 5) != null ? 1 : 0;
                int gold = roller.RollBarrelGold(table, 1, x, 5);
                if (gold > 0)
                {
                    golds++;
                    Assert.That(gold, Is.InRange(2, 5));
                }
            }

            Assert.That(items, Is.InRange(140, 260), "un oggetto ogni venti barili");
            Assert.That(potions, Is.InRange(140, 260), "una pozione ogni venti barili");
            Assert.That(golds, Is.InRange(500, 700), "oro da un barile su sette");
        }

        [Test, Description("Il ricordo tiene i barili rotti e quanti erano; uno di prima della M13 non ne ha, e nemici e casse restano validi")]
        public void LevelRecord_RemembersBrokenBarrels()
        {
            var record = new LevelRecord("Level_Crypt", 1, 10, 2, 4);
            record.AddBroken(1);
            record.AddBroken(3);
            Assert.IsTrue(record.Matches(10, 2));
            Assert.IsTrue(record.MatchesBreakables(4));
            Assert.IsFalse(record.MatchesBreakables(5));

            var read = JsonUtility.FromJson<LevelRecord>(JsonUtility.ToJson(record));
            CollectionAssert.AreEqual(new[] { 1, 3 }, read.Broken);
            Assert.IsTrue(read.MatchesBreakables(4));

            const string old = "{\"_scene\":\"Level_Crypt\",\"_depth\":1,\"_enemyCount\":10,\"_chestCount\":2,\"_killed\":[3],\"_opened\":[0],\"_items\":[]}";
            var before = JsonUtility.FromJson<LevelRecord>(old);
            Assert.IsTrue(before.Matches(10, 2), "nemici e casse del salvataggio di prima valgono ancora");
            CollectionAssert.AreEqual(new[] { 3 }, before.Killed);
            Assert.AreEqual(0, before.Broken.Count);
            Assert.IsFalse(before.MatchesBreakables(3), "i barili del livello ripartono interi");
        }

        [Test, Description("I tre barili da rompere: layer dei nemici, capsula e ostacolo che ritaglia il NavMesh, loot dei barili, tre suoni, pezzi spenti")]
        public void BarrelPrefabs_AreBreakable()
        {
            foreach (var name in new[] { "Barrel_Large", "Barrel_Small", "Barrel_Decorated" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab");
                Assert.IsNotNull(prefab, name);
                Assert.AreEqual(LayerMask.NameToLayer("Enemy"), prefab.layer, name);
                var barrel = prefab.GetComponent<Breakable>();
                Assert.IsNotNull(barrel, name);
                Assert.AreSame(Table("BarrelLoot"), barrel.LootTable, name);

                var so = new SerializedObject(barrel);
                Assert.IsTrue(so.FindProperty("_intact").objectReferenceValue is GameObject intact && intact.activeSelf, name);
                Assert.IsTrue(so.FindProperty("_broken").objectReferenceValue is GameObject broken && !broken.activeSelf, name);
                Assert.IsTrue(so.FindProperty("_hitArea").objectReferenceValue is CapsuleCollider, name);
                Assert.IsTrue(so.FindProperty("_burst").objectReferenceValue is ParticleSystem burst && !burst.main.playOnAwake, name);
                Assert.IsNotNull(so.FindProperty("_groundItemPrefab").objectReferenceValue, name);
                var clips = so.FindProperty("_breakClips");
                Assert.AreEqual(3, clips.arraySize, name);
                for (int i = 0; i < clips.arraySize; i++)
                {
                    Assert.IsNotNull(clips.GetArrayElementAtIndex(i).objectReferenceValue, name);
                }

                var obstacle = prefab.GetComponent<NavMeshObstacle>();
                Assert.IsTrue(obstacle.carving, name);
                Assert.AreEqual(obstacle.radius, barrel.Radius, 0.001f, name);
                Assert.That(barrel.Radius, Is.InRange(0.45f, 1f), name);
            }
        }

        [Test, Description("Nella cripta il barile 'b' e quelli del gruppo si rompono; in città il barile 'b' resta un arredo")]
        public void Tilesets_UseTheBreakableBarrels()
        {
            var dungeon = AssetDatabase.LoadAssetAtPath<LevelTileset>("Assets/_Project/Data/Levels/DungeonTileset.asset");
            var town = AssetDatabase.LoadAssetAtPath<LevelTileset>("Assets/_Project/Data/Levels/TownTileset.asset");
            Assert.IsNotNull(dungeon.GetMarkerPrefab('b').GetComponent<Breakable>());
            Assert.IsNull(town.GetMarkerPrefab('b').GetComponent<Breakable>());

            var group = dungeon.GetMarkerPrefab(DungeonPopulator.BarrelGroupSymbol);
            Assert.AreEqual(3, group.GetComponentsInChildren<Breakable>(true).Length);
            Assert.IsFalse(group.GetComponentsInChildren<BoxCollider>(true).Any(), "i collider dei barili di prima se ne sono andati");
        }

        [Test, Description("Nella cripta da 4 a 8 barili per livello e nelle caverne da 2 a 5, singoli e gruppi: quasi sempre almeno il minimo, e i due gruppi contro il muro escono tutti e due")]
        public void Levels_HaveTheirBarrels()
        {
            var crypt = AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");
            var caves = AssetDatabase.LoadAssetAtPath<CaveSettings>("Assets/_Project/Data/Levels/CaveSettings.asset");
            Assert.AreEqual(new Vector2Int(4, 8), crypt.BarrelsPerLevel);
            Assert.AreEqual(new Vector2Int(2, 5), caves.BarrelsPerLevel);

            var seen = new HashSet<char>();
            int cryptShort = 0;
            int cryptTotal = 0;
            int caveShort = 0;
            int caveTotal = 0;
            var cryptGenerator = crypt.CreateGenerator();
            var caveGenerator = caves.CreateGenerator();
            for (ulong seed = 1; seed <= 100; seed++)
            {
                var layout = cryptGenerator.Generate(seed, 1 + (int)(seed % 4));
                int barrels = DungeonPopulator.CountBarrels(layout);
                cryptTotal += barrels;
                cryptShort += barrels < 4 ? 1 : 0;
                Collect(layout, seen);

                // nelle caverne il 'b' è roccia: contano solo i gruppi
                int depth = caves.FirstDepth + (int)(seed % (ulong)(caves.LastDepth - caves.FirstDepth + 1));
                var cave = caveGenerator.Generate(seed, depth);
                int inCave = 0;
                for (int y = 0; y < cave.Height; y++)
                {
                    for (int x = 0; x < cave.Width; x++)
                    {
                        inCave += DungeonPopulator.BarrelsInGroup(cave[x, y]);
                    }
                }

                caveTotal += inCave;
                caveShort += inCave < 2 ? 1 : 0;
                Collect(cave, seen);
            }

            Assert.LessOrEqual(cryptShort, 5, "livelli della cripta con meno di 4 barili");
            Assert.LessOrEqual(caveShort, 5, "caverne con meno di 2 barili");
            Assert.That(cryptTotal / 100f, Is.InRange(5f, 10f), "barili per livello nella cripta");
            Assert.That(caveTotal / 100f, Is.InRange(2.5f, 5.5f), "barili per livello nelle caverne");
            foreach (char symbol in DungeonPopulator.BarrelWallSymbols)
            {
                Assert.IsTrue(seen.Contains(symbol), $"il gruppo '{symbol}' non esce mai");
            }
        }

        private static void Collect(DungeonLayout layout, HashSet<char> seen)
        {
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    seen.Add(layout[x, y]);
                }
            }
        }

        [Test, Description("Negli arredi della cripta nessun barile in piedi resta intero per sempre: o si rompe, o è una botte, o è a terra o in pila")]
        public void DungeonDecor_HasNoStandingBarrelThatNeverBreaks()
        {
            var barrels = new[] { "barrel_large.fbx", "barrel_small.fbx", "barrel_large_decorated.fbx" };
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Dungeon/Decor" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    string mesh = System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(filter.sharedMesh));
                    bool standing = Vector3.Dot(filter.transform.up, Vector3.up) > 0.9f;
                    if (barrels.Contains(mesh) && standing)
                    {
                        Assert.IsNotNull(filter.GetComponentInParent<Breakable>(true), $"{prefab.name}/{filter.name}");
                    }
                }
            }
        }
    }
}
