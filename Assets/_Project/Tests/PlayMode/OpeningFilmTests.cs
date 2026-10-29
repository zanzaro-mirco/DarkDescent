using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Films;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.Rendering;
using DarkDescent.Town;
using DarkDescent.UI;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DarkDescent.Tests
{
    /// <summary>L'apertura (D7 e D14 della M19): parte con la partita nuova del menu e alla fine ridà il borgo com'era.</summary>
    public class OpeningFilmTests : SandboxFixture
    {
        private string _folder;

        private static CompositionRoot Root => Object.FindFirstObjectByType<CompositionRoot>();

        private static GameObject Hero => GameObject.Find("Player");

        private static LevelContext Level => Object.FindFirstObjectByType<LevelManager>().CurrentLevel;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentOpening_" + Guid.NewGuid().ToString("N"));
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

        // una partita nuova dal menu, con i filmati accesi o no
        private IEnumerator NewGame(bool films)
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            Environment.SetEnvironmentVariable(CompositionRoot.FilmsVariable, films ? "1" : null);
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            var menu = Object.FindFirstObjectByType<MainMenu>();
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
            yield return null;
        }

        [UnityTest, Description("La partita nuova dal menu fa partire l'apertura nel borgo di notte, senza il cavaliere e senza comandi; Esc più Esc la salta subito")]
        public IEnumerator Opening_StartsWithANewGame_AndSkips()
        {
            yield return NewGame(films: true);
            var films = Root.Films;
            Assert.IsTrue(films.IsPlaying, "l'apertura parte");
            Assert.AreEqual(FilmLibrary.OpeningId, films.Current.Id);
            Assert.IsTrue(Level.IsTown);
            Assert.IsFalse(Hero.GetComponent<PlayerInputReader>().enabled, "i comandi spenti");
            Assert.IsTrue(Hero.GetComponentsInChildren<SkinnedMeshRenderer>().All(r => !r.enabled), "il cavaliere vero nascosto: si vede la sua copia");
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(Level.Daylight.IsHeld);
            Assert.AreEqual(DayMoment.Night, Level.Daylight.Phase.From, "comincia di notte");
            Assert.Greater(Object.FindObjectsByType<FilmActor>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, 5, "le comparse, anche quelle che entrano dopo");

            float pressed = Time.unscaledTime;
            films.Press();
            films.Press();
            Assert.IsFalse(films.IsPlaying);
            Assert.Less(Time.unscaledTime - pressed, 1f);
            yield return null;
            AssertVillageBack();
        }

        [UnityTest, Description("L'apertura arriva in fondo da sola: il borgo torna com'era e la camera del gioco riprende il cavaliere")]
        public IEnumerator Opening_PlaysToTheEnd_AndGivesTheVillageBack()
        {
            yield return NewGame(films: true);
            var films = Root.Films;
            var director = Root.CurrentFilm.Director;
            director.time = FilmLibrary.Opening.Duration - 0.4f;
            for (float t = 0f; films.IsPlaying; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 5f, "l'apertura non finisce");
                yield return null;
            }

            yield return null;
            AssertVillageBack();

            // la camera del filmato è sparita: dopo la sfumatura c'è solo quella del gioco
            yield return new WaitForSeconds(2.5f);
            var brain = Camera.main.GetComponent<CinemachineBrain>();
            Assert.IsFalse(brain.IsBlending);
            Assert.IsNotNull(((CinemachineCamera)brain.ActiveVirtualCamera).Follow.GetComponentInParent<PlayerInputReader>(), "la camera segue il cavaliere");
        }

        [UnityTest, Description("Senza la richiesta dei filmati, come nei test e con -newgame, la partita nuova comincia subito")]
        public IEnumerator NoOpening_WhenFilmsAreOff()
        {
            yield return NewGame(films: false);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(Root.Films.IsPlaying);
            Assert.IsNull(Root.CurrentFilm);
            Assert.IsTrue(Hero.GetComponent<PlayerInputReader>().enabled);
        }

        // il borgo del gioco, di giorno, con tutti al loro posto e il cavaliere libero
        private static void AssertVillageBack()
        {
            Assert.IsNull(Root.CurrentFilm, "il set smontato");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(Hero.GetComponent<PlayerInputReader>().enabled, "i comandi riaccesi");
            Assert.IsTrue(Hero.GetComponentsInChildren<SkinnedMeshRenderer>().All(r => r.enabled), "il cavaliere vero si vede");
            Assert.IsFalse(Level.Daylight.IsHeld, "l'ora torna all'orologio");
            Assert.IsEmpty(Object.FindObjectsByType<FilmActor>(FindObjectsInactive.Include, FindObjectsSortMode.None), "le comparse sparite");
            Assert.IsTrue(Object.FindObjectsByType<Villager>(FindObjectsInactive.Include, FindObjectsSortMode.None).All(v => v.gameObject.activeSelf), "gli abitanti al loro posto");
            Assert.IsTrue(Object.FindFirstObjectByType<Hearth>().IsBurning, "il fuoco acceso");
            Assert.IsFalse(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t => t.name.StartsWith("GraveDug")), "le tombe richiuse");
        }
    }
}
