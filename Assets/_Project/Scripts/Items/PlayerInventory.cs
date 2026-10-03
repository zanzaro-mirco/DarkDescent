using DarkDescent.Combat;
using DarkDescent.Stats;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Guscio Unity di quello che il cavaliere porta: per ora l'equipaggiamento, dal passo 4.4
    /// anche la griglia dell'inventario. Tiene l'arma di MeleeAttack allineata a quella
    /// equipaggiata; senza arma, i pugni.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats), typeof(MeleeAttack))]
    public class PlayerInventory : MonoBehaviour
    {
        [Tooltip("Equipaggiata all'avvio.")]
        [SerializeField] private WeaponDefinition _startingWeapon;

        [Tooltip("L'arma usata a mani nude.")]
        [SerializeField] private WeaponDefinition _unarmed;

        private MeleeAttack _attack;
        private Equipment _equipment;

        // Creata al primo accesso: i modelli in mano e i pannelli si iscrivono in OnEnable, e
        // l'ordine degli Awake tra componenti non è garantito
        public Equipment Equipment => _equipment ??= new Equipment(GetComponent<CharacterStats>().Sheet);

        public WeaponDefinition Unarmed => _unarmed;

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
