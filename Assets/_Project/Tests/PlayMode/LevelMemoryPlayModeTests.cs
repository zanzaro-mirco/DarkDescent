using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Save;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>I livelli lasciati restano com'erano, anche dopo Continua (10.9 della M10).</summary>
    public class LevelMemoryPlayModeTests : SandboxFixture
    {
        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentLevels_" + Guid.NewGuid().ToString("N"));
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

        private static GameObject PlayerObject => GameObject.Find("Player");

        private static IEnumerator OpenMenu()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
        }

        private static IEnumerator Travel(string scene, string entrance, int depth)
        {
            Manager.LoadLevel(scene, entrance, depth);
            yield return WaitUntilIn(level => level.Depth == depth && level.gameObject.scene.name == scene, scene + " non arriva");
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

        // gli oggetti a terra del livello, come "x,z:nome" in ordine: dove sono e cosa sono
        private static List<string> Ground()
        {
            var level = Manager.CurrentLevel.gameObject.scene;
            return UnityEngine.Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None)
                .Where(g => g.gameObject.scene == level && g.gameObject.activeInHierarchy && g.Item != null)
                .Select(g => $"{g.transform.position.x:0.0},{g.transform.position.z:0.0}:{(g.Gold > 0 ? "gold " + g.Gold : g.Item.Definition.name)}")
                .OrderBy(s => s)
                .ToList();
        }

        private static bool IsGone(EnemyAI enemy)
        {
            return enemy == null || !enemy.gameObject.activeSelf;
        }

        [UnityTest, Description("Ucciso un nemico, aperta una cassa e raccolto un oggetto, tornando dalla città nemico e oggetto non ci sono, la cassa è aperta e il resto è a terra dov'era; lo stesso dopo Continua")]
        public IEnumerator LeftLevel_StaysAsItWas()
        {
            yield return OpenMenu();
            Menu.NewGameButton.onClick.Invoke();
            Menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
            yield return Travel("Level_Crypt", "Start", 1);

            var level = Manager.CurrentLevel;
            int enemyCount = level.Enemies.Count;
            // la cassa lascia sempre un oggetto e l'oro: a terra resta qualcosa con qualunque seme
            int victim = level.Enemies.ToList().FindIndex(e => e != null && e.TryGetComponent(out LootDrop _));
            Assert.GreaterOrEqual(victim, 0, "un nemico con il suo loot");
            var health = level.Enemies[victim].GetComponent<Health>();
            health.TakeDamage(new DamageInfo(health.Max, DamageType.Physical, null));
            Assert.Greater(level.Chests.Count, 0);
            level.Chests[0].GetComponent<Interactable>().Use(PlayerObject);
            Assert.IsTrue(level.Chests[0].IsOpen);
            yield return new WaitForSeconds(1f);

            // raccolto, non torna
            var taken = UnityEngine.Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None).First(g => g.Gold > 0);
            taken.GetComponent<Interactable>().Use(PlayerObject);
            yield return null;
            var before = Ground();
            Assert.Greater(before.Count, 0, "qualcosa resta a terra");

            yield return Travel("Town", "Start", 0);
            yield return Travel("Level_Crypt", "Start", 1);
            level = Manager.CurrentLevel;
            Assert.AreEqual(enemyCount, level.Enemies.Count, "lo stesso livello dal seme");
            Assert.IsTrue(IsGone(level.Enemies[victim]), "il nemico ucciso non rinasce");
            Assert.AreEqual(enemyCount - 1, level.Enemies.Count(e => !IsGone(e)), "gli altri ci sono");
            Assert.IsTrue(level.Chests[0].IsOpen, "la cassa resta aperta");
            yield return null;
            CollectionAssert.AreEqual(before, Ground(), "gli oggetti a terra dov'erano, senza quello raccolto e senza doppioni");

            // salvato entrando: Continua rimette tutto com'era
            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(Path.Combine(_folder, "save_1.json"), out SaveData data));
            Assert.IsTrue(data.Levels.Any(l => l.Scene == "Level_Crypt" && l.Depth == 1 && l.Killed.Contains(victim)));
            yield return OpenMenu();
            Menu.ContinueButton.onClick.Invoke();
            yield return WaitUntilIn(l => l.Depth == 1, "Continua non riporta nella cripta");
            level = Manager.CurrentLevel;
            Assert.IsTrue(IsGone(level.Enemies[victim]));
            Assert.IsTrue(level.Chests[0].IsOpen);
            yield return null;
            CollectionAssert.AreEqual(before, Ground());
        }
    }
}
