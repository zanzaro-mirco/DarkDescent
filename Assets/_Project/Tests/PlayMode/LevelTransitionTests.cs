using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class LevelTransitionTests : SandboxFixture
    {
        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static LevelExit FindExit()
        {
            Assert.AreEqual(1, Manager.CurrentLevel.Exits.Count, "il livello 1 ha una scala");
            return Manager.CurrentLevel.Exits[0];
        }

        private IEnumerator WaitForScene(string sceneName, float timeout = 15f)
        {
            float elapsed = 0f;
            while (elapsed < timeout)
            {
                var manager = Manager;
                if (manager != null && !manager.IsTransitioning && manager.CurrentLevel != null
                    && manager.CurrentLevel.gameObject.scene.name == sceneName)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.Fail($"{sceneName} non si è caricato");
        }

        [UnityTest, Description("Un click sulla scala porta giù il cavaliere: livello 2, sull'ingresso, con la vita di prima")]
        public IEnumerator ClickOnStairs_GoesDownWithSameHealth()
        {
            yield return LoadCore();
            var exit = FindExit();

            // vicino alla scala, nella stanza da cui si arriva: lì non ci sono scheletri
            var approach = exit.GetComponent<Interaction.Interactable>().ApproachPoint;
            PlayerAgent.Warp(approach + exit.transform.forward * 3f);
            var health = Player.GetComponent<Health>();
            health.TakeDamage(new DamageInfo(20f, DamageType.Physical, null));
            float before = health.Current;
            yield return new WaitForSeconds(1f);

            ClickAt(exit.transform.position);
            yield return WaitForScene("Level_02");

            Assert.AreEqual(before, health.Current, "la vita scende con il cavaliere");
            Assert.AreEqual(2, SceneManager.sceneCount, "Core più il livello 2");
            Assert.IsTrue(PlayerAgent.isOnNavMesh);
            Assert.AreEqual("Level_02", SceneManager.GetActiveScene().name);
            Assert.Less(FlatDistance(Manager.CurrentLevel.GetEntrance("FromAbove").position, Player.position), 0.1f);
        }

        [UnityTest, Description("Lo schermo sfuma al nero durante il cambio e torna visibile dopo")]
        public IEnumerator Transition_FadesOutAndIn()
        {
            yield return LoadCore();
            var fader = Object.FindFirstObjectByType<ScreenFader>();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0f, fader.Alpha, 0.01f, "dopo l'avvio lo schermo è visibile");

            var exit = FindExit();
            PlayerAgent.Warp(exit.GetComponent<Interaction.Interactable>().ApproachPoint);

            float maxAlpha = 0f;
            float elapsed = 0f;
            while (elapsed < 10f && (Manager.IsTransitioning || Manager.CurrentLevel.gameObject.scene.name != "Level_02"))
            {
                maxAlpha = Mathf.Max(maxAlpha, fader.Alpha);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(1f, maxAlpha, 0.01f, "durante il cambio lo schermo è nero");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0f, fader.Alpha, 0.01f, "nel livello 2 lo schermo torna visibile");
        }

        [UnityTest, Description("Un morto sulle scale non cambia livello")]
        public IEnumerator DeadPlayer_DoesNotTakeStairs()
        {
            yield return LoadCore();
            var exit = FindExit();
            Player.GetComponent<Health>().TakeDamage(new DamageInfo(1000f, DamageType.Physical, null));

            // il corpo viene spostato sulla scala a mano: il trigger scatta, il cambio no
            Player.position = exit.GetComponent<Interaction.Interactable>().ApproachPoint;
            yield return new WaitForSeconds(0.5f);

            Assert.IsFalse(Manager.IsTransitioning);
            Assert.AreEqual("Level_01", Manager.CurrentLevel.gameObject.scene.name);
        }

        [UnityTest, Description("Il controller del cavaliere resta spento finché il livello nuovo non è pronto")]
        public IEnumerator Controller_DisabledUntilNewLevelReady()
        {
            yield return LoadCore();
            var controller = Player.GetComponent<PlayerController>();
            PlayerAgent.Warp(FindExit().GetComponent<Interaction.Interactable>().ApproachPoint);

            float elapsed = 0f;
            while (elapsed < 2f && !Manager.IsTransitioning)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(Manager.IsTransitioning, "raggiunta la scala parte il cambio");
            Assert.IsFalse(controller.enabled);
            yield return WaitForScene("Level_02");
            Assert.IsTrue(controller.enabled);
        }
    }
}
