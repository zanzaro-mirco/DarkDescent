using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Films;
using DarkDescent.Player;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace DarkDescent.Tests
{
    /// <summary>Il lettore dei filmati: bande, didascalie, salto, e il gioco che torna com'era (D9 e D14 della M19).</summary>
    public class FilmPlayerTests : SandboxFixture
    {
        private TimelineAsset _timeline;
        private GameObject _set;
        private string _folder;

        private static FilmPlayer Films => Object.FindFirstObjectByType<CompositionRoot>().Films;

        private static PlayerInputReader Reader => GameObject.Find("Player").GetComponent<PlayerInputReader>();

        private static FilmScript Script => new FilmScript("Prova", "film.prova", 0f, 10f, new[]
        {
            new FilmShot(0f, 1.2f, "A", FilmPlayer.SkipKey),
            new FilmShot(1.2f, 1.3f, "B"),
        });

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentFilms_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            if (_set != null)
            {
                Object.Destroy(_set);
            }

            if (_timeline != null)
            {
                Object.Destroy(_timeline);
            }

            yield return null;
            Directory.Delete(_folder, true);
        }

        // una partita nuova nel borgo e una Timeline vuota di 2,5 s, come quella di un filmato
        private IEnumerator Prepare()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            var menu = Object.FindFirstObjectByType<MainMenu>();
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();

            _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            _timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            _timeline.fixedDuration = 2.5d;
            _set = new GameObject("FilmSet");
            var director = _set.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.playableAsset = _timeline;
        }

        private PlayableDirector Director => _set.GetComponent<PlayableDirector>();

        [UnityTest, Description("Un filmato spegne i comandi e le finestre, mette le bande a 2,35:1 e la didascalia; alla fine riaccende tutto, con il tempo a 1")]
        public IEnumerator Film_PlaysAndGivesTheGameBack()
        {
            yield return Prepare();
            var hud = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.enabled && c.renderMode != RenderMode.WorldSpace).ToArray();
            Assert.IsNotEmpty(hud);
            int finished = 0;
            Films.Finished += _ => finished++;

            Time.timeScale = 0.5f;
            Assert.IsTrue(Films.Play(Script, Director));
            Assert.IsTrue(Films.IsPlaying);
            Assert.IsFalse(Reader.enabled, "i comandi del gioco sono spenti");
            Assert.IsTrue(hud.All(c => !c.enabled), "le finestre del gioco sono spente");
            Assert.AreEqual(1f, Time.timeScale);
            float expected = (1f - (float)Screen.width / Screen.height / 2.35f) * 0.5f;
            Assert.AreEqual(expected, Films.BandHeight, 1e-3f);
            Assert.IsFalse(Films.Play(Script, Director), "un filmato alla volta");

            for (float t = 0f; Films.CaptionText != "Esc or click to skip"; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 1.2f, "la didascalia della prima inquadratura non arriva");
                yield return null;
            }

            for (float t = 0f; Films.IsPlaying; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 4f, "il filmato non finisce");
                yield return null;
            }

            Assert.AreEqual(1, finished);
            Assert.IsTrue(Reader.enabled, "i comandi tornano");
            Assert.IsTrue(hud.All(c => c.enabled), "le finestre tornano");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreNotEqual(PlayState.Playing, Director.state);
        }

        [UnityTest, Description("Il primo Esc o click mostra l'invito a saltare, il secondo salta subito e il gioco riprende")]
        public IEnumerator SecondPress_SkipsAtOnce()
        {
            yield return Prepare();
            FilmScript ended = null;
            Films.Finished += script => ended = script;
            var script = Script;
            Films.Play(script, Director);
            yield return null;

            Films.Press();
            Assert.IsTrue(Films.IsPromptShowing);
            Assert.IsTrue(Films.IsPlaying);
            yield return null;
            Films.Press();

            Assert.IsFalse(Films.IsPlaying, "saltato nello stesso frame");
            Assert.AreSame(script, ended);
            Assert.IsTrue(Reader.enabled);
            Assert.AreNotEqual(PlayState.Playing, Director.state);
        }
    }
}
