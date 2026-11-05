using System.Collections;
using System.Linq;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Le ronde (24.3 della M24, D7): nella stanza della sandbox, con le ronde che nei test di solito sono spente.</summary>
    public class PatrolTests : SandboxFixture
    {
        [TearDown]
        public void Restore()
        {
            EnemyBehaviours.PatrolEnabled = false;
            Time.timeScale = 1f;
        }

        // la profondità per cui la cella di questo nemico dà una ronda (l'hash è della cella e della profondità: si cerca una che gira)
        private static int DepthThatPatrols(EnemyAI ai)
        {
            Vector3 home = ai.HomePosition;
            int cellX = Mathf.RoundToInt(home.x / LevelMap.CellSize);
            int cellZ = Mathf.RoundToInt(-home.z / LevelMap.CellSize);
            for (int depth = 1; depth < 200; depth++)
            {
                var plan = PatrolPlan.For(depth, cellX, cellZ, ai.Archetype.PatrolShare, ai.Archetype.PatrolRadius);
                if (plan.Patrols)
                {
                    return depth;
                }
            }

            Assert.Fail("nessuna profondità dà una ronda a questa cella");
            return 0;
        }

        private IEnumerator Patrolling()
        {
            yield return LoadSandbox();
            EnemyBehaviours.PatrolEnabled = true;
        }

        private static EnemyAI Skeleton()
        {
            return GameObject.Find("Skeleton").GetComponent<EnemyAI>();
        }

        [UnityTest, Description("Uno su quattro: un nemico di ronda gira tra i suoi punti, entro 8 m da casa, a 60% del passo")]
        public IEnumerator APatroller_WalksBetweenItsPoints()
        {
            yield return Patrolling();
            var ai = Skeleton();
            var agent = ai.GetComponent<NavMeshAgent>();
            float chaseSpeed = agent.speed;
            ai.BindPatrol(DepthThatPatrols(ai));
            Assert.GreaterOrEqual(ai.PatrolPoints.Count, 2, "i punti stanno sul NavMesh");
            Assert.LessOrEqual(ai.PatrolPoints.Count, 3);
            foreach (var point in ai.PatrolPoints)
            {
                Assert.LessOrEqual(FlatDistance(point, ai.HomePosition), 8.01f, "entro 8 m da casa");
                Assert.IsTrue(NavMesh.SamplePosition(point, out _, 0.2f, NavMesh.AllAreas), "sul NavMesh");
            }

            yield return null;
            yield return null;
            Time.timeScale = 3f;
            var moved = new System.Collections.Generic.List<Vector3>();
            bool slowStep = false;
            Vector3 last = ai.transform.position;
            for (float t = 0f; t < 40f; t += Time.deltaTime)
            {
                Assert.AreEqual(EnemyState.Patrol, ai.State, "non smette di girare");
                Assert.LessOrEqual(FlatDistance(ai.transform.position, ai.HomePosition), 8.6f, "non esce dal raggio");
                if (agent.hasPath && Mathf.Approximately(agent.speed, chaseSpeed * 0.6f))
                {
                    slowStep = true;
                }

                if (FlatDistance(ai.transform.position, last) > 1.5f)
                {
                    last = ai.transform.position;
                    moved.Add(last);
                }

                yield return null;
            }

            Assert.IsTrue(slowStep, "cammina a 60% del passo dell'inseguimento");
            Assert.GreaterOrEqual(moved.Count, 3, "si muove tra i punti, non sta fermo");
        }

        [UnityTest, Description("Vede il cavaliere: insegue, e alla velocità di prima")]
        public IEnumerator APatroller_SeesTheKnight_AndChases()
        {
            yield return Patrolling();
            var ai = Skeleton();
            var agent = ai.GetComponent<NavMeshAgent>();
            float chaseSpeed = agent.speed;
            ai.BindPatrol(DepthThatPatrols(ai));
            Time.timeScale = 3f;
            yield return null;
            for (float t = 0f; t < 8f && !agent.hasPath; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.AreEqual(EnemyState.Patrol, ai.State);
            Assert.AreEqual(chaseSpeed * 0.6f, agent.speed, 0.01f);

            Time.timeScale = 1f;
            PlayerAgent.Warp(ai.transform.position + new Vector3(4f, 0f, 0f));
            for (float t = 0f; t < 2f && ai.State == EnemyState.Patrol; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.AreEqual(EnemyState.Chase, ai.State);
            Assert.AreEqual(chaseSpeed, agent.speed, 0.01f, "inseguendo torna al passo pieno");
        }

        [UnityTest, Description("Senza l'interruttore (i test) o senza la profondità legata (un evocato) il nemico aspetta fermo")]
        public IEnumerator WithoutTheSwitchOrTheDepth_NobodyPatrols()
        {
            yield return Patrolling();
            var ai = Skeleton();
            int depth = DepthThatPatrols(ai);

            // senza profondità: non è del livello, resta fermo
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                Assert.AreEqual(EnemyState.Idle, ai.State);
                yield return null;
            }

            Assert.AreEqual(0, ai.PatrolPoints.Count);

            // con la profondità ma l'interruttore spento
            ai.BindPatrol(depth);
            EnemyBehaviours.PatrolEnabled = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                Assert.AreEqual(EnemyState.Idle, ai.State);
                yield return null;
            }
        }

        [UnityTest, Description("Una cella senza ronda (tre su quattro) resta ferma anche con tutto acceso")]
        public IEnumerator ACellWithoutAPatrol_StaysStill()
        {
            yield return Patrolling();
            var ai = Skeleton();
            Vector3 home = ai.HomePosition;
            int cellX = Mathf.RoundToInt(home.x / LevelMap.CellSize);
            int cellZ = Mathf.RoundToInt(-home.z / LevelMap.CellSize);
            int depth = Enumerable.Range(1, 200).First(d => !PatrolPlan.For(d, cellX, cellZ, ai.Archetype.PatrolShare, ai.Archetype.PatrolRadius).Patrols);
            ai.BindPatrol(depth);
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                Assert.AreEqual(EnemyState.Idle, ai.State);
                yield return null;
            }

            Assert.AreEqual(0, ai.PatrolPoints.Count);
        }

        [UnityTest, Description("Tornato a casa (il cavaliere torna in vita) riprende la ronda, non resta fermo, e col passo della ronda")]
        public IEnumerator AfterGoingHome_ItPatrolsAgain()
        {
            yield return Patrolling();
            var ai = Skeleton();
            var agent = ai.GetComponent<NavMeshAgent>();
            float chaseSpeed = agent.speed;
            ai.BindPatrol(DepthThatPatrols(ai));
            ai.Alert();
            yield return null;
            Assert.AreEqual(EnemyState.Chase, ai.State);
            Assert.AreEqual(chaseSpeed, agent.speed, 0.01f);

            ai.ReturnHome();
            yield return null;
            yield return null;
            Assert.AreEqual(EnemyState.Patrol, ai.State, "dopo il ritorno, di nuovo di ronda");

            Time.timeScale = 3f;
            for (float t = 0f; t < 8f && !agent.hasPath; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.AreEqual(chaseSpeed * 0.6f, agent.speed, 0.01f);
        }

        [UnityTest, Description("Rallentato di ronda (gelo del Mago) non si rallenta due volte: finita la ronda la velocità è quella rallentata dell'inseguimento, e a rallentamento finito quella di prima")]
        public IEnumerator ASlowedPatroller_IsNotSlowedTwice()
        {
            yield return Patrolling();
            var ai = Skeleton();
            var agent = ai.GetComponent<NavMeshAgent>();
            float chaseSpeed = agent.speed;
            ai.BindPatrol(DepthThatPatrols(ai));
            Time.timeScale = 3f;
            for (float t = 0f; t < 8f && !agent.hasPath; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.AreEqual(EnemyState.Patrol, ai.State);
            Time.timeScale = 1f;
            Assert.IsTrue(ai.Slow(0.5f, 3f));
            yield return null;
            yield return null;
            Assert.AreEqual(EnemyState.Chase, ai.State, "il gelo lo sveglia");
            Assert.AreEqual(chaseSpeed * 0.5f, agent.speed, 0.02f, "una volta sola il 50%");

            yield return new WaitForSeconds(3.3f);
            Assert.IsFalse(ai.IsSlowed);
            Assert.AreEqual(chaseSpeed, agent.speed, 0.02f, "finito il gelo, il passo di prima");
        }
    }
}
