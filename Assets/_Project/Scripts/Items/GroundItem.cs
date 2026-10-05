using DarkDescent.Interaction;
using DarkDescent.Localization;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un oggetto a terra: il suo modello disteso sul pavimento, una piccola luce per trovarlo nel
    /// buio, un collider per il click. Si clicca come le scale (un Interactable); raggiunto, va
    /// nell'inventario di chi lo usa, oppure resta e l'etichetta dice che non c'è posto. Il nome lo
    /// compone lui, nella lingua che l'HUD gli chiede.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable), typeof(BoxCollider))]
    public class GroundItem : MonoBehaviour, ILabelSource
    {
        [Tooltip("L'oggetto messo nella scena dalla mappa. Vuoto per quelli creati a runtime con Spawn.")]
        [SerializeField] private ItemDefinition _definition;

        [Tooltip("Dimensione minima del collider: un pugnale deve restare facile da cliccare.")]
        [SerializeField, Min(0.1f)] private float _minClickSize = 0.6f;

        private Interactable _interactable;
        private ItemInstance _item;
        private GameObject _model;
        private Light _glow;
        private bool _inventoryFull;

        public ItemInstance Item => _item;

        /// <summary>Il colore della luce a terra: quello della rarità.</summary>
        public Color GlowColor => _glow != null ? _glow.color : Color.clear;

        /// <summary>
        /// Mette a terra un oggetto vicino a <paramref name="near"/>, sul NavMesh: altrimenti potrebbe
        /// finire dentro un muro, dove il cavaliere non arriva.
        /// </summary>
        public static GroundItem Spawn(GroundItem prefab, ItemInstance item, Vector3 near)
        {
            Vector3 position = NavMesh.SamplePosition(near, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : near;

            // ruotato secondo il punto: due oggetti vicini non sembrano copiati e incollati, e niente Random
            float yaw = Mathf.Repeat(position.x * 37f + position.z * 53f, 360f);

            // Instantiate senza padre mette l'oggetto nella scena attiva, il livello: se ne va con lui
            var ground = Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f));
            ground.Show(item);
            return ground;
        }

        /// <summary>L'oggetto messo dalla mappa: per chi costruisce il livello, prima di Start.</summary>
        public void Configure(ItemDefinition definition)
        {
            _definition = definition;
        }

        public string GetLabel(Localizer localizer)
        {
            if (_item == null)
            {
                return string.Empty;
            }

            // il colore della rarità sul nome, come in Diablo; "(inventario pieno)" resta del colore dell'etichetta
            string name = "<color=" + RarityColors.TextHex(_item.Rarity) + ">" + ItemNamer.Name(_item, localizer) + "</color>";
            return _inventoryFull ? localizer.Format(TextKeys.InventoryFull, name) : name;
        }

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            _interactable.SetLabelSource(this);
            _glow = GetComponentInChildren<Light>(true);
        }

        private void Start()
        {
            // messo dalla mappa: l'istanza nasce qui
            if (_item == null && _definition != null)
            {
                Show(new ItemInstance(_definition));
            }
        }

        private void OnEnable()
        {
            _interactable.Used += HandleUsed;
        }

        private void OnDisable()
        {
            _interactable.Used -= HandleUsed;
        }

        private void Show(ItemInstance item)
        {
            _item = item;
            _inventoryFull = false;
            _interactable.NotifyLabelChanged();
            if (_glow != null)
            {
                _glow.color = RarityColors.Light(item.Rarity);
            }

            if (_model != null)
            {
                Destroy(_model);
            }

            // disteso: i modelli KayKit sono in piedi lungo +Y, con il davanti verso +Z
            _model = Instantiate(item.Definition.Model, transform, false);
            _model.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var renderers = _model.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            // centrato sull'oggetto e appoggiato al pavimento: i modelli KayKit hanno il perno
            // sull'impugnatura, e così click, etichetta e punto d'arrivo cadrebbero fuori dal modello
            Vector3 shift = new Vector3(transform.position.x - bounds.center.x, transform.position.y + 0.02f - bounds.min.y, transform.position.z - bounds.center.z);
            _model.transform.position += shift;
            bounds.center += shift;

            var box = GetComponent<BoxCollider>();
            Vector3 size = transform.InverseTransformVector(bounds.size);
            size = new Vector3(Mathf.Max(Mathf.Abs(size.x), _minClickSize), Mathf.Max(Mathf.Abs(size.y), 0.3f), Mathf.Max(Mathf.Abs(size.z), _minClickSize));
            box.center = transform.InverseTransformPoint(bounds.center);
            box.size = size;
        }

        private void HandleUsed(GameObject user)
        {
            if (_item == null || !user.TryGetComponent(out PlayerInventory inventory))
            {
                return;
            }

            if (inventory.TryPickUp(_item))
            {
                _item = null;
                Destroy(gameObject);
            }
            else
            {
                _inventoryFull = true;
                _interactable.NotifyLabelChanged();
            }
        }
    }
}
