using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkDescent.Audio;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Films;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.Rendering;
using DarkDescent.Town;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DarkDescent.Tests
{
    /// <summary>La vittoria in tre pezzi (D8 e D14 della M19): il re che cade, il portale, il ritorno nel borgo e la finestra.</summary>
    public class VictoryFilmTests : SandboxFixture
    {
        private string _folder;

        private static CompositionRoot Root => Object.FindFirstObjectByType<CompositionRoot>();

        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static GameObject Hero => GameObject.Find("Player");

        private static VictoryScreen Victory => Object.FindFirstObjectByType<VictoryScreen>();

        private static AmbiencePlayer Ambience => Object.FindFirstObjectByType<AmbiencePlayer>();

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentVictoryFilm_" + Guid.NewGuid().ToString("N"));
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

        // una partita nuova dal menu con i filmati accesi, l'apertura saltata, nella sala del re
        private IEnumerator ToTheKing()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            Environment.SetEnvironmentVariable(CompositionRoot.FilmsVariable, "1");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            var menu = Object.FindFirstObjectByType<MainMenu>();
            MenuSteps.StartNew(menu, 1);
            yield return WaitForLevel();
            yield return null;
            Root.Films.Press();
            Root.Films.Press();
            yield return null;
            Assert.IsFalse(Root.Films.IsPlaying, "l'apertura saltata");

            Manager.LoadLevel("Level_Caves", "FromAbove", 8);
            yield return WaitUntilIn(level => level.Depth == 8, "la profondità 8 non arriva");
        }

        private static IEnumerator WaitUntilIn(Func<LevelContext, bool> arrived, string what)
        {
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || !arrived(Manager.CurrentLevel); t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, what);
                yield return null;
            }

            yield return null;
        }

        private static IEnumerator WaitForFilm(string id)
        {
            for (float t = 0f; !Root.Films.IsPlaying || Root.Films.Current.Id != id; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, $"il filmato {id} non parte");
                yield return null;
            }
        }

        private static void KillTheKing()
        {
            var boss = Manager.CurrentLevel.Boss;
            boss.Health.TakeDamage(new DamageInfo(boss.Health.Max, DamageType.Physical, null));
        }

        // il cavaliere entra nel portale della vittoria, da due metri
        private static void EnterThePortal()
        {
            var portal = Object.FindFirstObjectByType<TownPortals>().VictoryPortal;
            Assert.IsNotNull(portal, "il portale della vittoria");
            Assert.IsTrue(NavMesh.SamplePosition(portal.transform.position + Vector3.back * 2f, out var hit, 2f, NavMesh.AllAreas));
            Hero.GetComponent<NavMeshAgent>().Warp(hit.position);
            Hero.GetComponent<PlayerController>().Use(portal.Interactable);
        }

        [UnityTest, Timeout(120000), Description("La prima vittoria: il re che cade al rallentatore e resta a terra; il portale fa vedere il cavaliere davanti alle ossa e poi parte; nel borgo il ritorno all'alba arriva in fondo da solo, il cavaliere è accanto al fuoco, e si apre la finestra")]
        public IEnumerator FirstVictory_PlaysTheThreePieces_ThenTheWindow()
        {
            yield return ToTheKing();
            KillTheKing();
            yield return WaitForFilm(FilmLibrary.VictoryFallId);
            Assert.IsFalse(Hero.GetComponent<PlayerInputReader>().enabled, "i comandi spenti");
            yield return null;
            yield return null;
            Assert.Less(Time.timeScale, 0.5f, "il colpo rallenta");

            // il primo tempo arriva in fondo da solo: il tempo torna a 1 e il re resta a terra
            Root.CurrentFilm.Director.time = FilmLibrary.VictoryFall.Duration - 0.2f;
            for (float t = 0f; Root.Films.IsPlaying; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 5f, "il primo tempo non finisce");
                yield return null;
            }

            yield return null;
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsNull(Root.CurrentFilm, "il set smontato");
            Assert.IsTrue(Hero.GetComponent<PlayerInputReader>().enabled, "si torna a giocare");
            var remains = Object.FindObjectsByType<FilmActor>(FindObjectsSortMode.None).SingleOrDefault(a => a.name == VictoryFallSet.RemainsName);
            Assert.IsNotNull(remains, "le ossa del re restano nella sala");
            Assert.AreEqual(Manager.CurrentLevel.gameObject.scene, remains.gameObject.scene, "e se ne vanno con il livello");

            // il portale aspetta il secondo tempo, poi il viaggio
            EnterThePortal();
            yield return WaitForFilm(FilmLibrary.VictoryLeaveId);
            Assert.IsTrue(Manager.CurrentLevel.Depth == 8 && !Manager.IsTransitioning, "il viaggio aspetta il filmato");
            Assert.IsTrue(Hero.GetComponentsInChildren<SkinnedMeshRenderer>().All(r => !r.enabled), "il cavaliere vero nascosto: si vede la sua copia");
            Root.CurrentFilm.Director.time = FilmLibrary.VictoryLeave.Duration - 0.2f;
            yield return WaitUntilIn(level => level.IsTown, "il portale non porta nel borgo");

            yield return WaitForFilm(FilmLibrary.VictoryReturnId);
            Assert.IsTrue(FilmsPanel.VictorySeen, "vista una volta, la vittoria si rivede dal menu");
            Assert.IsFalse(Victory.IsOpen, "la finestra aspetta il filmato");
            Assert.IsTrue(Manager.CurrentLevel.Daylight.IsHeld, "il borgo fermo all'alba");
            Root.CurrentFilm.Director.time = FilmLibrary.VictoryReturn.Duration - 0.3f;
            for (float t = 0f; !Victory.IsOpen; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 5f, "la finestra non si apre");
                yield return null;
            }

            Assert.IsNull(Root.CurrentFilm, "il set smontato");
            Assert.AreEqual(0f, Time.timeScale, "il gioco fermo sulle statistiche");
            Assert.IsFalse(Manager.CurrentLevel.Daylight.IsHeld, "l'ora torna all'orologio");
            Assert.IsTrue(Hero.GetComponentsInChildren<SkinnedMeshRenderer>().All(r => r.enabled), "il cavaliere vero si vede");
            // le comparse si distruggono a fine frame: un fotogramma e sono sparite (senza, il test falliva una volta ogni tanto)
            yield return null;
            Assert.IsEmpty(Object.FindObjectsByType<FilmActor>(FindObjectsInactive.Include, FindObjectsSortMode.None), "le comparse sparite");
            Assert.IsTrue(Object.FindObjectsByType<Villager>(FindObjectsInactive.Include, FindObjectsSortMode.None).All(v => v.gameObject.activeSelf), "gli abitanti al loro posto");
            var fire = Object.FindFirstObjectByType<Hearth>().transform.position;
            Assert.Less(Vector3.Distance(Hero.transform.position, fire), 6f, "il cavaliere vero dove si è fermata la copia, accanto al fuoco");
            Assert.IsTrue(Hero.GetComponent<NavMeshAgent>().isOnNavMesh);

            Victory.ContinueButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(Hero.GetComponent<PlayerInputReader>().enabled, "si gioca nel borgo");
        }

        [UnityTest, Timeout(120000), Description("Il re cade nel silenzio; la musica della vittoria parte al portale, va avanti nel viaggio e nel borgo, dove il borgo esulta, e finisce da sola sotto la finestra")]
        public IEnumerator VictoryMusic_RunsFromThePortalIntoTheVillage()
        {
            // l'apertura si salta appena parte e il re cade mezzo secondo dopo, mentre il suo brano sfuma
            // ancora: il primo tempo non ha un brano suo, e quello che suona finisce di sfumare
            yield return ToTheKing();
            KillTheKing();
            yield return WaitForFilm(FilmLibrary.VictoryFallId);
            Assert.IsTrue(Ambience.Hushed, "la musica della sala tace");
            yield return new WaitForSecondsRealtime(0.7f);
            var heard = Root.Films.Music;
            Assert.IsTrue(Root.Films.IsPlaying, "il re cade ancora");
            Assert.IsFalse(Root.Films.IsMusicPlaying, heard == null ? "il re cade nel silenzio"
                : $"il re cade nel silenzio: suona {heard.clip?.name} al volume {heard.volume:0.00}, al secondo {heard.time:0.0}");
            Root.CurrentFilm.Director.time = FilmLibrary.VictoryFall.Duration - 0.2f;
            for (float t = 0f; Root.Films.IsPlaying; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 5f, "il primo tempo non finisce");
                yield return null;
            }

            EnterThePortal();
            yield return WaitForFilm(FilmLibrary.VictoryLeaveId);
            yield return null;
            var music = Root.Films.Music;
            Assert.IsTrue(Root.Films.IsMusicPlaying, "la musica parte al portale");
            Assert.AreEqual("film_victory", music.clip.name);

            // in CI non c'è la scheda audio e l'orologio del brano corre molto più del tempo vero: rallentato,
            // non finisce dentro il test; che continui lo dice il volume, perché un brano che riparte da metà
            // entra da zero
            music.pitch = 0.02f;
            float full = music.volume;
            Root.CurrentFilm.Director.time = FilmLibrary.VictoryLeave.Duration - 0.2f;
            yield return WaitUntilIn(level => level.IsTown, "il portale non porta nel borgo");
            Assert.IsTrue(Root.Films.IsMusicPlaying, "la musica va avanti nel viaggio");
            float travelled = music.time;

            yield return WaitForFilm(FilmLibrary.VictoryReturnId);
            Assert.IsTrue(music.isPlaying);
            Assert.GreaterOrEqual(music.time, travelled, "continua, non torna indietro");
            Assert.AreEqual(full, music.volume, 1e-3f, "continua, non riparte con un'entrata");

            var cued = new List<string>();
            Root.CurrentFilm.Cued += clip => cued.Add(clip.name);
            Root.CurrentFilm.Director.time = 9d;
            for (float t = 0f; Root.Films.IsPlaying && Root.CurrentFilm.Director.time < 12.5d; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 6f, "il ritorno non va avanti");
                yield return null;
            }

            Assert.Contains("crowd_cheer_sa", cued, "il borgo esulta quando si alzano le braccia");
            Root.CurrentFilm.Director.time = FilmLibrary.VictoryReturn.Duration - 0.3f;
            for (float t = 0f; !Victory.IsOpen; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 5f, "la finestra non si apre");
                yield return null;
            }

            Assert.IsTrue(Root.Films.IsMusicPlaying, "finisce da sola, sotto la finestra");
            Assert.AreEqual(full, music.volume, 1e-3f, "senza sfumare");
            Assert.IsTrue(Ambience.Hushed, "e la musica del borgo aspetta");
        }

        [UnityTest, Timeout(120000), Description("Saltati, i tre pezzi lasciano il gioco com'era: dopo il primo si gioca, dopo il secondo si parte, dopo il terzo la finestra")]
        public IEnumerator SkippedVictory_StillReachesTheWindow()
        {
            yield return ToTheKing();
            KillTheKing();
            yield return WaitForFilm(FilmLibrary.VictoryFallId);
            yield return null;
            Root.Films.Press();
            Root.Films.Press();
            yield return null;
            Assert.AreEqual(1f, Time.timeScale, "saltato il rallentatore, il tempo è normale");
            Assert.IsTrue(Hero.GetComponent<PlayerInputReader>().enabled);

            EnterThePortal();
            yield return WaitForFilm(FilmLibrary.VictoryLeaveId);
            Root.Films.Press();
            Root.Films.Press();
            yield return WaitUntilIn(level => level.IsTown, "saltato, il portale non parte");

            yield return WaitForFilm(FilmLibrary.VictoryReturnId);
            Root.Films.Press();
            Root.Films.Press();
            yield return null;
            Assert.IsTrue(Victory.IsOpen, "saltato il ritorno, la finestra");
            Assert.IsNull(Root.CurrentFilm);
        }

        [UnityTest, Timeout(120000), Description("Il re battuto di nuovo a una difficoltà già vinta: nessun filmato, il portale porta subito nel borgo e non c'è la finestra")]
        public IEnumerator SecondVictory_PlaysNoFilm()
        {
            yield return ToTheKing();
            Root.Difficulty.Restore(0, 0, 0);
            KillTheKing();
            yield return null;
            yield return null;
            Assert.IsFalse(Root.Films.IsPlaying, "nessun primo tempo");
            Assert.AreEqual(1f, Time.timeScale);

            EnterThePortal();
            yield return WaitUntilIn(level => level.IsTown, "il portale non porta nel borgo");
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(Root.Films.IsPlaying, "nessun ritorno");
            Assert.IsFalse(Victory.IsOpen, "nessuna finestra");
            Assert.IsTrue(Hero.GetComponent<PlayerInputReader>().enabled);
        }
    }
}
