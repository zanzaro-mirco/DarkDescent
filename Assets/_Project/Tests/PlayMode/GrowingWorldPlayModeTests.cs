using System;
using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Progression;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>«Il mondo cresce con te» nel gioco vero (24.5 della M24, D12–D15): l'interruttore all'altare, gli oggetti, i nemici, l'esperienza.</summary>
    public class GrowingWorldPlayModeTests : SandboxFixture
    {
        private const ulong Seed = 11;

        private static CompositionRoot Root => UnityEngine.Object.FindFirstObjectByType<CompositionRoot>();

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static DifficultyPanel Panel => UnityEngine.Object.FindFirstObjectByType<DifficultyPanel>();

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, null);
            yield return null;
        }

        private static IEnumerator Travel(string scene, string entrance, int depth)
        {
            var before = Manager.CurrentLevel;
            Manager.LoadLevel(scene, entrance, depth);
            yield return null;
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel == before; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, scene + " non arriva");
                yield return null;
            }

            yield return null;
            yield return null;
        }

        // in città, al Crepuscolo, con un cavaliere del livello dato
        private IEnumerator StartAs(int heroLevel)
        {
            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, $"-newgame -difficulty 0 -herolevel {heroLevel}");
            yield return LoadGeneratedCore();
            Manager.RunSeed = Seed;
            Assert.IsTrue(Manager.CurrentLevel.IsTown);
        }

        private static float[] LifeOfTheLevel()
        {
            return Manager.CurrentLevel.Enemies
                .Where(e => e != null && e.gameObject.activeSelf && !e.TryGetComponent(out BossController _) && !e.TryGetComponent(out GuardianController _))
                .Select(e => e.GetComponent<Health>().Max)
                .ToArray();
        }

        [UnityTest, Description("L'interruttore sta all'altare, spento di default, e gira con un clic (Sì/No) senza conferma e senza fare un mondo nuovo")]
        public IEnumerator Switch_TurnsAtTheAltar()
        {
            yield return StartAs(1);
            Assert.IsNotNull(Panel.GrowingWorldButton, "il pulsante c'è nel pannello dell'altare");
            Assert.IsFalse(Root.Difficulty.GrowingWorld, "spento di default");

            Manager.CurrentLevel.Altar.GetComponent<Interaction.Interactable>().Use(Player.gameObject);
            Assert.IsTrue(Panel.IsOpen);
            StringAssert.Contains("No", Panel.GrowingWorldText);
            int world = Root.Difficulty.World;

            Panel.GrowingWorldButton.onClick.Invoke();
            Assert.IsTrue(Root.Difficulty.GrowingWorld);
            StringAssert.Contains("Yes", Panel.GrowingWorldText);
            Assert.AreEqual(world, Root.Difficulty.World, "non è un mondo nuovo");
            Assert.IsTrue(Panel.IsOpen, "il pannello resta aperto");

            Panel.GrowingWorldButton.onClick.Invoke();
            Assert.IsFalse(Root.Difficulty.GrowingWorld);
            StringAssert.Contains("No", Panel.GrowingWorldText);
        }

        [UnityTest, Description("Il livello degli oggetti: spento è la zona; acceso, un cavaliere di livello 20 alla profondità 1 trova oggetti da 17 (D12)")]
        public IEnumerator Switch_ChangesTheItemLevel()
        {
            yield return StartAs(20);
            Assert.AreEqual(1, Root.LootItemLevel(1));
            Root.Difficulty.SetGrowingWorld(true);
            Assert.AreEqual(17, Root.LootItemLevel(1));
            Assert.AreEqual(17, Root.LootItemLevel(2), "alla 2 la zona vale 3: sempre 17, il cavaliere è molto più su");
            Root.Difficulty.SetGrowingWorld(false);
            Assert.AreEqual(1, Root.LootItemLevel(1));
        }

        [UnityTest, Description("I nemici del livello caricato dopo che l'interruttore è acceso hanno ×1,64 di vita (cavaliere 20 alla 1, D13); il Custode della 4 non cresce (D14)")]
        public IEnumerator Switch_ScalesEnemiesButNotTheGuardian()
        {
            yield return StartAs(20);
            var settings = Player.GetComponent<PlayerProgress>().Settings;

            yield return Travel("Level_Crypt", "Start", 1);
            var plain = LifeOfTheLevel();
            Assert.Greater(plain.Length, 3, "ci sono nemici");

            Root.Difficulty.SetGrowingWorld(true);
            yield return Travel("Level_Crypt", "Start", 2);
            yield return Travel("Level_Crypt", "Start", 1);
            var grown = LifeOfTheLevel();
            Assert.AreEqual(plain.Length, grown.Length, "lo stesso livello dal seme");
            float expected = settings.EnemyGrowth(1, 20, Root.CurrentDifficulty.ZoneLevelBonus, true);
            Assert.AreEqual(1.64f, expected, 1e-3f);
            for (int i = 0; i < plain.Length; i++)
            {
                Assert.AreEqual(plain[i] * expected, grown[i], plain[i] * 0.02f, $"nemico {i}");
            }

            // il Custode alla 4: stessa vita con l'interruttore spento e acceso
            yield return Travel("Level_Crypt", "Start", 4);
            float onLife = Manager.CurrentLevel.Guardian.GetComponent<Health>().Max;
            Root.Difficulty.SetGrowingWorld(false);
            yield return Travel("Level_Crypt", "Start", 3);
            yield return Travel("Level_Crypt", "Start", 4);
            float offLife = Manager.CurrentLevel.Guardian.GetComponent<Health>().Max;
            Assert.AreEqual(offLife, onLife, 0.01f, "il Custode ha la sua taratura");
        }

        [UnityTest, Description("L'esperienza di un nemico ucciso alla 1 da un cavaliere di livello 20 è maggiore con l'interruttore acceso (il calo parte dal margine, D12)")]
        public IEnumerator Switch_RaisesTheExperienceOfAnOverLevelHero()
        {
            yield return StartAs(20);
            var progress = Player.GetComponent<PlayerProgress>();

            yield return Travel("Level_Crypt", "Start", 1);
            // due nemici dello stesso tipo, per avere la stessa esperienza di base
            var pair = Manager.CurrentLevel.Enemies
                .Where(e => e != null && e.gameObject.activeSelf && e.TryGetComponent(out LootDrop _) && !e.TryGetComponent(out SpecialEnemy _) && !e.TryGetComponent(out BossController _))
                .GroupBy(e => e.Archetype)
                .First(g => g.Count() >= 2)
                .Take(2)
                .ToArray();
            int offGain = KillAndMeasure(progress, pair[0]);

            Root.Difficulty.SetGrowingWorld(true);
            int onGain = KillAndMeasure(progress, pair[1]);
            Assert.Greater(offGain, 0);
            Assert.Greater(onGain, offGain, "acceso guadagna di più");
        }

        private static int KillAndMeasure(PlayerProgress progress, EnemyAI enemy)
        {
            int before = progress.Progress.Experience;
            int levelBefore = progress.Progress.Level;
            var health = enemy.GetComponent<Health>();
            health.TakeDamage(new DamageInfo(health.Max, DamageType.Physical, null));
            Assert.AreEqual(levelBefore, progress.Progress.Level, "il cavaliere non sale di livello con un nemico solo");
            return progress.Progress.Experience - before;
        }
    }
}
