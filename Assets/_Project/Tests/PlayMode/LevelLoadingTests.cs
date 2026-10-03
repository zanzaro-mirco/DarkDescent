using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class LevelLoadingTests : SandboxFixture
    {
        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        [UnityTest, Description("Core da sola carica il primo livello, lo rende scena attiva e ci mette il player sull'ingresso")]
        public IEnumerator Core_Alone_LoadsFirstLevel()
        {
            SceneManager.LoadScene("Core");
            yield return WaitForLevel();

            var level = Manager.CurrentLevel;
            Assert.AreEqual("Level_01", level.gameObject.scene.name);
            Assert.AreEqual(level.gameObject.scene, SceneManager.GetActiveScene(), "il livello deve essere la scena attiva");

            var player = GameObject.Find("Player");
            Assert.AreEqual("Core", player.scene.name, "il player vive in Core");
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().isOnNavMesh);
            Assert.IsTrue(player.GetComponent<PlayerController>().enabled);
            Assert.Less(FlatDistance(level.GetEntrance("Start").position, player.transform.position), 0.1f);
        }

        [UnityTest, Description("Cambiando livello la vita resta quella di prima e il livello vecchio sparisce")]
        public IEnumerator LoadLevel_KeepsPlayerHealthAndReplacesLevel()
        {
            yield return LoadSandbox(allSkeletons: true);
            var health = Player.GetComponent<Health>();
            health.TakeDamage(new DamageInfo(30f, DamageType.Physical, null));
            float before = health.Current;

            Manager.LoadLevel(SandboxScene, "Start");
            yield return WaitForLevel();

            Assert.AreEqual(before, health.Current, "la vita deve sopravvivere al cambio di livello");
            Assert.AreEqual(2, SceneManager.sceneCount, "Core più un livello solo");
            Assert.AreEqual(3, Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Length, "una sola copia dei nemici");
            Assert.IsTrue(PlayerAgent.isOnNavMesh, "l'agent si riaggancia al NavMesh del livello nuovo");
        }

        [UnityTest, Description("I nemici del livello nuovo sono collegati; quelli del vecchio non sono più seguiti")]
        public IEnumerator LoadLevel_RebindsEnemies()
        {
            yield return LoadSandbox(allSkeletons: true);
            var numbers = Object.FindFirstObjectByType<DamageNumbers>();
            Assert.AreEqual(4, numbers.TrackedCount, "player più tre scheletri");

            // uno scheletro morto e sparito prima del cambio non deve restare tra quelli seguiti
            Object.Destroy(GameObject.Find("Skeleton_B"));
            yield return null;

            Manager.LoadLevel(SandboxScene, "Start");
            yield return WaitForLevel();

            Assert.AreEqual(4, numbers.TrackedCount, "i numeri seguono solo player e scheletri del livello nuovo");

            // lo scheletro del livello nuovo vede il player e lo insegue: è collegato
            var skeleton = GameObject.Find("Skeleton").GetComponent<EnemyAI>();
            Player.GetComponent<NavMeshAgent>().Warp(skeleton.transform.position + new Vector3(2f, 0f, 0f));
            float elapsed = 0f;
            while (elapsed < 2f && skeleton.State == EnemyState.Idle)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            Assert.AreNotEqual(EnemyState.Idle, skeleton.State);
        }

        [UnityTest, Description("Durante il cambio di livello il controller è spento: i click non fanno niente")]
        public IEnumerator LoadLevel_DisablesControllerWhileTransitioning()
        {
            yield return LoadSandbox();
            var controller = Player.GetComponent<PlayerController>();

            Manager.LoadLevel(SandboxScene, "Start");

            Assert.IsTrue(Manager.IsTransitioning);
            Assert.IsFalse(controller.enabled);
            yield return WaitForLevel();
            Assert.IsTrue(controller.enabled);
        }
    }
}
