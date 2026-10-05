using UnityEngine;

namespace DarkDescent.Interaction
{
    /// <summary>
    /// Come si vede un Interactable sotto il cursore: i suoi renderer passano a un materiale più
    /// luminoso e la sua luce, se ce l'ha, si alza. Sta accanto all'Interactable e ne ascolta l'evento.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    public class InteractableHighlight : MonoBehaviour
    {
        [Tooltip("I renderer da illuminare: possono stare fuori da questo oggetto (la scala, lo stendardo).")]
        [SerializeField] private Renderer[] _renderers;

        [Tooltip("Lo stesso materiale dei renderer, con l'emissione accesa.")]
        [SerializeField] private Material _highlightMaterial;

        [Tooltip("Facoltativa: una luce dell'oggetto che si alza durante l'evidenziazione.")]
        [SerializeField] private Light _light;

        [SerializeField, Min(1f)] private float _lightMultiplier = 2f;

        private Interactable _interactable;
        private Material[][] _originalMaterials;
        private Material[][] _highlightMaterials;
        private float _baseLightIntensity;

        public bool IsShowing { get; private set; }

        /// <summary>
        /// Per chi lo crea da codice, come il costruttore dei livelli: va chiamato prima che l'oggetto
        /// si accenda, perché Awake prepara i materiali da questi renderer.
        /// </summary>
        public void Configure(Renderer[] renderers, Material highlightMaterial, Light light)
        {
            _renderers = renderers;
            _highlightMaterial = highlightMaterial;
            _light = light;
        }

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();

            // gli array si preparano una volta, come per il lampo dei colpi: lo scambio non alloca.
            // Scambio di materiale e non _EMISSION acceso a runtime: in build quella variante dello
            // shader c'è solo se un materiale salvato la usa (trappola 8 della M2).
            _originalMaterials = new Material[_renderers.Length][];
            _highlightMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _originalMaterials[i] = _renderers[i].sharedMaterials;
                _highlightMaterials[i] = new Material[_originalMaterials[i].Length];
                for (int j = 0; j < _highlightMaterials[i].Length; j++)
                {
                    _highlightMaterials[i][j] = _highlightMaterial;
                }
            }

            if (_light != null)
            {
                _baseLightIntensity = _light.intensity;
            }
        }

        private void OnEnable()
        {
            _interactable.HighlightChanged += Show;
            Show(_interactable.IsHighlighted);
        }

        private void OnDisable()
        {
            _interactable.HighlightChanged -= Show;
            Show(false);
        }

        private void Show(bool highlighted)
        {
            if (highlighted == IsShowing)
            {
                return;
            }

            IsShowing = highlighted;
            var materials = highlighted ? _highlightMaterials : _originalMaterials;
            for (int i = 0; i < _renderers.Length; i++)
            {
                // un renderer di un'altra scena può essere già distrutto allo scaricamento
                if (_renderers[i] != null)
                {
                    _renderers[i].sharedMaterials = materials[i];
                }
            }

            if (_light != null)
            {
                _light.intensity = highlighted ? _baseLightIntensity * _lightMultiplier : _baseLightIntensity;
            }
        }
    }
}
