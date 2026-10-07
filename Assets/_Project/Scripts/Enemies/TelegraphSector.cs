using DarkDescent.Combat;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Il settore rosso a terra del colpo caricato (D6 della M7): appare quando il colpo parte, si
    /// riempie dal centro verso il bordo mentre carica, e sparisce quando il colpo finisce o viene
    /// annullato. Il bordo è la portata vera del colpo: chi ne è fuori alla fine non viene colpito.
    /// Il materiale è non illuminato, così si legge anche nel buio delle caverne (trappola 6).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeleeAttack))]
    public class TelegraphSector : MonoBehaviour
    {
        [Tooltip("Non illuminato e trasparente: il fondo del settore.")]
        [SerializeField] private Material _material;

        [Tooltip("Il riempimento che cresce durante la carica, più acceso del fondo.")]
        [SerializeField] private Material _fillMaterial;

        [Tooltip("Quanto si aggiunge al raggio per il corpo del bersaglio: il colpo arriva se il bordo del cavaliere è dentro.")]
        [SerializeField, Min(0f)] private float _targetAllowance = 0.45f;

        [Tooltip("Altezza sopra il pavimento: abbastanza per non sparire nei sassi della terra.")]
        [SerializeField, Min(0f)] private float _height = 0.08f;

        [SerializeField, Range(4, 64)] private int _segments = 24;

        private MeleeAttack _attack;
        private Transform _fill;
        private GameObject _sector;
        private float _elapsed;
        private float _duration;

        /// <summary>Il settore si vede: per i test.</summary>
        public bool IsShown => _sector.activeSelf;

        /// <summary>Il raggio del settore in metri, dal centro del bruto.</summary>
        public float Radius { get; private set; }

        public float Arc => _attack.Weapon.Arc;

        private void Awake()
        {
            _attack = GetComponent<MeleeAttack>();
            var weapon = _attack.Weapon;
            float ownRadius = TryGetComponent(out NavMeshAgent agent) ? agent.radius : 0f;
            Radius = ownRadius + weapon.Range + weapon.RangeTolerance + _targetAllowance;

            var mesh = BuildMesh(weapon.Arc, _segments);
            _sector = CreatePart("Telegraph", transform, mesh, _material);
            _sector.transform.localPosition = Vector3.up * _height;
            _sector.transform.localScale = new Vector3(Radius, 1f, Radius);
            _fill = CreatePart("Fill", _sector.transform, mesh, _fillMaterial).transform;
            _fill.localPosition = Vector3.up * 0.01f;
            _sector.SetActive(false);
        }

        private void OnEnable()
        {
            _attack.SwingStarted += Show;
            _attack.SwingEnded += Hide;
        }

        private void OnDisable()
        {
            _attack.SwingStarted -= Show;
            _attack.SwingEnded -= Hide;
            Hide();
        }

        private void Update()
        {
            if (!_sector.activeSelf)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float t = _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 1f;
            _fill.localScale = new Vector3(t, 1f, t);
        }

        private void Show()
        {
            // i colpi leggeri non si caricano: niente settore
            if (_attack.IsQuickSwing)
            {
                return;
            }

            _elapsed = 0f;
            _duration = _attack.Weapon.HitDelay;
            _fill.localScale = new Vector3(0f, 1f, 0f);
            _sector.SetActive(true);
        }

        private void Hide()
        {
            if (_sector != null)
            {
                _sector.SetActive(false);
            }
        }

        private static GameObject CreatePart(string name, Transform parent, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        // Un ventaglio di raggio 1 sul piano, davanti (+Z), largo arc gradi: la scala lo porta alla
        // portata. Uno per bruto, fatto in Awake: niente allocazioni durante il gioco.
        private static Mesh BuildMesh(float arc, int segments)
        {
            var vertices = new Vector3[segments + 2];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Deg2Rad * (-arc * 0.5f + arc * i / segments);
                vertices[i + 1] = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            }

            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            var mesh = new Mesh { name = "TelegraphSector", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
