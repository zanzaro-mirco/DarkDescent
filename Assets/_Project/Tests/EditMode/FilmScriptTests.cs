using System.Linq;
using DarkDescent.Films;
using DarkDescent.Localization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>La lista delle inquadrature di un filmato (D2 e D14 della M19).</summary>
    public class FilmScriptTests
    {
        private static FilmScript Script(float min, float max, params FilmShot[] shots) => new FilmScript("Prova", "film.prova", min, max, shots);

        [Test, Description("Inquadrature in fila, dalla prima a 0 all'ultima: nessun problema, e la durata è dove finisce l'ultima")]
        public void Shots_InARow_HaveNoProblems()
        {
            var script = Script(5f, 20f,
                new FilmShot(0f, 4f, "Wide"),
                new FilmShot(4f, 3.5f, "Close", "film.prova.1"),
                new FilmShot(7.5f, 2.5f, "Wide"));

            CollectionAssert.IsEmpty(script.Problems());
            Assert.AreEqual(10f, script.Duration, 1e-4f);
        }

        [Test, Description("Un buco tra due inquadrature, una sopra l'altra, una vuota o senza camera: ognuna è un problema con il suo numero")]
        public void Gaps_Overlaps_AndEmptyShots_AreProblems()
        {
            var gap = Script(0f, 99f, new FilmShot(0f, 2f, "A"), new FilmShot(3f, 2f, "B"));
            StringAssert.Contains("buco prima dell'inquadratura 2", gap.Problems().Single());

            var overlap = Script(0f, 99f, new FilmShot(0f, 2f, "A"), new FilmShot(1.5f, 2f, "B"));
            StringAssert.Contains("l'inquadratura 2 comincia a 1.5 s", overlap.Problems().Single().Replace(',', '.'));

            var late = Script(0f, 99f, new FilmShot(0.5f, 2f, "A"));
            StringAssert.Contains("buco prima dell'inquadratura 1", late.Problems().Single());

            var empty = Script(0f, 99f, new FilmShot(0f, 0f, "A"), new FilmShot(0f, 1f, null));
            Assert.AreEqual(2, empty.Problems().Count);
            Assert.AreEqual(1, Script(0f, 99f).Problems().Count, "senza inquadrature");
        }

        [Test, Description("La durata deve stare tra il minimo e il massimo del filmato")]
        public void Duration_StaysInsideItsLimits()
        {
            StringAssert.Contains("fuori da", Script(45f, 60f, new FilmShot(0f, 30f, "A")).Problems().Single());
            StringAssert.Contains("fuori da", Script(45f, 60f, new FilmShot(0f, 61f, "A")).Problems().Single());
            CollectionAssert.IsEmpty(Script(45f, 60f, new FilmShot(0f, 45f, "A")).Problems());
        }

        [Test, Description("L'inquadratura e la didascalia di un istante: la fine di una è l'inizio della dopo, e fuori dal filmato niente")]
        public void ShotAt_FindsTheShotAndItsCaption()
        {
            var script = Script(0f, 99f, new FilmShot(0f, 2f, "A", "film.uno"), new FilmShot(2f, 2f, "B"));

            Assert.AreEqual("A", script.ShotAt(0f).Camera);
            Assert.AreEqual("B", script.ShotAt(2f).Camera);
            Assert.AreEqual("film.uno", script.CaptionAt(1.99f));
            Assert.IsNull(script.CaptionAt(3f), "la seconda non ha didascalia");
            Assert.IsNull(script.ShotAt(-0.1f));
            Assert.IsNull(script.ShotAt(4f));
        }

        [Test, Description("Le inquadrature di fila con la stessa camera sono un movimento solo, anche se la didascalia cambia")]
        public void CameraSpan_JoinsShotsWithTheSameCamera()
        {
            var script = Script(0f, 99f, new FilmShot(0f, 2f, "A", "film.uno"), new FilmShot(2f, 3f, "A", "film.due"), new FilmShot(5f, 1f, "B"));

            Assert.IsTrue(script.CameraSpan(3f, out string camera, out float start, out float end));
            Assert.AreEqual(("A", 0f, 5f), (camera, start, end));
            Assert.IsTrue(script.CameraSpan(5.5f, out camera, out start, out end));
            Assert.AreEqual(("B", 5f, 6f), (camera, start, end));
            Assert.IsFalse(script.CameraSpan(6f, out _, out _, out _), "dopo la fine");
        }

        [Test, Description("L'apertura si monta: inquadrature in fila tra 40 e 60 s, camere che il set sa muovere, ogni didascalia e il titolo nelle due lingue")]
        public void Opening_IsReady()
        {
            var opening = FilmLibrary.Opening;
            CollectionAssert.IsEmpty(opening.Problems());
            var cameras = new[]
            {
                FilmLibrary.NightCamera, FilmLibrary.GravesCamera, FilmLibrary.DawnCamera, FilmLibrary.ArrivalCamera,
                FilmLibrary.AltarCamera, FilmLibrary.TitleCamera, FilmLibrary.DescentCamera,
            };
            CollectionAssert.IsSubsetOf(opening.Shots.Select(s => s.Camera).Distinct(), cameras);

            var strings = StringTable.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv").text);
            var keys = opening.Shots.SelectMany(s => new[] { s.CaptionKey, s.TitleKey }).Where(k => k != null).Append(opening.TitleKey).ToArray();
            Assert.Greater(keys.Length, 5);
            foreach (string key in keys)
            {
                foreach (string language in new[] { "en", "it" })
                {
                    Assert.IsTrue(strings.TryGet(key, strings.IndexOf(language), out string text) && !string.IsNullOrWhiteSpace(text), $"{key} in {language}");
                }
            }
        }
    }
}
