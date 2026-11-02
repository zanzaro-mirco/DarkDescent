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

        [Tooltip("Che cosa compare nel mondo: il prefab del proiettile, dell'anello di fiamme o dell'alone.")]
        [SerializeField] private GameObject _effect;

        [SerializeField, Min(0f)] private float _manaCost = 6f;

        [Tooltip("Secondi prima di poterlo rilanciare; 0 per il proiettile, che ha solo il costo.")]
        [SerializeField, Min(0f)] private float _cooldown;

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

        [Header("Abilità del Guerriero (M20)")]
        [Tooltip("Il danno del colpo, in percentuale di un colpo dell'arma: 160 per il Colpo possente.")]
        [SerializeField, Min(0f)] private float _weaponPercent;

        [Tooltip("Secondi di stordimento a chi colpisce, prima della regola dello stordimento.")]
        [SerializeField, Min(0f)] private float _stun;

        [Tooltip("Punti percentuali in più del suo tipo: il danno del Grido, la vita dell'Ultimo baluardo, il colpo dell'Esecuzione sui feriti.")]
        [SerializeField, Min(0f)] private float _bonusPercent;

        [Tooltip("La vita del nemico, in percentuale, sotto cui vale il colpo forte dell'Esecuzione.")]
        [SerializeField, Range(0f, 100f)] private float _threshold;

        [Tooltip("La distanza minima del bersaglio, per la Carica; la massima è la gittata.")]
        [SerializeField, Min(0f)] private float _minRange;

        [Tooltip("La velocità di movimento mentre dura, in parte di quella normale: il Muro di scudi a metà.")]
        [SerializeField, Range(0f, 1f)] private float _moveFactor = 1f;

        [Tooltip("Secondi tra un colpo e l'altro, per la Tempesta d'acciaio.")]
        [SerializeField, Min(0f)] private float _tick;

        public string Id => _id;
        public string NameKey => _nameKey;
        public Sprite Icon => _icon;
        public SpellKind Kind => _kind;
        public GameObject Effect => _effect;
        public float ManaCost => _manaCost;
        public float Cooldown => _cooldown;
        public int MinDamage => _minDamage;
        public int MaxDamage => _maxDamage;
        public float MagicDivisor => _magicDivisor;
        public float Speed => _speed;
        public float Range => _range;
        public float Radius => _radius;
        public int ArmorPercent => _armorPercent;
        public float Duration => _duration;
        public float WeaponPercent => _weaponPercent;
        public float Stun => _stun;
        public float BonusPercent => _bonusPercent;
        public float Threshold => _threshold;
        public float MinRange => _minRange;
        public float MoveFactor => _moveFactor;
        public float Tick => _tick;

        /// <summary>Un'abilità del Guerriero (M20), non un incantesimo: la fa <c>WarriorAbilities</c>.</summary>
        public bool IsWarriorAbility => _kind >= SpellKind.Strike;

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
