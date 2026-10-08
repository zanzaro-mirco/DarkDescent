using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using DarkDescent.Progression;
using DarkDescent.Stats;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// La crescita del cavaliere nel gioco (passo 8.2 della M8): esperienza dai nemici uccisi, barra
    /// nell'HUD, salita di livello con la vita piena, punti spesi con i "+" del pannello.
    /// </summary>
    public class LevelUpTests : SandboxFixture
    {
        private static DamageInfo Damage(float amount)
        {
            return new DamageInfo(amount, DamageType.Physical, null);
        }

        [UnityTest, Description("Uno scheletro ucciso dà la sua esperienza alla profondità del livello; la barra in basso si riempie e dice livello ed esperienza")]
        public IEnumerator Kill_GivesExperience()
        {
            yield return LoadSandbox();
            var progress = Player.GetComponent<PlayerProgress>();
            var skeleton = Object.FindFirstObjectByType<EnemyAI>();
            int depth = Object.FindFirstObjectByType<LevelManager>().CurrentLevel.Depth;
            int expected = progress.Settings.ExperienceForKill(skeleton.Archetype.Experience, depth, 1);

            skeleton.GetComponent<Health>().TakeDamage(Damage(1000f));
            yield return null;

            Assert.AreEqual(1, progress.Progress.Level);
            Assert.AreEqual(expected, progress.Progress.Experience);
            var bar = Object.FindFirstObjectByType<ExperienceBar>();
            Assert.AreEqual(expected / 100f, bar.FillAmount, 0.001f);
            Assert.AreEqual($"Level 1 – {expected} / 100", bar.LabelText);

            skeleton.GetComponent<Health>().TakeDamage(Damage(1000f));
            Assert.AreEqual(expected, progress.Progress.Experience, "un morto non dà esperienza due volte");
        }

        [UnityTest, Description("Salendo di livello la vita si riempie, con i 2 punti in più, si accende la luce dorata; il pannello mostra i punti e un + li spende")]
        public IEnumerator LevelUp_RefillsLifeAndSpendsPoints()
        {
            yield return LoadSandbox();
            Object.FindFirstObjectByType<EnemyAI>().enabled = false;
            var progress = Player.GetComponent<PlayerProgress>();
            var health = Player.GetComponent<Health>();
            var stats = Player.GetComponent<CharacterStats>();
            float maxBefore = health.Max;
            health.TakeDamage(Damage(health.Max * 0.5f));
            int shown = 0;
            progress.LevelUpShown += _ => shown++;

            progress.Progress.Add(100);
            yield return null;

            Assert.AreEqual(2, progress.Progress.Level);
            Assert.AreEqual(maxBefore + 2f, health.Max, "2 di vita a livello");
            Assert.AreEqual(health.Max, health.Current, "salendo la vita si riempie");
            Assert.AreEqual(1, shown);
            Assert.IsTrue(progress.IsGlowing, "la luce dorata");

            var panel = Object.FindFirstObjectByType<CharacterPanel>();
            panel.Toggle();
            yield return null;
            StringAssert.Contains("Level 2", panel.HeaderText);
            StringAssert.Contains("5 points to spend", panel.HeaderText);
            var raise = panel.RaiseButton(StatType.Strength);
            Assert.IsTrue(raise.gameObject.activeInHierarchy, "con punti da spendere i + si vedono");

            // accanto alla riga della Forza
            Assert.AreEqual(panel.LineWorldPosition(0).y, raise.transform.position.y, 1f);

            float strength = stats.Strength;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, raise.transform.position);
            Move(Mouse.position, screen);
            yield return null;
            Press(Mouse.leftButton);
            yield return null;
            Release(Mouse.leftButton);
            yield return null;

            Assert.AreEqual(strength + 1f, stats.Strength, "il + della Forza ne compra un punto");
            Assert.AreEqual(4, progress.Progress.UnspentPoints);
            Assert.IsFalse(PlayerAgent.hasPath, "il click sul pannello non fa camminare il cavaliere");

            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(progress.Progress.TrySpend(StatType.Vitality));
            }

            Assert.IsFalse(raise.gameObject.activeSelf, "spesi tutti, i + spariscono");
            StringAssert.DoesNotContain("points to spend", panel.HeaderText);
        }
    }
}
