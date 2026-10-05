using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class ExplorationTests
    {
        // due stanze divise da un muro, la sinistra collegata da un corridoio alla stanza in basso
        private const string Sample =
            "@depth 1\n@entrance Start\n" +
            "#########\n" +
            "#...#...#\n" +
            "#.<.#.c.#\n" +
            "#...#...#\n" +
            "##.######\n" +
            "##.######\n" +
            "#..>....#\n" +
            "#########\n";

        private const int Size = 10;

        private static Color32 Pixel(Texture2D texture, int height, int x, int y, int i, int j)
        {
            return texture.GetPixel(x * Size + i, (height - 1 - y) * Size + j);
        }

        [Test, Description("Si scopre per passi sul pavimento: la stanza dietro il muro resta nascosta anche se è vicina")]
        public void Reveal_WalksTheFloor()
        {
            var exploration = new Exploration(LevelMap.Parse(Sample));

            Assert.IsTrue(exploration.Reveal(new Vector2Int(2, 2), 3));

            Assert.AreEqual(11, exploration.ExploredCount, "la stanza di 9 celle e due del corridoio");
            Assert.IsTrue(exploration.IsExplored(1, 1));
            Assert.IsTrue(exploration.IsExplored(2, 5), "tre passi giù per il corridoio");
            Assert.IsFalse(exploration.IsExplored(2, 6), "il quarto no");
            Assert.IsFalse(exploration.IsExplored(5, 2), "dietro il muro, a tre celle in linea d'aria");
            Assert.IsFalse(exploration.IsExplored(4, 2), "la roccia non si scopre");
            Assert.IsFalse(exploration.IsExplored(-1, 0));

            Assert.IsFalse(exploration.Reveal(new Vector2Int(2, 2), 3), "niente di nuovo");
            Assert.IsFalse(exploration.Reveal(new Vector2Int(4, 2), 3), "dalla roccia non si scopre niente");
            Assert.IsTrue(exploration.Reveal(new Vector2Int(2, 5), 3));
            Assert.IsTrue(exploration.IsExplored(3, 6));
        }

        [Test, Description("In diagonale si scopre un quadrato, ma non si taglia lo spigolo di un muro")]
        public void Reveal_DiagonalsDoNotCutCorners()
        {
            var exploration = new Exploration(LevelMap.Parse(Sample));

            exploration.Reveal(new Vector2Int(1, 1), 2);
            Assert.IsTrue(exploration.IsExplored(3, 3), "l'angolo opposto della stanza, due passi in diagonale");

            exploration.Reveal(new Vector2Int(2, 5), 1);
            Assert.IsTrue(exploration.IsExplored(2, 6));
            Assert.IsFalse(exploration.IsExplored(1, 6), "dal corridoio alla stanza sotto si passa per la cella davanti, non per lo spigolo");
        }

        [Test, Description("Lo stesso livello ricostruito riprende le celle viste; una mappa di altra misura no")]
        public void CopyExplored_KeepsTheSeenCells()
        {
            var before = new Exploration(LevelMap.Parse(Sample));
            before.Reveal(new Vector2Int(6, 2), 3);
            var after = new Exploration(LevelMap.Parse(Sample));

            Assert.IsTrue(after.CopyExplored(before));
            Assert.AreEqual(before.ExploredCount, after.ExploredCount);
            Assert.IsTrue(after.IsExplored(6, 2));
            Assert.IsFalse(after.IsExplored(2, 2));

            var other = new Exploration(LevelMap.Parse("#####\n#.<.#\n#####\n"));
            Assert.IsFalse(other.CopyExplored(before));
            Assert.AreEqual(0, other.ExploredCount);
        }

        [Test, Description("La cella di un punto del mondo: centri sui multipli di 4 metri, le righe verso -Z")]
        public void CellAt_MatchesTheBuilder()
        {
            Assert.AreEqual(new Vector2Int(3, 5), Exploration.CellAt(LevelMap.CellCenter(3, 5)));
            Assert.AreEqual(new Vector2Int(3, 5), Exploration.CellAt(LevelMap.CellCenter(3, 5) + new Vector3(1.9f, 0f, -1.9f)));
            Assert.AreEqual(new Vector2Int(4, 5), Exploration.CellAt(LevelMap.CellCenter(3, 5) + new Vector3(2.1f, 0f, 0f)));
        }

        [Test, Description("L'automappa: muri a linee sui lati verso la roccia, pavimento accennato, casse, ingresso e scala; il non scoperto è trasparente")]
        public void PaintExplored_DrawsOnlyWhatWasSeen()
        {
            var map = LevelMap.Parse(Sample);
            var exploration = new Exploration(map);
            exploration.Reveal(new Vector2Int(2, 2), 3);
            var texture = MapPainter.CreateExploredTexture(map, Size);
            var pixels = new Color32[texture.width * texture.height];
            try
            {
                MapPainter.PaintExplored(exploration, texture, pixels);
                int h = map.Height;

                Assert.AreEqual(MapPainter.ExploredWall, Pixel(texture, h, 1, 1, 5, Size - 1), "lato nord della cella in alto a sinistra");
                Assert.AreEqual(MapPainter.ExploredWall, Pixel(texture, h, 1, 1, 0, 5), "lato ovest");
                Assert.AreEqual(MapPainter.ExploredFloor, Pixel(texture, h, 1, 1, 5, 5), "il mezzo è pavimento");
                Assert.AreEqual(MapPainter.ExploredFloor, Pixel(texture, h, 2, 3, 5, 0), "verso il corridoio non c'è muro");
                Assert.AreEqual(MapPainter.Entrance, Pixel(texture, h, 2, 2, 5, 5));
                Assert.AreEqual(MapPainter.Unknown, Pixel(texture, h, 6, 2, 5, 5), "la cassa dietro il muro non si vede");
                Assert.AreEqual(MapPainter.Unknown, Pixel(texture, h, 3, 6, 5, 5));

                exploration.Reveal(new Vector2Int(2, 5), 3);
                MapPainter.PaintExplored(exploration, texture, pixels);
                Assert.AreEqual(MapPainter.ExploredStairs, Pixel(texture, h, 3, 6, 5, 5), "la scala, piena");

                exploration.Reveal(new Vector2Int(6, 2), 3);
                MapPainter.PaintExplored(exploration, texture, pixels);
                Assert.AreEqual(MapPainter.Chest, Pixel(texture, h, 6, 2, 5, 5));
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
