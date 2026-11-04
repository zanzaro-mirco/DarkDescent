using System;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Magic;
using DarkDescent.Stats;
using DarkDescent.Talents;
using UnityEngine;

namespace DarkDescent.Classes
{
    /// <summary>
    /// Una classe di personaggio (D2 della M21): il Guerriero, il Mago, e domani il Ranger e il Ladro. È
    /// un dato: nome, modello, albero della classe e specializzazioni, attributi di partenza e come si
    /// spendono i punti, quanto in fretta torna il mana, l'abilità del tasto sinistro. Un solo prefab
    /// del giocatore: alla creazione si applica una classe (<c>PlayerClass</c>) e il resto si adatta.
    /// </summary>
    [CreateAssetMenu(menuName = "DarkDescent/Classe di personaggio", fileName = "Class")]
    public sealed class CharacterClass : ScriptableObject
    {
        /// <summary>L'id del Guerriero: la classe dei salvataggi di prima della M21.</summary>
        public const string WarriorId = "warrior";

        [Tooltip("L'id nel salvataggio: warrior, mage.")]
        [SerializeField] private string _id;

        [Tooltip("Chiave del nome nella tabella delle stringhe, come class.warrior; la descrizione aggiunge .desc, il nome di partenza .name.")]
        [SerializeField] private string _nameKey;

        [Tooltip("Quale corpo mostrare sul rig comune: knight, mage (PlayerBody).")]
        [SerializeField] private string _body;

        [SerializeField] private TalentTree _classTree;

        [SerializeField] private TalentTree[] _specializations = Array.Empty<TalentTree>();

        [Tooltip("Gli attributi di un personaggio nuovo.")]
        [SerializeField] private float _strength = 10f;
        [SerializeField] private float _dexterity = 10f;
        [SerializeField] private float _magic = 10f;
        [SerializeField] private float _vitality = 10f;

        [Tooltip("Come si spendono i punti attributo, a ogni livello, nella misura delle difficoltà e in -herolevel.")]
        [SerializeField] private StatType[] _pointPattern = Array.Empty<StatType>();

        [Tooltip("Quante volte il mana di tutti torna in un secondo: 1 è il 2% del massimo.")]
        [SerializeField, Min(0f)] private float _manaRegenFactor = 1f;

        [Tooltip("L'abilità del tasto sinistro: senza mana e senza ricarica (D18 della M20).")]
        [SerializeField] private SpellDefinition _attackSpell;

        [Tooltip("L'arma che un personaggio nuovo ha in mano al posto di quella del prefab: la bacchetta del Mago. Vuoto: resta quella del prefab.")]
        [SerializeField] private ItemDefinition _startingWeapon;

        [Tooltip("L'oggetto nella mano libera di un personaggio nuovo: il tomo del Mago.")]
        [SerializeField] private ItemDefinition _startingOffhand;

        [Tooltip("L'emblema della classe nella creazione del personaggio.")]
        [SerializeField] private Sprite _icon;

        public string Id => _id;
        public string NameKey => _nameKey;
        public string DescriptionKey => _nameKey + ".desc";
        public string DefaultNameKey => _nameKey + ".name";
        public string Body => _body;
        public TalentTree ClassTree => _classTree;
        public IReadOnlyList<TalentTree> Specializations => _specializations ?? Array.Empty<TalentTree>();
        public float Strength => _strength;
        public float Dexterity => _dexterity;
        public float Magic => _magic;
        public float Vitality => _vitality;
        public IReadOnlyList<StatType> PointPattern => _pointPattern ?? Array.Empty<StatType>();
        public float ManaRegenFactor => _manaRegenFactor;
        public SpellDefinition AttackSpell => _attackSpell;
        public ItemDefinition StartingWeapon => _startingWeapon;
        public ItemDefinition StartingOffhand => _startingOffhand;
        public Sprite Icon => _icon;

        /// <summary>Ha un albero: solo allora si può scegliere nella creazione del personaggio.</summary>
        public bool IsPlayable => _classTree != null;

        /// <summary>Il mana che torna ogni secondo, in parte del massimo.</summary>
        public float ManaRegenPerSecond => CombatFormulas.ManaRegenPerSecond * _manaRegenFactor;

        /// <summary>Una classe senza asset, per i test e per gli strumenti.</summary>
        public static CharacterClass Create(string id, string body, TalentTree classTree, IEnumerable<TalentTree> specializations,
            float strength, float dexterity, float magic, float vitality, IEnumerable<StatType> pointPattern, float manaRegenFactor = 1f,
            SpellDefinition attackSpell = null)
        {
            var created = CreateInstance<CharacterClass>();
            created.name = id;
            created.Define(id, "class." + id, body, classTree, specializations, strength, dexterity, magic, vitality, pointPattern, manaRegenFactor, attackSpell);
            return created;
        }

        /// <summary>Riempie i campi: lo strumento dell'editor e i test; poi si salva l'asset.</summary>
        public void Define(string id, string nameKey, string body, TalentTree classTree, IEnumerable<TalentTree> specializations,
            float strength, float dexterity, float magic, float vitality, IEnumerable<StatType> pointPattern, float manaRegenFactor,
            SpellDefinition attackSpell)
        {
            _id = id;
            _nameKey = nameKey;
            _body = body;
            _classTree = classTree;
            _specializations = (specializations ?? Enumerable.Empty<TalentTree>()).ToArray();
            _strength = strength;
            _dexterity = dexterity;
            _magic = magic;
            _vitality = vitality;
            _pointPattern = (pointPattern ?? Enumerable.Empty<StatType>()).ToArray();
            _manaRegenFactor = manaRegenFactor;
            _attackSpell = attackSpell;
        }

        /// <summary>Lo strumento dell'editor: l'emblema della classe.</summary>
        public void DefineIcon(Sprite icon)
        {
            _icon = icon;
        }

        /// <summary>Lo strumento dell'editor: l'equipaggiamento di partenza.</summary>
        public void DefineKit(ItemDefinition weapon, ItemDefinition offhand)
        {
            _startingWeapon = weapon;
            _startingOffhand = offhand;
        }
    }
}
