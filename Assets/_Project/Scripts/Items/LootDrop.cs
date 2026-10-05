using DarkDescent.Combat;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Alla morte lascia a terra, davanti al corpo, quello che dice la sua loot table (D8 della M5).
    /// Il seme è quello della partita mescolato con la profondità e la cella di partenza: lo stesso
    /// nemico lascia lo stesso oggetto in qualunque ordine si uccida. Lo collega il CompositionRoot.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class LootDrop : MonoBehaviour
    {
        [SerializeField] private LootTable _lootTable;

        [SerializeField] private GroundItem _groundItemPrefab;

        [Tooltip("Quanto davanti al corpo cade l'oggetto: sotto il corpo non si vedrebbe e non si cliccherebbe.")]
        [SerializeField, Min(0f)] private float _forwardOffset = 0.9f;

        [Tooltip("Quanto di lato all'oggetto cade la pozione, se c'è: i due non si coprono.")]
        [SerializeField, Min(0f)] private float _potionSideOffset = 0.7f;

        [Tooltip("Da che altezza partono gli oggetti in volo.")]
        [SerializeField, Min(0f)] private float _dropHeight = 1f;

        // la cella di partenza in decimetri: il nemico si muove, il suo seme no
        private const float CellsPerMeter = 10f;

        private Health _health;
        private LootRoller _roller;
        private int _depth = 1;
        private int _cellX;
        private int _cellZ;

        public LootTable LootTable => _lootTable;

        public void Bind(LootRoller roller, int depth)
        {
            _roller = roller;
            _depth = depth;
        }

        /// <summary>Quello che lascerà alla morte, senza cambiare niente: per i test e per rigiocare un seme.</summary>
        public ItemInstance Preview()
        {
            return _roller?.Roll(_lootTable, _depth, _cellX, _cellZ);
        }

        /// <summary>La pozione che lascerà in più alla morte, senza cambiare niente; null se non ne lascia.</summary>
        public ItemInstance PreviewPotion()
        {
            return _roller?.RollPotion(_lootTable, _depth, _cellX, _cellZ);
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _cellX = Mathf.RoundToInt(transform.position.x * CellsPerMeter);
            _cellZ = Mathf.RoundToInt(transform.position.z * CellsPerMeter);
        }

        private void OnEnable()
        {
            _health.Died += HandleDied;
        }

        private void OnDisable()
        {
            _health.Died -= HandleDied;
        }

        private void HandleDied()
        {
            if (_groundItemPrefab == null)
            {
                return;
            }

            Vector3 front = transform.position + transform.forward * _forwardOffset;

            // gli oggetti escono dal corpo, all'altezza del petto
            Vector3 from = transform.position + Vector3.up * _dropHeight;
            var item = Preview();
            if (item != null)
            {
                GroundItem.Spawn(_groundItemPrefab, item, front, from);
            }

            var potion = PreviewPotion();
            if (potion != null)
            {
                GroundItem.Spawn(_groundItemPrefab, potion, front + transform.right * _potionSideOffset, from);
            }
        }
    }
}
