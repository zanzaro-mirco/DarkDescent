using DarkDescent.Combat;
using DarkDescent.Stats;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Guscio Unity di <see cref="Items.Inventory"/>: griglia, equipaggiamento e oggetto sul cursore
    /// del cavaliere. Tiene l'arma di MeleeAttack allineata a quella equipaggiata (senza arma, i
    /// pugni) e mette a terra gli oggetti lasciati.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats), typeof(MeleeAttack))]
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

        private MeleeAttack _attack;
        private Inventory _inventory;

        // Creato al primo accesso: modelli in mano e pannelli si iscrivono in OnEnable, e l'ordine
        // degli Awake tra componenti e oggetti non è garantito
        public Inventory Inventory => _inventory ??= new Inventory(
            new InventoryGrid(_gridSize.x, _gridSize.y),
            new Equipment(GetComponent<CharacterStats>().Sheet));

        public Equipment Equipment => Inventory.Equipment;

        public WeaponDefinition Unarmed => _unarmed;

        /// <summary>Raccoglie un oggetto nella griglia; false se non c'è posto.</summary>
        public bool TryPickUp(ItemInstance item)
        {
            return Inventory.TryPickUp(item);
        }

        /// <summary>Lascia a terra, ai piedi del cavaliere, l'oggetto sul cursore.</summary>
        public GroundItem DropHeld()
        {
            ItemInstance item = Inventory.ReleaseHeld();
            return item != null ? GroundItem.Spawn(_groundItemPrefab, item, transform.position) : null;
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
        }

        private void OnEnable()
        {
            Equipment.Changed += HandleEquipmentChanged;
        }

        private void OnDisable()
        {
            Equipment.Changed -= HandleEquipmentChanged;
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
        }

        private void HandleEquipmentChanged(EquipSlot slot)
        {
            if (slot == EquipSlot.Weapon)
            {
                var weapon = Equipment.Get(EquipSlot.Weapon)?.Definition as WeaponDefinition;
                _attack.SetWeapon(weapon != null ? weapon : _unarmed);
            }
        }
    }
}
