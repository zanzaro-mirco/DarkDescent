using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class SwarmTests : SandboxFixture
    {
        // Sei dello sciame a ovest, un cunicolo largo una cella, la stanza del cavaliere; a est, chiusi
        // nella roccia e lontani, altri tre che non devono svegliarsi.
        private const string Map = @"@depth 5
@entrance Start
#################
#ww.###....####w#
#www.......<###w#
#w..###....####w#
#################";

        private EnemyPack _pack;

        private static T Load<T>(string path) where T : Object
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }

        [UnityTearDown]
        public IEnumerator ReleasePack()
        {
            _pack?.Release();
            _pack = null;
            yield return null;
        }

        // Al posto della cripta, la mappa di prova: costruita, NavMesh cotto, nemici legati al cavaliere
        // con tiri che mancano sempre, così il cavaliere resta in piedi.
        private IEnumerator BuildTestLevel()
        {
            yield return LoadGeneratedCore();
            var crypt = SceneManager.GetSceneByName("Level_Crypt");
            yield return SceneManager.UnloadSceneAsync(crypt);
            var scene = SceneManager.CreateScene("SwarmTest");
            SceneManager.SetActiveScene(scene);

            var context = new LevelBuilder(Load<LevelTileset>("Assets/_Project/Data/Levels/CaveTileset.asset")).Build(LevelMap.Parse(Map), activateEnemies: false);
            var surface = context.GetComponentInChildren<NavMeshSurface>();
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();

            // il cavaliere resterebbe dove l'ha lasciato la cripta, che cambia a ogni avvio: a volte in
            // vista del gruppo ovest prima del test. Nella stanza a est, lontano da tutti
            PlaceKnight(9, 2);
            context.transform.Find(LevelBuilder.EnemiesGroup).gameObject.SetActive(true);

            var knight = Player.GetComponent<Health>();
            var misses = new FixedRandomSource(0.99);
            foreach (var enemy in context.Enemies)
            {
                enemy.Bind(knight);
                enemy.GetComponent<MeleeAttack>().SetRandomSource(misses);
            }

            _pack = new EnemyPack(context.Enemies);
            yield return null;
        }

        private void PlaceKnight(int x, int y)
        {
            PlayerAgent.enabled = false;
            Player.position = LevelMap.CellCenter(x, y);
            PlayerAgent.enabled = true;
        }

        private static List<EnemyAI> Group(bool west)
        {
            return Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None)
                .Where(e => (e.transform.position.x < 30f) == west).ToList();
        }

        [Test, Description("Lo sciame è lo scheletro più piccolo, più veloce e più fragile (D5): 1,6 volte la velocità, un terzo della vita, scala 0,85, colpi più deboli e più frequenti, in branco")]
        public void Swarm_IsTheSmallFastFragileSkeleton()
        {
            var skeleton = Load<GameObject>("Assets/_Project/Prefabs/Skeleton.prefab");
            var swarm = Load<GameObject>("Assets/_Project/Prefabs/Swarm.prefab");

            Assert.AreEqual(1.6f * skeleton.GetComponent<NavMeshAgent>().speed, swarm.GetComponent<NavMeshAgent>().speed, 0.01f);
            Assert.Less(swarm.GetComponent<NavMeshAgent>().radius, skeleton.GetComponent<NavMeshAgent>().radius, "più stretto, per i corridoi (trappola 5)");
            // dall'asset, non da un'istanza: la vita si calcola in Awake
            float MaxLife(GameObject prefab) => new UnityEditor.SerializedObject(prefab.GetComponent<Health>()).FindProperty("_maxHealth").floatValue;
            Assert.AreEqual(MaxLife(skeleton) / 3f, MaxLife(swarm), 0.01f);
            Assert.AreEqual(0.85f, swarm.transform.Find("Model").localScale.x, 0.001f);

            var skeletonWeapon = skeleton.GetComponent<MeleeAttack>().Weapon;
            var swarmWeapon = swarm.GetComponent<MeleeAttack>().Weapon;
            Assert.Less(swarmWeapon.MaxDamage, skeletonWeapon.MinDamage);
            Assert.Less(swarmWeapon.AttackInterval, skeletonWeapon.AttackInterval);

            Assert.Greater(swarm.GetComponent<EnemyAI>().Archetype.PackRadius, 0f);
            Assert.AreEqual(0f, skeleton.GetComponent<EnemyAI>().Archetype.PackRadius, "lo scheletro va per conto suo");
            Assert.IsNotNull(swarm.GetComponentInChildren<Animator>().runtimeAnimatorController, "si anima con il controller dello scheletro");
        }

        [UnityTest, Description("Uno dello sciame vede il cavaliere e sveglia il gruppo, anche chi è troppo lontano per vederlo; il gruppo chiuso a est resta fermo")]
        public IEnumerator Pack_WakesTogether()
        {
            yield return BuildTestLevel();
            var west = Group(west: true);
            var east = Group(west: false);
            Assert.AreEqual((6, 3), (west.Count, east.Count));
            Assert.IsTrue(west.All(e => e.State == EnemyState.Idle));

            int spotted = 0;
            void Count(EnemyAI _) => spotted++;
            foreach (var enemy in west)
            {
                enemy.Spotted += Count;
            }

            // all'imbocco del cunicolo: a portata d'occhio (8 m) ce ne sono al più due
            PlaceKnight(4, 2);
            for (float time = 0f; west.Any(e => e.State == EnemyState.Idle); time += Time.deltaTime)
            {
                Assert.Less(time, 2f, "il gruppo non si è svegliato tutto");
                yield return null;
            }

            foreach (var enemy in west)
            {
                enemy.Spotted -= Count;
            }

            Assert.That(spotted, Is.InRange(1, 2), "gli altri li ha svegliati il branco");
            Assert.IsTrue(east.All(e => e.State == EnemyState.Idle), "il branco lontano non sente");
        }

        [UnityTest, Description("Sei dello sciame passano tutti dal cunicolo largo una cella e arrivano al cavaliere, senza incastrarsi")]
        public IEnumerator Swarm_FlowsThroughANarrowCorridor()
        {
            yield return BuildTestLevel();
            var west = Group(west: true);
            PlaceKnight(4, 2);
            yield return new WaitForSeconds(1.5f);

            // il cavaliere passa dall'altra parte: per raggiungerlo si passa uno alla volta. Appena oltre
            // il cunicolo: lo sciame lascia chi è a più di 30 m da casa (seconda prova della build M7)
            PlaceKnight(8, 2);
            float elapsed = 0f;
            while (west.Any(e => Vector3.Distance(e.transform.position, Player.position) > 3f))
            {
                elapsed += Time.deltaTime;
                Assert.Less(elapsed, 12f, $"incastrati: {string.Join(", ", west.Where(e => Vector3.Distance(e.transform.position, Player.position) > 3f).Select(e => $"{e.name} a {Vector3.Distance(e.transform.position, Player.position):F1} m"))}");
                yield return null;
            }

            Debug.Log($"sei dello sciame dal cunicolo al cavaliere in {elapsed:F1} s");
        }
    }
}
