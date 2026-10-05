using System.Collections;
using System.Linq;
using System.Text;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class DungeonDescentTests : SandboxFixture
    {
        private const string CryptScene = "Level_Crypt";

        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static IEnumerator WaitForDepth(int depth, float timeout = 15f)
        {
            float elapsed = 0f;
            while (elapsed < timeout)
            {
                var manager = Manager;
                if (!manager.IsTransitioning && manager.CurrentLevel != null && manager.CurrentLevel.Depth == depth)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.Fail($"la profondità {depth} non si è caricata");
        }

        // la cripta in breve: gruppi con quanti figli e dove (somma delle posizioni in pianta), e cosa
        // lascerà ogni nemico
        private static string Signature(LevelContext context)
        {
            var sb = new StringBuilder();
            foreach (Transform group in context.transform)
            {
                Vector3 sum = Vector3.zero;
                foreach (Transform child in group)
                {
                    sum += child.position;
                }

                sb.Append($"{group.name}:{group.childCount}@{sum.x:F1},{sum.z:F1}\n");
            }

            foreach (var enemy in context.Enemies)
            {
                var item = enemy.GetComponent<LootDrop>().Preview();
                sb.Append(item == null ? "-" : $"{item.Definition.name}/{item.Rarity}").Append(' ');
            }

            return sb.ToString();
        }

        [UnityTest, Description("All'avvio di Core si genera il livello 1 della cripta: buio, con cinque scheletri sul NavMesh cotto a runtime, il cavaliere sull'ingresso e la scala per il livello 2")]
        public IEnumerator Start_GeneratesTheFirstLevel()
        {
            yield return LoadGeneratedCore();
            var level = Manager.CurrentLevel;

            Assert.AreEqual(CryptScene, level.gameObject.scene.name);
            Assert.AreEqual(CryptScene, SceneManager.GetActiveScene().name, "luci e Instantiate vanno nella cripta");
            Assert.AreEqual(1, level.Depth);
            Assert.IsTrue(PlayerAgent.isOnNavMesh, "il cavaliere sta sul NavMesh");

            Assert.AreEqual(5, level.Enemies.Count, "3 + 2 × profondità");
            foreach (var enemy in level.Enemies)
            {
                Assert.IsTrue(enemy.GetComponent<NavMeshAgent>().isOnNavMesh, $"{enemy.name} fuori dal NavMesh");
            }

            Assert.AreEqual(1, level.Exits.Count);
            Assert.AreEqual(CryptScene, level.Exits[0].TargetScene);
            Assert.AreEqual(2, level.Exits[0].TargetDepth);
            Assert.AreEqual("Descend to level 2", level.Exits[0].GetComponent<Interactable>().GetLabel(Localizer));

            Assert.AreEqual(AmbientMode.Flat, RenderSettings.ambientMode);
            Assert.IsNotNull(Object.FindFirstObjectByType<Volume>(), "post-processing della cripta");
            int shadowLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l => l.shadows != LightShadows.None);
            Assert.AreEqual(1, shadowLights, "le ombre le fa solo la luce del cavaliere");
        }

        [UnityTest, Description("Dalla scala si scende di livello generato in livello generato fino al 4, che non ha la scala")]
        public IEnumerator Descent_ReachesTheFourthLevel()
        {
            yield return LoadGeneratedCore();
            for (int depth = 1; depth < 4; depth++)
            {
                var exit = Manager.CurrentLevel.Exits[0];
                PlayerAgent.Warp(exit.GetComponent<Interactable>().ApproachPoint);
                yield return WaitForDepth(depth + 1);

                var dungeon = Object.FindFirstObjectByType<DungeonLevel>();
                Debug.Log($"profondità {depth + 1}: generazione {dungeon.Timings.generate} ms, costruzione {dungeon.Timings.build} ms, NavMesh {dungeon.Timings.bake} ms");
                Assert.AreEqual(2, SceneManager.sceneCount, "Core più un livello solo");
                Assert.IsTrue(PlayerAgent.isOnNavMesh, $"profondità {depth + 1}: cavaliere fuori dal NavMesh");
                Assert.AreEqual(3 + 2 * (depth + 1), Manager.CurrentLevel.Enemies.Count);
            }

            Assert.AreEqual(0, Manager.CurrentLevel.Exits.Count, "il livello 4 non ha la scala: le caverne arrivano alla M7");
        }

        [UnityTest, Description("Con lo stesso seme della partita lo stesso livello torna uguale, nemici e oggetti compresi; con un altro seme no")]
        public IEnumerator SameSeed_SameDungeon()
        {
            yield return LoadGeneratedCore();
            Manager.RunSeed = 4711;
            Manager.LoadLevel(CryptScene, "FromAbove", 2);
            yield return WaitForDepth(2);
            string first = Signature(Manager.CurrentLevel);

            Manager.LoadLevel(CryptScene, "FromAbove", 3);
            yield return WaitForDepth(3);
            Manager.LoadLevel(CryptScene, "FromAbove", 2);
            yield return WaitForDepth(2);
            Assert.AreEqual(first, Signature(Manager.CurrentLevel));

            Manager.RunSeed = 4712;
            Manager.LoadLevel(CryptScene, "FromAbove", 3);
            yield return WaitForDepth(3);
            Manager.LoadLevel(CryptScene, "FromAbove", 2);
            yield return WaitForDepth(2);
            Assert.AreNotEqual(first, Signature(Manager.CurrentLevel));
        }
    }
}
