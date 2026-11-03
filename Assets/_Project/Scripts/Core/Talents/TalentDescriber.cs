using System;
using System.Collections.Generic;
using DarkDescent.Localization;
using DarkDescent.Stats;

namespace DarkDescent.Talents
{
    /// <summary>
    /// Le frasi del tooltip di un nodo (D10 della M20), scritte dai dati degli effetti e non a mano, così
    /// un numero ritarato dell'albero cambia anche il testo. Ogni frase è una chiave della tabella con il
    /// suo numero: "+3% al danno", "Carica: -2 s di ricarica", "Ogni uccisione dà 3 di mana".
    /// </summary>
    public static class TalentDescriber
    {
        /// <summary>Le chiavi che servono a descrivere un effetto: per il test delle due lingue.</summary>
        public static IEnumerable<string> KeysOf(TalentEffect effect)
        {
            switch (effect.Kind)
            {
                case TalentEffectKind.Stat:
                    yield return StatKey(effect);
                    if (effect.Condition != TalentCondition.None)
                    {
                        yield return ConditionKey(effect.Condition);
                    }

                    break;
                case TalentEffectKind.Ability:
                    yield return "talent.param." + effect.Param;
                    yield return effect.Ability.NameKey;
                    break;
                case TalentEffectKind.Trigger:
                    yield return TriggerKey(effect);
                    if (effect.Outcome == TalentOutcome.Cast)
                    {
                        yield return effect.Ability.NameKey;
                    }

                    if (effect.Outcome == TalentOutcome.Buff)
                    {
                        yield return "talent.statname." + effect.Stat;
                    }

                    break;
            }
        }

        /// <summary>La frase di un effetto con <paramref name="rank"/> gradi presi (almeno uno).</summary>
        public static string Describe(TalentEffect effect, int rank, Localizer localizer)
        {
            float value = effect.Value(Math.Max(1, rank));
            switch (effect.Kind)
            {
                case TalentEffectKind.Stat:
                    string line = localizer.Format(StatKey(effect), Number(value));
                    return effect.Condition == TalentCondition.None ? line : line + " " + localizer.Get(ConditionKey(effect.Condition));
                case TalentEffectKind.Ability:
                    return localizer.Format("talent.param." + effect.Param, localizer.Get(effect.Ability.NameKey), Signed(value));
                default:
                    if (effect.Outcome == TalentOutcome.Cast)
                    {
                        return localizer.Format(TriggerKey(effect), Number(value), localizer.Get(effect.Ability.NameKey));
                    }

                    // la statistica del Buff è il terzo pezzo della frase, col suo nome: "velocità d'attacco"
                    return effect.Outcome == TalentOutcome.Buff
                        ? localizer.Format(TriggerKey(effect), Number(value), Number(effect.Duration), localizer.Get("talent.statname." + effect.Stat))
                        : localizer.Format(TriggerKey(effect), Number(value));
            }
        }

        /// <summary>Tutte le frasi di un'opzione, a capo l'una dell'altra.</summary>
        public static string Describe(TalentOption option, int rank, Localizer localizer)
        {
            var lines = new List<string>();
            foreach (var effect in option.Effects)
            {
                lines.Add(Describe(effect, rank, localizer));
            }

            return string.Join("\n", lines);
        }

        private static string StatKey(TalentEffect effect)
        {
            return "talent.stat." + effect.Stat + (effect.Modifier == ModifierKind.Percent ? ".percent" : "");
        }

        private static string ConditionKey(TalentCondition condition) => "talent.when." + condition;

        private static string TriggerKey(TalentEffect effect) => $"talent.trigger.{effect.Trigger}.{effect.Outcome}";

        private static string Number(float value) => Math.Abs(value).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        private static string Signed(float value) => (value >= 0f ? "+" : "-") + Number(value);
    }
}
