using DarkDescent.Combat;
using DarkDescent.Player;
using DarkDescent.Stats;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Guscio Unity di <see cref="Items.Inventory"/>: griglia, equipaggiamento, cintura e oggetto sul
    /// cursore del cavaliere. Tiene l'arma di MeleeAttack allineata a quella equipaggiata (senza arma,
    /// i pugni), il blocco allineato allo scudo, mette a terra gli oggetti lasciati e beve le pozioni.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats), typeof(MeleeAttack), typeof(Health))]
    public class PlayerInventory : MonoBehaviour
    {
        [Tooltip("Celle della griglia: 10 × 4 come Diablo 1 (D8 della M4).")]
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(10, 4);

        [Tooltip("Equipaggiata all'avvio.")]
        [SerializeField] private WeaponDefinition _startingWeapon;

        [Tooltip("L'arma usata a mani nude.")]
        [SerializeField] private WeaponDefinition _unarmed;

        [Tooltip("Il prefab degli oggetti a terra, per quelli lasciati dal cursore.")]
        [SerializeField] private GroundItem _groundItemPrefab;

        [Tooltip("Da che altezza cade un oggetto lasciato dal cursore: la mano.")]
        [SerializeField, Min(0f)] private float _dropHeight = 1.1f;

        [Tooltip("Messe nella cintura all'avvio (D14 della M6).")]
        [SerializeField] private PotionDefinition _startingPotion;

        [SerializeField, Range(0, Belt.Size)] private int _startingPotions = 2;

        private MeleeAttack _attack;
        private ShieldBlock _block;
        private Health _health;
        private PlayerInputReader _reader;
        private Inventory _inventory;

        // Creato al primo accesso: modelli in mano e pannelli si iscrivono in OnEnable, e l'ordine
        // degli Awake tra componenti e oggetti non è garantito
        public Inventory Inventory => _inventory ??= new Inventory(
            new InventoryGrid(_gridSize.x, _gridSize.y),
            new Equipment(GetComponent<CharacterStats>().Sheet));

        public Equipment Equipment => Inventory.Equipment;

        public WeaponDefinition Unarmed => _unarmed;

        public Belt Belt => Inventory.Belt;

        /// <summary>Raccoglie un oggetto nella griglia; false se non c'è posto.</summary>
        public bool TryPickUp(ItemInstance item)
        {
            return Inventory.TryPickUp(item);
        }

        /// <summary>Lascia a terra, ai piedi del cavaliere, l'oggetto sul cursore.</summary>
        public GroundItem DropHeld()
        {
            ItemInstance item = Inventory.ReleaseHeld();
            return item != null ? GroundItem.Spawn(_groundItemPrefab, item, transform.position, transform.position + Vector3.up * _dropHeight) : null;
        }

        /// <summary>Beve la pozione di un posto della cintura (da 0); false se è vuoto, se è morto o se ha la vita piena.</summary>
        public bool DrinkFromBelt(int slot)
        {
            return CanDrink(Belt.Get(slot)) && Drink(Inventory.TakePotionFromBelt(slot));
        }

        /// <summary>Beve la pozione che occupa una cella della griglia, con il click destro; false se non si può.</summary>
        public bool DrinkAt(Vector2Int cell)
        {
            return CanDrink(Inventory.Grid.ItemAt(cell)) && Drink(Inventory.TakePotionAt(cell));
        }

        // a vita piena la pozione resta dov'è: in Diablo si sprecherebbe, qui il tasto non fa niente
        private bool CanDrink(ItemInstance item)
        {
            return item?.Definition is PotionDefinition && !_health.IsDead && _health.Current < _health.Max;
        }

        private bool Drink(PotionDefinition potion)
        {
            return potion != null && _health.Heal(potion.HealAmount(_health.Max)) > 0f;
        }

        /// <summary>Chiudendo l'inventario l'oggetto sul cursore torna nella griglia, o a terra se non c'è posto.</summary>
        public void PutAwayHeld()
        {
            if (Inventory.Held != null && !Inventory.TryStoreHeld())
            {
                DropHeld();
            }
        }

        private void Awake()
        {
            _attack = GetComponent<MeleeAttack>();
            // facoltativo: senza, lo scudo dà solo Armatura
            _block = GetComponent<ShieldBlock>();
            _health = GetComponent<Health>();
            // facoltativo: senza, la cintura si usa solo con il mouse
            _reader = GetComponent<PlayerInputReader>();
        }

        private void OnEnable()
        {
            Equipment.Changed += HandleEquipmentChanged;
            if (_reader != null)
            {
                _reader.BeltSlotUsed += HandleBeltSlotUsed;
            }
        }

        private void OnDisable()
        {
            Equipment.Changed -= HandleEquipmentChanged;
            if (_reader != null)
            {
                _reader.BeltSlotUsed -= HandleBeltSlotUsed;
            }
        }

        private void HandleBeltSlotUsed(int slot)
        {
            DrinkFromBelt(slot);
        }

        // in Start e non in Awake: a questo punto tutti gli iscritti di OnEnable ascoltano già
        private void Start()
        {
            if (_startingWeapon != null && Equipment.Get(EquipSlot.Weapon) == null)
            {
                Equipment.TryEquip(new ItemInstance(_startingWeapon), out _);
            }
            else
            {
                HandleEquipmentChanged(EquipSlot.Weapon);
            }

            for (int i = 0; _startingPotion != null && i < _startingPotions; i++)
            {
                Belt.TryAdd(new ItemInstance(_startingPotion));
            }
        }

        private void HandleEquipmentChanged(EquipSlot slot)
        {
            if (slot == EquipSlot.Weapon)
            {
                var item = Equipment.Get(EquipSlot.Weapon);
                if (item?.Definition is WeaponDefinition weapon)
                {
                    _attack.SetWeapon(weapon, ItemStats.Sum(item, AffixEffect.WeaponDamagePercent));
                }
                else
                {
                    _attack.SetWeapon(_unarmed);
                }
            }
            else if (slot == EquipSlot.Offhand && _block != null)
            {
                var item = Equipment.Get(EquipSlot.Offhand);
                bool isShield = item?.Definition is ArmorDefinition;
                _block.SetShield(isShield, isShield ? ItemStats.ShieldBlock(item) : 0);
            }
        }
    }
}
