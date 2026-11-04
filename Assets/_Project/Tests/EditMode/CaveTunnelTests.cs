using System.Collections.Generic;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>La grotta del borgo e lo sbocco del cunicolo alla profondità 1 (D6, D7 e D13 della M18).</summary>
    public class CaveTunnelTests
    {
        private const string VillageTilesetPath = "Assets/_Project/Data/Levels/VillageTileset.asset";
        private const string CryptTilesetPath = "Assets/_Project/Data/Levels/DungeonTileset.asset";
        private const string CaveTilesetPath = "Assets/_Project/Data/Levels/CaveTileset.asset";
        private const string CryptSettingsPath = "Assets/_Project/Data/Levels/CryptSettings.asset";
        private const string CaveSettingsPath = "Assets/_Project/Data/Levels/CaveSettings.asset";

        private static readonly ulong[] Seeds = { 1UL, 7UL, 42UL, 4711UL, 20261026UL, 987654321UL };

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        [Test, Description("L'uscita del borgo è la bocca della grotta: senza stendardo, con il bagliore, l'area del click sull'arco e i massi che si accendono con un materiale dello stesso atlante")]
        public void Village_ExitIsTheCave()
        {
            var tileset = Load<LevelTileset>(VillageTilesetPath);
            var cave = tileset.StairsDown;
            Assert.AreEqual("CaveExit", cave.name);
            Assert.IsNull(tileset.ExitBanner, "davanti al monte non c'è un muro per lo stendardo");
            Assert.IsNull(tileset.EntranceMouth, "nel borgo si arriva dal portale o dalla grotta");

            Assert.AreEqual("Glow", cave.GetComponentInChildren<Light>().name, "la prima luce è quella che si alza sotto il cursore");
            var click = cave.transform.Find("ClickArea");
            Assert.AreEqual(LayerMask.NameToLayer("Interactable"), click.gameObject.layer);
            Assert.Greater(click.GetComponent<BoxCollider>().size.y, 3f, "si clicca anche sull'arco");

            var unlit = cave.transform.Find("Unlit");
            var lit = cave.GetComponentsInChildren<Renderer>().Where(r => !r.transform.IsChildOf(unlit)).ToArray();
            Assert.That(lit.Length, Is.GreaterThan(0));
            var highlight = tileset.HighlightMaterial;
            Assert.IsTrue(highlight.IsKeywordEnabled("_EMISSION"));
            foreach (var renderer in lit)
            {
                Assert.AreEqual(highlight.GetTexture("_BaseMap"), renderer.sharedMaterial.GetTexture("_BaseMap"), renderer.name);
            }

            // la cella dell'uscita resta fuori dal NavMesh, come la scala: si scende dal bordo
            int walkable = LayerMask.GetMask("Ground", "Obstacle");
            Assert.IsTrue(cave.GetComponentsInChildren<Transform>(true).All(t => (walkable & (1 << t.gameObject.layer)) == 0));
        }

        [Test, Description("Lo sbocco del cunicolo è anche la scala in su (M22): a ogni profondità della cripta e delle caverne le mappe generate lo chiedono con @arrival e @upexit, quelle a mano no. Alla 1 porta al borgo, nelle altre un livello su, dalla prima delle caverne alla cripta")]
        public void Tunnel_IsTheStairsUpAtEveryDepth()
        {
            Assert.AreEqual("TunnelMouth", Load<LevelTileset>(CryptTilesetPath).EntranceMouth.name);
            Assert.AreEqual("TunnelMouth", Load<LevelTileset>(CaveTilesetPath).EntranceMouth.name, "anche le caverne hanno la loro scala in su");

            var crypt = Load<DungeonSettings>(CryptSettingsPath);
            var caves = Load<DungeonSettings>(CaveSettingsPath);
            foreach (var seed in Seeds)
            {
                foreach (var (settings, depth, scene, entrance, target) in new[]
                {
                    (crypt, 1, "Town", "FromCrypt", 0), (crypt, 2, "Level_Crypt", "FromBelow", 1), (crypt, 4, "Level_Crypt", "FromBelow", 3),
                    (caves, 5, "Level_Crypt", "FromBelow", 4), (caves, 8, "Level_Caves", "FromBelow", 7),
                })
                {
                    var map = DungeonLevel.CreateMap(settings, seed, depth, out _);
                    var arrival = map.GetDirective(LevelBuilder.ArrivalDirective);
                    Assert.AreEqual(4, arrival.Count, $"seme {seed}, profondità {depth}: nessuno sbocco");
                    Assert.AreEqual(LevelBuilder.TunnelArrival, arrival[0]);
                    Assert.IsTrue(TunnelSpot.TryRead(arrival, out _, out _), $"seme {seed}: {string.Join(" ", arrival)}");
                    CollectionAssert.AreEqual(new[] { scene, entrance, target.ToString() }, map.GetDirective(LevelBuilder.UpExitDirective), $"seme {seed}, profondità {depth}");
                }
            }

            var hand = LevelMap.Parse(Load<TextAsset>("Assets/_Project/Levels/Level_01.txt").text);
            CollectionAssert.IsEmpty(hand.GetDirective(LevelBuilder.ArrivalDirective));
            CollectionAssert.IsEmpty(hand.GetDirective(LevelBuilder.UpExitDirective));
        }

        [Test, Description("DungeonSettings.TryGetUpExit: la cripta dalla 2 alla 4 sale di un livello nella stessa scena e dalla 1 va al borgo; le caverne uguale, dalla 5 alla profondità 4 della cripta; senza scena di sopra la scala non c'è")]
        public void UpExit_FollowsTheSettings()
        {
            var crypt = Load<DungeonSettings>(CryptSettingsPath);
            var caves = Load<DungeonSettings>(CaveSettingsPath);
            Assert.IsTrue(crypt.TryGetUpExit(3, out string scene, out string entrance, out int depth));
            Assert.AreEqual(("Level_Crypt", "FromBelow", 2), (scene, entrance, depth));
            Assert.IsTrue(crypt.TryGetUpExit(1, out scene, out entrance, out depth));
            Assert.AreEqual(("Town", "FromCrypt", 0), (scene, entrance, depth));
            Assert.IsTrue(caves.TryGetUpExit(5, out scene, out entrance, out depth));
            Assert.AreEqual(("Level_Crypt", "FromBelow", 4), (scene, entrance, depth));
            Assert.IsTrue(caves.TryGetUpExit(7, out scene, out entrance, out depth));
            Assert.AreEqual(("Level_Caves", "FromBelow", 6), (scene, entrance, depth));
            Assert.IsFalse(crypt.TryGetUpExit(5, out _, out _, out _), "la cripta non copre la profondità 5");

            var bare = ScriptableObject.CreateInstance<DungeonSettings>();
            Assert.IsFalse(bare.TryGetUpExit(1, out _, out _, out _), "senza una scena di sopra la scala in su non c'è");
            Object.DestroyImmediate(bare);
        }

        [Test, Description("Lo sbocco prende il posto del muro dietro la sua cella: i massi arrivano in cima al muro, la galleria entra nella roccia, nella stanza sporge poco e sui muri accanto resta vicino al muro")]
        public void Tunnel_TakesThePlaceOfTheWall()
        {
            var mouth = Object.Instantiate(Load<LevelTileset>(CryptTilesetPath).EntranceMouth);
            try
            {
                float half = LevelMap.CellSize * 0.5f;
                float top = 0f, back = 0f;
                foreach (var filter in mouth.GetComponentsInChildren<MeshFilter>())
                {
                    foreach (var vertex in filter.sharedMesh.vertices)
                    {
                        Vector3 point = filter.transform.TransformPoint(vertex);
                        Assert.LessOrEqual(Mathf.Abs(point.x), half + 1f, $"{filter.name} copre troppo dei muri accanto");
                        Assert.LessOrEqual(point.z, 0.5f, $"{filter.name} entra troppo nella stanza");
                        Assert.GreaterOrEqual(point.z, -half - LevelMap.CellSize, $"{filter.name} esce dalla cella di roccia dietro");
                        if (Mathf.Abs(point.x) > half + 0.3f)
                        {
                            Assert.LessOrEqual(point.z, -half + 1f, $"{filter.name} si stacca dal muro accanto");
                        }

                        top = Mathf.Max(top, point.y);
                        back = Mathf.Min(back, point.z);
                    }
                }

                Assert.GreaterOrEqual(top, 3.9f, "alto come il muro che sostituisce");
                Assert.Less(back, -half - 1.5f, "la galleria entra nella roccia");
                Assert.Greater(Vector3.Dot(mouth.transform.Find("Mouth").forward, Vector3.forward), 0.99f, "l'apertura verso la stanza");

                // il cavaliere compare al centro della stanza, larga almeno 3: davanti o di sbieco
                var collider = mouth.GetComponentInChildren<BoxCollider>();
                float cell = LevelMap.CellSize;
                foreach (var entrance in new[] { new Vector3(0f, 1f, cell), new Vector3(-cell, 1f, cell), new Vector3(cell, 1f, cell) })
                {
                    Assert.Greater(Vector3.Distance(collider.ClosestPoint(entrance), entrance), 1.5f, "il click sul punto d'arrivo arriva al pavimento");
                }
            }
            finally
            {
                Object.DestroyImmediate(mouth);
            }
        }

        // una stanza a mano: le righe della mappa, la stanza e l'ingresso
        private static DungeonLayout Layout(RectInt room, Vector2Int entrance, params string[] rows)
        {
            var cells = new char[rows[0].Length, rows.Length];
            for (int y = 0; y < rows.Length; y++)
            {
                for (int x = 0; x < rows[y].Length; x++)
                {
                    cells[x, y] = rows[y][x];
                }
            }

            return new DungeonLayout(cells, new List<RectInt> { room }, 0, 0, entrance, entrance);
        }

        [Test, Description("TunnelSpot sceglie il muro nord e la cella più vicina all'ingresso, ma ad almeno due celle da lui, anche con la torcia o accanto a un corridoio che parte dal suo muro; salta gli angoli, le celle occupate, quelle davanti a un corridoio e quelle dove un corridoio parte di lato, e se il nord è tutto preso passa all'ovest; senza un posto a due celle ripiega sul più vicino")]
        public void TunnelSpot_TakesAFreeStretchOfWall()
        {
            var room = new RectInt(1, 1, 5, 5);
            var entrance = new Vector2Int(3, 3);
            Assert.IsTrue(TunnelSpot.TryChoose(Layout(room, entrance, "#######", "#.....#", "#.....#", "#..<..#", "#.....#", "#.....#", "#######"), out var cell, out var wall));
            Assert.AreEqual((new Vector2Int(3, 1), MapDirection.North), (cell, wall), "di fronte all'ingresso, a due celle");

            Assert.IsTrue(TunnelSpot.TryChoose(Layout(room, entrance, "#######", "#..B..#", "#.....#", "#..<..#", "#.....#", "#.....#", "#######"), out cell, out wall));
            Assert.AreEqual((new Vector2Int(2, 1), MapDirection.North), (cell, wall), "accanto al barile");

            Assert.IsTrue(TunnelSpot.TryChoose(Layout(room, entrance, "#######", "#..T..#", "#.....#", "#..<..#", "#.....#", "#.....#", "#######"), out cell, out wall));
            Assert.AreEqual((new Vector2Int(3, 1), MapDirection.North), (cell, wall), "al posto della torcia");

            Assert.IsTrue(TunnelSpot.TryChoose(Layout(room, entrance, "###.###", "#.....#", "#.....#", "#..<..#", "#.....#", "#.....#", "#######"), out cell, out wall));
            Assert.AreEqual((new Vector2Int(2, 1), MapDirection.North), (cell, wall), "accanto alla bocca del corridoio, non davanti");

            // il corridoio parte verso est dalla fila del muro nord: la cella più vicina all'ingresso è
            // quella del suo imbocco, e il fronte di massi lo stringerebbe (ronde del 31 ott e del 1 nov 2026)
            var corner = new Vector2Int(5, 3);
            Assert.IsTrue(TunnelSpot.TryChoose(Layout(room, corner, "#######", "#......", "#.....#", "#....<#", "#.....#", "#.....#", "#######"), out cell, out wall));
            Assert.AreEqual((new Vector2Int(4, 1), MapDirection.North), (cell, wall), "non all'imbocco del corridoio di lato");

            Assert.IsTrue(TunnelSpot.TryChoose(Layout(room, entrance, "#######", "#BBBBB#", "#.....#", "#..<..#", "#.....#", "#.....#", "#######"), out cell, out wall));
            Assert.AreEqual((new Vector2Int(1, 3), MapDirection.West), (cell, wall), "il nord è tutto preso, e gli angoli non vanno");

            // un posto a una cella dall'ingresso non è il primo: con l'ingresso in mezzo a una stanza da 3 il muro nord è a una cella, ma l'ovest...
            var small = new RectInt(1, 1, 5, 3);
            Assert.IsTrue(TunnelSpot.TryChoose(Layout(small, new Vector2Int(3, 2), "#######", "#.....#", "#..<..#", "#.....#", "#######"), out cell, out wall));
            Assert.AreEqual((new Vector2Int(1, 2), MapDirection.West), (cell, wall), "il nord è a una cella: si prende l'ovest, a due");

            // nessun muro a due celle: si ripiega sul più vicino, com'era
            var tiny = new RectInt(1, 1, 3, 3);
            Assert.IsTrue(TunnelSpot.TryChoose(Layout(tiny, new Vector2Int(2, 2), "#####", "#...#", "#.<.#", "#...#", "#####"), out cell, out wall));
            Assert.AreEqual((new Vector2Int(2, 1), MapDirection.North), (cell, wall), "nessuna a due celle: la più vicina sul muro nord");

            Assert.IsFalse(TunnelSpot.TryChoose(Layout(new RectInt(1, 1, 1, 1), new Vector2Int(1, 1), "###", "#<#", "###"), out _, out _), "una cella sola non ha muri liberi");
        }

        [Test, Description("Con i semi fissi e con quelli delle ronde del 31 ott e del 1 nov 2026 lo sbocco trova sempre un muro, dietro c'è roccia, le celle di lato sono della stanza d'ingresso, e tolta la sua cella da ogni pavimento si arriva ancora all'ingresso: il cunicolo non chiude nessun passaggio")]
        public void Tunnel_NeverClosesAPassage()
        {
            var settings = Load<DungeonSettings>(CryptSettingsPath);
            var seeds = Enumerable.Range(1, 40).Select(i => (ulong)i * 7919UL).Concat(new[] { 4188803450199181896UL, 9483114763172317484UL });
            foreach (ulong seed in seeds)
            {
                var map = DungeonLevel.CreateMap(settings, seed, 1, out var layout);
                var entrance = map.Markers.Single(m => m.Symbol == '<');
                Assert.IsTrue(TunnelSpot.TryRead(map.GetDirective(LevelBuilder.ArrivalDirective), out var mouth, out var wall),
                    $"seme {seed}: nessun muro libero{System.Environment.NewLine}{Around(map, entrance)}");
                Vector3 behind = LevelMap.ToWorld(wall);
                Assert.IsTrue(map.IsRock(mouth.x + Mathf.RoundToInt(behind.x), mouth.y - Mathf.RoundToInt(behind.z)), $"seme {seed}: dietro non c'è il muro");
                var along = new Vector2Int(Mathf.Abs(Mathf.RoundToInt(behind.z)), Mathf.Abs(Mathf.RoundToInt(behind.x)));
                var room = layout.Rooms[layout.StartRoom];
                Assert.IsTrue(room.Contains(mouth + along) && room.Contains(mouth - along),
                    $"seme {seed}: lo sbocco in ({mouth.x}, {mouth.y}) ha di lato un angolo o l'imbocco di un corridoio{System.Environment.NewLine}{Around(map, entrance)}");

                var seen = new HashSet<Vector2Int> { new Vector2Int(entrance.X, entrance.Y), mouth };
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(new Vector2Int(entrance.X, entrance.Y));
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var step in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
                    {
                        var next = current + step;
                        if (map.IsFloor(next.x, next.y) && seen.Add(next))
                        {
                            queue.Enqueue(next);
                        }
                    }
                }

                for (int y = 0; y < map.Height; y++)
                {
                    for (int x = 0; x < map.Width; x++)
                    {
                        if (map.IsFloor(x, y) && !seen.Contains(new Vector2Int(x, y)))
                        {
                            Assert.Fail($"seme {seed}: la cella ({x}, {y}) resta chiusa fuori");
                        }
                    }
                }
            }
        }

        // le celle attorno all'ingresso, per il messaggio di un fallimento
        private static string Around(LevelMap map, MapMarker entrance)
        {
            var sb = new System.Text.StringBuilder();
            for (int y = entrance.Y - 4; y <= entrance.Y + 4; y++)
            {
                for (int x = entrance.X - 4; x <= entrance.X + 4; x++)
                {
                    sb.Append(map.GetSymbol(x, y));
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
