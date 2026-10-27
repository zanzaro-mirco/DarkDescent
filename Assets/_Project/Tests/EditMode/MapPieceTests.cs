using System.Linq;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>I pezzi della mappa del borgo (M18, 18.2): case e paesaggio che occupano più celle e fanno da muro.</summary>
    public class MapPieceTests
    {
        [Test, Description("I simboli di @solid e le celle coperte non sono pavimento; senza la direttiva lo stesso simbolo è un marcatore sul pavimento")]
        public void Solid_IsNotFloor()
        {
            var map = LevelMap.Parse("@solid H\n.H%.");
            Assert.IsTrue(map.IsFloor(0, 0));
            Assert.IsFalse(map.IsFloor(1, 0), "la casa");
            Assert.IsFalse(map.IsFloor(2, 0), "la cella coperta");
            Assert.IsTrue(map.IsSolid('H'));
            Assert.IsTrue(map.IsCovered(2, 0));
            CollectionAssert.AreEqual(new[] { ('H', 1, 0) }, map.Markers.Select(m => (m.Symbol, m.X, m.Y)).ToArray(), "la cella coperta non è un marcatore");

            Assert.IsTrue(LevelMap.Parse(".H.").IsFloor(1, 0), "senza @solid, H è un marcatore come gli altri");
        }

        [Test, Description("Contro un pezzo o una cella coperta non va un muro: solo contro la roccia")]
        public void BoundaryEdges_SkipPieces()
        {
            var edges = LevelMap.Parse("@solid H\n.H").BoundaryEdges().ToArray();
            CollectionAssert.DoesNotContain(edges, (0, 0, MapDirection.East), "la casa fa da muro da sola");
            Assert.AreEqual(3, edges.Length, "nord, sud e ovest, contro la roccia");
            Assert.AreEqual(3, LevelMap.Parse(".%").BoundaryEdges().Count());
        }

        [Test, Description("Il pezzo prende le celle coperte attaccate a lui e sta nel loro centro")]
        public void Footprint_CollectsCoveredCells()
        {
            var map = LevelMap.Parse("@solid H\n%%.\n%H.\n...");
            var anchor = map.Markers.Single();
            var cells = MapPiece.Footprint(map, anchor);
            CollectionAssert.AreEqual(new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }, cells);
            Assert.AreEqual(new Vector3(2f, 0f, -2f), MapPiece.Center(cells));
        }

        [Test, Description("Due pezzi divisi da un pavimento non si prendono le celle a vicenda")]
        public void Footprint_StopsAtOtherPieces()
        {
            var map = LevelMap.Parse("@solid HJ\nH%.J%");
            Assert.AreEqual(2, MapPiece.Footprint(map, map.Markers[0]).Count);
            Assert.AreEqual(2, MapPiece.Footprint(map, map.Markers[1]).Count);
        }

        [Test, Description("La porta sta sul lato del simbolo: in una casa 2×2 il simbolo in alto a destra guarda a est, a pari merito con nord")]
        public void Facing_FollowsTheSymbolSide()
        {
            Assert.AreEqual(MapDirection.North, Facing("@solid H\n...\n.H.\n.%.", 'H'), "davanti il simbolo, dietro la cella coperta");
            Assert.AreEqual(MapDirection.East, Facing("@solid H\n....\n.%H.\n.%%.\n....", 'H'));
            Assert.AreEqual(MapDirection.West, Facing("@solid H\n....\n.H%.\n.%%.\n....", 'H'), "in alto a sinistra: ovest prima di nord");
            Assert.AreEqual(MapDirection.South, Facing("@solid H\n...\n.H.\n...", 'H'), "una cella sola: prima sud");
        }

        [Test, Description("Un pezzo che non tocca il pavimento è paesaggio: non ha una porta")]
        public void Facing_NoneForScenery()
        {
            var map = LevelMap.Parse("@solid Y\n#Y#\n###\n...");
            Assert.IsFalse(MapPiece.TryFacing(map, map.Markers[0], MapPiece.Footprint(map, map.Markers[0]), out _));
        }

        private static MapDirection Facing(string text, char symbol)
        {
            var map = LevelMap.Parse(text);
            var anchor = map.Markers.Single(m => m.Symbol == symbol);
            Assert.IsTrue(MapPiece.TryFacing(map, anchor, MapPiece.Footprint(map, anchor), out var side));
            return side;
        }
    }
}
