using System;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Disegna una <see cref="LevelMap"/> in una texture: un pixel per cella per la finestra del
    /// generatore nell'editor (D10 della M6), a linee e solo dove si è già passati per l'automappa
    /// (D15). La riga 0 della mappa, il nord, finisce in alto nell'immagine.
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

        // l'automappa: pavimento appena accennato e muri a linee chiare, come in Diablo
        public static readonly Color32 ExploredFloor = new Color32(120, 105, 85, 60);
        public static readonly Color32 ExploredWall = new Color32(220, 195, 145, 255);
        public static readonly Color32 ExploredStairs = new Color32(240, 200, 60, 220);

        /// <summary>Il colore di un simbolo della mappa; gli altri marcatori sono pavimento.</summary>
        public static Color32 ColorOf(char symbol)
        {
            switch (symbol)
            {
                case LevelMap.Rock: return Rock;
                case DungeonGenerator.EntranceSymbol: return Entrance;
                case DungeonGenerator.StairsSymbol: return Stairs;
                case DungeonPopulator.EnemySymbol: return Enemy;
                case CavePopulator.SwarmSymbol: return Enemy;
                case CavePopulator.BruteSymbol: return Enemy;
                case DungeonPopulator.ChestSymbol: return Chest;
                case DungeonPopulator.TorchSymbol: return Torch;
                case CavePopulator.CandleSymbol: return Torch;
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
        /// La texture dell'automappa: <paramref name="pixelsPerCell"/> pixel per lato di cella,
        /// abbastanza per disegnare i muri come linee.
        /// </summary>
        public static Texture2D CreateExploredTexture(LevelMap map, int pixelsPerCell)
        {
            return new Texture2D(map.Width * pixelsPerCell, map.Height * pixelsPerCell, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
        }

        /// <summary>
        /// Ridisegna l'automappa: per ogni cella scoperta il pavimento appena accennato, una linea su
        /// ogni lato che confina con la roccia, la scala piena, un quadratino per casse e ingresso.
        /// Il resto è trasparente. <paramref name="pixels"/> è il buffer riusato, grande quanto la texture.
        /// </summary>
        public static void PaintExplored(Exploration exploration, Texture2D texture, Color32[] pixels)
        {
            var map = exploration.Map;
            int size = texture.width / map.Width;
            if (size < 1 || texture.width != map.Width * size || texture.height != map.Height * size || pixels.Length != texture.width * texture.height)
            {
                throw new ArgumentException($"La texture {texture.width}×{texture.height} non è un multiplo della mappa {map.Width}×{map.Height}, o il buffer non è grande quanto lei.");
            }

            // un decimo di cella: nella minimappa resta più di un pixel, nella vista sovrapposta non diventa una striscia
            int line = Math.Max(1, size / 10);
            int markFrom = size * 3 / 10, markTo = size - markFrom;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    bool explored = exploration.IsExplored(x, y);
                    char symbol = map.GetSymbol(x, y);
                    bool north = !map.IsFloor(x, y - 1), south = !map.IsFloor(x, y + 1);
                    bool west = !map.IsFloor(x - 1, y), east = !map.IsFloor(x + 1, y);
                    Color32 fill = symbol == DungeonGenerator.StairsSymbol ? ExploredStairs : ExploredFloor;
                    Color32 mark = symbol == DungeonPopulator.ChestSymbol ? Chest : symbol == DungeonGenerator.EntranceSymbol ? Entrance : Unknown;

                    // la riga 0 della texture è in basso: la cella (x, y) parte dalla riga (altezza - 1 - y)
                    int originX = x * size, originY = (map.Height - 1 - y) * size;
                    for (int j = 0; j < size; j++)
                    {
                        int row = (originY + j) * texture.width + originX;
                        for (int i = 0; i < size; i++)
                        {
                            Color32 color = Unknown;
                            if (explored)
                            {
                                bool wall = (north && j >= size - line) || (south && j < line) || (west && i < line) || (east && i >= size - line);
                                bool marked = mark.a > 0 && i >= markFrom && i < markTo && j >= markFrom && j < markTo;
                                color = wall ? ExploredWall : marked ? mark : fill;
                            }

                            pixels[row + i] = color;
                        }
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
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
