using System;
using DarkDescent.Combat;
using DarkDescent.Core;
using UnityEngine;

namespace DarkDescent.Magic
{
    /// <summary>
    /// I numeri di un incantesimo (D3 della M9): costo, ricarica, danno e quello che serve al suo
    /// tipo. Dato immutabile, come le basi degli oggetti; i numeri si tarano nell'asset.
    /// </summary>
    [CreateAssetMenu(fileName = "Spell", menuName = "DarkDescent/Spell")]
    public sealed class SpellDefinition : ScriptableObject
    {
        [Tooltip("Generato alla creazione e mai più cambiato: è quello che salva il salvataggio.")]
        [SerializeField] private string _id;

        [Tooltip("La chiave del nome nella tabella delle stringhe, come spell.firebolt.")]
        [SerializeField] private string _nameKey;

        [SerializeField] private Sprite _icon;

        [SerializeField] private SpellKind _kind;

        [SerializeField, Min(0f)] private float _manaCost = 6f;

        [Tooltip("Secondi prima di poterlo rilanciare; 0 per il proiettile, che ha solo il costo.")]
        [SerializeField, Min(0f)] private float _cooldown;

        [Tooltip("Lo si conosce dall'inizio; gli altri si imparano dalle pergamene (D5).")]
        [SerializeField] private bool _knownFromStart;

        [Header("Danno: tiro tra minimo e massimo, più Magia / divisore")]
        [SerializeField, Min(0)] private int _minDamage;

        [SerializeField, Min(0)] private int _maxDamage;

        [SerializeField, Min(0.1f)] private float _magicDivisor = 4f;

        [Header("Proiettile")]
        [Tooltip("Metri al secondo.")]
        [SerializeField, Min(0f)] private float _speed = 14f;

        [Tooltip("Metri prima di spegnersi senza colpire niente.")]
        [SerializeField, Min(0f)] private float _range = 14f;

        [Header("Area")]
        [SerializeField, Min(0f)] private float _radius = 4f;

        [Header("Potenziamento")]
        [Tooltip("Armatura in più, in percentuale di quella attuale.")]
        [SerializeField, Min(0)] private int _armorPercent;

        [Tooltip("Secondi che dura.")]
        [SerializeField, Min(0f)] private float _duration;

        public string Id => _id;
        public string NameKey => _nameKey;
        public Sprite Icon => _icon;
        public SpellKind Kind => _kind;
        public float ManaCost => _manaCost;
        public float Cooldown => _cooldown;
        public bool KnownFromStart => _knownFromStart;
        public int MinDamage => _minDamage;
        public int MaxDamage => _maxDamage;
        public float MagicDivisor => _magicDivisor;
        public float Speed => _speed;
        public float Range => _range;
        public float Radius => _radius;
        public int ArmorPercent => _armorPercent;
        public float Duration => _duration;

        /// <summary>Il danno di un lancio con questa Magia: non usa la Forza né il livello (D3).</summary>
        public float RollDamage(float magic, IRandomSource random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            int span = _maxDamage - _minDamage + 1;
            int roll = Math.Min(_maxDamage, _minDamage + (int)(random.NextDouble() * span));
            return CombatFormulas.SpellDamage(roll, magic, _magicDivisor);
        }

        /// <summary>Danno minimo e massimo con questa Magia: quello che mostra il tooltip.</summary>
        public (int min, int max) DamageRange(float magic)
        {
            return (CombatFormulas.SpellDamage(_minDamage, magic, _magicDivisor), CombatFormulas.SpellDamage(_maxDamage, magic, _magicDivisor));
        }
    }
}
