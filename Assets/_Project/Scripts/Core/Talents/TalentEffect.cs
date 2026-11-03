using System;
using DarkDescent.Magic;
using DarkDescent.Stats;
using UnityEngine;

namespace DarkDescent.Talents
{
    /// <summary>
    /// Un effetto di un nodo dell'albero (D13 della M20): un modificatore della scheda, un numero di
    /// un'abilità o qualcosa che scatta a un colpo, a un'uccisione o a un blocco. Il valore è per grado:
    /// con tre gradi presi conta tre volte. Dato immutabile, come gli affissi.
    /// </summary>
    [Serializable]
    public sealed class TalentEffect
    {
        [SerializeField] private TalentEffectKind _kind;

        [Tooltip("La statistica: quella che cambia per Stat, quella in più per qualche secondo per Trigger con Buff.")]
        [SerializeField] private StatType _stat;

        [SerializeField] private ModifierKind _modifier;

        [SerializeField] private TalentCondition _condition;

        [Tooltip("L'abilità di cui il nodo cambia un numero, per Ability.")]
        [SerializeField] private SpellDefinition _ability;

        [SerializeField] private AbilityParam _param;

        [SerializeField] private TalentTrigger _trigger;

        [SerializeField] private TalentOutcome _outcome;

        [Tooltip("Il valore per ogni grado preso.")]
        [SerializeField] private float _perRank;

        [Tooltip("Secondi che dura, per Trigger con Buff.")]
        [SerializeField, Min(0f)] private float _duration;

        public TalentEffectKind Kind => _kind;
        public StatType Stat => _stat;
        public ModifierKind Modifier => _modifier;
        public TalentCondition Condition => _condition;
        public SpellDefinition Ability => _ability;
        public AbilityParam Param => _param;
        public TalentTrigger Trigger => _trigger;
        public TalentOutcome Outcome => _outcome;
        public float PerRank => _perRank;
        public float Duration => _duration;

        /// <summary>Il valore con <paramref name="rank"/> gradi presi.</summary>
        public float Value(int rank) => _perRank * Math.Max(0, rank);

        /// <summary>Un modificatore della scheda: fisso (punti) o in percentuale, sempre o a una condizione.</summary>
        public static TalentEffect ForStat(StatType stat, float perRank, ModifierKind modifier = ModifierKind.Flat,
            TalentCondition condition = TalentCondition.None)
        {
            return new TalentEffect { _kind = TalentEffectKind.Stat, _stat = stat, _perRank = perRank, _modifier = modifier, _condition = condition };
        }

        /// <summary>Un numero di un'abilità, nella sua unità.</summary>
        public static TalentEffect ForAbility(SpellDefinition ability, AbilityParam param, float perRank)
        {
            if (ability == null)
            {
                throw new ArgumentNullException(nameof(ability));
            }

            return new TalentEffect { _kind = TalentEffectKind.Ability, _ability = ability, _param = param, _perRank = perRank };
        }

        /// <summary>Qualcosa che scatta; per <see cref="TalentOutcome.Buff"/> servono la statistica e la durata, per <see cref="TalentOutcome.Cast"/> l'abilità.</summary>
        public static TalentEffect OnTrigger(TalentTrigger trigger, TalentOutcome outcome, float perRank, StatType stat = default, float duration = 0f,
            SpellDefinition ability = null)
        {
            return new TalentEffect { _kind = TalentEffectKind.Trigger, _trigger = trigger, _outcome = outcome, _perRank = perRank, _stat = stat, _duration = duration, _ability = ability };
        }
    }
}
