using System.Collections.Generic;
using DarkDescent.Combat;
using DarkDescent.Magic;
using DarkDescent.Stats;
using DarkDescent.Talents;
using UnityEngine;

namespace DarkDescent.Progression
{
    /// <summary>
    /// L'albero delle abilità del cavaliere (M20): crea il <see cref="TalentBuild"/> con l'albero
    /// del Guerriero e le sue due specializzazioni, gli passa il livello della crescita e mette sulla
    /// scheda i passivi applicati, con sé come sorgente, così si tolgono insieme a ogni *Applica* e
    /// all'altare. Quelli a una condizione valgono finché la condizione regge: lo scudo in mano, la
    /// vita sotto il 30%; le due mani arrivano con le armi a due mani (20.5). Fa scattare anche gli
    /// effetti al colpo, al blocco e all'uccisione: mana, il colpo rimandato, il colpo dopo il blocco,
    /// la velocità dopo un'uccisione.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats), typeof(PlayerProgress))]
    public class PlayerTalents : MonoBehaviour
    {
        /// <summary>Sotto questa parte della vita vale la condizione della vita bassa (Ultima difesa).</summary>
        public const float LowLifeFraction = 0.3f;

        [SerializeField] private TalentTree _classTree;

        [SerializeField] private TalentTree[] _specializations;

        private readonly List<StatModifier> _modifiers = new List<StatModifier>();
        private readonly Dictionary<TalentEffect, float> _buffs = new Dictionary<TalentEffect, float>();
        private readonly List<TalentEffect> _expired = new List<TalentEffect>();

        // la sorgente del colpo dopo il blocco (Vendicatore): si toglie dopo il primo colpo a segno
        private readonly object _avenger = new object();
        private bool _avengerUsed;

        private TalentBuild _build;
        private CharacterProgress _progress;
        private CharacterStats _stats;
        private Health _health;
        private ShieldBlock _block;
        private MeleeAttack _attack;
        private PlayerMagic _magic;
        private bool _lowLife;

        // creato al primo accesso, come la crescita: pannello e salvataggio si collegano dal
        // composition root, in un ordine che rispetto all'Awake non è garantito
        public TalentBuild Build => _build ??= new TalentBuild(_classTree, _specializations, Progress.Level);

        /// <summary>Vero sotto il 30% della vita: l'Ultima difesa vale.</summary>
        public bool IsLowLife => _lowLife;

        private CharacterProgress Progress => _progress ??= GetComponent<PlayerProgress>().Progress;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _health = GetComponent<Health>();
            _block = GetComponent<ShieldBlock>();
            _attack = GetComponent<MeleeAttack>();
            _magic = GetComponent<PlayerMagic>();
        }

        private void OnEnable()
        {
            Progress.Changed += HandleProgressChanged;
            Build.Applied += Refresh;
            if (_block != null)
            {
                _block.ShieldChanged += Refresh;
                _block.Blocked += HandleBlocked;
            }

            if (_health != null)
            {
                _health.HealthChanged += HandleHealthChanged;
            }

            if (_attack != null)
            {
                _attack.HitLanded += HandleHit;
                _attack.WeaponChanged += Refresh;
            }

            HandleProgressChanged();
            Refresh();
        }

        private void OnDisable()
        {
            Progress.Changed -= HandleProgressChanged;
            Build.Applied -= Refresh;
            if (_block != null)
            {
                _block.ShieldChanged -= Refresh;
                _block.Blocked -= HandleBlocked;
            }

            if (_health != null)
            {
                _health.HealthChanged -= HandleHealthChanged;
            }

            if (_attack != null)
            {
                _attack.HitLanded -= HandleHit;
                _attack.WeaponChanged -= Refresh;
            }

            EndBuffs();
        }

        /// <summary>
        /// Un nemico ucciso dal cavaliere: il mana del Massacro e la velocità della Frenesia. Lo chiama
        /// il composition root, che dà l'esperienza delle uccisioni.
        /// </summary>
        public void HandleKill()
        {
            foreach (var (effect, rank) in Build.Triggers(TalentTrigger.Kill))
            {
                switch (effect.Outcome)
                {
                    case TalentOutcome.Mana:
                        RestoreMana(effect.Value(rank));
                        break;
                    case TalentOutcome.Buff:
                        // un'uccisione dentro la durata la rinnova, senza sommarsi
                        _stats.Sheet.RemoveModifiersFrom(effect);
                        _stats.Sheet.AddModifier(new StatModifier(effect.Stat, ModifierKind.Flat, effect.Value(rank), effect));
                        _buffs[effect] = effect.Duration;
                        break;
                }
            }
        }

