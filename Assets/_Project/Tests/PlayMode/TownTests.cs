using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Interaction;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.Save;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>La città: si comincia lì e la scala scende alla cripta (D3 della M10).</summary>
    public class TownTests : SandboxFixture
    {
        private const string TownScene = "Town";

        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentTown_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static MainMenu Menu => UnityEngine.Object.FindFirstObjectByType<MainMenu>();

        // dal menu, una partita nuova nel posto 1: come nella build
        private static IEnumerator StartNewGame()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            Menu.NewGameButton.onClick.Invoke();
            Menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
        }

        [UnityTest, Description("Una partita nuova comincia in città: profondità 0, senza nemici né casse, scena attiva per luci e Instantiate, il cavaliere sul NavMesh, il nome senza numero, l'automappa, luce calda, la sua musica")]
        public IEnumerator NewGame_StartsInTown()
        {
            yield return StartNewGame();
            var level = Manager.CurrentLevel;
            Assert.AreEqual(TownScene, level.gameObject.scene.name);
            Assert.AreEqual(TownScene, SceneManager.GetActiveScene().name);
            Assert.AreEqual(0, level.Depth);
            Assert.IsTrue(level.IsTown);
            Assert.AreEqual(0, level.Enemies.Count, "in città non si combatte");
            Assert.AreEqual(0, level.Chests.Count);
            Assert.IsTrue(GameObject.Find("Player").GetComponent<NavMeshAgent>().isOnNavMesh);

            Assert.AreEqual("Camp", UnityEngine.Object.FindFirstObjectByType<LevelTitle>().Text, "la città non ha un numero");
            Assert.IsNotNull(UnityEngine.Object.FindFirstObjectByType<ExplorationTracker>().Exploration, "costruita dalla mappa: l'automappa c'è");

            Assert.AreEqual(1, level.Exits.Count);
            Assert.AreEqual(("Level_Crypt", "Start", 1), (level.Exits[0].TargetScene, level.Exits[0].TargetEntrance, level.Exits[0].TargetDepth));
            Assert.AreEqual("Descend to level 1", level.Exits[0].GetComponent<Interactable>().GetLabel(Localizer));

            // calda e più chiara della cripta, senza ombre oltre a quella del cavaliere
            Assert.Greater(RenderSettings.ambientLight.r, RenderSettings.ambientLight.b);
            Assert.Greater(RenderSettings.ambientLight.r, 0.1f);
            int shadowLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l => l.shadows != LightShadows.None);
            Assert.AreEqual(1, shadowLights);

            // la sua musica e il crepitio del fuoco, senza versi nel buio
            var ambience = UnityEngine.Object.FindFirstObjectByType<DarkDescent.Audio.AmbiencePlayer>();
            Assert.AreEqual("town_loop_the_bards_tale", ambience.Music.clip.name);
            Assert.AreEqual("bed_town_fire", ambience.Bed.clip.name);
            Assert.AreEqual(0, ambience.Current.StingerCount);
        }

        [UnityTest, Description("La scala della città porta alla profondità 1 della cripta, sul suo ingresso iniziale")]
        public IEnumerator Stairs_LeadToTheCrypt()
        {
            yield return StartNewGame();
            var player = GameObject.Find("Player");
            var agent = player.GetComponent<NavMeshAgent>();
            var exit = Manager.CurrentLevel.Exits[0].transform;
            agent.Warp(exit.position + exit.forward * 2.5f);
            agent.SetDestination(exit.position);
            for (float t = 0f; Manager.CurrentSceneName != "Level_Crypt"; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 8f, "la scala non porta alla cripta");
                yield return null;
            }

            yield return WaitForLevel();
            Assert.AreEqual(1, Manager.CurrentLevel.Depth);
            Assert.AreEqual("Start", Manager.CurrentEntrance);
            Assert.AreEqual(5, Manager.CurrentLevel.Enemies.Count, "la stessa cripta di sempre");
        }

        [UnityTest, Description("Salvata in città, la partita dice di essere in città e Continua riporta lì")]
        public IEnumerator SavedInTown_ContinuesInTown()
        {
            yield return StartNewGame();
            string path = Path.Combine(_folder, "save_1.json");
            for (float t = 0f; !File.Exists(path); t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 5f, "entrando in città non ha salvato");
                yield return null;
            }

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(path, out SaveData data));
            Assert.IsTrue(data.InTown);
            Assert.AreEqual((TownScene, 0), (data.Scene, data.Depth));

            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            Menu.LoadButton.onClick.Invoke();
            StringAssert.Contains("Slot 1 – level 1 · Camp", Menu.SlotView(1).Text);
            Menu.ContinueButton.onClick.Invoke();
            yield return WaitForLevel();
            Assert.IsTrue(Manager.CurrentLevel.IsTown);
        }
    }
}
