using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DarkDescent.Audio;
using DarkDescent.Core;
using DarkDescent.Films;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DarkDescent.Tests
{
    /// <summary>La musica e i suoni dell'apertura (D10 della M19): il brano suo, il borgo che tace, le candele e la terra.</summary>
    public class FilmSoundTests : SandboxFixture
    {
        private string _folder;

        private static CompositionRoot Root => Object.FindFirstObjectByType<CompositionRoot>();

        private static AmbiencePlayer Ambience => Object.FindFirstObjectByType<AmbiencePlayer>();

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentFilmSound_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.FilmsVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        private IEnumerator NewGame()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            Environment.SetEnvironmentVariable(CompositionRoot.FilmsVariable, "1");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            var menu = Object.FindFirstObjectByType<MainMenu>();
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
            yield return null;
        }

        // il regista al secondo from, poi avanti da solo fino al secondo until
        private static IEnumerator RunFilm(float from, float until)
        {
            Root.CurrentFilm.Director.time = from;
            for (float t = 0f; Root.Films.IsPlaying && Root.CurrentFilm.Director.time < until; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, until - from + 3f, "il filmato non va avanti");
                yield return null;
            }
        }

        private static IEnumerator WaitUntil(Func<bool> done, float seconds, string what)
        {
            for (float t = 0f; !done(); t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, seconds, what);
                yield return null;
            }
        }

        [UnityTest, Timeout(120000), Description("L'apertura ha il suo brano nel gruppo della musica e il borgo tace; le candele e la terra suonano quando il filmato ci passa; alla fine il brano sfuma e torna la musica del borgo")]
        public IEnumerator Opening_PlaysItsMusic_AndHushesTheVillage()
        {
            yield return NewGame();
            var films = Root.Films;
            Assert.IsTrue(films.IsPlaying, "l'apertura parte");
            Assert.IsTrue(films.IsMusicPlaying, "con la sua musica");
            Assert.AreEqual("film_opening", films.Music.clip.name);
            Assert.AreEqual("Music", films.Music.outputAudioMixerGroup.name, "il volume della musica delle opzioni");
            Assert.IsTrue(Ambience.Hushed);
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(0f, Ambience.Music.volume, 1e-3f, "la musica del borgo tace");

            var cued = new List<string>();
            Root.CurrentFilm.Cued += clip => cued.Add(clip.name);
            yield return RunFilm(2.2f, 3f);
            Assert.Contains("candle_snuff_sa", cued, "la prima candela si spegne a 2,5 s");
            yield return RunFilm(12.2f, 13f);
            Assert.Contains("earth_break_sa", cued, "il primo scheletro esce a 1,5 s dal cimitero");

            int before = cued.Count;
            Root.CurrentFilm.Director.time = 30d;
            yield return null;
            yield return null;
            Assert.AreEqual(before, cued.Count, "un salto del tempo non fa suonare quello che salta");

            Root.CurrentFilm.Director.time = FilmLibrary.Opening.Duration - 0.2f;
            yield return WaitUntil(() => !films.IsPlaying, 5f, "l'apertura non finisce");
            Assert.IsTrue(films.IsMusicPlaying, "il brano sfuma, non si tronca");
            yield return WaitUntil(() => !films.IsMusicPlaying, 5f, "il brano non sfuma");
            yield return null;
            Assert.IsFalse(Ambience.Hushed);
            yield return new WaitForSecondsRealtime(1f);
            Assert.Greater(Ambience.Music.volume, 0f, "la musica del borgo torna");
        }

        [UnityTest, Timeout(120000), Description("Saltata l'apertura, il suo brano sfuma in meno di un secondo")]
        public IEnumerator SkippedOpening_FadesItsMusicQuickly()
        {
            yield return NewGame();
            var films = Root.Films;
            yield return new WaitForSecondsRealtime(0.5f);
            films.Press();
            films.Press();
            Assert.IsFalse(films.IsPlaying);
            yield return WaitUntil(() => !films.IsMusicPlaying, 1.2f, "il brano non sfuma in fretta");
        }
    }
}
