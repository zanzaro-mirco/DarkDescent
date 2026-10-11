using System.Collections.Generic;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>La mappa di testo della città (D3 della M10).</summary>
    public class TownMapTests
    {
        private const string MapPath = "Assets/_Project/Data/Levels/Town.txt";
        private const string TilesetPath = "Assets/_Project/Data/Levels/TownTileset.asset";

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

        [Test, Description("Ogni simbolo della mappa ha un prefab nel tileset della città, e nessuno è un nemico")]
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
    }
}
