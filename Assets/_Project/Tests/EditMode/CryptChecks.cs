using System.Linq;
using DarkDescent.Levels;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// I controlli della cripta, comuni ai test di ogni push e alla ronda notturna (passo 15.2): null
    /// se il livello va bene, altrimenti il primo errore.
    /// </summary>
    public static class CryptChecks
    {
        /// <summary>Una cripta di 28 × 28 celle ha almeno cinque stanze.</summary>
        public const int FewestRooms = 5;

        /// <summary>
        /// Almeno cinque stanze, delle misure delle impostazioni, che non si sovrappongono né si toccano;
        /// ingresso e scala in stanze diverse; il bordo di roccia.
        /// </summary>
        public static string Rooms(DungeonLayout layout, DungeonSettings settings)
        {
            var rooms = layout.Rooms;
            if (rooms.Count < FewestRooms)
            {
                return $"{rooms.Count} stanze";
            }

            if (layout.StartRoom == layout.ExitRoom)
            {
                return "ingresso e scala nella stessa stanza";
            }

            for (int i = 0; i < rooms.Count; i++)
            {
                var grown = new RectInt(rooms[i].x - 1, rooms[i].y - 1, rooms[i].width + 2, rooms[i].height + 2);
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    if (grown.Overlaps(rooms[j]))
                    {
                        return $"le stanze {rooms[i]} e {rooms[j]} si toccano";
                    }
                }

                bool fits = rooms[i].width >= settings.MinRoom && rooms[i].width <= settings.MaxRoom
                    && rooms[i].height >= settings.MinRoom && rooms[i].height <= settings.MaxRoom;
                if (!fits)
                {
                    return $"stanza {rooms[i]} fuori misura";
                }
            }

            return MapChecks.Border(layout);
        }

        /// <summary>Scheletri e casse dei numeri delle impostazioni (D6 della M6), mai uno scheletro nella stanza d'ingresso.</summary>
        public static string Population(DungeonLayout layout, DungeonSettings settings, int depth)
        {
            int skeletons = MapChecks.Count(layout, DungeonPopulator.EnemySymbol);
            if (skeletons != settings.EnemyCount(depth))
            {
                return $"{skeletons} scheletri invece di {settings.EnemyCount(depth)}";
            }

            int chests = MapChecks.Count(layout, DungeonPopulator.ChestSymbol);
            if (chests != settings.ChestCount(depth))
            {
                return $"{chests} casse invece di {settings.ChestCount(depth)}";
            }

            RectInt start = layout.Rooms[layout.StartRoom];
            for (int y = start.yMin; y < start.yMax; y++)
            {
                for (int x = start.xMin; x < start.xMax; x++)
                {
                    if (layout[x, y] == DungeonPopulator.EnemySymbol)
                    {
                        return $"scheletro nella stanza d'ingresso in ({x}, {y})";
                    }
                }
            }

            return null;
        }

        /// <summary>Ogni stanza ha almeno una torcia, e ogni torcia ha un muro alto a nord o a est dove il builder la appende.</summary>
        public static string Torches(DungeonLayout layout)
        {
            foreach (var room in layout.Rooms)
            {
                bool lit = false;
                for (int y = room.yMin; y < room.yMax && !lit; y++)
                {
                    for (int x = room.xMin; x < room.xMax && !lit; x++)
                    {
                        lit = layout[x, y] == DungeonPopulator.TorchSymbol;
                    }
                }

                if (!lit)
                {
                    return $"stanza {room} al buio\n{MapChecks.Text(layout)}";
                }
            }

            foreach (var torch in MapChecks.Find(layout, c => c == DungeonPopulator.TorchSymbol))
            {
                if (layout.IsFloor(torch.x, torch.y - 1) && layout.IsFloor(torch.x + 1, torch.y))
                {
                    return $"torcia in {torch} senza muro alto";
                }
            }

            return null;
        }

        /// <summary>
        /// Gli ostacoli stanno contro i muri delle stanze, mai in un corridoio; in mezzo solo il punto
        /// d'interesse (D4 della M13); mai accanto alla scala o all'ingresso.
        /// </summary>
        public static string ObstaclesAgainstWalls(DungeonLayout layout)
        {
            foreach (var cell in MapChecks.Find(layout, DungeonPopulator.IsObstacle))
            {
                var room = layout.Rooms.FirstOrDefault(r => r.Contains(cell));
                if (!room.Contains(cell))
                {
                    return $"ostacolo in un corridoio in {cell}";
                }

                char symbol = layout[cell.x, cell.y];
                bool border = cell.x == room.xMin || cell.y == room.yMin || cell.x == room.xMax - 1 || cell.y == room.yMax - 1;
                if (!border && symbol != DungeonPopulator.BarrelGroupSymbol && !DungeonPopulator.IsFeature(symbol))
                {
                    return $"ostacolo in mezzo alla stanza in {cell}";
                }

                // non dentro le balaustre della scala, non addosso al cavaliere appena entrato
                if (Next(cell, layout.Stairs))
                {
                    return $"ostacolo accanto alla scala in {cell}";
                }

                if (Next(cell, layout.Entrance))
                {
                    return $"ostacolo accanto all'ingresso in {cell}";
                }
            }

            return null;
        }

        private static bool Next(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }
    }
}
