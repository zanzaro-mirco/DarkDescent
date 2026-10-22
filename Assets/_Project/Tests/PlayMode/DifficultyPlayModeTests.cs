using System;
using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Stats;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Le difficoltà in gioco (passo 14.3): nemici, re, esperienza, morte, -difficulty e -herolevel.</summary>
    public class DifficultyPlayModeTests : SandboxFixture
    {
        private static CompositionRoot Root => UnityEngine.Object.FindFirstObjectByType<CompositionRoot>();

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static DeathScreen Death => UnityEngine.Object.FindFirstObjectByType<DeathScreen>();

        private static ChampionSettings Champions => AssetDatabase.LoadAssetAtPath<ChampionSettings>("Assets/_Project/Data/Enemies/ChampionSettings.asset");

        private static DamageInfo Damage(float amount) => new DamageInfo(amount, DamageType.Physical, null);

        [TearDown]
        public void ForgetArguments()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, null);
        }

        // la sandbox con le opzioni di una partita nuova, come se fossero sulla riga di comando
        private IEnumerator LoadSandboxWith(string arguments)
        {
            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, arguments);
            yield return LoadSandbox();
        }

        private GameObject Skeleton => GameObject.Find("Skeleton");

        [UnityTest, Description("All'Ombra lo scheletro ha la vita per 2,8, il danno per 2, 24 punti in più a colpire e 6 di Armatura (passo 14.6)")]
        public IEnumerator Shadow_SkeletonIsStronger()
        {
            yield return LoadSandbox();
            Assert.AreEqual(0, Root.Difficulty.Current, "senza opzioni si gioca al Crepuscolo");
            float life = Skeleton.GetComponent<Health>().Max;
            float toHit = Skeleton.GetComponent<CharacterStats>().ToHit;
            float armor = Skeleton.GetComponent<CharacterStats>().Armor;
            Assert.AreEqual(1f, Skeleton.GetComponent<MeleeAttack>().DamageMultiplier);

            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, "-difficulty 1");
            SceneManager.LoadScene("Core");
            SceneManager.LoadScene(SandboxScene, LoadSceneMode.Additive);
            yield return WaitForLevel();
            yield return null;

            Assert.AreEqual((1, 0), (Root.Difficulty.Current, Root.Difficulty.HighestCompleted), "l'Ombra, con il Crepuscolo completato");
            Assert.AreEqual(life * 2.8f, Skeleton.GetComponent<Health>().Max, 0.01f);
            Assert.AreEqual(Skeleton.GetComponent<Health>().Max, Skeleton.GetComponent<Health>().Current, 0.01f, "a vita piena");
            Assert.AreEqual(2f, Skeleton.GetComponent<MeleeAttack>().DamageMultiplier);
            Assert.AreEqual(toHit + 24f, Skeleton.GetComponent<CharacterStats>().ToHit);
            Assert.AreEqual(armor + 6f, Skeleton.GetComponent<CharacterStats>().Armor);
        }

        [UnityTest, Description("Un campione all'Ombra ha i due moltiplicatori insieme: vita 2,8 × 3, danno 2 × 1,5")]
        public IEnumerator Shadow_ChampionHasBothMultipliers()
        {
            yield return LoadSandboxWith("-difficulty 1");
            var health = Skeleton.GetComponent<Health>();
            float life = health.Max;

            var special = Skeleton.AddComponent<SpecialEnemy>();
            special.Promote(EnemyRank.Champion, Array.Empty<string>());
            special.Bind(Champions, Player.GetComponent<Health>(), Player.GetComponent<MovementSlow>());

            Assert.AreEqual(life * Champions.For(EnemyRank.Champion).Life, health.Max, 0.01f);
            Assert.AreEqual(2f * Champions.For(EnemyRank.Champion).Damage, Skeleton.GetComponent<MeleeAttack>().DamageMultiplier, 1e-4f);
        }

        [UnityTest, Description("All'Ombra uno scheletro vale 2,3 volte l'esperienza, con la zona alzata di 10 (passo 14.6)")]
        public IEnumerator Shadow_KillGivesMoreExperience()
        {
            yield return LoadSandboxWith("-difficulty 1");
            var progress = Player.GetComponent<PlayerProgress>();
            int gained = 0;
            progress.Progress.ExperienceGained += amount => gained += amount;
            int expected = progress.Settings.ExperienceForKill(70, Manager.CurrentLevel.Depth, 1, 10, 2.3f);

            Skeleton.GetComponent<Health>().TakeDamage(Damage(100_000f));
            yield return null;

            Assert.AreEqual(expected, gained);
            Assert.AreEqual(progress.Settings.ExperienceForKill(70, Manager.CurrentLevel.Depth, 1) * 2.3, gained, 1, "2,3 volte il Crepuscolo");
        }

        [UnityTest, Description("Morire all'Ombra costa il 10% dell'oro e il 5% dell'esperienza del livello; la schermata lo dice")]
        public IEnumerator Shadow_DeathCosts()
        {
            yield return LoadSandboxWith("-difficulty 1");
            var inventory = Player.GetComponent<PlayerInventory>().Inventory;
            var progress = Player.GetComponent<PlayerProgress>().Progress;
            inventory.RestoreGold(1000);
            progress.Restore(9, 2000, 0);

            Player.GetComponent<Health>().TakeDamage(Damage(100_000f));
            yield return null;

            Assert.AreEqual(900, inventory.Gold);
            Assert.AreEqual((9, 2000 - 168), (progress.Level, progress.Experience), "il 5% di 3360, senza scendere di livello");
            Assert.AreEqual("−100 gold, −168 experience", Death.LossText);
        }

        [UnityTest, Description("Al Crepuscolo morire non costa niente e la schermata non scrive la riga")]
        public IEnumerator Dusk_DeathIsFree()
        {
            yield return LoadSandbox();
            var inventory = Player.GetComponent<PlayerInventory>().Inventory;
            var progress = Player.GetComponent<PlayerProgress>().Progress;
            inventory.RestoreGold(1000);
            progress.Restore(9, 2000, 0);

            Player.GetComponent<Health>().TakeDamage(Damage(100_000f));
            yield return null;

            Assert.AreEqual(1000, inventory.Gold);
            Assert.AreEqual(2000, progress.Experience);
            Assert.AreEqual(string.Empty, Death.LossText);
        }

        [UnityTest, Description("-difficulty 3 -herolevel 32: Abisso I aperto, cavaliere di livello 32 con i punti spesi e un raro di livello 32 per slot")]
        public IEnumerator CommandLine_PreparesTheGame()
        {
            yield return LoadSandboxWith("-difficulty 3 -herolevel 32");
            Assert.AreEqual((3, 2), (Root.Difficulty.Current, Root.Difficulty.HighestCompleted));

            var progress = Player.GetComponent<PlayerProgress>().Progress;
            Assert.AreEqual((32, 0, 0), (progress.Level, progress.Experience, progress.UnspentPoints));

            var equipment = Player.GetComponent<PlayerInventory>().Equipment;
            foreach (var slot in new[] { EquipSlot.Weapon, EquipSlot.Body, EquipSlot.Helm, EquipSlot.Ring, EquipSlot.Ring2, EquipSlot.Amulet })
            {
                var item = equipment.Get(slot);
                Assert.IsNotNull(item, slot.ToString());
                Assert.AreEqual(Rarity.Rare, item.Rarity, slot.ToString());
                Assert.AreEqual(32, item.ItemLevel, slot.ToString());
                Assert.IsTrue(item.Affixes.All(a => a.Definition.Grade >= 3), $"{slot}: al livello 32 solo i gradi III e IV");
            }

            var health = Player.GetComponent<Health>();
            Assert.AreEqual(health.Max, health.Current, "a vita piena");
        }

        [UnityTest, Description("Il salvataggio porta la difficoltà, la più alta completata e il mondo, e li rimette (D14)")]
        public IEnumerator Save_CarriesTheDifficulty()
        {
            yield return LoadSandboxWith("-difficulty 2");
            var save = UnityEngine.Object.FindFirstObjectByType<Save.SaveGame>();
            Root.Difficulty.NewWorld();
            var data = save.Capture();
            Assert.AreEqual((2, 1, 1), (data.Difficulty, data.HighestDifficulty, data.World));

            Root.Difficulty.Restore(0, -1, 0);
            save.Apply(data);
            Assert.AreEqual((2, 1, 1), (Root.Difficulty.Current, Root.Difficulty.HighestCompleted, Root.Difficulty.World));
        }

        [UnityTest, Description("Il re dell'Abisso I prende tre modificatori diversi, senza anello né punto sull'automappa")]
        public IEnumerator AbyssOne_BossHasThreeModifiers()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, "-difficulty 3");
            yield return LoadGeneratedCore();
            Manager.RunSeed = 1;
            Manager.LoadLevel("Level_Caves", "FromAbove", 8);
            yield return null;
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel.Depth != 8; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, "la profondità 8 non arriva");
                yield return null;
            }

            yield return null;
            var boss = Manager.CurrentLevel.Boss;
            Assert.IsNotNull(boss);
            Assert.IsTrue(boss.TryGetComponent(out SpecialEnemy special), "il re dell'Abisso ha i modificatori");
            Assert.AreEqual(EnemyRank.Boss, special.Rank);
            Assert.AreEqual(3, special.ActiveModifiers.Count);
            Assert.AreEqual(3, special.ActiveModifiers.Distinct().Count());
            Assert.IsNull(special.Ring);
            Assert.AreEqual(1f, special.ExperienceMultiplier, "l'esperienza la alza la difficoltà, non il grado");
        }

        [UnityTest, Description("L'anello di fuoco del re cresce con la difficoltà, come i suoi colpi: all'Abisso I i 28–38 di sempre per il moltiplicatore del danno (passo 14.6)")]
        public IEnumerator AbyssOne_RingGrowsWithTheDifficulty()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, "-difficulty 3");
            yield return LoadGeneratedCore();
            Manager.RunSeed = 1;
            Manager.LoadLevel("Level_Caves", "FromAbove", 8);
            yield return null;
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel.Depth != 8; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, "la profondità 8 non arriva");
                yield return null;
            }

            yield return null;
            var boss = Manager.CurrentLevel.Boss;
            var attack = boss.GetComponent<MeleeAttack>();
            Assert.GreaterOrEqual(attack.DamageMultiplier + 1e-4f, Root.CurrentDifficulty.Damage);

            // il cavaliere regge tutto: conta solo il colpo di fuoco
            var health = Player.GetComponent<Health>();
            health.MultiplyMax(10_000f);
            health.SetCurrent(health.Max);
            float ring = 0f;
            float multiplier = 0f;
            void Record(DamageInfo info, float current)
            {
                if (info.Type == DamageType.Fire && ring == 0f)
                {
                    ring = info.Amount;
                    multiplier = attack.DamageMultiplier;
                }
            }

            health.Damaged += Record;
            Vector3 home = boss.GetComponent<EnemyAI>().HomePosition;
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(home + Vector3.back * 2f, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas));
            Player.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(hit.position);
            for (float t = 0f; !boss.IsFighting; t += Time.deltaTime)
            {
                Assert.Less(t, 3f, "lo scontro non comincia");
                yield return null;
            }

            boss.Health.TakeDamage(Damage(boss.Health.Max * 0.8f));
            for (float t = 0f; ring == 0f; t += Time.deltaTime)
            {
                Assert.Less(t, 12f, "l'anello non arriva");
                yield return null;
            }

            health.Damaged -= Record;
            Assert.That(ring / multiplier, Is.InRange(28f - 1e-3f, 38f + 1e-3f), "il colpo di sempre, moltiplicato");
            Assert.GreaterOrEqual(ring, 28f * Root.CurrentDifficulty.Damage - 1e-3f);
        }
    }
}
