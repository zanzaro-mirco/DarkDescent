using System;
using System.Collections.Generic;
using DarkDescent.Core;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Il contenuto di una cripta generata (D6 della scheda M6): torce sui muri alti, casse e oggetti
    /// di scena contro i muri, scheletri a gruppi, più numerosi in profondità. Tutto come marcatori
    /// della mappa, gli stessi delle mappe a mano. Dalla M11 (D4) gli arredi: ogni stanza ha un tema e
    /// qualche composizione di mobili contro i muri, con un flusso suo del seme; dal passo 11.7 anche
    /// qualche mucchietto a terra; dalla M13 (D2–D4) un punto d'interesse al centro delle stanze grandi
    /// al posto del colonnato, mai due arredi uguali nella stessa stanza e mucchietti legati al tema.
    /// Logica pura.
    /// </summary>
    public sealed class DungeonPopulator
    {
        public const char TorchSymbol = 'T';
        public const char EnemySymbol = 'S';
        public const char ChestSymbol = 'c';

        /// <summary>Uno scheletro mago (D6 della M9): da solo, in una stanza.</summary>
        public const char MageSymbol = 'm';

        // barile, casse di legno, pilastro: ostacoli, il NavMesh ci gira attorno
        private const string PropSymbols = "bxp";

        /// <summary>
        /// Le composizioni di mobili per tema (D4 della M11), cinque per tema dal passo 11.7: deposito,
        /// studio, cella, rovina; dal passo 13.3 sacchi e anfore nel deposito, la gabbia nella cella e il
        /// quinto tema, la cappella (sarcofago, statua, altare). Ognuna occupa una cella contro un muro
        /// ed è un ostacolo. Le lettere sono finite: i simboli nuovi sono cifre e segni.
        /// </summary>
        public static readonly string[] DecorThemes = { "DEFad78", "LNOeg", "RUVhj9", "WYZnq", "+&%=" };

        /// <summary>
        /// Un gruppo di barili al centro della stanza, il punto d'interesse più comune (D4 della M13): un
        /// ostacolo. Le cifre sono i simboli della M13: le lettere sono finite.
        /// </summary>
        public const char BarrelGroupSymbol = '0';

        /// <summary>
        /// I gruppi di barili da rompere contro un muro o in un angolo (D5 della M13): due barili, o tre.
        /// Arrivano dopo gli arredi, finché il livello non ha i barili delle impostazioni.
        /// </summary>
        public const string BarrelWallSymbols = "1!";

        // il barile singolo degli oggetti di scena
        private const char BarrelSymbol = 'b';

        /// <summary>I pozzi del punto d'interesse (D7 della M13): della vita e del mana.</summary>
        public const string WellSymbols = "()";

        /// <summary>I santuari del punto d'interesse (D6 della M13): Furia, Vento, Pietra e Sapienza.</summary>
        public const string ShrineSymbols = "[]{}";

        /// <summary>Il lato minimo, in celle, di una stanza con un punto d'interesse (dove andava il colonnato).</summary>
        public const int MinFeatureRoom = 5;

        /// <summary>
        /// I mucchietti a terra (passo 11.7 della M11): ossa e teschi, piatti e bottiglie, armi rotte; dal
        /// passo 13.3 libri, macerie, cocci, catene e candele. Stanno anche in mezzo alla stanza e si
        /// attraversano: non sono ostacoli.
        /// </summary>
        public const string ScatterSymbols = "vyz23456";

        /// <summary>Piatti e bottiglie: solo accanto a un mobile della stanza (D3 della M13).</summary>
        public const char TableScatterSymbol = 'y';

        /// <summary>
        /// I mucchietti di ogni tema, nello stesso ordine di <see cref="DecorThemes"/> (D3 della M13):
        /// piatti e bottiglie e cocci nel deposito, piatti e bottiglie e libri nello studio, ossa e catene
        /// nella cella, armi rotte e macerie nella rovina, candele nella cappella.
        /// </summary>
        public static readonly string[] ScatterThemes = { "y4", "y2", "v5", "z3", "6" };

        private static readonly string DecorSymbols = string.Concat(DecorThemes) + BarrelGroupSymbol + BarrelWallSymbols + WellSymbols
            + ShrineSymbols + ScatterSymbols;

        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private readonly DungeonSettings _settings;

        public DungeonPopulator(DungeonSettings settings)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Un simbolo che il cavaliere non attraversa: casse, forzieri, oggetti di scena, arredi tranne i mucchietti.</summary>
        public static bool IsObstacle(char symbol)
        {
            return IsChest(symbol) || PropSymbols.IndexOf(symbol) >= 0 || (IsDecor(symbol) && !IsScatter(symbol)) || CavePopulator.IsDecor(symbol);
        }

        /// <summary>Una cassa di qualunque tipo: quella di sempre, il forziere o il tesoro del re (D8 della M12).</summary>
        public static bool IsChest(char symbol)
        {
            return symbol == ChestSymbol || symbol == SpecialEnemyPlanner.GoldChestSymbol || symbol == SpecialEnemyPlanner.TreasureSymbol;
        }

        /// <summary>Un arredo: una composizione di mobili, un gruppo di barili al centro o un mucchietto a terra.</summary>
        public static bool IsDecor(char symbol)
        {
            return DecorSymbols.IndexOf(symbol) >= 0;
        }

        /// <summary>Un mucchietto a terra, che si attraversa.</summary>
        public static bool IsScatter(char symbol)
        {
            return ScatterSymbols.IndexOf(symbol) >= 0;
        }

        /// <summary>Un pozzo o un santuario: si usa una volta, ed è un ostacolo al centro della stanza.</summary>
        public static bool IsFeature(char symbol)
        {
            return WellSymbols.IndexOf(symbol) >= 0 || ShrineSymbols.IndexOf(symbol) >= 0;
        }

        /// <summary>Quanti barili da rompere ci sono nei gruppi di barili: tre al centro, due o tre contro il muro.</summary>
        public static int BarrelsInGroup(char symbol)
        {
            switch (symbol)
            {
                case BarrelGroupSymbol:
                    return 3;
                case '1':
                    return 2;
                case '!':
                    return 3;
                default:
                    return 0;
            }
        }

        public void Populate(DungeonLayout layout, int depth, IRandomSource random)
        {
            var reserved = Reserved(layout);

            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                PlaceTorches(layout, layout.Rooms[i]);
            }

            PlaceChests(layout, depth, random, reserved);
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                int props = Range(random, 0, _settings.MaxPropsPerRoom);
                for (int p = 0; p < props; p++)
                {
                    TryPlaceAgainstWall(layout, layout.Rooms[i], PropSymbols[Range(random, 0, PropSymbols.Length - 1)], random, reserved);
                }
            }

            PlaceEnemies(layout, depth, random, reserved);

            // per ultimi: i tiri in più non spostano niente di quello che c'era prima (trappola 10 della M7)
            PlaceMages(layout, depth, random, reserved);
        }

        /// <summary>
        /// Gli arredi (D4 della M11), dopo tutto il resto e con un flusso suo del seme: nemici, casse e
        /// mappa dei semi già provati non cambiano. Ogni stanza ha un tema e una composizione ogni
        /// quattro celle di lato, fino al massimo delle impostazioni, contro i muri e mai su un
        /// passaggio, e qualche mucchietto a terra del suo tema. Dalla M13 le stanze grandi hanno al
        /// centro un punto d'interesse (D4), e nella stessa stanza nessun arredo si ripete (D3).
        /// </summary>
        public void Decorate(DungeonLayout layout, int depth, IRandomSource random)
        {
            if (_settings.MaxDecorPerRoom <= 0)
            {
                return;
            }

            var reserved = Reserved(layout);
            int wells = 0;
            int shrines = 0;
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                RectInt room = layout.Rooms[i];
                int themeIndex = Range(random, 0, DecorThemes.Length - 1);

                // il punto d'interesse prima dei mobili: vuole il centro, i mobili il bordo
                if (room.width >= MinFeatureRoom && room.height >= MinFeatureRoom)
                {
                    var kind = _settings.FeatureFor(random.NextDouble(), depth);
                    if ((kind == RoomFeatureKind.Well && wells++ > 0) || (kind == RoomFeatureKind.Shrine && shrines++ > 0))
                    {
                        kind = RoomFeatureKind.Barrels;
                    }

                    if (i != layout.StartRoom && kind != RoomFeatureKind.None)
                    {
                        PlaceFeature(layout, i, kind, depth, reserved);
                    }
                }

                var used = new HashSet<char>();
                var compositions = new List<Vector2Int>();
                int count = Math.Min(_settings.MaxDecorPerRoom, 1 + (room.width + room.height) / 4);
                for (int d = 0; d < count; d++)
                {
                    char symbol = PickUnused(DecorThemes[themeIndex], used, random);
                    if (symbol == '\0')
                    {
                        break;
                    }

                    var cell = TryPlaceAgainstWall(layout, room, symbol, random, reserved, keepChestsFree: true);
                    if (cell.HasValue)
                    {
                        compositions.Add(cell.Value);
                    }
                }

                int scatter = Range(random, 0, _settings.MaxScatterPerRoom);
                for (int s = 0; s < scatter; s++)
                {
                    char symbol = PickUnused(ScatterThemes[themeIndex], used, random);
                    if (symbol == '\0')
                    {
                        break;
                    }

                    if (symbol == TableScatterSymbol)
                    {
                        TryPlaceBeside(layout, room, compositions, symbol, random, reserved);
                    }
                    else
                    {
                        TryPlaceScatter(layout, room, symbol, random, reserved);
                    }
                }
            }

            PlaceWallBarrels(layout, random, reserved);
        }

        // I gruppi di barili contro i muri (D5 della M13), dopo tutti gli arredi: finché il livello non ha
        // i barili del tiro, una stanza a caso, mai la stessa due volte, ne prende un gruppo. Di due se ne
        // mancano due o meno, sennò di due o di tre
        private void PlaceWallBarrels(DungeonLayout layout, IRandomSource random, HashSet<Vector2Int> reserved)
        {
            var range = _settings.BarrelsPerLevel;
            int wanted = Range(random, range.x, range.y);
            int count = CountBarrels(layout);
            var rooms = new List<int>();
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                rooms.Add(i);
            }

            while (count < wanted && rooms.Count > 0)
            {
                int pick = Range(random, 0, rooms.Count - 1);
                char symbol = wanted - count <= 2 ? BarrelWallSymbols[0] : BarrelWallSymbols[Range(random, 0, BarrelWallSymbols.Length - 1)];
                if (TryPlaceAgainstWall(layout, layout.Rooms[rooms[pick]], symbol, random, reserved, keepChestsFree: true).HasValue)
                {
                    count += BarrelsInGroup(symbol);
                }

                rooms.RemoveAt(pick);
            }
        }

        /// <summary>I barili da rompere della cripta: quelli singoli e quelli dei gruppi.</summary>
        public static int CountBarrels(DungeonLayout layout)
        {
            int count = 0;
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    char symbol = layout[x, y];
                    count += symbol == BarrelSymbol ? 1 : BarrelsInGroup(symbol);
                }
            }

            return count;
        }

        // Un simbolo del gruppo che la stanza non ha ancora, o '\0' se li ha tutti
        private static char PickUnused(string symbols, HashSet<char> used, IRandomSource random)
        {
            var free = new List<char>();
            foreach (char c in symbols)
            {
                if (!used.Contains(c))
                {
                    free.Add(c);
                }
            }

            if (free.Count == 0)
            {
                return '\0';
            }

            char pick = free[Range(random, 0, free.Count - 1)];
            used.Add(pick);
            return pick;
        }

        // Il punto d'interesse sulla cella libera più vicina al centro della stanza, lontano dai muri, dai
        // passaggi, da casse e scala e dal percorso tra le porte, e che non chiuda il cammino. La cella
        // resta riservata: le trappole arrivano nel passo 13.7 nella stessa cella.
        private static void PlaceFeature(DungeonLayout layout, int roomIndex, RoomFeatureKind kind, int depth, HashSet<Vector2Int> reserved)
        {
            RectInt room = layout.Rooms[roomIndex];
            var path = DoorPaths(layout, room);
            var candidates = new List<Vector2Int>();
            for (int y = room.yMin + 1; y < room.yMax - 1; y++)
            {
                for (int x = room.xMin + 1; x < room.xMax - 1; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (layout[x, y] == LevelMap.Floor && !reserved.Contains(cell) && !path.Contains(cell)
                        && !NearDoorway(layout, room, cell) && !NextToChestOrStairs(layout, cell))
                    {
                        candidates.Add(cell);
                    }
                }
            }

            // il centro della stanza in coordinate doppie, per restare sugli interi
            int cx = room.xMin + room.xMax - 1;
            int cy = room.yMin + room.yMax - 1;
            candidates.Sort((a, b) => Distance2(a, cx, cy).CompareTo(Distance2(b, cx, cy)));
            foreach (var cell in candidates)
            {
                // ogni punto d'interesse è un ostacolo tranne le trappole, che arrivano nel passo 13.7: per
                // loro si prova con i barili e la cella resta vuota
                bool traps = kind == RoomFeatureKind.Traps;
                layout.TryPlace(traps ? BarrelGroupSymbol : FeatureSymbol(kind, cell, depth), cell.x, cell.y);
                bool connected = IsConnected(layout);
                if (!connected || traps)
                {
                    layout.Remove(cell.x, cell.y);
                }

                if (connected)
                {
                    reserved.Add(cell);
                    layout.AddFeature(new RoomFeature(roomIndex, kind, cell));
                    return;
                }
            }
        }

        // Il simbolo del punto d'interesse; quale pozzo e quale santuario lo dice un hash della cella e della
        // profondità, non il flusso degli arredi: gli arredi dei semi già provati non scorrono
        private static char FeatureSymbol(RoomFeatureKind kind, Vector2Int cell, int depth)
        {
            switch (kind)
            {
                case RoomFeatureKind.Well:
                    return WellSymbols[CellPick(cell, depth, WellSymbols.Length)];
                case RoomFeatureKind.Shrine:
                    return ShrineSymbols[CellPick(cell, depth, ShrineSymbols.Length)];
                default:
                    return BarrelGroupSymbol;
            }
        }

        private static int CellPick(Vector2Int cell, int depth, int count)
        {
            unchecked
            {
                uint h = (uint)cell.x * 0x9E3779B1u ^ (uint)cell.y * 0x85EBCA77u ^ (uint)depth * 0xC2B2AE3Du;
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                h *= 0x297A2D39u;
                h ^= h >> 15;
                return (int)(h % (uint)count);
            }
        }

        private static int Distance2(Vector2Int cell, int cx, int cy)
        {
            int dx = 2 * cell.x - cx;
            int dy = 2 * cell.y - cy;
            return dx * dx + dy * dy;
        }

        // Le celle dei percorsi più brevi, dentro la stanza, tra le porte, l'ingresso e la scala che
        // stanno nella stanza: il punto d'interesse non le occupa, chi attraversa la stanza va dritto
        private static HashSet<Vector2Int> DoorPaths(DungeonLayout layout, RectInt room)
        {
            var ends = new List<Vector2Int>();
            for (int y = room.yMin; y < room.yMax; y++)
            {
                for (int x = room.xMin; x < room.xMax; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (layout.IsFloor(x, y) && IsDoorway(layout, room, cell))
                    {
                        ends.Add(cell);
                    }
                }
            }

            if (room.Contains(layout.Entrance))
            {
                ends.Add(layout.Entrance);
            }

            if (room.Contains(layout.Stairs))
            {
                ends.Add(layout.Stairs);
            }

            var path = new HashSet<Vector2Int>();
            for (int a = 0; a < ends.Count; a++)
            {
                var parents = PathsFrom(layout, room, ends[a]);
                for (int b = a + 1; b < ends.Count; b++)
                {
                    for (var at = ends[b]; parents.ContainsKey(at); at = parents[at])
                    {
                        path.Add(at);
                        if (at == ends[a])
                        {
                            break;
                        }
                    }
                }
            }

            return path;
        }

        // Visita in ampiezza dentro la stanza: per ogni cella raggiunta, la cella da cui ci si arriva
        private static Dictionary<Vector2Int, Vector2Int> PathsFrom(DungeonLayout layout, RectInt room, Vector2Int start)
        {
            var parents = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps)
                {
                    var next = cell + step;
                    if (room.Contains(next) && !parents.ContainsKey(next) && (Walkable(layout, next.x, next.y) || next == layout.Stairs))
                    {
                        parents[next] = cell;
                        queue.Enqueue(next);
                    }
                }
            }

            return parents;
        }

        // Piatti e bottiglie su una cella libera accanto a un mobile della stanza, mai su un passaggio
        private static bool TryPlaceBeside(DungeonLayout layout, RectInt room, List<Vector2Int> compositions, char symbol, IRandomSource random, HashSet<Vector2Int> reserved)
        {
            var candidates = new List<Vector2Int>();
            foreach (var composition in compositions)
            {
                foreach (var step in Steps)
                {
                    var cell = composition + step;
                    if (room.Contains(cell) && layout[cell.x, cell.y] == LevelMap.Floor && !reserved.Contains(cell)
                        && !NearDoorway(layout, room, cell) && !candidates.Contains(cell))
                    {
                        candidates.Add(cell);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            var pick = candidates[Range(random, 0, candidates.Count - 1)];
            return layout.TryPlace(symbol, pick.x, pick.y);
        }

        private static bool NextToChestOrStairs(DungeonLayout layout, Vector2Int cell)
        {
            foreach (var step in Steps)
            {
                char c = layout[cell.x + step.x, cell.y + step.y];
                if (c == ChestSymbol || c == DungeonGenerator.StairsSymbol)
                {
                    return true;
                }
            }

            return false;
        }

        // Un mucchietto su una cella libera della stanza, lontano dai muri se si può: si attraversa,
        // quindi non chiude niente e non serve controllare il cammino
        private static bool TryPlaceScatter(DungeonLayout layout, RectInt room, char symbol, IRandomSource random, HashSet<Vector2Int> reserved)
        {
            var inner = new List<Vector2Int>();
            var border = new List<Vector2Int>();
            for (int y = room.yMin; y < room.yMax; y++)
            {
                for (int x = room.xMin; x < room.xMax; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (layout[x, y] != LevelMap.Floor || reserved.Contains(cell))
                    {
                        continue;
                    }

                    bool edge = x == room.xMin || y == room.yMin || x == room.xMax - 1 || y == room.yMax - 1;
                    (edge ? border : inner).Add(cell);
                }
            }

            var candidates = inner.Count > 0 ? inner : border;
            if (candidates.Count == 0)
            {
                return false;
            }

            var pick = candidates[Range(random, 0, candidates.Count - 1)];
            return layout.TryPlace(symbol, pick.x, pick.y);
        }

        // davanti alla scala si deve poter arrivare, ai suoi lati ci sono le balaustre, e il cavaliere
        // non deve comparire addosso a un barile: queste celle restano libere
        private static HashSet<Vector2Int> Reserved(DungeonLayout layout)
        {
            var reserved = new HashSet<Vector2Int>
            {
                layout.Stairs + Vector2Int.up, layout.Stairs + Vector2Int.left, layout.Stairs + Vector2Int.right,
            };
            foreach (var step in Steps)
            {
                reserved.Add(layout.Entrance + step);
            }

            return reserved;
        }

        // ogni mago in una stanza diversa se ce ne sono abbastanza, su una cella libera qualsiasi
        private void PlaceMages(DungeonLayout layout, int depth, IRandomSource random, HashSet<Vector2Int> reserved)
        {
            Vector2Int range = _settings.MagesAt(depth);
            if (range.y <= 0)
            {
                return;
            }

            int count = Range(random, range.x, range.y);
            var rooms = OtherRooms(layout, random);
            for (int m = 0; m < count && rooms.Count > 0; m++)
            {
                RectInt room = layout.Rooms[rooms[m % rooms.Count]];
                var free = new List<Vector2Int>();
                for (int y = room.yMin; y < room.yMax; y++)
                {
                    for (int x = room.xMin; x < room.xMax; x++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (layout[x, y] == LevelMap.Floor && !reserved.Contains(cell))
                        {
                            free.Add(cell);
                        }
                    }
                }

                if (free.Count > 0)
                {
                    var pick = free[Range(random, 0, free.Count - 1)];
                    layout.TryPlace(MageSymbol, pick.x, pick.y);
                }
            }
        }

        // Sul muro nord della stanza, dove oltre c'è roccia; se non c'è posto, sul muro est. Una ogni
        // CellsPerTorch celle di larghezza, distribuite: il builder le appende al muro alto della cella.
        private void PlaceTorches(DungeonLayout layout, RectInt room)
        {
            var candidates = new List<Vector2Int>();
            for (int x = room.xMin; x < room.xMax; x++)
            {
                if (!layout.IsFloor(x, room.yMin - 1) && layout[x, room.yMin] == LevelMap.Floor)
                {
                    candidates.Add(new Vector2Int(x, room.yMin));
                }
            }

            if (candidates.Count == 0)
            {
                for (int y = room.yMin; y < room.yMax; y++)
                {
                    if (!layout.IsFloor(room.xMax, y) && layout[room.xMax - 1, y] == LevelMap.Floor)
                    {
                        candidates.Add(new Vector2Int(room.xMax - 1, y));
                    }
                }
            }

            int count = Math.Min(candidates.Count, 1 + (room.width - 1) / _settings.CellsPerTorch);
            for (int i = 0; i < count; i++)
            {
                var cell = candidates[(2 * i + 1) * candidates.Count / (2 * count)];
                layout.TryPlace(TorchSymbol, cell.x, cell.y);
            }
        }

        // Le casse nelle stanze lontane dall'ingresso, una per stanza finché ce ne sono.
        private void PlaceChests(DungeonLayout layout, int depth, IRandomSource random, HashSet<Vector2Int> reserved)
        {
            var rooms = OtherRooms(layout, random);
            int placed = 0;
            for (int i = 0; placed < _settings.ChestCount(depth) && i < rooms.Count * 2; i++)
            {
                if (TryPlaceAgainstWall(layout, layout.Rooms[rooms[i % rooms.Count]], ChestSymbol, random, reserved).HasValue)
                {
                    placed++;
                }
            }
        }

        private void PlaceEnemies(DungeonLayout layout, int depth, IRandomSource random, HashSet<Vector2Int> reserved)
        {
            var rooms = OtherRooms(layout, random);
            int remaining = _settings.EnemyCount(depth);
            for (int i = 0; remaining > 0 && i < rooms.Count * 4; i++)
            {
                RectInt room = layout.Rooms[rooms[i % rooms.Count]];
                var free = new List<Vector2Int>();
                for (int y = room.yMin; y < room.yMax; y++)
                {
                    for (int x = room.xMin; x < room.xMax; x++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (layout[x, y] == LevelMap.Floor && !reserved.Contains(cell))
                        {
                            free.Add(cell);
                        }
                    }
                }

                int group = Math.Min(remaining, Range(random, _settings.MinGroup, _settings.MaxGroup));
                for (int g = 0; g < group && free.Count > 0; g++)
                {
                    int pick = Range(random, 0, free.Count - 1);
                    layout.TryPlace(EnemySymbol, free[pick].x, free[pick].y);
                    free.RemoveAt(pick);
                    remaining--;
                }
            }
        }

        // Tutte le stanze tranne quella d'ingresso, in ordine mescolato dal seme.
        private static List<int> OtherRooms(DungeonLayout layout, IRandomSource random)
        {
            var rooms = new List<int>();
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                if (i != layout.StartRoom)
                {
                    rooms.Add(i);
                }
            }

            for (int i = rooms.Count - 1; i > 0; i--)
            {
                int j = Range(random, 0, i);
                (rooms[i], rooms[j]) = (rooms[j], rooms[i]);
            }

            return rooms;
        }

        // Un ostacolo su una cella del bordo della stanza, mai su un passaggio verso un corridoio né
        // accanto: se comunque chiude il cammino verso un pezzo del livello, si toglie (trappola 5).
        // Gli arredi lasciano libere anche le celle accanto alle casse, che si aprono dal davanti
        // (passo 11.7); casse e oggetti di scena no, per non cambiare i livelli dei semi già provati.
        private static Vector2Int? TryPlaceAgainstWall(DungeonLayout layout, RectInt room, char symbol, IRandomSource random, HashSet<Vector2Int> reserved, bool keepChestsFree = false)
        {
            var candidates = new List<Vector2Int>();
            for (int y = room.yMin; y < room.yMax; y++)
            {
                for (int x = room.xMin; x < room.xMax; x++)
                {
                    bool border = x == room.xMin || y == room.yMin || x == room.xMax - 1 || y == room.yMax - 1;
                    var cell = new Vector2Int(x, y);
                    if (border && layout[x, y] == LevelMap.Floor && !reserved.Contains(cell) && !NearDoorway(layout, room, cell)
                        && !(keepChestsFree && NextToChestOrStairs(layout, cell)))
                    {
                        candidates.Add(cell);
                    }
                }
            }

            while (candidates.Count > 0)
            {
                int pick = Range(random, 0, candidates.Count - 1);
                var cell = candidates[pick];
                candidates.RemoveAt(pick);
                layout.TryPlace(symbol, cell.x, cell.y);
                if (IsConnected(layout))
                {
                    return cell;
                }

                layout.Remove(cell.x, cell.y);
            }

            return null;
        }

        // La cella, o una vicina dentro la stanza, confina con un pavimento fuori dalla stanza: un corridoio.
        private static bool NearDoorway(DungeonLayout layout, RectInt room, Vector2Int cell)
        {
            if (IsDoorway(layout, room, cell))
            {
                return true;
            }

            foreach (var step in Steps)
            {
                var next = cell + step;
                if (room.Contains(next) && IsDoorway(layout, room, next))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDoorway(DungeonLayout layout, RectInt room, Vector2Int cell)
        {
            foreach (var step in Steps)
            {
                var next = cell + step;
                if (!room.Contains(next) && layout.IsFloor(next.x, next.y))
                {
                    return true;
                }
            }

            return false;
        }

        // Dall'ingresso si arriva a piedi su ogni cella che non sia roccia, scala o ostacolo.
        private static bool IsConnected(DungeonLayout layout)
        {
            var seen = new bool[layout.Width, layout.Height];
            var queue = new Queue<Vector2Int>();
            seen[layout.Entrance.x, layout.Entrance.y] = true;
            queue.Enqueue(layout.Entrance);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps)
                {
                    var next = cell + step;
                    if (Walkable(layout, next.x, next.y) && !seen[next.x, next.y])
                    {
                        seen[next.x, next.y] = true;
                        queue.Enqueue(next);
                    }
                }
            }

            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (Walkable(layout, x, y) && !seen[x, y])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Walkable(DungeonLayout layout, int x, int y)
        {
            char c = layout[x, y];
            return c != LevelMap.Rock && c != DungeonGenerator.StairsSymbol && !IsObstacle(c);
        }

        private static int Range(IRandomSource random, int min, int max)
        {
            return Math.Min(max, min + (int)(random.NextDouble() * (max - min + 1)));
        }
    }
}
