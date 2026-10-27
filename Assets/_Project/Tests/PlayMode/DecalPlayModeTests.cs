using System.Collections;
using System.Linq;
using DarkDescent.Levels;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class DecalPlayModeTests : SandboxFixture
    {
        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static IEnumerator Enter(string scene, int depth)
        {
            Manager.LoadLevel(scene, DungeonLevel.EntranceAt(depth), depth);
            for (float time = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel.Depth != depth; time += Time.unscaledDeltaTime)
            {
                Assert.Less(time, 15f, $"la profondità {depth} non si è caricata");
                yield return null;
            }
        }

        [UnityTest, Description("Cripta e caverne hanno le macchie della mappa in una mesh sola, appena sopra il pavimento, senza collider e fuori dai layer del NavMesh")]
        public IEnumerator Levels_HaveTheirDecals()
        {
            yield return LoadGeneratedCore();
            foreach (var (scene, depth) in new[] { ("Level_Crypt", 2), ("Level_Caves", 6) })
            {
                yield return Enter(scene, depth);
                var level = Manager.CurrentLevel;
                var decals = level.GetComponentInChildren<DecalLayer>();
                Assert.IsNotNull(decals, scene);
                int expected = DecalSpot.FromMap(level.Map).Count;
                Assert.Greater(expected, 0, scene);
                Assert.AreEqual(expected, decals.Count, scene);
                Assert.AreEqual(expected * 4, decals.Mesh.vertexCount, $"{scene}: un quadrato per macchia");
                Assert.AreEqual(1, decals.GetComponent<MeshRenderer>().sharedMaterials.Length, "un materiale solo");
                Assert.AreEqual(level.Tileset.DecalMaterial, decals.GetComponent<MeshRenderer>().sharedMaterial);
                Assert.IsEmpty(decals.GetComponentsInChildren<Collider>(), $"{scene}: niente collider");

                var surface = level.GetComponentInChildren<NavMeshSurface>();
                Assert.AreEqual(0, surface.layerMask & (1 << decals.gameObject.layer), $"{scene}: il NavMesh non le vede");

                // sopra il pavimento: il punto più alto delle piastrelle di base (le varianti hanno sassi sopra)
                var floorMesh = level.Tileset.Floor.GetComponentInChildren<MeshFilter>().sharedMesh;
                float floorTop = level.transform.Find("Floors").GetComponentsInChildren<MeshFilter>()
                    .Where(f => f.sharedMesh == floorMesh).Max(f => f.GetComponent<Renderer>().bounds.max.y);
                float lowest = decals.Mesh.vertices.Min(v => v.y);
                Assert.Greater(lowest, floorTop, $"{scene}: le macchie stanno sopra le piastrelle (pavimento a {floorTop})");
                Assert.Less(lowest, floorTop + 0.05f, $"{scene}: ma appoggiate");
                Debug.Log($"[DarkDescent] {scene}: pavimento a {floorTop:F3}, macchie a {lowest:F3}");
            }
        }
    }
}
