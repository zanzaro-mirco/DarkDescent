using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Le macchie a terra di un livello (D5 della M17) in una mesh sola, fatta dal <see cref="LevelBuilder"/>:
    /// un quadrato per macchia, un materiale con l'atlante. Niente ombre proiettate né collider; la mesh
    /// è sua e se ne va con il livello.
    /// </summary>
    [DisallowMultipleComponent]
    public class DecalLayer : MonoBehaviour
    {
        public const string ObjectName = "Decals";

        private Mesh _mesh;

        /// <summary>Quante macchie ci sono.</summary>
        public int Count { get; private set; }

        public Mesh Mesh => _mesh;

        public void Build(IReadOnlyList<DecalSpot> spots, Material material, float lift)
        {
            var vertices = new List<Vector3>(spots.Count * 4);
            var uvs = new List<Vector2>(spots.Count * 4);
            var triangles = new List<int>(spots.Count * 6);
            foreach (var spot in spots)
            {
                spot.AddQuad(vertices, uvs, triangles, lift, DecalPlanner.Columns, DecalPlanner.Rows);
            }

            var normals = new Vector3[vertices.Count];
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = Vector3.up;
            }

            _mesh = new Mesh { name = ObjectName };
            _mesh.SetVertices(vertices);
            _mesh.SetUVs(0, uvs);
            _mesh.SetNormals(normals);
            _mesh.SetTriangles(triangles, 0);
            _mesh.RecalculateBounds();
            Count = spots.Count;

            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = true;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private void OnDestroy()
        {
            // anche nei test in EditMode, dove i livelli si costruiscono e si buttano senza Play
            if (_mesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_mesh);
            }
            else
            {
                DestroyImmediate(_mesh);
            }
        }
    }
}
