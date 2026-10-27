using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Le coordinate della texture prese dalla posizione nel mondo (M18, 18.2): le piastrelle da 4 m del
    /// borgo e il prato oltre il bordo fanno un terreno solo, che si ripete ogni
    /// <see cref="MetersPerRepeat"/> metri invece che a ogni cella. Ogni piastrella ha la sua copia
    /// della mesh, fatta quando si accende: a livello costruito, nel posto in cui resta.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    public class WorldUv : MonoBehaviour
    {
        [Tooltip("Ogni quanti metri la texture si ripete.")]
        [SerializeField, Min(1f)] private float _metersPerRepeat = 32f;

        private Mesh _mesh;

        public float MetersPerRepeat => _metersPerRepeat;

        /// <summary>Rifà le coordinate dalla posizione di adesso. Anche per le foto dall'editor, dove Awake non parte.</summary>
        public void Apply()
        {
            var filter = GetComponent<MeshFilter>();
            if (_mesh == null)
            {
                _mesh = Instantiate(filter.sharedMesh);
                _mesh.name = filter.sharedMesh.name + " (mondo)";
                _mesh.hideFlags = HideFlags.DontSave;
                filter.sharedMesh = _mesh;
            }

            var vertices = _mesh.vertices;
            var uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 world = transform.TransformPoint(vertices[i]);
                uv[i] = new Vector2(world.x, world.z) / _metersPerRepeat;
            }

            _mesh.uv = uv;
        }

        private void Awake()
        {
            Apply();
        }

        private void OnDestroy()
        {
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
