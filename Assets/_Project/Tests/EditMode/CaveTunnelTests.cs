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

        [Test, Description("Lo sbocco del cunicolo è solo nella cripta e solo alla profondità 1: le mappe generate lo chiedono con @arrival e la cella del muro, quelle a mano no")]
        public void Tunnel_OnlyAtDepthOneOfTheCrypt()
        {
            Assert.AreEqual("TunnelMouth", Load<LevelTileset>(CryptTilesetPath).EntranceMouth.name);
            Assert.IsNull(Load<LevelTileset>(CaveTilesetPath).EntranceMouth, "le caverne restano come sono");

            var settings = Load<DungeonSettings>(CryptSettingsPath);
            foreach (var seed in Seeds)
            {
                var arrival = DungeonLevel.CreateMap(settings, seed, 1, out _).GetDirective(LevelBuilder.ArrivalDirective);
                Assert.AreEqual(4, arrival.Count, $"seme {seed}");
                Assert.AreEqual(LevelBuilder.TunnelArrival, arrival[0]);
                Assert.IsTrue(TunnelSpot.TryRead(arrival, out _, out _), $"seme {seed}: {string.Join(" ", arrival)}");
                CollectionAssert.IsEmpty(DungeonLevel.CreateMap(settings, seed, 2, out _).GetDirective(LevelBuilder.ArrivalDirective), $"seme {seed}");
            }

            var hand = LevelMap.Parse(Load<TextAsset>("Assets/_Project/Levels/Level_01.txt").text);
            CollectionAssert.IsEmpty(hand.GetDirective(LevelBuilder.ArrivalDirective));
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

        [Test, Description("TunnelSpot sceglie il muro nord e la cella più vicina all'ingresso, anche con la torcia o accanto a un corridoio; salta gli angoli, le celle occupate e quelle davanti a un corridoio, e se il nord è tutto preso passa all'ovest")]
        public void TunnelSpot_TakesAFreeStretchOfWall()
        {
            var room = new RectInt(1, 1, 5, 3);
            var entrance = new Vector2Int(3, 2);
            Assert.IsTrue(TunnelSpot.TryChoose(Layout(room, entrance, "#######", "#.....#", "#..<..#", "#.....#", "#######"), out var cell, out var wall));
            Assert.AreEqual((new Vector2Int(3, 1), MapDirection.North), (cell, wall), "di fronte all'ingresso");

            Assert.IsTrue(TunnelSpot.TryChoose(Layout(room, entrance, "#######", "#..B..#", "#..<..#", "#.....#", "#######"), out cell, out wall));
            Assert.AreEqual((new Vector2Int(2, 1), MapDirection.North), (cell, wall), "accanto al barile");

            Assert.IsTrue(TunnelSpot.TryChoose(Layout(room, entrance, "#######", "#..T..#", "#..<..#", "#.....#", "#######"), out cell, out wall));
            Assert.AreEqual((new Vector2Int(3, 1), MapDirection.North), (cell, wall), "al posto della torcia");

            Assert.IsTrue(TunnelSpot.TryChoose(Layout(room, entrance, "###.###", "#.....#", "#..<..#", "#.....#", "#######"), out cell, out wall));
            Assert.AreEqual((new Vector2Int(2, 1), MapDirection.North), (cell, wall), "accanto alla bocca del corridoio, non davanti");

            Assert.IsTrue(TunnelSpot.TryChoose(Layout(room, entrance, "#######", "#BBBBB#", "#..<..#", "#.....#", "#######"), out cell, out wall));
            Assert.AreEqual((new Vector2Int(1, 2), MapDirection.West), (cell, wall), "il nord è tutto preso, e gli angoli non vanno");

            Assert.IsFalse(TunnelSpot.TryChoose(Layout(new RectInt(1, 1, 1, 1), new Vector2Int(1, 1), "###", "#<#", "###"), out _, out _), "una cella sola non ha muri liberi");
        }

        [Test, Description("Con i semi fissi lo sbocco trova sempre un muro, dietro c'è roccia, e tolta la sua cella da ogni pavimento si arriva ancora all'ingresso: il cunicolo non chiude nessun passaggio")]
        public void Tunnel_NeverClosesAPassage()
        {
            var settings = Load<DungeonSettings>(CryptSettingsPath);
            for (ulong seed = 1; seed <= 40; seed++)
            {
                var map = DungeonLevel.CreateMap(settings, seed * 7919UL, 1, out _);
                var entrance = map.Markers.Single(m => m.Symbol == '<');
                Assert.IsTrue(TunnelSpot.TryRead(map.GetDirective(LevelBuilder.ArrivalDirective), out var mouth, out var wall),
                    $"seme {seed}: nessun muro libero{System.Environment.NewLine}{Around(map, entrance)}");
                Vector3 behind = LevelMap.ToWorld(wall);
                Assert.IsTrue(map.IsRock(mouth.x + Mathf.RoundToInt(behind.x), mouth.y - Mathf.RoundToInt(behind.z)), $"seme {seed}: dietro non c'è il muro");

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
