using System.Collections;
using DarkDescent.Characters;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class DeathAndRestartTests : SandboxFixture
    {
        private const float ShowDelay = 2f;

        private static DamageInfo Damage(float amount)
        {
            return new DamageInfo(amount, DamageType.Physical, null);
        }

        private static DeathScreen FindDeathScreen()
        {
            return Object.FindFirstObjectByType<DeathScreen>();
        }

        private IEnumerator KillPlayerAndWaitForScreen()
        {
            Player.GetComponent<Health>().TakeDamage(Damage(1000f));
            float elapsed = 0f;
            while (elapsed < ShowDelay + 1f && !FindDeathScreen().IsShown)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(FindDeathScreen().IsShown, "la schermata di morte deve comparire");
        }

        [UnityTest, Description("Morto il player: input e agent spenti, animazione di morte, schermata solo dopo la caduta")]
        public IEnumerator PlayerDies_StopsEverythingThenShowsScreen()
        {
            yield return LoadSandbox();
            var start = Player.position;

            Player.GetComponent<Health>().TakeDamage(Damage(1000f));

            Assert.IsFalse(Player.GetComponent<PlayerController>().enabled, "il controller va spento");
            Assert.IsFalse(Player.GetComponent<PlayerInputReader>().enabled, "l'input va spento");
            Assert.IsFalse(PlayerAgent.enabled, "l'agent va spento");
            Assert.IsTrue(Player.GetComponentInChildren<CharacterAnimatorDriver>().IsDead);
            Assert.IsFalse(FindDeathScreen().IsShown, "la schermata aspetta la fine della caduta");

            ClickAt(new Vector3(-4f, 0f, 0f));
            yield return new WaitForSeconds(0.5f);
            Assert.Less(Vector3.Distance(start, Player.position), 0.01f, "un morto non cammina");

            yield return new WaitForSeconds(ShowDelay);
            Assert.IsTrue(FindDeathScreen().IsShown);
        }

        [UnityTest, Description("Ricomincia ricarica Core e riparte dal primo livello: vita piena, scheletri vivi e fermi, timeScale a 1")]
        public IEnumerator Restart_ReloadsCleanScene()
        {
            yield return LoadSandbox();
            GameObject.Find("Skeleton").GetComponent<Health>().TakeDamage(Damage(10f));
            yield return KillPlayerAndWaitForScreen();

            // un hit stop rimasto a metà non deve sopravvivere al riavvio (trappola 6)
            Time.timeScale = 0.3f;

            var oldPlayer = Player.gameObject;
            var button = GameObject.Find("RestartButton").GetComponent<RectTransform>();
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center));
            Move(Mouse.position, screen);
            yield return null;
            yield return null;
            Press(Mouse.leftButton);
            Release(Mouse.leftButton);

            // il click arriva al bottone al frame dopo, Core si ricarica a quello dopo ancora: si
            // aspetta che il player vecchio sparisca, poi che il LevelManager nuovo carichi il livello
            float elapsed = 0f;
            while (oldPlayer != null && elapsed < 5f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(oldPlayer == null, "Ricomincia deve ricaricare Core");
            yield return WaitForLevel();

            Assert.AreEqual(1f, Time.timeScale, "timeScale va rimesso a 1");
            var player = GameObject.Find("Player");
            var playerHealth = player.GetComponent<Health>();
            Assert.IsFalse(playerHealth.IsDead);
            Assert.AreEqual(playerHealth.Max, playerHealth.Current);
            Assert.IsTrue(player.GetComponent<PlayerController>().enabled);
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().isOnNavMesh);

            Assert.AreEqual("Level_Crypt", Object.FindFirstObjectByType<LevelManager>().CurrentLevel.gameObject.scene.name, "si riparte dal primo livello (D8), la cripta generata");
            var enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            Assert.IsNotEmpty(enemies);
            foreach (var enemy in enemies)
            {
                var enemyHealth = enemy.GetComponent<Health>();
                Assert.AreEqual(enemyHealth.Max, enemyHealth.Current, $"{enemy.name} riparte con la vita piena");
                Assert.AreEqual(EnemyState.Idle, enemy.State);
            }

            Assert.IsFalse(FindDeathScreen().IsShown, "dopo il riavvio la schermata è nascosta");
            Assert.AreEqual(1f, Object.FindFirstObjectByType<HealthOrb>().FillAmount, 0.001f);
        }

        [UnityTest, Description("Il corpo dello scheletro resta qualche secondo, poi sparisce")]
        public IEnumerator SkeletonCorpse_DisappearsAfterDelay()
        {
            yield return LoadSandbox();
            var skeleton = GameObject.Find("Skeleton");
            skeleton.GetComponent<Health>().TakeDamage(Damage(1000f));

            yield return new WaitForSeconds(2f);
            Assert.IsTrue(skeleton != null, "il corpo deve restare per qualche secondo");

            yield return new WaitForSeconds(4f);
            Assert.IsTrue(skeleton == null, "il corpo deve sparire");
        }
    }
}
