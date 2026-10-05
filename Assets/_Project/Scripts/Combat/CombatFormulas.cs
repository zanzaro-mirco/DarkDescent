using System;
using DarkDescent.Core;

namespace DarkDescent.Combat
{
    /// <summary>
    /// La formula del colpo (piano § 2, D1 della M4), in un punto solo e senza Unity: la usano
    /// MeleeAttack per tirare e il pannello del personaggio per mostrare.
    /// </summary>
    public static class CombatFormulas
    {
        public const float MinHitChance = 5f;
        public const float MaxHitChance = 95f;
        public const float MaxBlockChance = 75f;

        private const float BaseHitChance = 75f;
        private const float BaseLife = 50f;
        private const float LifePerVitality = 2f;

        /// <summary>
        /// Probabilità di colpire, in punti percentuali: 75 + Destrezza / 2 − Armatura, più il bonus
        /// a colpire degli affissi, tra 5 e 95.
        /// </summary>
        public static float HitChance(float dexterity, float armor, float toHitBonus = 0f)
        {
            float chance = BaseHitChance + dexterity / 2f - armor + toHitBonus;
            return Math.Min(MaxHitChance, Math.Max(MinHitChance, chance));
        }

        public static bool RollHit(float hitChance, IRandomSource random)
        {
            return random.NextDouble() * 100.0 < hitChance;
        }

        /// <summary>Probabilità di bloccare con uno scudo: blocco dello scudo + Destrezza / 2, tra 0 e 75 (D1 della M5).</summary>
        public static float BlockChance(float shieldBlock, float dexterity)
        {
            return Math.Min(MaxBlockChance, Math.Max(0f, shieldBlock + dexterity / 2f));
        }

        public static bool RollBlock(float blockChance, IRandomSource random)
        {
            return random.NextDouble() * 100.0 < blockChance;
        }

        /// <summary>
        /// Un valore dell'oggetto con un affisso in percentuale ("+40% danno"), arrotondato per
        /// difetto: la spada corta 6–9 con +40% fa 8–12.
        /// </summary>
        public static int ApplyPercent(int value, int percent)
        {
            return value * (100 + percent) / 100;
        }

        /// <summary>Moltiplicatore del danno dato dalla Forza: 1 + Forza / 100.</summary>
        public static float StrengthMultiplier(float strength)
        {
            return 1f + strength / 100f;
        }

        /// <summary>
        /// Un tiro intero tra minimo e massimo dell'arma, estremi compresi, moltiplicato per la Forza
        /// e arrotondato come nel pannello del personaggio; almeno 1.
        /// </summary>
        public static float RollDamage(int minDamage, int maxDamage, float strength, IRandomSource random)
        {
            int span = maxDamage - minDamage + 1;
            // il Min protegge dall'arrotondamento di un NextDouble vicinissimo a 1
            int roll = Math.Min(maxDamage, minDamage + (int)(random.NextDouble() * span));
            // intero: con i decimali la vita restava frazionaria e l'ultimo colpo poteva mostrare "0"
            return Math.Max(1f, (float)Math.Round(roll * StrengthMultiplier(strength)));
        }

        /// <summary>Vita massima: 50 + 2 × Vitalità. Con la Vitalità 25 del cavaliere, 100.</summary>
        public static float MaxLife(float vitality)
        {
            return BaseLife + LifePerVitality * vitality;
        }
    }
}
