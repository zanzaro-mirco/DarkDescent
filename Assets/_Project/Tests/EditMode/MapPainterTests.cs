using DarkDescent.Editor;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class MapPainterTests
    {
        private const string Sample = "@depth 2\n@entrance Start\n#####\n#<.S#\n#.c>#\n#####\n";

        [Test, Description("Una mappa scritta come testo e riletta è la stessa: celle, marcatori, direttive")]
        public void ToText_RoundTrips()
        {
            var map = LevelMap.Parse(Sample);
            var again = LevelMap.Parse(map.ToText("prova"));

            Assert.AreEqual(map.ToText(), again.ToText());
            Assert.AreEqual("2", again.GetDirective("depth")[0]);
            Assert.AreEqual(map.Markers.Count, again.Markers.Count);
            StringAssert.StartsWith("// prova\n@depth 2\n@entrance Start\n#####", map.ToText("prova"));
        }

        [Test, Description("Un pixel per cella, il nord in alto, un colore per simbolo; le celle non scoperte restano trasparenti")]
        public void Paint_ColorsEachCell()
        {
            var map = LevelMap.Parse(Sample);
            var texture = MapPainter.CreateTexture(map);
            try
            {
                MapPainter.Paint(map, texture);

                // la riga 1 della mappa (ingresso) è la penultima dall'alto: y = altezza - 1 - 1
                Assert.AreEqual(MapPainter.Entrance, (Color32)texture.GetPixel(1, 2));
                Assert.AreEqual(MapPainter.Enemy, (Color32)texture.GetPixel(3, 2));
                Assert.AreEqual(MapPainter.Chest, (Color32)texture.GetPixel(2, 1));
                Assert.AreEqual(MapPainter.Stairs, (Color32)texture.GetPixel(3, 1));
                Assert.AreEqual(MapPainter.Rock, (Color32)texture.GetPixel(0, 0));

                MapPainter.Paint(map, texture, (x, y) => x < 2);
                Assert.AreEqual(MapPainter.Entrance, (Color32)texture.GetPixel(1, 2));
                Assert.AreEqual(MapPainter.Unknown, (Color32)texture.GetPixel(3, 2), "non scoperta");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test, Description("La finestra del generatore disegna proprio il livello che il gioco costruisce con quel seme e quella profondità")]
        public void GeneratorWindow_ShowsTheGameLevel()
        {
            var window = DungeonGeneratorWindow.Open();
            try
            {
                window.Generate(4711, 2);
                var settings = AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");
                var expected = DungeonLevel.CreateMap(settings, 4711, 2, "Level_Crypt", out _);

                Assert.AreEqual(expected.ToText(), window.Map.ToText());
                Assert.AreEqual(expected.Width, window.Preview.width);
                StringAssert.Contains("7 scheletri", window.Stats);
                StringAssert.Contains("scala a", window.Stats);

                window.Generate(4711, settings.LastDepth);
                StringAssert.Contains("nessuna scala", window.Stats);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
