using System.Collections.Generic;
using System.Linq;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Gli arredi della cripta (D4 della M11; mucchietti dal passo 11.7; punto d'interesse e niente doppioni dalla M13).</summary>
    public class DecorTests
    {
        private const int Seeds = 100;
        private const int ObstacleLayer = 7;
        private const int EnemyLayer = 8;
        private const int InteractableLayer = 9;

        private DungeonSettings _settings;
        private DungeonSettings _bare;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<DungeonSettings>();
            _bare = ScriptableObject.CreateInstance<DungeonSettings>();
            var so = new SerializedObject(_bare);
            so.FindProperty("_maxDecorPerRoom").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_bare);
        }

        private static IEnumerable<Vector2Int> Cells(DungeonLayout layout, System.Func<char, bool> wanted)
        {
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (wanted(layout[x, y]))
                    {
                        yield return new Vector2Int(x, y);
                    }
                }
            }
        }

        [Test, Description("Gli arredi hanno un flusso loro del seme: tolti, la cripta è uguale cella per cella a quella senza arredi, con gli stessi nemici, casse e oggetti di scena")]
        public void Decor_DoesNotMoveAnythingElse()
        {
            var decorated = new DungeonGenerator(_settings);
            var bare = new DungeonGenerator(_bare);
            for (int depth = 1; depth <= 4; depth++)
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var with = decorated.Generate(seed, depth);
                    var without = bare.Generate(seed, depth);
                    Assert.AreEqual(0, Cells(without, DungeonPopulator.IsDecor).Count());
                    for (int y = 0; y < with.Height; y++)
                    {
                        for (int x = 0; x < with.Width; x++)
                        {
                            char c = with[x, y];
                            Assert.AreEqual(DungeonPopulator.IsDecor(c) ? LevelMap.Floor : c, without[x, y], $"seme {seed}, profondità {depth}, cella ({x}, {y})");
                        }
                    }
                }
            }
        }

        private static bool IsComposition(char c)
        {
            return DungeonPopulator.IsDecor(c) && c != DungeonPopulator.BarrelGroupSymbol && DungeonPopulator.BarrelWallSymbols.IndexOf(c) < 0
                && !DungeonPopulator.IsFeature(c) && !DungeonPopulator.IsScatter(c);
        }

        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        private static bool IsBorder(RectInt room, Vector2Int cell)
        {
            return cell.x == room.xMin || cell.y == room.yMin || cell.x == room.xMax - 1 || cell.y == room.yMax - 1;
        }

        [Test, Description("Ogni stanza ha un tema solo; le composizioni stanno sul bordo della stanza, contro un muro, mai accanto a una cassa (si apre dal davanti), e sono tante: almeno una a stanza")]
        public void Decor_FollowsTheRoomTheme()
        {
            var generator = new DungeonGenerator(_settings);
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = generator.Generate(seed, 2);
                var decor = Cells(layout, IsComposition).ToList();
                Assert.GreaterOrEqual(decor.Count, layout.Rooms.Count, $"seme {seed}");
                foreach (var room in layout.Rooms)
                {
                    var inside = decor.Where(room.Contains).ToList();
                    var themes = inside.Select(c => DungeonPopulator.DecorThemes.Single(t => t.IndexOf(layout[c.x, c.y]) >= 0)).Distinct().ToList();
                    Assert.LessOrEqual(themes.Count, 1, $"seme {seed}: una stanza con più temi");
                    foreach (var cell in inside)
                    {
                        Assert.IsTrue(IsBorder(room, cell), $"seme {seed}: composizione in mezzo alla stanza");
                        foreach (var step in Steps)
                        {
                            Assert.AreNotEqual(DungeonPopulator.ChestSymbol, layout[cell.x + step.x, cell.y + step.y], $"seme {seed}: composizione accanto a una cassa in ({cell.x}, {cell.y})");
                        }
                    }
                }

                Assert.IsTrue(decor.All(c => DungeonPopulator.IsObstacle(layout[c.x, c.y])), "gli arredi sono ostacoli");
            }
        }

        [Test, Description("Nella stessa stanza nessuna composizione e nessun mucchietto si ripete, e i mucchietti sono quelli del tema della stanza (D3 della M13)")]
        public void Decor_NeverRepeatsInARoom()
        {
            var generator = new DungeonGenerator(_settings);
            int scatterSeen = 0;
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = generator.Generate(seed, 2);
                foreach (var room in layout.Rooms)
                {
                    var symbols = Cells(layout, c => IsComposition(c) || DungeonPopulator.IsScatter(c))
                        .Where(room.Contains).Select(c => layout[c.x, c.y]).ToList();
                    Assert.AreEqual(symbols.Count, symbols.Distinct().Count(), $"seme {seed}: un arredo ripetuto nella stanza {room}");

                    // il tema si legge dai mobili: in una stanza dove non ne è entrato nessuno non si sa
                    int theme = System.Array.FindIndex(DungeonPopulator.DecorThemes, t => symbols.Any(c => t.IndexOf(c) >= 0));
                    foreach (char c in symbols.Where(DungeonPopulator.IsScatter))
                    {
                        scatterSeen++;
                        if (theme >= 0)
                        {
                            StringAssert.Contains(c.ToString(), DungeonPopulator.ScatterThemes[theme], $"seme {seed}: mucchietto '{c}' fuori tema");
                        }
                    }
                }
            }

            Assert.Greater(scatterSeen, Seeds, "almeno un mucchietto a livello");
        }

        [Test, Description("Tutti e cinque i temi compaiono, cappella compresa, e ogni composizione e ogni mucchietto esce almeno una volta in 100 livelli (passo 13.3)")]
        public void EveryThemeAndPieceShowsUp()
        {
            var generator = new DungeonGenerator(_settings);
            var seen = new HashSet<char>();
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = generator.Generate(seed, 2);
                foreach (var cell in Cells(layout, DungeonPopulator.IsDecor))
                {
                    seen.Add(layout[cell.x, cell.y]);
                }
            }

            foreach (char symbol in string.Concat(DungeonPopulator.DecorThemes) + DungeonPopulator.ScatterSymbols)
            {
                Assert.IsTrue(seen.Contains(symbol), $"'{symbol}' non esce mai");
            }
        }

        [Test, Description("Piatti e bottiglie stanno solo accanto a un mobile della stanza (D3 della M13)")]
        public void TableScatter_LiesBesideFurniture()
        {
            var generator = new DungeonGenerator(_settings);
            int seen = 0;
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = generator.Generate(seed, 2);
                foreach (var cell in Cells(layout, c => c == DungeonPopulator.TableScatterSymbol))
                {
                    seen++;
                    var room = layout.Rooms.First(r => r.Contains(cell));
                    bool beside = Steps.Select(s => cell + s).Any(n => room.Contains(n) && IsComposition(layout[n.x, n.y]));
                    Assert.IsTrue(beside, $"seme {seed}: piatti e bottiglie lontani dai mobili in ({cell.x}, {cell.y})");
                }
            }

            Assert.Greater(seen, Seeds / 4, "piatti e bottiglie ci sono");
        }

        [Test, Description("Niente più colonnato (D2 della M13)")]
        public void Columns_AreGone()
        {
            var generator = new DungeonGenerator(_settings);
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = generator.Generate(seed, 3);
                Assert.AreEqual(0, Cells(layout, c => c == 'o').Count(), $"seme {seed}");
            }
        }

        [Test, Description("Il punto d'interesse sta solo nelle stanze da 5 x 5 in su, mai in quella di partenza, al centro e lontano dai passaggi, da casse e scala; al più un pozzo e un santuario per livello, e niente trappole prima della profondità 3 (D4 della M13)")]
        public void Features_StandInTheMiddleOfBigRooms()
        {
            var generator = new DungeonGenerator(_settings);
            var kinds = new Dictionary<RoomFeatureKind, int>();
            int bigRooms = 0;
            for (int depth = 1; depth <= 4; depth++)
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var layout = generator.Generate(seed, depth);
                    bigRooms += layout.Rooms.Where((r, i) => i != layout.StartRoom
                        && r.width >= DungeonPopulator.MinFeatureRoom && r.height >= DungeonPopulator.MinFeatureRoom).Count();
                    Assert.LessOrEqual(layout.Features.Count(f => f.Kind == RoomFeatureKind.Well), 1, $"seme {seed}: due pozzi");
                    Assert.LessOrEqual(layout.Features.Count(f => f.Kind == RoomFeatureKind.Shrine), 1, $"seme {seed}: due santuari");
                    Assert.AreEqual(layout.Features.Count, layout.Features.Select(f => f.Room).Distinct().Count(), $"seme {seed}: due punti in una stanza");
                    foreach (var feature in layout.Features)
                    {
                        kinds[feature.Kind] = kinds.TryGetValue(feature.Kind, out int n) ? n + 1 : 1;
                        var room = layout.Rooms[feature.Room];
                        var cell = feature.Cell;
                        Assert.AreNotEqual(layout.StartRoom, feature.Room, $"seme {seed}: punto d'interesse nella stanza di partenza");
                        Assert.IsTrue(room.width >= DungeonPopulator.MinFeatureRoom && room.height >= DungeonPopulator.MinFeatureRoom, $"seme {seed}: stanza piccola");
                        Assert.IsTrue(room.Contains(cell) && !IsBorder(room, cell), $"seme {seed}: punto d'interesse sul bordo");
                        Assert.AreNotEqual(RoomFeatureKind.None, feature.Kind);
                        if (depth < 3)
                        {
                            Assert.AreNotEqual(RoomFeatureKind.Traps, feature.Kind, $"seme {seed}: trappole alla profondità {depth}");
                        }

                        // barili, pozzo, santuario o spuntoni stanno nella cella
                        string expected = feature.Kind == RoomFeatureKind.Barrels ? DungeonPopulator.BarrelGroupSymbol.ToString()
                            : feature.Kind == RoomFeatureKind.Well ? DungeonPopulator.WellSymbols
                            : feature.Kind == RoomFeatureKind.Shrine ? DungeonPopulator.ShrineSymbols : DungeonPopulator.SpikeSymbol.ToString();
                        StringAssert.Contains(layout[cell.x, cell.y].ToString(), expected, $"seme {seed}: la cella del punto d'interesse ({feature.Kind})");
                        foreach (var next in Steps.Select(s => cell + s))
                        {
                            char c = layout[next.x, next.y];
                            Assert.AreNotEqual(DungeonPopulator.ChestSymbol, c, $"seme {seed}: punto d'interesse davanti a una cassa");
                            Assert.AreNotEqual(DungeonGenerator.StairsSymbol, c, $"seme {seed}: punto d'interesse accanto alla scala");
                            Assert.IsTrue(room.Contains(next), $"seme {seed}: punto d'interesse su un passaggio");
                        }
                    }

                    Assert.AreEqual(layout.Features.Count(f => f.Kind == RoomFeatureKind.Barrels),
                        Cells(layout, c => c == DungeonPopulator.BarrelGroupSymbol).Count(), $"seme {seed}: gruppi di barili");
                    Assert.AreEqual(layout.Features.Count(f => f.Kind == RoomFeatureKind.Well || f.Kind == RoomFeatureKind.Shrine),
                        Cells(layout, DungeonPopulator.IsFeature).Count(), $"seme {seed}: pozzi e santuari");
                }
            }

            Assert.Greater(bigRooms, 100, "stanze grandi da provare");
            int total = kinds.Values.Sum();
            Assert.Greater(total, bigRooms / 2, "più di metà delle stanze grandi ha un punto d'interesse");
            foreach (RoomFeatureKind kind in new[] { RoomFeatureKind.Barrels, RoomFeatureKind.Well, RoomFeatureKind.Shrine, RoomFeatureKind.Traps })
            {
                Assert.IsTrue(kinds.ContainsKey(kind), $"nessun {kind}");
            }

            Assert.Greater(kinds[RoomFeatureKind.Barrels], kinds[RoomFeatureKind.Well], "i barili sono i più comuni");
        }

        [Test, Description("Il tiro del punto d'interesse: barili 35%, pozzo 15%, santuario 15%, trappole 10% dalla profondità 3 (prima barili), niente il resto")]
        public void FeatureFor_FollowsTheChances()
        {
            Assert.AreEqual(RoomFeatureKind.Barrels, _settings.FeatureFor(0.0, 3));
            Assert.AreEqual(RoomFeatureKind.Barrels, _settings.FeatureFor(0.34, 3));
            Assert.AreEqual(RoomFeatureKind.Well, _settings.FeatureFor(0.36, 3));
            Assert.AreEqual(RoomFeatureKind.Shrine, _settings.FeatureFor(0.6, 3));
            Assert.AreEqual(RoomFeatureKind.Traps, _settings.FeatureFor(0.7, 3));
            Assert.AreEqual(RoomFeatureKind.Barrels, _settings.FeatureFor(0.7, 2));
            Assert.AreEqual(RoomFeatureKind.None, _settings.FeatureFor(0.76, 3));
            Assert.AreEqual(RoomFeatureKind.None, _settings.FeatureFor(0.999, 3));
        }

        [Test, Description("Rotazione e scarto vengono dalla cella: la stessa cella dà sempre gli stessi, celle vicine danno valori diversi, dentro i limiti")]
        public void Jitter_ComesFromTheCell()
        {
            var angles = new HashSet<float>();
            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 20; x++)
                {
                    float angle = DecorJitter.Angle(x, y);
                    Vector2 offset = DecorJitter.Offset(x, y);
                    Assert.AreEqual(angle, DecorJitter.Angle(x, y));
                    Assert.AreEqual(offset, DecorJitter.Offset(x, y));
                    Assert.That(angle, Is.InRange(0f, 345f));
                    Assert.AreEqual(0f, angle % 15f);
                    Assert.LessOrEqual(Mathf.Abs(offset.x), DecorJitter.MaxOffset + 1e-5f);
                    Assert.LessOrEqual(Mathf.Abs(offset.y), DecorJitter.MaxOffset + 1e-5f);
                    angles.Add(angle);
                }
            }

            Assert.GreaterOrEqual(angles.Count, 20, "tutti i versi, o quasi");
            Assert.AreNotEqual(DecorJitter.Angle(3, 4), DecorJitter.Angle(4, 3), "non simmetrico");
        }

        [Test, Description("Qualche mucchietto a terra: si attraversa, quindi non è un ostacolo; ossa e armi rotte di preferenza in mezzo alla stanza")]
        public void Scatter_LiesInTheMiddle()
        {
            var generator = new DungeonGenerator(_settings);
            int total = 0;
            int free = 0;
            int inner = 0;
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = generator.Generate(seed, 2);
                foreach (var cell in Cells(layout, DungeonPopulator.IsScatter))
                {
                    total++;
                    Assert.IsFalse(DungeonPopulator.IsObstacle(layout[cell.x, cell.y]));
                    if (layout[cell.x, cell.y] == DungeonPopulator.TableScatterSymbol)
                    {
                        continue;
                    }

                    free++;
                    var room = layout.Rooms.First(r => r.Contains(cell));
                    if (!IsBorder(room, cell))
                    {
                        inner++;
                    }
                }
            }

            Assert.Greater(total, Seeds, "almeno uno a livello");
            Assert.Greater(inner, free * 3 / 4, "quasi tutti lontani dai muri");
        }

        [Test, Description("Ogni arredo ha il suo prefab nel tileset della cripta: sta nella cella, a terra, con il materiale del dungeon (le ossa il loro); composizioni e barili hanno i collider sul layer degli ostacoli, tranne i barili da rompere che stanno su quello dei nemici (D5 della M13) e il click di pozzi e santuari, i mucchietti nessun collider")]
        public void Compositions_AreInTheTileset()
        {
            var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>("Assets/_Project/Data/Levels/DungeonTileset.asset");
            foreach (char symbol in string.Concat(DungeonPopulator.DecorThemes) + DungeonPopulator.BarrelGroupSymbol + DungeonPopulator.BarrelWallSymbols
                + DungeonPopulator.WellSymbols + DungeonPopulator.ShrineSymbols + DungeonPopulator.ScatterSymbols)
            {
                var prefab = tileset.GetMarkerPrefab(symbol);
                Assert.IsNotNull(prefab, $"'{symbol}' senza prefab");
                var colliders = prefab.GetComponentsInChildren<Collider>();
                if (DungeonPopulator.IsScatter(symbol))
                {
                    Assert.AreEqual(0, colliders.Length, prefab.name + ": un mucchietto si attraversa");
                }
                else
                {
                    Assert.Greater(colliders.Length, 0, prefab.name);
                }

                // i barili da rompere stanno sul layer dei nemici; pozzi e santuari hanno anche il click
                var outside = colliders.Where(c => c.gameObject.layer != (c.GetComponentInParent<Breakable>(true) != null ? EnemyLayer : ObstacleLayer)
                    && !(c.gameObject.layer == InteractableLayer && DungeonPopulator.IsFeature(symbol)));
                Assert.IsEmpty(outside.Select(c => $"{c.name} sul layer {c.gameObject.layer}"), prefab.name + ": collider fuori dal layer degli ostacoli");
                foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>())
                {
                    Assert.IsTrue(renderer.sharedMaterials.All(m => m != null && (m.name == "M_Dungeon" || m.name == "M_Bone"
                        || (DungeonPopulator.IsFeature(symbol) && (m.name.StartsWith("M_Crystal") || m.name.StartsWith("M_WellLiquid"))))),
                        $"{prefab.name}: {renderer.name}");
                    var mesh = renderer.GetComponent<MeshFilter>().sharedMesh.bounds;
                    Vector3 min = prefab.transform.InverseTransformPoint(renderer.transform.TransformPoint(mesh.min));
                    Vector3 max = prefab.transform.InverseTransformPoint(renderer.transform.TransformPoint(mesh.max));
                    foreach (var corner in new[] { min, max })
                    {
                        Assert.That(corner.x, Is.InRange(-LevelMap.CellSize / 2f, LevelMap.CellSize / 2f), $"{prefab.name}: {renderer.name} esce dalla cella");
                        Assert.That(corner.z, Is.InRange(-LevelMap.CellSize / 2f, LevelMap.CellSize / 2f), $"{prefab.name}: {renderer.name} esce dalla cella");
                        Assert.GreaterOrEqual(corner.y, -0.05f, $"{prefab.name}: {renderer.name} sotto il pavimento");
                    }
                }
            }
        }
    }
}
