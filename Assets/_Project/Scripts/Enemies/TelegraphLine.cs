using UnityEngine;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// La striscia rossa a terra della carica del Custode (D8 della M24): parte dai suoi piedi, va nella direzione in cui caricherà e
    /// si riempie dall'inizio mentre il tempo passa. Gli stessi materiali del cerchio dell'anello del re. Chi decide quando è il Custode.
    /// </summary>
    [DisallowMultipleComponent]
    public class TelegraphLine : MonoBehaviour
    {
        [Tooltip("Non illuminato e trasparente: il fondo della striscia.")]
        [SerializeField] private Material _material;

        [Tooltip("Il riempimento che cresce durante il tempo.")]
        [SerializeField] private Material _fillMaterial;

        [Tooltip("Altezza sopra il pavimento: abbastanza per non sparire nei sassi della terra.")]
        [SerializeField, Min(0f)] private float _height = 0.09f;

        private Mesh _mesh;
        private Transform _strip;
        private Transform _fill;
        private float _elapsed;
        private float _duration;
        private float _length;

        /// <summary>La striscia si vede: per i test.</summary>
        public bool IsShown => _strip != null && _strip.gameObject.activeSelf;

        public float Length => _length;

        private void Awake()
        {
            // un rettangolo unitario con l'origine al centro del lato corto vicino al Custode: la lunghezza e la larghezza sono la scala
            _mesh = new Mesh { name = "TelegraphLine" };
            _mesh.vertices = new[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(0.5f, 0f, 1f), new Vector3(-0.5f, 0f, 1f) };
            _mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            _mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            _strip = TelegraphSector.CreatePart("Telegraph", null, _mesh, _material).transform;
            _fill = TelegraphSector.CreatePart("Fill", _strip, _mesh, _fillMaterial).transform;
            _fill.localPosition = Vector3.up * 0.01f;
            _strip.gameObject.SetActive(false);
        }

        // la mesh fatta in Awake non è della scena: se ne va con il Custode
        private void OnDestroy()
        {
            Destroy(_mesh);
            if (_strip != null)
            {
                Destroy(_strip.gameObject);
            }
        }

        /// <summary>Mostra la striscia da <paramref name="from"/> verso <paramref name="direction"/>, lunga e larga in metri veri, piena dopo <paramref name="duration"/> secondi.</summary>
        public void Show(Vector3 from, Vector3 direction, float length, float width, float duration)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.forward;
            }

            // fuori dal Custode (che è scalato e si gira): in coordinate del mondo
            _strip.position = new Vector3(from.x, from.y + _height, from.z);
            _strip.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            _strip.localScale = new Vector3(width, 1f, length);
            _length = length;
            _elapsed = 0f;
            _duration = duration;
            _fill.localScale = new Vector3(1f, 1f, 0f);
            _strip.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_strip != null)
            {
                _strip.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!IsShown)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float t = _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 1f;
            _fill.localScale = new Vector3(1f, 1f, t);
        }
    }
}
