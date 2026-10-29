using System.Collections.Generic;
using System.Linq;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>La mappa di testo della città (D3 della M10), che dalla M18 è il borgo.</summary>
    public class TownMapTests
    {
        private const string MapPath = "Assets/_Project/Data/Levels/Town.txt";
        private const string TilesetPath = "Assets/_Project/Data/Levels/VillageTileset.asset";

        // i pezzi del borgo che hanno una porta, e il recinto del cimitero, che sta su celle di pavimento
        private const string Buildings = "HIJGVDBCWQO";
        private const char Fence = 'n';

        // la velocità del NavMeshAgent del cavaliere e i secondi della D2
        private const float WalkSpeed = 5f;
        private const float MaxWalk = 10f;

        private static LevelMap Load()
        {
            return LevelMap.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(MapPath).text);
        }

        [Test, Description("Profondità 0, l'ingresso Start, una sola scala che scende alla profondità 1 della cripta")]
        public void Map_HasEntranceAndStairsToTheCrypt()
        {
            var map = Load();
            CollectionAssert.AreEqual(new[] { "0" }, map.GetDirective("depth"));
            CollectionAssert.AreEqual(new[] { "Start" }, map.GetDirective("entrance"));
            CollectionAssert.AreEqual(new[] { "Level_Crypt", "Start", "1" }, map.GetDirective("exit"));

            int entrances = 0, stairs = 0;
            foreach (var marker in map.Markers)
            {
                entrances += marker.Symbol == '<' ? 1 : 0;
                stairs += marker.Symbol == '>' ? 1 : 0;
            }

            Assert.AreEqual((1, 1), (entrances, stairs));
        }

        [Test, Description("Ogni simbolo della mappa ha un prefab nel tileset del borgo, e nessuno è un nemico")]
        public void Symbols_AreFurnitureOfTheTownTileset()
        {
            var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>(TilesetPath);
            Assert.AreEqual("level.town", tileset.NameKey);
            foreach (var marker in Load().Markers)
            {
                if (marker.Symbol == '<' || marker.Symbol == '>' || marker.Symbol == 'T')
                {
                    continue;
                }

                var prefab = tileset.GetMarkerPrefab(marker.Symbol);
                Assert.IsNotNull(prefab, $"'{marker.Symbol}' in ({marker.X}, {marker.Y}) senza prefab");
                Assert.IsNull(prefab.GetComponentInChildren<Enemies.EnemyAI>(), $"'{marker.Symbol}' è un nemico");
            }
        }

        [Test, Description("Dall'ingresso si raggiunge ogni pavimento della sala, scala compresa")]
        public void Floors_AreAllConnected()
        {
            var map = Load();
            Vector2Int start = default;
            int floors = 0;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (map.IsFloor(x, y))
                    {
                        floors++;
                        if (map.GetSymbol(x, y) == '<')
                        {
                            start = new Vector2Int(x, y);
                        }
                    }
                }
            }

            var seen = new HashSet<Vector2Int> { start };
            var open = new Queue<Vector2Int>();
            open.Enqueue(start);
            var steps = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (open.Count > 0)
            {
                var cell = open.Dequeue();
                foreach (var step in steps)
                {
                    var next = cell + step;
                    if (map.IsFloor(next.x, next.y) && seen.Add(next))
                    {
                        open.Enqueue(next);
                    }
                }
            }

            Assert.AreEqual(floors, seen.Count);
        }

        [Test, Description("Ogni cella coperta è di un pezzo solo, e le celle di ogni pezzo fanno un rettangolo (M18)")]
        public void Pieces_AreRectanglesThatDoNotMerge()
        {
            var map = Load();
            var owned = new HashSet<Vector2Int>();
            int covered = 0;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    covered += map.IsCovered(x, y) ? 1 : 0;
                }
            }

            foreach (var marker in map.Markers.Where(m => map.IsSolid(m.Symbol)))
            {
                var cells = MapPiece.Footprint(map, marker);
                int width = cells.Max(c => c.x) - cells.Min(c => c.x) + 1;
                int height = cells.Max(c => c.y) - cells.Min(c => c.y) + 1;
                Assert.AreEqual(width * height, cells.Count, $"'{marker.Symbol}' in ({marker.X}, {marker.Y}) non è un rettangolo");
                foreach (var cell in cells)
                {
                    Assert.IsTrue(owned.Add(cell), $"({cell.x}, {cell.y}) è di due pezzi");
                }
            }

            Assert.AreEqual(covered, owned.Count(c => map.IsCovered(c.x, c.y)), "nessuna cella coperta senza il suo pezzo");
        }

        [Test, Description("Case, forgia, cappella, pozzo, cripta e grotta toccano il pavimento: hanno una porta sulla strada (M18)")]
        public void Buildings_FaceTheStreet()
        {
            var map = Load();
            foreach (var marker in map.Markers.Where(m => Buildings.IndexOf(m.Symbol) >= 0))
            {
                Assert.IsTrue(MapPiece.TryFacing(map, marker, MapPiece.Footprint(map, marker), out _), $"'{marker.Symbol}' in ({marker.X}, {marker.Y}) senza strada davanti");
            }
        }

        [Test, Description("Dal portale al mercante, al fabbro, alla guaritrice, all'altare e alla grotta meno di 10 s a piedi, girando attorno ai recinti (D2 e D18 della M18)")]
        public void Paths_FromThePortalAreShort()
        {
            var map = Load();
            var portal = map.Markers.Single(m => m.Symbol == 'P');
            var distance = new Dictionary<Vector2Int, int> { [new Vector2Int(portal.X, portal.Y)] = 0 };
            var open = new Queue<Vector2Int>(distance.Keys);
            var steps = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (open.Count > 0)
            {
                var cell = open.Dequeue();
                foreach (var step in steps)
                {
                    var next = cell + step;
                    if (map.IsFloor(next.x, next.y) && map.GetSymbol(next.x, next.y) != Fence && !distance.ContainsKey(next))
                    {
                        distance[next] = distance[cell] + 1;
                        open.Enqueue(next);
                    }
                }
            }

            foreach (char target in new[] { 'M', 'E', 'L', 'A', '>' })
            {
                var marker = map.Markers.Single(m => m.Symbol == target);
                Assert.IsTrue(distance.TryGetValue(new Vector2Int(marker.X, marker.Y), out int cells), $"'{target}' non si raggiunge");

                // a passi di cella senza diagonali: per eccesso, il cavaliere taglia gli angoli
                Assert.LessOrEqual(cells * LevelMap.CellSize, WalkSpeed * MaxWalk, $"'{target}' a {cells} celle");
            }
        }
    }
}
