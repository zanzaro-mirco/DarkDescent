using System.Collections;
using System.Linq;
using DarkDescent.Audio;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class CaveDescentTests : SandboxFixture
    {
        private const string CaveScene = "Level_Caves";

        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static LevelTileset CaveTileset => UnityEditor.AssetDatabase.LoadAssetAtPath<LevelTileset>("Assets/_Project/Data/Levels/CaveTileset.asset");

        private static IEnumerator Enter(string scene, int depth)
        {
            Manager.LoadLevel(scene, "FromAbove", depth);
            for (float time = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel.Depth != depth; time += Time.unscaledDeltaTime)
            {
                Assert.Less(time, 15f, $"la profondità {depth} non si è caricata");
                yield return null;
            }
        }

        [UnityTest, Description("Le caverne hanno il loro aspetto: terra, roccia crepata, macerie basse, candele al posto delle torce, luce e suono loro; nemici e cavaliere sul NavMesh")]
        public IEnumerator Caves_HaveTheirLook()
        {
            yield return LoadGeneratedCore();
            yield return Enter(CaveScene, 5);
            var level = Manager.CurrentLevel;
            var tileset = CaveTileset;

            Assert.AreEqual(CaveScene, SceneManager.GetActiveScene().name);
            Assert.AreEqual(tileset.AmbientColor, RenderSettings.ambientLight);
            Assert.AreSame(tileset.Ambience, Object.FindFirstObjectByType<AmbiencePlayer>().Current, "il suono delle caverne");
            Assert.IsNotNull(level.Map, "l'automappa ha la mappa delle caverne");

            var names = level.GetComponentsInChildren<Transform>().Select(t => t.name).ToList();
            Assert.That(names, Has.Some.StartsWith("CaveFloor"));
            Assert.That(names, Has.Some.StartsWith("CaveFloorRocky"), "anche la terra con i sassi");
            Assert.That(names, Has.Some.StartsWith("CaveWall"));
            Assert.That(names, Has.Some.StartsWith("CaveLowWall"));
            Assert.That(names, Has.None.StartsWith("WallTorch"), "niente torce nelle caverne");
            int candles = level.GetComponentsInChildren<Light>().Count(l => l.transform.parent.name.StartsWith("Candle"));
            Assert.Greater(candles, 3, "le candele fanno luce");
            int shadowLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l => l.shadows != LightShadows.None);
            Assert.AreEqual(1, shadowLights, "le ombre le fa solo la luce del cavaliere, non le candele");
            Assert.IsNotNull(Object.FindFirstObjectByType<UnityEngine.Rendering.Volume>(), "post-processing delle caverne");

            int skeletons = level.Map.Markers.Count(m => m.Symbol == DungeonPopulator.EnemySymbol);
            int swarm = level.Map.Markers.Count(m => m.Symbol == CavePopulator.SwarmSymbol);
            Assert.That(skeletons, Is.InRange(4, 6), "due gruppi di scheletri da 2 a 3 al 5 (D10)");
            Assert.That(swarm, Is.InRange(4, 6), "un gruppo di sciame al 5");
            int brutes = level.Map.Markers.Count(m => m.Symbol == CavePopulator.BruteSymbol);
            Assert.AreEqual(1, brutes, "un bruto al 5");
            Assert.AreEqual(skeletons + swarm + brutes, level.Enemies.Count);
            foreach (var enemy in level.Enemies)
            {
                Assert.IsTrue(enemy.GetComponent<NavMeshAgent>().isOnNavMesh, $"{enemy.name} fuori dal NavMesh");
            }

            Assert.IsTrue(PlayerAgent.isOnNavMesh);
            Assert.AreEqual(1, level.Exits.Count);
            Assert.AreEqual((CaveScene, 6), (level.Exits[0].TargetScene, level.Exits[0].TargetDepth));
        }

        [UnityTest, Description("Con lo stesso seme la stessa caverna torna uguale, nemici e oggetti compresi; con un altro seme no")]
        public IEnumerator SameSeed_SameCave()
        {
            yield return LoadGeneratedCore();
            Manager.RunSeed = 4711;
            yield return Enter(CaveScene, 6);
            string first = DungeonDescentTests.Signature(Manager.CurrentLevel);

            yield return Enter(CaveScene, 7);
            yield return Enter(CaveScene, 6);
            Assert.AreEqual(first, DungeonDescentTests.Signature(Manager.CurrentLevel));

            Manager.RunSeed = 4712;
            yield return Enter(CaveScene, 7);
            yield return Enter(CaveScene, 6);
            Assert.AreNotEqual(first, DungeonDescentTests.Signature(Manager.CurrentLevel));
        }
    }
}
