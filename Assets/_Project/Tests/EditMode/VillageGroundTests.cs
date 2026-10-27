using System.Linq;
using DarkDescent.Levels;
using DarkDescent.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Il terreno del borgo (M18, 18.2): la texture stesa nel mondo, l'erba e i sassi sparsi.</summary>
    public class VillageGroundTests
    {
        private const string TilesetPath = "Assets/_Project/Data/Levels/VillageTileset.asset";

        [Test, Description("La scelta dello sparso viene dalla cella: stessa cella, stesso numero; numeri tra 0 e 1, varianti dentro il conto")]
        public void Jitter_ChanceAndPickComeFromTheCell()
        {
            Assert.AreEqual(DecorJitter.Chance(7, 3, 11u), DecorJitter.Chance(7, 3, 11u));
            Assert.AreNotEqual(DecorJitter.Chance(7, 3, 11u), DecorJitter.Chance(7, 3, 12u), "un sale diverso, un numero diverso");

            int below = 0;
            for (int x = 0; x < 40; x++)
            {
                for (int y = 0; y < 40; y++)
                {
                    float chance = DecorJitter.Chance(x, y, 5u);
                    Assert.That(chance, Is.InRange(0f, 0.99999f));
                    below += chance < 0.5f ? 1 : 0;
                    Assert.That(DecorJitter.Pick(x, y, 6), Is.InRange(0, 5));
                }
            }

            Assert.That(below, Is.InRange(700, 900), "metà delle celle circa sotto 0,5");
        }

        [Test, Description("Le coordinate della texture sono la posizione nel mondo divisa per il passo: due piastrelle accanto continuano lo stesso terreno")]
        public void WorldUv_FollowsTheWorld()
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                tile.transform.position = new Vector3(8f, 0f, -4f);
                var world = tile.AddComponent<WorldUv>();
                world.Apply();

                var mesh = tile.GetComponent<MeshFilter>().sharedMesh;
                for (int i = 0; i < mesh.vertexCount; i++)
                {
                    Vector3 position = tile.transform.TransformPoint(mesh.vertices[i]);
                    Assert.AreEqual(new Vector2(position.x, position.z) / world.MetersPerRepeat, mesh.uv[i]);
                }
            }
            finally
            {
                Object.DestroyImmediate(tile);
            }
        }

        [Test, Description("Il borgo ha erba e sassi sparsi: fuori dal NavMesh, senza collider; il prato e la strada hanno la loro texture")]
        public void Tileset_ScattersGrassAndStones()
        {
            var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>(TilesetPath);
            Assert.That(tileset.Scatter.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(tileset.ScatterChance, Is.InRange(0.3f, 0.8f));
            foreach (var prefab in tileset.Scatter)
            {
                Assert.IsNotNull(prefab);
                Assert.IsTrue(prefab.GetComponentsInChildren<Transform>(true).All(t => t.gameObject.layer == 0), $"{prefab.name}: sul layer di base, fuori dal NavMesh");
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true), $"{prefab.name}: si attraversa");
            }

            foreach (var ground in new[] { tileset.Floor, tileset.Backdrop, tileset.GetMarkerPrefab(',') })
            {
                var renderer = ground.GetComponentInChildren<WorldUv>().GetComponent<Renderer>();
                Assert.IsNotNull(renderer.sharedMaterial.GetTexture("_BaseMap"), $"{ground.name}: il terreno ha la sua texture");
            }
        }
    }
}