        /// <summary>La condizione di un passivo, adesso.</summary>
        public bool Holds(TalentCondition condition)
        {
            switch (condition)
            {
                case TalentCondition.None:
                    return true;
                case TalentCondition.Shield:
                    return _block != null && _block.HasShield;
                case TalentCondition.LowLife:
                    return _lowLife;
                case TalentCondition.TwoHanded:
                    return _attack != null && _attack.Weapon != null && _attack.Weapon.TwoHanded;
                default:
                    return false;
            }
        }

        private void HandleProgressChanged()
        {
            Build.SetLevel(Progress.Level);
        }

        // i passivi applicati sulla scheda, con le condizioni di adesso: via tutti i vecchi, dentro i nuovi
        private void Refresh()
        {
            var sheet = GetComponent<CharacterStats>().Sheet;
            sheet.RemoveModifiersFrom(this);
            _modifiers.Clear();
            Build.CollectModifiers(_modifiers, this, Holds);
            foreach (var modifier in _modifiers)
            {
                sheet.AddModifier(modifier);
            }
        }

        // solo quando la vita passa sopra o sotto il 30%: la vita in più dell'albero cambia il massimo,
        // e a ogni colpo rifare la scheda sarebbe un giro senza fine
        private void HandleHealthChanged(float current, float max)
        {
            bool low = current > 0f && current < max * LowLifeFraction;
            if (low != _lowLife)
            {
                _lowLife = low;
                Refresh();
            }
        }

        private void HandleHit(DamageInfo info)
        {
            foreach (var (effect, rank) in Build.Triggers(TalentTrigger.Hit))
            {
                if (effect.Outcome == TalentOutcome.Mana)
                {
                    RestoreMana(effect.Value(rank));
                }
            }

            // il colpo dopo il blocco vale per tutti i colpi di questo frame (un Turbine), poi si toglie
            _avengerUsed = true;
        }

        private void HandleBlocked(DamageInfo info)
        {
            foreach (var (effect, rank) in Build.Triggers(TalentTrigger.Block))
            {
                switch (effect.Outcome)
                {
                    case TalentOutcome.Mana:
                        RestoreMana(effect.Value(rank));
                        break;
                    case TalentOutcome.Reflect:
                        Reflect(info, effect.Value(rank));
                        break;
                    case TalentOutcome.NextHit:
                        _stats.Sheet.RemoveModifiersFrom(_avenger);
                        _stats.Sheet.AddModifier(new StatModifier(StatType.DamageBonus, ModifierKind.Flat, effect.Value(rank), _avenger));
                        _avengerUsed = false;
                        break;
                }
            }
        }

        // la Rappresaglia: il colpo bloccato non tira il danno, quindi si rimanda una parte di un colpo medio
        private static void Reflect(DamageInfo info, float percent)
        {
            if (info.Source == null || !info.Source.TryGetComponent(out Health attacker) || attacker.IsDead
                || !info.Source.TryGetComponent(out MeleeAttack attack))
            {
                return;
            }

            float amount = attack.AverageDamage() * percent / 100f;
            if (amount > 0f)
            {
                attacker.TakeDamage(new DamageInfo(amount, DamageType.Physical, null));
            }
        }

        private void RestoreMana(float amount)
        {
            if (_magic != null && amount > 0f && _health != null && !_health.IsDead)
            {
                _magic.Mana.Restore(amount);
            }
        }

        private void Update()
        {
            if (_buffs.Count > 0)
            {
                _expired.Clear();
                foreach (var effect in new List<TalentEffect>(_buffs.Keys))
                {
                    float left = _buffs[effect] - Time.deltaTime;
                    _buffs[effect] = left;
                    if (left <= 0f)
                    {
                        _expired.Add(effect);
                    }
                }

                foreach (var effect in _expired)
                {
                    _buffs.Remove(effect);
                    _stats.Sheet.RemoveModifiersFrom(effect);
                }
            }
        }

        private void LateUpdate()
        {
            if (_avengerUsed)
            {
                _avengerUsed = false;
                _stats.Sheet.RemoveModifiersFrom(_avenger);
            }
        }

        private void EndBuffs()
        {
            foreach (var effect in _buffs.Keys)
            {
                _stats.Sheet.RemoveModifiersFrom(effect);
            }

            _buffs.Clear();
            _stats.Sheet.RemoveModifiersFrom(_avenger);
        }
    }
}
