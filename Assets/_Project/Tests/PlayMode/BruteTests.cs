using System.Collections;
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
    public class BruteTests : SandboxFixture
    {
        // Una stanza: il bruto al centro guarda a sud, il cavaliere arriva una cella più in basso.
        private const string Map = @"@depth 5
@entrance Start
#######
#.....#
#..B..#
#.....#
#..<..#
#######";

        private EnemyAI _brute;
        private TelegraphSector _sector;
        private Health _knight;

        private static T Load<T>(string path) where T : Object
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private IEnumerator BuildTestLevel(IRandomSource rolls)
        {
            yield return LoadGeneratedCore();
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("Level_Crypt"));
            SceneManager.SetActiveScene(SceneManager.CreateScene("BruteTest"));

            var context = new LevelBuilder(Load<LevelTileset>("Assets/_Project/Data/Levels/CaveTileset.asset")).Build(LevelMap.Parse(Map), activateEnemies: false);
            var surface = context.GetComponentInChildren<NavMeshSurface>();
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            context.transform.Find(LevelBuilder.EnemiesGroup).gameObject.SetActive(true);

            _knight = Player.GetComponent<Health>();
            _brute = context.Enemies.Single();
            _brute.Bind(_knight);
            _brute.GetComponent<MeleeAttack>().SetRandomSource(rolls);
            _sector = _brute.GetComponent<TelegraphSector>();

            PlayerAgent.enabled = false;
            Player.position = LevelMap.CellCenter(3, 3);
            PlayerAgent.enabled = true;
            yield return null;
        }

        private IEnumerator WaitForState(EnemyState state, float timeout)
        {
            for (float time = 0f; _brute.State != state; time += Time.deltaTime)
            {
                Assert.Less(time, timeout, $"il bruto è in {_brute.State}, non in {state}");
                yield return null;
            }
        }

        [Test, Description("Il bruto è lo scheletro grosso, lento e resistente (D6): 0,6 volte la velocità, tre volte la vita, scala 1,3, colpi forti in un settore davanti, con la carica e il recupero")]
        public void Brute_IsTheBigSlowToughSkeleton()
        {
            var skeleton = Load<GameObject>("Assets/_Project/Prefabs/Skeleton.prefab");
            var brute = Load<GameObject>("Assets/_Project/Prefabs/Brute.prefab");
            float MaxLife(GameObject prefab) => new UnityEditor.SerializedObject(prefab.GetComponent<Health>()).FindProperty("_maxHealth").floatValue;

            Assert.AreEqual(0.6f * skeleton.GetComponent<NavMeshAgent>().speed, brute.GetComponent<NavMeshAgent>().speed, 0.01f);
            Assert.AreEqual(3f * MaxLife(skeleton), MaxLife(brute), 0.01f);
            Assert.AreEqual(1.3f, brute.transform.Find("Model").localScale.x, 0.001f);

            var weapon = brute.GetComponent<MeleeAttack>().Weapon;
            Assert.Greater(weapon.MinDamage, skeleton.GetComponent<MeleeAttack>().Weapon.MaxDamage);
            Assert.AreEqual(0.9f, weapon.HitDelay, 0.001f, "la carica");
            Assert.Less(weapon.Arc, 180f, "un settore davanti, non tutto attorno");
            Assert.AreEqual(0.6f, brute.GetComponent<EnemyAI>().Archetype.RecoverTime, 0.001f, "il recupero");
            Assert.IsNotNull(brute.GetComponent<TelegraphSector>());
            Assert.AreEqual(360f, skeleton.GetComponent<MeleeAttack>().Weapon.Arc, "lo scheletro colpisce come prima");
        }

        [UnityTest, Description("Il cavaliere passa dietro il bruto mentre carica: il colpo va a vuoto, il settore sparisce e il bruto resta fermo nel recupero")]
        public IEnumerator Dodge_StepOutOfTheSector()
        {
            yield return BuildTestLevel(new FixedRandomSource(0.0));
            float life = _knight.Current;
            Assert.IsFalse(_sector.IsShown);

            yield return WaitForState(EnemyState.WindUp, 5f);
            Assert.IsTrue(_sector.IsShown, "caricando il settore si vede");
            float radius = _sector.Radius;
            Assert.LessOrEqual(Vector3.Distance(Flat(_brute.transform.position), Flat(Player.position)), radius, "il cavaliere è nel settore");

            // dietro il bruto, alla stessa distanza: dentro la portata, fuori dall'arco
            Vector3 behind = _brute.transform.position - _brute.transform.forward * 1.6f;
            PlayerAgent.Warp(behind);

            yield return WaitForState(EnemyState.Recover, 2f);
            Assert.AreEqual(life, _knight.Current, "fuori dal settore non si viene colpiti");
            Assert.IsFalse(_sector.IsShown, "finito il colpo il settore sparisce");

            Vector3 still = _brute.transform.position;
            Quaternion facing = _brute.transform.rotation;
            yield return new WaitForSeconds(0.45f);
            Assert.AreEqual(EnemyState.Recover, _brute.State, "nel recupero resta fermo");
            Assert.Less(Vector3.Distance(still, _brute.transform.position), 0.05f);
            Assert.Less(Quaternion.Angle(facing, _brute.transform.rotation), 1f, "e non si gira");

            yield return WaitForState(EnemyState.Chase, 1f);
        }

        [UnityTest, Description("Chi resta nel settore alla fine della carica prende il colpo, e forte")]
        public IEnumerator StayInside_GetHit()
        {
            // i tiri in ciclo: quanti colpi leggeri dopo questo (uno), colpisce, non bloccato, danno minimo
            yield return BuildTestLevel(new FixedRandomSource(0.0, 0.0, 0.99, 0.0));
            float life = _knight.Current;
            yield return WaitForState(EnemyState.WindUp, 5f);
            yield return WaitForState(EnemyState.Recover, 2f);

            float damage = life - _knight.Current;
            Assert.GreaterOrEqual(damage, _brute.GetComponent<MeleeAttack>().Weapon.MinDamage, "il colpo del bruto è arrivato");
        }

        [UnityTest, Description("Dopo il colpo forte il bruto dà colpi leggeri, senza carica né settore, e poi di nuovo uno forte (prova della M7)")]
        public IEnumerator AfterTheHeavy_QuickSwings()
        {
            // 0,0 al tiro dei colpi leggeri: uno solo tra due colpi forti
            yield return BuildTestLevel(new FixedRandomSource(0.0));
            var attack = _brute.GetComponent<MeleeAttack>();
            yield return WaitForState(EnemyState.WindUp, 5f);
            Assert.IsFalse(attack.IsQuickSwing, "il primo è forte");
            yield return WaitForState(EnemyState.Recover, 2f);
            yield return WaitForState(EnemyState.Attack, 2f);

            for (float time = 0f; !attack.IsSwinging; time += Time.deltaTime)
            {
                Assert.Less(time, 3f, "il colpo leggero non parte");
                yield return null;
            }

            Assert.IsTrue(attack.IsQuickSwing, "dopo il forte, uno leggero");
            Assert.IsFalse(_sector.IsShown, "senza settore");
            for (float time = 0f; attack.IsSwinging; time += Time.deltaTime)
            {
                Assert.AreEqual(EnemyState.Attack, _brute.State, "senza carica");
                Assert.Less(time, 1f);
                yield return null;
            }

            yield return WaitForState(EnemyState.WindUp, 4f);
            Assert.IsTrue(_sector.IsShown, "poi di nuovo il colpo forte, con il settore");
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
