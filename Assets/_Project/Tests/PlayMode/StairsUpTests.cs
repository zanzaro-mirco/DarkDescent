using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
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
                Assert.AreSame(up, level.Exits[level.Exits.Count - 1], $"profondità {depth}: la scala in su sta in fondo alle uscite");
                if (depth < 8)
                {
                    Assert.AreSame(level.DownExits.First(), level.Exits[0], $"profondità {depth}: Exits[0] è la scala giù");
                }

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
        // quello che si fa a un livello prima di lasciarlo: nemici uccisi, una cassa aperta, le cose a terra, la mappa vista
        private sealed class Marks
        {
            public readonly List<int> Killed = new List<int>();
            public int Opened;
            public List<string> Ground;
            public int Explored;
            public int EnemyCount;
        }

        private static GameObject PlayerObject => GameObject.Find("Player");

        private static List<string> GroundNow()
        {
            var scene = Manager.CurrentLevel.gameObject.scene;
            return Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None)
                .Where(g => g.gameObject.scene == scene && g.gameObject.activeInHierarchy && g.Item != null)
                .Select(g => $"{g.transform.position.x:0.0},{g.transform.position.z:0.0}:{(g.Gold > 0 ? "gold " + g.Gold : g.Item.Definition.name)}")
                .OrderBy(s => s)
                .ToList();
        }

        private static bool IsGone(EnemyAI enemy) => enemy == null || !enemy.gameObject.activeSelf;

        private IEnumerator MarkTheLevel(Marks marks)
        {
            var level = Manager.CurrentLevel;
            marks.EnemyCount = level.Enemies.Count;
            for (int i = 0; i < level.Enemies.Count && (marks.Killed.Count < 3 || GroundNow().Count == 0) && marks.Killed.Count < 8; i++)
            {
                var enemy = level.Enemies[i];
                if (enemy == null || !enemy.TryGetComponent(out LootDrop _))
                {
                    continue;
                }

                var health = enemy.GetComponent<Health>();
                health.TakeDamage(new DamageInfo(health.Max, DamageType.Physical, null));
                marks.Killed.Add(i);
                if (marks.Opened == 0 && level.Chests.Count > 0)
                {
                    level.Chests[0].GetComponent<Interactable>().Use(PlayerObject);
                    marks.Opened = 1;
                }

                yield return new WaitForSeconds(1f);
            }

            Assert.Greater(marks.Killed.Count, 0, "un nemico con il suo loot");
            Assert.IsTrue(level.Chests.Count == 0 || level.Chests[0].IsOpen, "la cassa è aperta");
            marks.Ground = GroundNow();
            marks.Explored = Object.FindFirstObjectByType<ExplorationTracker>().Exploration.ExploredCount;
        }

        private void CheckTheLevelIsAsItWas(Marks marks, int depth)
        {
            var level = Manager.CurrentLevel;
            Assert.AreEqual(depth, level.Depth);
            Assert.AreEqual(marks.EnemyCount, level.Enemies.Count, $"profondità {depth}: lo stesso livello dal seme");
            Assert.IsTrue(marks.Killed.All(v => IsGone(level.Enemies[v])), $"profondità {depth}: i nemici uccisi non rinascono");
            Assert.AreEqual(marks.EnemyCount - marks.Killed.Count, level.Enemies.Count(e => !IsGone(e)), $"profondità {depth}: gli altri ci sono");
            if (marks.Opened > 0)
            {
                Assert.IsTrue(level.Chests[0].IsOpen, $"profondità {depth}: la cassa resta aperta");
            }

            CollectionAssert.AreEqual(marks.Ground, GroundNow(), $"profondità {depth}: le cose a terra dov'erano, senza doppioni");
            Assert.GreaterOrEqual(Object.FindFirstObjectByType<ExplorationTracker>().Exploration.ExploredCount, marks.Explored, $"profondità {depth}: l'automappa ricorda quello che si era visto");
        }

        [UnityTest, Timeout(300000), Description("Il livello di sopra torna com'era (D6): alla profondità 1 e dalle caverne alla cripta, dopo una discesa e una risalita i nemici uccisi non ci sono, la cassa è aperta, le cose a terra sono dov'erano e l'automappa ricorda quello che si era visto")]
        public IEnumerator Climb_LevelAboveIsAsItWas()
        {
            yield return LoadGeneratedCore();
            yield return WaitFor("Level_Crypt", 1);

            // alla 1 e alla 4: la 4 tocca anche il confine tra le caverne e la cripta
            for (int depth = 1; depth <= 4; depth++)
            {
                if (depth == 1 || depth == 4)
                {
                    var marks = new Marks();
                    yield return MarkTheLevel(marks);

                    StepOn(Manager.CurrentLevel.DownExits.First());
                    yield return WaitFor(SceneOf(depth + 1), depth + 1);
                    StepOn(Manager.CurrentLevel.UpExit);
                    yield return WaitFor(SceneOf(depth), depth);
                    yield return null;
                    yield return null;
                    CheckTheLevelIsAsItWas(marks, depth);
                }

                if (depth < 4)
                {
                    StepOn(Manager.CurrentLevel.DownExits.First());
                    yield return WaitFor(SceneOf(depth + 1), depth + 1);
                }
            }
        }
    }
}
