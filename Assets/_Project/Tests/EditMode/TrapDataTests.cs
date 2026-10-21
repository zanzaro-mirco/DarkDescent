using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Le trappole (D8 della M13): numeri, generatore, prefab e tileset.</summary>
    public class TrapDataTests
    {
        private static LevelTileset Tileset(string name) => AssetDatabase.LoadAssetAtPath<LevelTileset>($"Assets/_Project/Data/Levels/{name}.asset");

        private static int Count(DungeonLayout layout, char symbol)
        {
            int count = 0;
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    count += layout[x, y] == symbol ? 1 : 0;
                }
            }

            return count;
        }

        [Test, Description("Gli spuntoni tolgono il 10% della vita alla profondità 3, due punti in più a profondità, al più il 25%; l'esplosione fa 15 + 6 a profondità")]
        public void TrapRules_GrowWithDepth()
        {
            Assert.AreEqual(0.10f, TrapRules.SpikeFraction(3), 1e-5f);
            Assert.AreEqual(0.14f, TrapRules.SpikeFraction(5), 1e-5f);
            Assert.AreEqual(0.20f, TrapRules.SpikeFraction(8), 1e-5f);
            Assert.AreEqual(0.25f, TrapRules.SpikeFraction(30), 1e-5f);
            Assert.AreEqual(0.10f, TrapRules.SpikeFraction(1), 1e-5f);
            Assert.AreEqual(39f, TrapRules.ExplosionDamage(4));
            Assert.AreEqual(63f, TrapRules.ExplosionDamage(8));
        }

        [Test, Description("Nessun barile esplosivo prima della profondità 4, poi uno su cinque, sempre lo stesso dalla stessa cella")]
        public void ExplosiveBarrels_OneInFiveFromDepthFour()
        {
            int explosive = 0;
            for (int x = 0; x < 2000; x++)
            {
                Assert.IsFalse(TrapRules.IsExplosiveBarrel(4711UL, 3, x, 7));
                bool now = TrapRules.IsExplosiveBarrel(4711UL, 4, x, 7);
                Assert.AreEqual(now, TrapRules.IsExplosiveBarrel(4711UL, 4, x, 7));
                explosive += now ? 1 : 0;
            }

            Assert.That(explosive, Is.InRange(330, 470), "uno su cinque");
        }

        [Test, Description("Nella cripta niente spuntoni prima della profondità 3, poi da 0 a 2 per livello, nei corridoi dritti o al centro di una stanza; si attraversano")]
        public void Crypt_PlacesSpikesFromDepthThree()
        {
            var crypt = AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");
            Assert.AreEqual(new Vector2Int(0, 2), crypt.SpikesPerLevel);
            var generator = crypt.CreateGenerator();
            int inCorridors = 0;
            int inRooms = 0;
            for (ulong seed = 1; seed <= 100; seed++)
            {
                Assert.AreEqual(0, Count(generator.Generate(seed, 1 + (int)(seed % 2)), DungeonPopulator.SpikeSymbol), $"seme {seed}: spuntoni prima della 3");
                var layout = generator.Generate(seed, 3 + (int)(seed % 2));
                Assert.LessOrEqual(Count(layout, DungeonPopulator.SpikeSymbol), 2, $"seme {seed}");
                for (int y = 0; y < layout.Height; y++)
                {
                    for (int x = 0; x < layout.Width; x++)
                    {
                        if (layout[x, y] != DungeonPopulator.SpikeSymbol)
                        {
                            continue;
                        }

                        bool room = layout.Rooms.Any(r => r.Contains(new Vector2Int(x, y)));
                        inRooms += room ? 1 : 0;
                        inCorridors += room ? 0 : 1;
                    }
                }
            }

            Assert.Greater(inCorridors, 20, "spuntoni nei corridoi");
            Assert.Greater(inRooms, 3, "spuntoni come punto d'interesse");
            Assert.IsFalse(DungeonPopulator.IsObstacle(DungeonPopulator.SpikeSymbol), "si attraversano");
        }

        [Test, Description("Nelle caverne da 1 a 3 spuntoni per livello, nei cunicoli, lontani dall'ingresso")]
        public void Caves_PlaceSpikesInPassages()
        {
            var caves = AssetDatabase.LoadAssetAtPath<CaveSettings>("Assets/_Project/Data/Levels/CaveSettings.asset");
            Assert.AreEqual(new Vector2Int(1, 3), caves.SpikesPerLevel);
            var generator = caves.CreateGenerator();
            int total = 0;
            for (ulong seed = 1; seed <= 60; seed++)
            {
                var layout = generator.Generate(seed, caves.FirstDepth + (int)(seed % 3));
                int count = Count(layout, DungeonPopulator.SpikeSymbol);
                Assert.LessOrEqual(count, 3, $"seme {seed}");
                total += count;
                for (int y = 0; y < layout.Height; y++)
                {
                    for (int x = 0; x < layout.Width; x++)
                    {
                        if (layout[x, y] != DungeonPopulator.SpikeSymbol)
                        {
                            continue;
                        }

                        bool northSouth = !layout.IsFloor(x, y - 1) && !layout.IsFloor(x, y + 1);
                        bool eastWest = !layout.IsFloor(x - 1, y) && !layout.IsFloor(x + 1, y);
                        Assert.IsTrue(northSouth || eastWest, $"seme {seed}: ({x}, {y}) fuori da un cunicolo");
                        Assert.GreaterOrEqual(Mathf.Max(Mathf.Abs(x - layout.Entrance.x), Mathf.Abs(y - layout.Entrance.y)), 4, $"seme {seed}: vicino all'ingresso");
                    }
                }
            }

            Assert.Greater(total, 60, "in media più di uno per caverna");
        }

        [Test, Description("Gli spuntoni della cripta portano la loro piastrella al posto del pavimento, con il collider del pavimento; quelli delle caverne spuntano dalla roccia")]
        public void SpikePrefabs_AreInTheTilesets()
        {
            var crypt = Tileset("DungeonTileset");
            var cave = Tileset("CaveTileset");
            Assert.IsTrue(crypt.ReplacesFloor(DungeonPopulator.SpikeSymbol));
            Assert.IsFalse(cave.ReplacesFloor(DungeonPopulator.SpikeSymbol));
            Assert.IsFalse(crypt.ReplacesFloor('b'));

            var floor = crypt.GetMarkerPrefab(DungeonPopulator.SpikeSymbol);
            Assert.AreEqual(LayerMask.NameToLayer("Ground"), floor.layer);
            Assert.IsNotNull(floor.GetComponent<BoxCollider>());
            var rock = cave.GetMarkerPrefab(DungeonPopulator.SpikeSymbol);
            Assert.IsNull(rock.GetComponent<Collider>(), "nelle caverne il pavimento resta quello di roccia");
            Assert.IsFalse(rock.transform.Find("Tile").GetComponent<MeshRenderer>().enabled);
            foreach (var prefab in new[] { floor, rock })
            {
                var so = new SerializedObject(prefab.GetComponent<SpikeTrap>());
                Assert.IsNotNull(so.FindProperty("_spikes").objectReferenceValue, prefab.name);
                Assert.IsNotNull(so.FindProperty("_warningClip").objectReferenceValue, prefab.name);
                Assert.IsNotNull(so.FindProperty("_strikeClip").objectReferenceValue, prefab.name);
            }
        }

        [Test, Description("Ogni barile da rompere ha la versione esplosiva spenta, la miccia, il lampo e i due suoni; esplosivi possono esserlo solo quelli dei gruppi")]
        public void ExplosiveBarrels_AreReady()
        {
            foreach (var name in new[] { "Barrel_Large", "Barrel_Small", "Barrel_Decorated" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Dungeon/{name}.prefab");
                var barrel = prefab.GetComponent<Breakable>();
                Assert.IsFalse(barrel.CanExplode, name + ": da solo non esplode");
                var so = new SerializedObject(barrel);
                Assert.IsTrue(so.FindProperty("_explosiveModel").objectReferenceValue is GameObject model && !model.activeSelf, name);
                Assert.IsTrue(so.FindProperty("_fuseSparks").objectReferenceValue is ParticleSystem fuse && !fuse.main.playOnAwake, name);
                Assert.IsTrue(so.FindProperty("_explosion").objectReferenceValue is ParticleSystem blast && !blast.main.playOnAwake, name);
                Assert.IsTrue(so.FindProperty("_flash").objectReferenceValue is Light flash && !flash.enabled, name);
                Assert.IsNotNull(so.FindProperty("_fuseClip").objectReferenceValue, name);
                Assert.IsNotNull(so.FindProperty("_explosionClip").objectReferenceValue, name);
                Assert.AreEqual(3f, barrel.ExplosionRadius);
            }

            var crypt = Tileset("DungeonTileset");
            Assert.IsFalse(crypt.GetMarkerPrefab('b').GetComponent<Breakable>().CanExplode, "il barile contro il muro non esplode");
            foreach (char symbol in DungeonPopulator.BarrelGroupSymbol + DungeonPopulator.BarrelWallSymbols)
            {
                var group = crypt.GetMarkerPrefab(symbol);
                Assert.IsTrue(group.GetComponentsInChildren<Breakable>(true).All(b => b.CanExplode), group.name);
            }
        }
    }
}
