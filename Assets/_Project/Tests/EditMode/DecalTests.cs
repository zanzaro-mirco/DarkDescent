using System.Collections.Generic;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Editor;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class DecalTests
    {
        private static DungeonSettings Crypt => AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");

        private static CaveSettings Caves => AssetDatabase.LoadAssetAtPath<CaveSettings>("Assets/_Project/Data/Levels/CaveSettings.asset");

        // cripta alle profondità 1-4, caverne alle 5-8 (la 8 con la sala del re)
        private static IEnumerable<(DungeonSettings settings, int depth)> Levels()
        {
            for (int depth = 1; depth <= 8; depth++)
            {
                yield return (depth <= 4 ? Crypt : Caves, depth);
            }
        }

        [Test, Description("Stesso seme, stesse macchie; un seme diverso ne dà altre")]
        public void SameSeed_SameDecals()
        {
            foreach (var (settings, depth) in Levels())
            {
                var first = DungeonLevel.CreateMap(settings, 4242, depth, out _).GetDirective(DecalSpot.Directive);
                var again = DungeonLevel.CreateMap(settings, 4242, depth, out _).GetDirective(DecalSpot.Directive);
                var other = DungeonLevel.CreateMap(settings, 4243, depth, out _).GetDirective(DecalSpot.Directive);
                Assert.Greater(first.Count, 0, $"profondità {depth}: almeno qualche macchia");
                CollectionAssert.AreEqual(first, again, $"profondità {depth}");
                CollectionAssert.AreNotEqual(first, other, $"profondità {depth}");
            }
        }

        [Test, Description("Le macchie non toccano il livello: la griglia, i nemici, le casse e gli arredi sono quelli di prima")]
        public void Decals_LeaveTheLevelAsItWas()
        {
            foreach (var settings in new[] { Crypt, Caves })
            {
                for (ulong seed = 1; seed <= 20; seed++)
                {
                    var layout = settings.CreateGenerator().Generate(seed, settings == Crypt ? 2 : 6);
                    var before = layout.ToMap(null).ToText();
                    var spots = DecalPlanner.Plan(layout, settings.DecalThemes, new SplitMix64Source(SeedMixer.ForDecals(seed)));
                    Assert.AreEqual(before, layout.ToMap(null).ToText(), $"{settings.name}, seme {seed}");
                    Assert.Greater(spots.Count, 0);
                }
            }
        }

        [Test, Description("Mai sopra un marcatore (scale, trappole, casse, pozzi, santuari, nemici, arredi) né accanto a scala, ingresso, cancello e punti d'interesse; una per cella, dentro la sua cella")]
        public void Decals_StayOnFreeFloor()
        {
            int total = 0;
            foreach (var (settings, depth) in Levels())
            {
                for (ulong seed = 1; seed <= 25; seed++)
                {
                    var map = DungeonLevel.CreateMap(settings, seed, depth, out var layout);
                    var clear = DecalPlanner.KeepClear(layout);
                    var spots = DecalSpot.FromMap(map);
                    Assert.AreEqual(map.GetDirective(DecalSpot.Directive).Count, spots.Count, "ogni parola si legge");
                    Assert.AreEqual(spots.Count, spots.Select(s => (s.X, s.Y)).Distinct().Count(), "una per cella");
                    foreach (var spot in spots)
                    {
                        string where = $"{settings.name} profondità {depth} seme {seed}, cella {spot.X},{spot.Y}";
                        Assert.AreEqual(LevelMap.Floor, map.GetSymbol(spot.X, spot.Y), where);
                        Assert.IsFalse(clear.Contains(new Vector2Int(spot.X, spot.Y)), where);
                        Assert.LessOrEqual(spot.Reach, LevelMap.CellSize / 2f, where);
                        Assert.That(spot.Tile, Is.InRange(0, DecalPlanner.TileCount - 1), where);
                        Assert.That(spot.Size, Is.InRange(DecalPlanner.MinSize, DecalPlanner.MaxSize), where);
                        Assert.AreEqual(0, spot.Angle % 15, where);
                    }

                    total += spots.Count;
                }
            }

            Assert.Greater(total, 8 * 25 * 3, "in media qualche macchia per livello");
        }

        [Test, Description("Il tema della stanza sceglie le macchie: la cripta e le caverne usano solo le caselle dei loro temi")]
        public void Themes_PickTheTiles()
        {
            foreach (var settings in new[] { Crypt, Caves })
            {
                var allowed = new HashSet<int>(settings.DecalThemes.SelectMany(t => t));
                var used = new HashSet<int>();
                for (ulong seed = 1; seed <= 30; seed++)
                {
                    var map = DungeonLevel.CreateMap(settings, seed, settings == Crypt ? 3 : 7, out _);
                    used.UnionWith(DecalSpot.FromMap(map).Select(s => s.Tile));
                }

                CollectionAssert.IsSubsetOf(used, allowed, settings.name);
                CollectionAssert.AreEquivalent(allowed, used, $"{settings.name}: in 30 livelli escono tutte");
            }

            Assert.IsFalse(DecalPlanner.CaveThemes.SelectMany(t => t).Contains(DecalPlanner.Soot), "niente fuliggine nelle caverne");
        }

        [Test, Description("Nella cripta al più tre macchie per stanza")]
        public void Crypt_AtMostThreePerRoom()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var map = DungeonLevel.CreateMap(Crypt, seed, 2, out var layout);
                var spots = DecalSpot.FromMap(map);
                foreach (var room in layout.Rooms)
                {
                    Assert.LessOrEqual(spots.Count(s => room.Contains(new Vector2Int(s.X, s.Y))), DecalPlanner.MaxPerArea, $"seme {seed}");
                }
            }
        }

        [Test, Description("La parola della direttiva si rilegge uguale; quelle sbagliate si saltano")]
        public void Token_RoundTrips()
        {
            var spot = new DecalSpot(3, 7, 5, 135, -4, 2, 16);
            Assert.IsTrue(DecalSpot.TryParse(spot.ToToken(), out var back));
            Assert.AreEqual(spot, back);
            Assert.IsFalse(DecalSpot.TryParse("3,7,5", out _));
            Assert.IsFalse(DecalSpot.TryParse("3,7,5,a,0,0,16", out _));
            Assert.IsFalse(DecalSpot.TryParse(null, out _));
        }

        [Test, Description("Il quadrato: quattro vertici alzati sul pavimento attorno al centro, le coordinate della sua casella, due triangoli rivolti in alto")]
        public void AddQuad_UsesTheTileAndFacesUp()
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            new DecalSpot(2, 1, 5, 0, 0, 0, 20).AddQuad(vertices, uvs, triangles, 0.02f, DecalPlanner.Columns, DecalPlanner.Rows);
            Assert.AreEqual(4, vertices.Count);
            Assert.AreEqual(6, triangles.Count);
            var center = LevelMap.CellCenter(2, 1);
            foreach (var v in vertices)
            {
                Assert.AreEqual(0.02f, v.y, 1e-5f);
                Assert.AreEqual(1f, Mathf.Abs(v.x - center.x), 1e-4f, "mezzo lato di 2 m");
            }

            // la casella 5 è la seconda della riga in basso: u da 0,25 a 0,5, v da 0 a 0,5
            Assert.AreEqual(0.25f, uvs.Min(u => u.x), 1e-5f);
            Assert.AreEqual(0.5f, uvs.Max(u => u.x), 1e-5f);
            Assert.AreEqual(0f, uvs.Min(u => u.y), 1e-5f);
            Assert.AreEqual(0.5f, uvs.Max(u => u.y), 1e-5f);

            var normal = Vector3.Cross(vertices[triangles[1]] - vertices[triangles[0]], vertices[triangles[2]] - vertices[triangles[0]]);
            Assert.Greater(normal.y, 0f, "la faccia davanti guarda in alto");
        }

        [Test, Description("Il seme delle macchie è un dominio a parte: non coincide con quelli del livello, degli arredi e dei nemici speciali")]
        public void DecalSeed_IsItsOwn()
        {
            ulong level = SeedMixer.ForLevel(99, 3);
            Assert.AreNotEqual(SeedMixer.ForDecals(level), SeedMixer.ForDecor(level));
            Assert.AreNotEqual(SeedMixer.ForDecals(level), SeedMixer.ForSpecials(level));
            Assert.AreNotEqual(SeedMixer.ForDecals(level), level);
        }

        [Test, Description("L'atlante si importa con i mipmap, compresso, al più 1024; il materiale è trasparente, prima degli altri trasparenti, nei tileset di cripta e caverne e non in quello della città")]
        public void AtlasAndMaterial_AreSetUp()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(GeneratedDecalImporter.AtlasPath);
            Assert.IsNotNull(importer, "l'atlante c'è");
            Assert.IsTrue(importer.mipmapEnabled);
            Assert.AreEqual(1024, importer.maxTextureSize);
            Assert.IsTrue(importer.alphaIsTransparency);
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(GeneratedDecalImporter.AtlasPath);
            Assert.AreEqual(2 * atlas.height, atlas.width, "quattro colonne e due righe di caselle quadrate");

            var material = AssetDatabase.LoadAssetAtPath<Material>(DecalTools.MaterialPath);
            Assert.IsNotNull(material);
            Assert.AreEqual(atlas, material.GetTexture("_BaseMap"));
            Assert.AreEqual(DecalTools.DecalQueue, material.renderQueue);
            Assert.Less(material.renderQueue, 3000);
            Assert.IsTrue(material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"));

            foreach (string path in DecalTools.Tilesets)
            {
                Assert.AreEqual(material, AssetDatabase.LoadAssetAtPath<LevelTileset>(path).DecalMaterial, path);
            }

            Assert.IsNull(AssetDatabase.LoadAssetAtPath<LevelTileset>("Assets/_Project/Data/Levels/VillageTileset.asset").DecalMaterial, "la città no");
        }
    }
}
