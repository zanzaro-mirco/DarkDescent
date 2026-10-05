using System;
using System.Collections;
using DarkDescent.Interaction;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Una cassa che si apre con un click (D7 della scheda M6): il cavaliere ci va, il coperchio si
    /// alza, e davanti cade un oggetto della sua loot table, a volte con una pozione (D14). Una volta
    /// sola. Il seme viene dalla cella, come per i nemici (ADR-029): la stessa cassa dà lo stesso
    /// oggetto. La collega il CompositionRoot.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    public class Chest : MonoBehaviour
    {
        // la cella in decimetri, come LootDrop: la cassa non si muove, ma il seme resta della cella
        private const float CellsPerMeter = 10f;

        [SerializeField] private LootTable _lootTable;

        [SerializeField] private GroundItem _groundItemPrefab;

        [Tooltip("Il coperchio: ruota sul suo perno, che nel modello di KayKit è la cerniera dietro.")]
        [SerializeField] private Transform _lid;

        [Tooltip("Di quanto si alza il coperchio, in gradi.")]
        [SerializeField] private float _openAngle = 110f;

        [SerializeField, Min(0.05f)] private float _openDuration = 0.35f;

        [Tooltip("Il collider del click: si spegne quando la cassa è aperta, e il cursore non la vede più.")]
        [SerializeField] private Collider _clickArea;

        [Tooltip("Quanto davanti alla cassa cade l'oggetto.")]
        [SerializeField, Min(0f)] private float _dropDistance = 1.3f;

        [Tooltip("Quanto di lato all'oggetto cade la pozione, se c'è.")]
        [SerializeField, Min(0f)] private float _potionSideOffset = 0.7f;

        [SerializeField] private AudioSource _audio;

        [SerializeField] private AudioClip[] _openClips;

        private Interactable _interactable;
        private LootRoller _roller;
        private int _depth = 1;
        private int _cellX;
        private int _cellZ;

        /// <summary>La cassa si è aperta; l'argomento è l'oggetto caduto, null se non ne è caduto nessuno.</summary>
        public event Action<GroundItem> Opened;

        public bool IsOpen { get; private set; }

        public void Bind(LootRoller roller, int depth)
        {
            _roller = roller;
            _depth = depth;
        }

        /// <summary>Quello che lascerà aprendosi, senza cambiare niente: per i test e per rigiocare un seme.</summary>
        public ItemInstance Preview()
        {
            return _roller?.Roll(_lootTable, _depth, _cellX, _cellZ);
        }

        /// <summary>La pozione che lascerà in più aprendosi, senza cambiare niente; null se non ne lascia.</summary>
        public ItemInstance PreviewPotion()
        {
            return _roller?.RollPotion(_lootTable, _depth, _cellX, _cellZ);
        }

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            _cellX = Mathf.RoundToInt(transform.position.x * CellsPerMeter);
            _cellZ = Mathf.RoundToInt(transform.position.z * CellsPerMeter);
        }

        private void OnEnable()
        {
            _interactable.Used += HandleUsed;
        }

        private void OnDisable()
        {
            _interactable.Used -= HandleUsed;
        }

        private void HandleUsed(GameObject user)
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;

            // spento il click, il cursore passa oltre: niente più nome né evidenziazione
            _interactable.SetHighlighted(false);
            if (_clickArea != null)
            {
                _clickArea.enabled = false;
            }

            if (_audio != null && _openClips.Length > 0)
            {
                // la variante dalla cella, non da Random: la stessa cassa suona sempre uguale
                _audio.PlayOneShot(_openClips[Mathf.Abs(_cellX + _cellZ) % _openClips.Length]);
            }

            if (_lid != null)
            {
                StartCoroutine(OpenLid());
            }

            GroundItem dropped = null;
            Vector3 front = transform.position + transform.forward * _dropDistance;
            var item = Preview();
            if (item != null && _groundItemPrefab != null)
            {
                dropped = GroundItem.Spawn(_groundItemPrefab, item, front);
            }

            var potion = PreviewPotion();
            if (potion != null && _groundItemPrefab != null)
            {
                GroundItem.Spawn(_groundItemPrefab, potion, front + transform.right * _potionSideOffset);
            }

            Opened?.Invoke(dropped);
        }

        // Il davanti della cassa è il suo +Z: ruotando attorno a X in negativo il bordo davanti si alza.
        private IEnumerator OpenLid()
        {
            Quaternion closed = _lid.localRotation;
            Quaternion open = closed * Quaternion.Euler(-_openAngle, 0f, 0f);
            for (float t = 0f; t < _openDuration; t += Time.deltaTime)
            {
                _lid.localRotation = Quaternion.Slerp(closed, open, Mathf.SmoothStep(0f, 1f, t / _openDuration));
                yield return null;
            }

            _lid.localRotation = open;
        }
    }
}
