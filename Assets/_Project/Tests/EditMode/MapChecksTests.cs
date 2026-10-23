using System.Collections.Generic;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>I controlli comuni delle mappe (passo 15.2) trovano gli errori: su mappe scritte a mano, non generate.</summary>
    public class MapChecksTests
    {
        // '#' roccia, '.' pavimento, '<' ingresso, '>' scala, 'c' cassa; la prima riga è il nord
        private static DungeonLayout Layout(params string[] rows)
        {
            var cells = new char[rows[0].Length, rows.Length];
            Vector2Int entrance = default, stairs = default;
            for (int y = 0; y < rows.Length; y++)
            {
                for (int x = 0; x < rows[y].Length; x++)
                {
                    cells[x, y] = rows[y][x];
                    if (rows[y][x] == DungeonGenerator.EntranceSymbol)
                    {
                        entrance = new Vector2Int(x, y);
                    }
                    else if (rows[y][x] == DungeonGenerator.StairsSymbol)
                    {
                        stairs = new Vector2Int(x, y);
                    }
                }
            }

            return new DungeonLayout(cells, new List<RectInt>(), 0, 0, entrance, stairs);
        }

        [Test, Description("Un pavimento isolato: irraggiungibile, e i controlli dicono dove; senza, niente da dire")]
        public void Island_IsFound()
        {
            var island = Layout(
                "#######",
                "#.>#..#",
                "#...#.#",
                "#<..###",
                "#######");
            StringAssert.StartsWith("(4, 1) irraggiungibile", MapChecks.Reachable(island, stairs: true));
            Assert.IsNull(MapChecks.Exits(island, stairs: true));
            Assert.IsNull(MapChecks.Border(island));

            var whole = Layout(
                "#######",
                "#.>#..#",
                "#......",
                "#<..###",
                "#######");
            Assert.IsNull(MapChecks.Reachable(whole, stairs: true));
            Assert.AreEqual("pavimento sul bordo in (6, 2)", MapChecks.Border(whole));
        }

        [Test, Description("Una cassa che chiude il corridoio: a piedi si arriva, girando attorno agli ostacoli no; ed è in un cunicolo")]
        public void BlockingChest_IsFound()
        {
            var blocked = Layout(
                "########",
                "######>#",
                "#<.c...#",
                "########");
            Assert.IsNull(MapChecks.Reachable(blocked, stairs: true));
            StringAssert.StartsWith("(4, 2) chiusa da un ostacolo", MapChecks.ObstaclesLeavePath(blocked, stairs: true));
            Assert.AreEqual("ostacolo in un cunicolo in (3, 2)", CaveChecks.Corridors(blocked));

            var open = Layout(
                "########",
                "######>#",
                "#<.....#",
                "########");
            Assert.IsNull(MapChecks.ObstaclesLeavePath(open, stairs: true));
            Assert.IsNull(CaveChecks.Corridors(open));
            Assert.IsNull(MapChecks.Exits(open, stairs: true));
        }

        [Test, Description("Due ingressi, una scala senza la roccia a nord o vicina all'ingresso: le uscite non vanno")]
        public void WrongExits_AreFound()
        {
            Assert.AreEqual("2 ingressi", MapChecks.Exits(Layout(
                "######",
                "####>#",
                "#<.<.#",
                "######"), stairs: true));
            Assert.AreEqual("a nord della scala non c'è la roccia", MapChecks.Exits(Layout(
                "######",
                "#...##",
                "#.>..#",
                "#<...#",
                "######"), stairs: true));
            StringAssert.StartsWith("scala a 2 passi, il punto più lontano a 5", MapChecks.Exits(Layout(
                "#########",
                "#>#######",
                "#.<.....#",
                "#########"), stairs: true));
        }
    }
}
