using System;
using System.Collections;
using DarkDescent.Interaction;
using DarkDescent.Localization;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un oggetto a terra: il suo modello disteso sul pavimento, una piccola luce per trovarlo nel
    /// buio, un collider per il click. Si clicca come le scale (un Interactable); raggiunto, va
    /// nell'inventario di chi lo usa, oppure resta e l'HUD dice per un momento che non c'è posto. Il nome lo
    /// compone lui, nella lingua che l'HUD gli chiede. Un oggetto che cade da un nemico, da una cassa o
    /// dalla mano vola fin qui con una capriola: si muove solo il modello, mentre click, etichetta e
    /// punto d'arrivo stanno già dove si posa.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable), typeof(BoxCollider))]
    public class GroundItem : MonoBehaviour, ILabelSource
    {
        [Tooltip("L'oggetto messo nella scena dalla mappa. Vuoto per quelli creati a runtime con Spawn.")]
        [SerializeField] private ItemDefinition _definition;

        [Tooltip("Dimensione minima del collider: un pugnale deve restare facile da cliccare.")]
        [SerializeField, Min(0.1f)] private float _minClickSize = 0.6f;

        [Tooltip("Quanto dura la caduta, rimbalzo compreso.")]
        [SerializeField, Min(0.05f)] private float _dropDuration = 0.55f;

        [Tooltip("Quanto sale l'arco sopra la retta tra partenza e arrivo.")]
        [SerializeField, Min(0f)] private float _arcHeight = 1.1f;

        [SerializeField, Min(0f)] private float _bounceHeight = 0.12f;

        [Tooltip("Giri della capriola in volo.")]
        [SerializeField, Min(0f)] private float _spinTurns = 1f;

        [Tooltip("Il suono quando tocca terra.")]
        [SerializeField] private AudioSource _audio;

        [SerializeField] private AudioClip[] _metalClips;

        [Tooltip("Per le pozioni.")]
        [SerializeField] private AudioClip[] _glassClips;

        private Interactable _interactable;
        private ItemInstance _item;
        private GameObject _model;
        private Light _glow;
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private Vector3 _restCenter;

        /// <summary>Tocca terra: la luce si accende e si sente il suono.</summary>
        public event Action Landed;

        public ItemInstance Item => _item;

        public bool IsFlying { get; private set; }

        /// <summary>Il colore della luce a terra: quello della rarità.</summary>
        public Color GlowColor => _glow != null ? _glow.color : Color.clear;

        /// <summary>
        /// Mette a terra un oggetto vicino a <paramref name="near"/>, sul NavMesh: altrimenti potrebbe
        /// finire dentro un muro, dove il cavaliere non arriva.
        /// </summary>
        public static GroundItem Spawn(GroundItem prefab, ItemInstance item, Vector3 near)
        {
            return Spawn(prefab, item, near, null);
        }

        /// <summary>Come l'altro, ma l'oggetto arriva in volo da <paramref name="from"/>: il centro del modello parte da lì.</summary>
        public static GroundItem Spawn(GroundItem prefab, ItemInstance item, Vector3 near, Vector3? from)
        {
            Vector3 position = NavMesh.SamplePosition(near, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : near;

            // ruotato secondo il punto: due oggetti vicini non sembrano copiati e incollati, e niente Random
            float yaw = Mathf.Repeat(position.x * 37f + position.z * 53f, 360f);

            // Instantiate senza padre mette l'oggetto nella scena attiva, il livello: se ne va con lui
            var ground = Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f));
            ground.Show(item);
            if (from.HasValue)
            {
                ground.StartCoroutine(ground.Fly(from.Value));
            }

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

            // il colore della rarità sul nome, come in Diablo
            return "<color=" + RarityColors.TextHex(_item.Rarity) + ">" + ItemNamer.Name(_item, localizer) + "</color>";
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

            _restPosition = _model.transform.localPosition;
            _restRotation = _model.transform.localRotation;
            _restCenter = box.center;
        }

        private IEnumerator Fly(Vector3 from)
        {
            IsFlying = true;
            if (_glow != null)
            {
                _glow.enabled = false;
            }

            Vector3 landing = transform.TransformPoint(_restCenter);
            for (float time = 0f; time < _dropDuration; time += Time.deltaTime)
            {
                Pose(from, landing, time / _dropDuration);
                yield return null;
            }

            Pose(from, landing, 1f);
            IsFlying = false;
            if (_glow != null)
            {
                _glow.enabled = true;
            }

            PlayLandingSound();
            Landed?.Invoke();
        }

        // la capriola gira attorno al centro del modello, non al suo perno sull'impugnatura
        private void Pose(Vector3 from, Vector3 landing, float t)
        {
            Vector3 offset = DropArc.Position(from, landing, _arcHeight, _bounceHeight, t) - landing;
            Quaternion spin = Quaternion.AngleAxis(DropArc.Spin(_spinTurns, t), Vector3.right);
            _model.transform.localPosition = _restCenter + spin * (_restPosition - _restCenter) + transform.InverseTransformVector(offset);
            _model.transform.localRotation = spin * _restRotation;
        }

        private void PlayLandingSound()
        {
            var clips = _item != null && _item.Definition is PotionDefinition ? _glassClips : _metalClips;
            if (_audio == null || clips == null || clips.Length == 0)
            {
                return;
            }

            // la variante dal punto, non da Random, come per le casse
            Vector3 p = transform.position;
            _audio.PlayOneShot(clips[Mathf.Abs(Mathf.RoundToInt(p.x * 10f) + Mathf.RoundToInt(p.z * 10f)) % clips.Length]);
        }

        private void HandleUsed(GameObject user)
        {
            if (_item == null || !user.TryGetComponent(out PlayerInventory inventory))
            {
                return;
            }

            // senza posto resta a terra: lo dice l'HUD, ascoltando l'inventario (seconda prova della M7)
            if (inventory.TryPickUp(_item))
            {
                _item = null;
                Destroy(gameObject);
            }
        }
    }
}
