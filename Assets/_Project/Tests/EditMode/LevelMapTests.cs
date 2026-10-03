using System;
using System.Linq;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class LevelMapTests
    {
        [Test, Description("Larghezza dalla riga più lunga, altezza dalle righe della griglia; le righe corte sono roccia in coda")]
        public void Parse_ReadsSizeAndPadsShortRows()
        {
            var map = LevelMap.Parse("#####\n#..\n#####\n");

            Assert.AreEqual(5, map.Width);
            Assert.AreEqual(3, map.Height);
            Assert.IsTrue(map.IsFloor(1, 1));
            Assert.IsTrue(map.IsFloor(2, 1));
            Assert.IsFalse(map.IsFloor(3, 1), "la riga corta continua con roccia");
        }

        [Test, Description("Punto e marcatori sono pavimento; cancelletto, spazio e fuori griglia sono roccia")]
        public void IsFloor_DistinguishesFloorAndRock()
        {
            var map = LevelMap.Parse(".S #");

            Assert.IsTrue(map.IsFloor(0, 0));
            Assert.IsTrue(map.IsFloor(1, 0), "sotto un marcatore c'è pavimento");
            Assert.IsFalse(map.IsFloor(2, 0), "lo spazio è roccia");
            Assert.IsFalse(map.IsFloor(3, 0));
            Assert.IsFalse(map.IsFloor(-1, 0), "fuori dalla griglia è roccia");
            Assert.IsFalse(map.IsFloor(0, 1));
        }

        [Test, Description("I marcatori escono con simbolo e posizione, riga per riga")]
        public void Parse_CollectsMarkers()
        {
            var map = LevelMap.Parse("#####\n#<.S#\n#.T>#\n#####");

            var markers = map.Markers.Select(m => (m.Symbol, m.X, m.Y)).ToArray();
            CollectionAssert.AreEqual(new[] { ('<', 1, 1), ('S', 3, 1), ('T', 2, 2), ('>', 3, 2) }, markers);
            Assert.AreEqual('S', map.GetSymbol(3, 1));
        }

        [Test, Description("Direttive con @ e commenti con // non entrano nella griglia")]
        public void Parse_ReadsDirectivesAndSkipsComments()
        {
            var map = LevelMap.Parse("// livello di prova\n@exit Level_02 FromAbove\n@entrance Start\n#.#\r\n");

            Assert.AreEqual(1, map.Height);
            CollectionAssert.AreEqual(new[] { "Level_02", "FromAbove" }, map.GetDirective("exit"));
            CollectionAssert.AreEqual(new[] { "Start" }, map.GetDirective("entrance"));
            Assert.AreEqual(0, map.GetDirective("assente").Count);
        }

        [Test, Description("Una mappa senza celle è un errore")]
        public void Parse_RejectsEmptyMap()
        {
            Assert.Throws<FormatException>(() => LevelMap.Parse("// solo un commento\n\n"));
        }

        [Test, Description("Ogni lato di pavimento che tocca la roccia è un bordo: una cella sola ne ha quattro, due affiancate sei")]
        public void BoundaryEdges_FollowFloorOutline()
        {
            Assert.AreEqual(4, LevelMap.Parse(".").BoundaryEdges().Count());

            var edges = LevelMap.Parse("..").BoundaryEdges().ToArray();
            Assert.AreEqual(6, edges.Length);
            CollectionAssert.DoesNotContain(edges, (0, 0, MapDirection.East), "tra due pavimenti non c'è muro");
            CollectionAssert.Contains(edges, (1, 0, MapDirection.East));
        }

        [Test, Description("Celle da 4 m: le colonne vanno verso +X, le righe verso -Z")]
        public void CellCenter_UsesFourMeterGrid()
        {
            Assert.AreEqual(new Vector3(8f, 0f, -12f), LevelMap.CellCenter(2, 3));
            Assert.AreEqual(Vector3.forward, LevelMap.ToWorld(MapDirection.North));
            Assert.AreEqual(Vector3.left, LevelMap.ToWorld(MapDirection.West));
        }
    }
}
