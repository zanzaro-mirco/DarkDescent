using System;
using System.Collections;
using System.IO;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.Save;
using DarkDescent.Town;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>La vittoria: il portale, le statistiche e i riconoscimenti (D9 della M10).</summary>
    public class VictoryPlayModeTests : SandboxFixture
    {
        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentVictory_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static MainMenu Menu => UnityEngine.Object.FindFirstObjectByType<MainMenu>();

        private static VictoryScreen Victory => UnityEngine.Object.FindFirstObjectByType<VictoryScreen>();

        private static IEnumerator WaitUntilIn(Func<LevelContext, bool> arrived, string what)
        {
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || !arrived(Manager.CurrentLevel); t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, what);
                yield return null;
            }

            yield return null;
        }

        [UnityTest, Description("Morto il re si apre il portale della vittoria; attraversato si arriva in città, il gioco si ferma sulle statistiche e il salvataggio è in città; i riconoscimenti salgono e dal pulsante si torna al menu, da cui Continua riporta in città")]
        public IEnumerator Victory_FromBossToTheMenu()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            Menu.NewGameButton.onClick.Invoke();
            Menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();

            Manager.LoadLevel("Level_Caves", "FromAbove", 8);
            yield return WaitUntilIn(level => level.Depth == 8, "la profondità 8 non arriva");
            var boss = Manager.CurrentLevel.Boss;
            boss.Health.TakeDamage(new DamageInfo(boss.Health.Max, DamageType.Physical, null));
            yield return null;

            var portal = UnityEngine.Object.FindFirstObjectByType<TownPortals>().VictoryPortal;
            Assert.IsNotNull(portal, "morto il re si apre il portale della vittoria");
            Assert.AreEqual("Leave the depths", portal.Interactable.GetLabel(Localizer));
            var player = GameObject.Find("Player");
            Assert.IsTrue(NavMesh.SamplePosition(portal.transform.position + Vector3.back * 2f, out var hit, 2f, NavMesh.AllAreas));
            player.GetComponent<NavMeshAgent>().Warp(hit.position);
            player.GetComponent<PlayerController>().Use(portal.Interactable);
            yield return WaitUntilIn(level => level.IsTown, "il portale della vittoria non porta in città");

            Assert.IsTrue(Victory.IsOpen, "la schermata di vittoria");
            Assert.AreEqual(0f, Time.timeScale, "il gioco è fermo");
            StringAssert.Contains("Enemies killed: 1", Victory.StatsText);
            StringAssert.Contains("Level: ", Victory.StatsText);
            StringAssert.Contains("Play time: ", Victory.StatsText);
            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(Path.Combine(_folder, "save_1.json"), out SaveData data));
            Assert.IsTrue(data.InTown, "il salvataggio è in città");
            Assert.IsTrue(data.BossDefeated);

            // dalla M17 i riconoscimenti stanno nel menu: qui Continua o Menu principale
            Assert.IsTrue(Victory.ContinueButton.gameObject.activeInHierarchy);
            Victory.MenuButton.onClick.Invoke();
            yield return null;
            yield return null;
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsNotNull(Menu, "di nuovo il menu iniziale");
            Menu.ContinueButton.onClick.Invoke();
            yield return WaitUntilIn(level => level.IsTown, "Continua non riporta in città");
            Assert.IsFalse(Victory.IsOpen);
        }
    }
}
