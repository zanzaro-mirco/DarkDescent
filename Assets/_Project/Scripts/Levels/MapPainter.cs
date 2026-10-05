using System;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Disegna una <see cref="LevelMap"/> in una texture, un pixel per cella: la usano la finestra
    /// del generatore nell'editor (D10 della M6) e l'automappa (D15). La riga 0 della mappa, il nord,
    /// finisce in alto nell'immagine.
    /// </summary>
    public static class MapPainter
    {
        public static readonly Color32 Unknown = new Color32(0, 0, 0, 0);
        public static readonly Color32 Rock = new Color32(18, 16, 20, 255);
        public static readonly Color32 Floor = new Color32(120, 112, 100, 255);
        public static readonly Color32 Entrance = new Color32(80, 200, 110, 255);
        public static readonly Color32 Stairs = new Color32(240, 200, 60, 255);
        public static readonly Color32 Enemy = new Color32(210, 50, 40, 255);
        public static readonly Color32 Chest = new Color32(230, 130, 40, 255);
        public static readonly Color32 Torch = new Color32(255, 230, 150, 255);
        public static readonly Color32 Obstacle = new Color32(85, 65, 45, 255);

        /// <summary>Il colore di un simbolo della mappa; gli altri marcatori sono pavimento.</summary>
        public static Color32 ColorOf(char symbol)
        {
            switch (symbol)
            {
                case LevelMap.Rock: return Rock;
                case DungeonGenerator.EntranceSymbol: return Entrance;
                case DungeonGenerator.StairsSymbol: return Stairs;
                case DungeonPopulator.EnemySymbol: return Enemy;
                case DungeonPopulator.ChestSymbol: return Chest;
                case DungeonPopulator.TorchSymbol: return Torch;
                default: return DungeonPopulator.IsObstacle(symbol) ? Obstacle : Floor;
            }
        }

        /// <summary>
        /// Una texture grande quanto la mappa, con i pixel netti: ingrandita resta a quadretti.
        /// </summary>
        public static Texture2D CreateTexture(LevelMap map)
        {
            return new Texture2D(map.Width, map.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
        }

        /// <summary>
        /// Ridisegna tutta la mappa. Con <paramref name="visible"/>, le celle non scoperte restano
        /// trasparenti; senza, si vede tutto.
        /// </summary>
        public static void Paint(LevelMap map, Texture2D texture, Func<int, int, bool> visible = null)
        {
            if (texture.width != map.Width || texture.height != map.Height)
            {
                throw new ArgumentException($"La texture è {texture.width}×{texture.height}, la mappa {map.Width}×{map.Height}.");
            }

            var pixels = new Color32[map.Width * map.Height];
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    bool seen = visible == null || visible(x, y);
                    pixels[(map.Height - 1 - y) * map.Width + x] = seen ? ColorOf(map.GetSymbol(x, y)) : Unknown;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
        }
    }
}
