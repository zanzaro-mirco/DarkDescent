using DarkDescent.Combat;
using UnityEngine;

namespace DarkDescent.Characters
{
    /// <summary>
    /// Lampo bianco sul personaggio colpito: per un istante tutti i renderer del modello, arma
    /// compresa, passano a un materiale bianco unlit. Sta sul modello e ascolta la Health del parent.
    /// </summary>
    [DisallowMultipleComponent]
    public class HitFlash : MonoBehaviour
    {
        [Tooltip("Materiale bianco unlit usato per il lampo.")]
        [SerializeField] private Material _flashMaterial;

        [Tooltip("Durata del lampo in secondi reali: deve vedersi anche durante l'hit stop.")]
        [SerializeField, Min(0f)] private float _duration = 0.1f;

        [Tooltip("Vuoto = Health del parent, risolta in Awake.")]
        [SerializeField] private Health _health;

        private Renderer[] _renderers;
        private Material[][] _originalMaterials;
        private Material[][] _flashMaterials;
        private float _remaining;

        public bool IsFlashing => _remaining > 0f;

        private void Awake()
        {
            if (_health == null)
            {
                _health = GetComponentInParent<Health>();
            }

            // gli array si preparano una volta, così il lampo non alloca a ogni colpo.
            // Scambio di materiale e non _BaseColor bianco: su URP Lit il colore base moltiplica la
            // texture e non schiarisce (trappola 8). E non l'emissione: i materiali KayKit stanno
            // dentro l'FBX, nessuno usa _EMISSION, e in build quella variante dello shader viene tolta.
            _renderers = GetComponentsInChildren<Renderer>(true);
            _originalMaterials = new Material[_renderers.Length][];
            _flashMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _originalMaterials[i] = _renderers[i].sharedMaterials;
                _flashMaterials[i] = new Material[_originalMaterials[i].Length];
                for (int j = 0; j < _flashMaterials[i].Length; j++)
                {
                    _flashMaterials[i][j] = _flashMaterial;
                }
            }
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.Damaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= HandleDamaged;
            }

            if (IsFlashing)
            {
                Restore();
            }
        }

        private void HandleDamaged(DamageInfo info, float applied)
        {
            if (!IsFlashing)
            {
                Apply(_flashMaterials);
            }

            _remaining = _duration;
        }

        private void Update()
        {
            if (!IsFlashing)
            {
                return;
            }

            _remaining -= Time.unscaledDeltaTime;
            if (_remaining <= 0f)
            {
                Restore();
            }
        }

        private void Restore()
        {
            _remaining = 0f;
            Apply(_originalMaterials);
        }

        private void Apply(Material[][] materials)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].sharedMaterials = materials[i];
            }
        }
    }
}
