using System;
using DarkDescent.Characters;
using DarkDescent.Classes;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Magic;
using DarkDescent.Stats;
using UnityEngine;

namespace DarkDescent.Progression
{
    /// <summary>
    /// La classe del personaggio (D2 della M21). Un solo prefab del giocatore: <see cref="Apply"/> gli
    /// dà gli alberi (<see cref="PlayerTalents.SetClass"/>), il corpo (<see cref="PlayerBody"/>), il mana
    /// che torna più o meno in fretta e, a un personaggio nuovo, gli attributi di partenza. Va chiamato
    /// prima che pannello e salvataggio si colleghino all'albero, e prima di rimettere uno stato salvato
    /// (cambiare gli alberi lo azzera): dal composition root, dalla creazione del personaggio e dal
    /// salvataggio.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats), typeof(PlayerTalents))]
    public class PlayerClass : MonoBehaviour
    {
        [SerializeField] private CharacterClassCatalog _catalog;

        private CharacterClass _current;
        private float _armorLevel = -1f;

        /// <summary>Cambiata la classe o il nome: chi li mostra li rilegge.</summary>
        public event Action Changed;

        public CharacterClassCatalog Catalog => _catalog;

        /// <summary>La classe di adesso; il Guerriero finché non se ne è applicata un'altra.</summary>
        public CharacterClass Current => _current != null ? _current : _catalog != null ? _catalog.Warrior : null;

        /// <summary>Il nome scelto alla creazione; vuoto per un personaggio di prima, che ha quello di partenza.</summary>
        public string CharacterName { get; private set; } = "";

        /// <summary>
        /// Applica una classe e un nome. Con <paramref name="freshAttributes"/> gli attributi tornano quelli di
        /// partenza della classe (un personaggio nuovo); senza, restano com'erano (un salvataggio li rimette).
        /// </summary>
        public void Apply(CharacterClass characterClass, string characterName, bool freshAttributes)
        {
            if (characterClass == null)
            {
                throw new ArgumentNullException(nameof(characterClass));
            }

            _current = characterClass;
            CharacterName = characterName ?? "";

            var talents = GetComponent<PlayerTalents>();
            if (characterClass.ClassTree != null)
            {
                talents.SetClass(characterClass);
            }

            var body = GetComponentInChildren<PlayerBody>(true);
            if (body != null && !string.IsNullOrEmpty(characterClass.Body))
            {
                body.Show(characterClass.Body);
            }

            var magic = GetComponent<PlayerMagic>();
            if (magic != null)
            {
                magic.ManaRegenPerSecond = characterClass.ManaRegenPerSecond;

                // il colpo del sinistro: l'Attacco del Guerriero, il Dardo arcano del Mago (D3 della M21)
                magic.SetAttackSpell(characterClass.AttackSpell);
            }

            RefreshArmor();

            if (freshAttributes)
            {
                var sheet = GetComponent<CharacterStats>().Sheet;
                sheet.SetBase(StatType.Strength, characterClass.Strength);
                sheet.SetBase(StatType.Dexterity, characterClass.Dexterity);
                sheet.SetBase(StatType.Magic, characterClass.Magic);
                sheet.SetBase(StatType.Vitality, characterClass.Vitality);

                // prima l'equipaggiamento: il tomo dà del mana in più, e il pieno si conta dopo
                EquipKit(characterClass);

                // un personaggio nuovo comincia con vita e mana pieni, qualunque siano i suoi attributi
                var health = GetComponent<Health>();
                if (health != null)
                {
                    health.SetCurrent(health.Max);
                }

                if (magic != null)
                {
                    magic.Mana.Fill();
                }
            }

            Changed?.Invoke();
        }

        // l'Armatura di classe (D11 della M21), con il livello di adesso: una classe porta via quella dell'altra
        private void RefreshArmor()
        {
            var sheet = GetComponent<CharacterStats>().Sheet;
            _armorLevel = sheet.Get(StatType.Level);
            sheet.RemoveModifiersFrom(this);
            float armor = Current != null ? Current.ArmorAt(_armorLevel) : 0f;
            if (armor > 0f)
            {
                sheet.AddModifier(new StatModifier(StatType.Armor, ModifierKind.Flat, armor, this));
            }
        }

        // sale con i livelli
        private void Update()
        {
            if (_current != null && GetComponent<CharacterStats>().Sheet.Get(StatType.Level) != _armorLevel)
            {
                RefreshArmor();
            }
        }

        // l'equipaggiamento di un personaggio nuovo: dopo gli attributi, perché l'arma chiede la sua Magia. Quello che c'era
        // (la spada del prefab, che può già essere in mano o no) si lascia andare: il Mago non la porta
        private void EquipKit(CharacterClass characterClass)
        {
            var inventory = GetComponent<PlayerInventory>();
            if (inventory == null || characterClass.StartingWeapon == null)
            {
                return;
            }

            var equipment = inventory.Equipment;
            equipment.Unequip(EquipSlot.Weapon);
            equipment.Unequip(EquipSlot.Offhand);
            equipment.TryEquip(new ItemInstance(characterClass.StartingWeapon), out _);
            if (characterClass.StartingOffhand != null)
            {
                equipment.TryEquip(new ItemInstance(characterClass.StartingOffhand), out _);
            }
        }
    }
}
