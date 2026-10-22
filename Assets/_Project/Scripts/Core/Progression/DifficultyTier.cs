using System;
using DarkDescent.Items;
using UnityEngine;

namespace DarkDescent.Progression
{
    /// <summary>
    /// Un gradino della difficoltà (D2, D5, D9 e D11 della M14): quanto si alza il livello della zona,
    /// quanto crescono i nemici, quanto rende il loot e quanto costa morire. Il Crepuscolo ha tutto a
    /// zero e a uno: è il gioco di prima.
    /// </summary>
    [Serializable]
    public sealed class DifficultyTier
    {
        [Tooltip("La chiave del nome nella tabella delle stringhe.")]
        [SerializeField] private string _nameKey = string.Empty;

        [Tooltip("Il colore del cristallo dell'altare e del nome nel pannello.")]
        [SerializeField] private Color _color = Color.white;

        [Header("Nemici (D5)")]
        [Tooltip("Livelli della zona in più: decide il livello degli oggetti e la riduzione dell'esperienza.")]
        [SerializeField, Min(0)] private int _zoneLevelBonus;

        [SerializeField, Min(1f)] private float _life = 1f;

        [SerializeField, Min(1f)] private float _damage = 1f;

        [Tooltip("Punti a colpire in più dei nemici, contro l'Armatura del cavaliere.")]
        [SerializeField, Min(0)] private int _toHit;

        [Tooltip("Armatura in più dei nemici, contro il colpire del cavaliere.")]
        [SerializeField, Min(0)] private int _armor;

        [Tooltip("Quanti modificatori dei campioni prende il re.")]
        [SerializeField, Min(0)] private int _bossModifiers;

        [Header("Ricompense (D5, D11)")]
        [SerializeField, Min(1f)] private float _experience = 1f;

        [SerializeField, Min(1f)] private float _gold = 1f;

        [Tooltip("I pesi delle rarità: normale, magico, raro.")]
        [SerializeField, Min(0)] private int _normalWeight = 65;

        [SerializeField, Min(0)] private int _magicWeight = 28;

        [SerializeField, Min(0)] private int _rareWeight = 7;

        [Header("Morte (D9)")]
        [Tooltip("La parte dell'oro addosso che si perde morendo.")]
        [SerializeField, Range(0f, 1f)] private float _deathGold;

        [Tooltip("La parte dell'esperienza del livello che si perde morendo, mai sotto l'inizio del livello.")]
        [SerializeField, Range(0f, 1f)] private float _deathExperience;

        [NonSerialized] private RarityTable _rarities;

        public string NameKey => _nameKey;

        public Color Color => _color;

        public int ZoneLevelBonus => _zoneLevelBonus;

        public float Life => _life;

        public float Damage => _damage;

        public int ToHit => _toHit;

        public int Armor => _armor;

        public int BossModifiers => _bossModifiers;

        public float Experience => _experience;

        public float Gold => _gold;

        public float DeathGold => _deathGold;

        public float DeathExperience => _deathExperience;

        /// <summary>La tabella delle rarità del gradino, creata la prima volta che serve.</summary>
        public RarityTable Rarities => _rarities ??= new RarityTable(_normalWeight, _magicWeight, _rareWeight);
    }
}
