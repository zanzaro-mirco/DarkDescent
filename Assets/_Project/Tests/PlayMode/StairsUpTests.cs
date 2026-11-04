using System.Collections;
using System.Linq;
using DarkDescent.Interaction;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>La scala in su (D2–D8 della M22): lo sbocco del cunicolo che risale al livello di sopra e, dal primo, al borgo.</summary>
    public class StairsUpTests : SandboxFixture
    {
        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private IEnumerator WaitFor(string scene, int depth, float timeout = 20f)
        {
            for (float t = 0f; t < timeout; t += Time.unscaledDeltaTime)
            {
                var manager = Manager;
                if (!manager.IsTransitioning && manager.CurrentLevel != null && manager.CurrentLevel.Depth == depth
                    && manager.CurrentLevel.gameObject.scene.name == scene)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{scene} alla profondità {depth} non si è caricata");
        }

        private void StepOn(LevelExit exit)
        {
            Vector3 approach = exit.GetComponent<Interactable>().ApproachPoint;
            Assert.IsTrue(NavMesh.SamplePosition(approach, out var hit, 1.5f, NavMesh.AllAreas), $"{exit.name}: il punto davanti all'uscita non è sul NavMesh");
            PlayerAgent.Warp(hit.position);
        }

        private static string SceneOf(int depth) => depth > 4 ? "Level_Caves" : "Level_Crypt";

        [UnityTest, Timeout(300000), Description("Dalla profondità 8 alla 1 ogni scala in su porta al livello di sopra, all'ingresso FromBelow davanti alla sua scala giù (le caverne alla cripta), e dalla 1 si esce nel borgo davanti alla grotta; dal borgo si riscende")]
        public IEnumerator Climb_BackThroughEveryLevelToTheVillage()
        {
            yield return LoadGeneratedCore();
            yield return WaitFor("Level_Crypt", 1);
            for (int depth = 1; depth < 8; depth++)
            {
                StepOn(Manager.CurrentLevel.DownExits.First());
                yield return WaitFor(SceneOf(depth + 1), depth + 1);
            }

            for (int depth = 8; depth >= 1; depth--)
            {
                var level = Manager.CurrentLevel;
                Assert.IsNotNull(level.UpExit, $"profondità {depth}: nessuna scala in su");
                Assert.AreEqual(depth == 1 ? 0 : depth - 1, level.UpExit.TargetDepth, $"profondità {depth}");
                Assert.AreEqual(depth == 1 ? "Town" : SceneOf(depth - 1), level.UpExit.TargetScene, $"profondità {depth}");

                StepOn(level.UpExit);
                if (depth == 1)
                {
                    yield return WaitFor("Town", 0);
                    Assert.AreEqual("FromCrypt", Manager.CurrentEntrance);
                    Assert.IsTrue(PlayerAgent.isOnNavMesh, "dal cunicolo il cavaliere esce nel borgo sul NavMesh");
                    Assert.IsNull(Manager.CurrentLevel.UpExit, "il borgo non ha una scala in su");
                    Assert.IsFalse(Manager.CurrentLevel.Exits[0].GetComponent<BoxCollider>().bounds.Contains(Player.position), "si esce dal cunicolo fuori dal trigger della grotta");
                    break;
                }

                yield return WaitFor(SceneOf(depth - 1), depth - 1);
                Assert.AreEqual("FromBelow", Manager.CurrentEntrance, $"risalito alla profondità {depth - 1}");
                Assert.IsTrue(PlayerAgent.isOnNavMesh, $"profondità {depth - 1}: cavaliere fuori dal NavMesh");

                // arrivato davanti alla scala giù, senza riscendere: fuori dal suo trigger, e dopo un secondo ancora lì
                var down = Manager.CurrentLevel.DownExits.First();
                Assert.IsFalse(down.GetComponent<BoxCollider>().bounds.Contains(Player.position), $"profondità {depth - 1}: si arriva dentro il trigger della scala giù");
                Assert.Less(Vector3.Distance(Player.position, down.transform.position), 8f, $"profondità {depth - 1}: l'arrivo è lontano dalla scala");
                yield return new WaitForSeconds(1f);
                Assert.AreEqual(depth - 1, Manager.CurrentLevel.Depth, "dopo l'arrivo il livello non cambia: niente rimbalzo");
            }

            // dal borgo si riscende alla profondità 1, dall'ingresso di sempre
            StepOn(Manager.CurrentLevel.Exits[0]);
            yield return WaitFor("Level_Crypt", 1);
            Assert.AreEqual("Start", Manager.CurrentEntrance);
        }

        [UnityTest, Timeout(300000), Description("L'ingresso di ogni livello non è nel trigger della sua scala in su: arrivando dall'alto non si risale subito, e dopo un secondo si è ancora lì (profondità 1–8)")]
        public IEnumerator Arrival_FromAboveDoesNotBounce()
        {
            yield return LoadGeneratedCore();
            yield return WaitFor("Level_Crypt", 1);
            for (int depth = 1; depth <= 8; depth++)
            {
                var level = Manager.CurrentLevel;
                var up = level.UpExit;
                Assert.IsNotNull(up, $"profondità {depth}");
                Assert.IsFalse(up.GetComponent<BoxCollider>().bounds.Contains(Player.position), $"profondità {depth}: l'arrivo è dentro il trigger della scala in su");
                yield return new WaitForSeconds(1f);
                Assert.AreEqual(depth, Manager.CurrentLevel.Depth, $"profondità {depth}: rimbalzo");
                if (depth < 8)
                {
                    StepOn(level.DownExits.First());
                    yield return WaitFor(SceneOf(depth + 1), depth + 1);
                }
            }
        }
    }
}
