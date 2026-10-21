using System.Collections.Generic;
using DarkDescent.Combat;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Alla morte lascia a terra, davanti al corpo, quello che dice la sua loot table (D8 della M5).
    /// Il seme è quello della partita mescolato con la profondità e la cella di partenza: lo stesso
    /// nemico lascia lo stesso oggetto in qualunque ordine si uccida. Lo collega il CompositionRoot.
    /// Campioni e unici hanno un bonus (D7 della M12): oggetti in più attorno al corpo e più oro.
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

        [Tooltip("Di quanti gradi, dal davanti, cadono gli oggetti in più di campioni e unici.")]
        [SerializeField, Range(0f, 180f)] private float _extraAngle = 75f;

        // la cella di partenza in decimetri: il nemico si muove, il suo seme no
        private const float CellsPerMeter = 10f;

        // la scorta di un campione o di un unico lascia un oggetto con metà della probabilità (D13 della M13)
        private const float EscortChanceFactor = 0.5f;

        private Health _health;
        private LootRoller _roller;
        private int _depth = 1;
        private int _cellX;
        private int _cellZ;
        private LootBonus _bonus;
        private float _chanceFactor = 1f;

        // riusata a ogni morte: gli oggetti in più non creano liste nuove
        private readonly List<ItemInstance> _extra = new List<ItemInstance>();

        public LootTable LootTable => _lootTable;

        /// <summary>Il loot in più di campioni e unici, o null per un nemico normale.</summary>
        public LootBonus Bonus => _bonus;

        /// <summary>L'oggetto in più che porta, come la pergamena del livello (D5 della M9), o null.</summary>
        public ItemInstance Carried { get; private set; }

        /// <summary>Gli dà un oggetto in più da lasciare insieme al suo loot: la pergamena del livello.</summary>
        public void Carry(ItemInstance item)
        {
            Carried = item;
        }

        public void Bind(LootRoller roller, int depth, LootBonus bonus = null)
        {
            _roller = roller;
            _depth = depth;
            _bonus = bonus;
        }

        /// <summary>Quello che lascerà alla morte, senza cambiare niente: per i test e per rigiocare un seme.</summary>
        public ItemInstance Preview()
        {
            // campioni e unici lasciano solo i tiri in più (D13 della M13)
            if (_bonus != null && _bonus.ReplacesRoll)
            {
                return null;
            }

            return _roller?.Roll(_lootTable, _depth, _cellX, _cellZ, _chanceFactor);
        }

        /// <summary>Un nemico di scorta (D13 della M13): lascia un oggetto con metà della probabilità del suo tipo.</summary>
        public void MarkEscort()
        {
            _chanceFactor = EscortChanceFactor;
        }

        public bool IsEscort => _chanceFactor < 1f;

        /// <summary>La pozione che lascerà in più alla morte, senza cambiare niente; null se non ne lascia.</summary>
        public ItemInstance PreviewPotion()
        {
            return _roller?.RollPotion(_lootTable, _depth, _cellX, _cellZ);
        }

        /// <summary>L'oro che lascerà in più, senza cambiare niente; 0 se non ne lascia (D4 della M10).</summary>
        public int PreviewGold()
        {
            return _roller?.RollGold(_lootTable, _depth, _cellX, _cellZ, _bonus) ?? 0;
        }

        /// <summary>Gli oggetti in più che lascerà, in <paramref name="into"/>, senza cambiare niente; nessuno senza bonus.</summary>
        public void PreviewExtra(List<ItemInstance> into)
        {
            into.Clear();
            _roller?.RollExtra(_lootTable, _bonus, Preview(), _depth, _cellX, _cellZ, into);
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

            // la pergamena dall'altra parte: i tre oggetti non si coprono
            if (Carried != null)
            {
                GroundItem.Spawn(_groundItemPrefab, Carried, front - transform.right * _potionSideOffset, from);
                Carried = null;
            }

            // l'oro un passo più avanti dell'oggetto
            int gold = PreviewGold();
            if (gold > 0)
            {
                GroundItem.SpawnGold(_groundItemPrefab, _lootTable.Gold, gold, front + transform.forward * _potionSideOffset, from);
            }

            // gli oggetti in più ai lati del corpo, alternati destra e sinistra e sempre più indietro
            PreviewExtra(_extra);
            for (int i = 0; i < _extra.Count; i++)
            {
                float angle = (i % 2 == 0 ? 1f : -1f) * (_extraAngle + 40f * (i / 2));
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
                GroundItem.Spawn(_groundItemPrefab, _extra[i], transform.position + direction * (_forwardOffset + 0.3f), from);
            }

            _extra.Clear();
        }
    }
}
