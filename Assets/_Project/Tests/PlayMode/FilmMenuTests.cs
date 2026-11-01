using System;
using System.Collections;
using System.IO;
using DarkDescent.Core;
using DarkDescent.Films;
using DarkDescent.Levels;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DarkDescent.Tests
{
    /// <summary>Il pulsante Filmati del menu iniziale (D18 della M19): l'elenco, e i filmati rivisti che tornano al menu senza salvare.</summary>
    public class FilmMenuTests : SandboxFixture
    {
        private string _folder;

        private static CompositionRoot Root => Object.FindFirstObjectByType<CompositionRoot>();

        private static MainMenu Menu => Object.FindFirstObjectByType<MainMenu>();

        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentFilmMenu_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        // il menu iniziale, con i filmati spenti come in ogni test: rivederli li accende da sé
        private static IEnumerator OpenMenu()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            Assert.IsTrue(Menu.IsShown);
        }

        private static IEnumerator WaitForFilm(string id)
        {
            for (float t = 0f; !Root.Films.IsPlaying || Root.Films.Current.Id != id; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 20f, $"il filmato {id} non parte");
                yield return null;
            }
        }

        // il filmato che gira arriva in fondo da solo
        private static IEnumerator ToTheEnd(FilmScript script)
        {
            Root.CurrentFilm.Director.time = script.Duration - 0.2f;
            for (float t = 0f; Root.Films.IsPlaying && Root.Films.Current.Id == script.Id; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 5f, $"il filmato {script.Id} non finisce");
                yield return null;
            }
        }

        // la scena ricaricata: un composition root nuovo, con il menu
        private static IEnumerator WaitForMenuAgain(CompositionRoot before)
        {
            for (float t = 0f; Root == null || ReferenceEquals(Root, before) || !Menu.IsShown; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 10f, "non si torna al menu");
                yield return null;
            }
        }

        [UnityTest, Description("Filmati sta sotto Riconoscimenti e apre l'elenco: L'ultima candela sempre, La vittoria solo dopo averla vista; Indietro torna al menu")]
        public IEnumerator FilmsButton_ListsTheOpening_AndTheVictoryOnlyOnceSeen()
        {
            ForgetPreferences();
            yield return OpenMenu();
            var menu = Menu;
            Assert.IsTrue(menu.FilmsButton.gameObject.activeInHierarchy);
            Assert.AreEqual(menu.CreditsButton.transform.GetSiblingIndex() + 1, menu.FilmsButton.transform.GetSiblingIndex(), "sotto Riconoscimenti");

            menu.FilmsButton.onClick.Invoke();
            Assert.IsTrue(menu.Films.IsOpen);
            Assert.IsFalse(menu.NewGameButton.gameObject.activeInHierarchy, "l'elenco al posto del menu");
            Assert.IsTrue(menu.Films.OpeningButton.gameObject.activeInHierarchy);
            Assert.IsFalse(menu.Films.VictoryButton.gameObject.activeInHierarchy, "la vittoria non si rovina prima di averla vista");

            menu.Films.BackButton.onClick.Invoke();
            Assert.IsFalse(menu.Films.IsOpen);
            Assert.IsTrue(menu.NewGameButton.gameObject.activeInHierarchy);

            FilmsPanel.MarkVictorySeen();
            menu.FilmsButton.onClick.Invoke();
            Assert.IsTrue(menu.Films.VictoryButton.gameObject.activeInHierarchy, "vista una volta, si rivede");
            yield return null;
        }

        [UnityTest, Timeout(120000), Description("L'ultima candela rivista dal menu parte nel borgo, anche con i filmati spenti; saltata, si torna al menu e non c'è nessun salvataggio")]
        public IEnumerator RewatchedOpening_ReturnsToTheMenu_WithoutSaving()
        {
            ForgetPreferences();
            yield return OpenMenu();
            Menu.FilmsButton.onClick.Invoke();
            Menu.Films.OpeningButton.onClick.Invoke();
            Assert.IsFalse(Menu.IsShown, "il menu si chiude");
            yield return WaitForFilm(FilmLibrary.OpeningId);
            Assert.IsTrue(Manager.CurrentLevel.IsTown);

            var before = Root;
            Root.Films.Press();
            Root.Films.Press();
            yield return WaitForMenuAgain(before);
            Assert.IsEmpty(Directory.GetFiles(_folder), "nessun posto usato");
            Assert.IsFalse(FilmsPanel.VictorySeen);
        }

        [UnityTest, Timeout(180000), Description("La vittoria rivista dal menu: il re cade nella sala del seme fisso, che ha la porta; il cavaliere entra nel portale da solo; il ritorno nel borgo; poi il menu, senza la finestra e senza salvare")]
        public IEnumerator RewatchedVictory_PlaysTheThreePieces_ThenTheMenu()
        {
            ForgetPreferences();
            FilmsPanel.MarkVictorySeen();
            yield return OpenMenu();
            Menu.FilmsButton.onClick.Invoke();
            Menu.Films.VictoryButton.onClick.Invoke();
            yield return WaitForFilm(FilmLibrary.VictoryFallId);
            var level = Manager.CurrentLevel;
            Assert.AreEqual(8, level.Depth);
            Assert.IsNotNull(level.BossGate, "la sala del re del seme della vittoria ha la porta");
            yield return null;
            yield return null;
            Assert.Less(Time.timeScale, 1f, "il colpo rallenta");

            yield return ToTheEnd(FilmLibrary.VictoryFall);
            yield return WaitForFilm(FilmLibrary.VictoryLeaveId);
            yield return ToTheEnd(FilmLibrary.VictoryLeave);
            yield return WaitForFilm(FilmLibrary.VictoryReturnId);
            Assert.IsTrue(Manager.CurrentLevel.IsTown);

            var before = Root;
            yield return ToTheEnd(FilmLibrary.VictoryReturn);
            yield return WaitForMenuAgain(before);
            Assert.IsFalse(Object.FindFirstObjectByType<VictoryScreen>().IsOpen, "nessuna finestra delle statistiche");
            Assert.IsEmpty(Directory.GetFiles(_folder), "nessun posto usato");
        }
    }
}
