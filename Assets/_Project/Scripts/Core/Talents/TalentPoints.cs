using System;

namespace DarkDescent.Talents
{
    /// <summary>
    /// I punti dell'albero (D6 della M20): uno a ogni livello dal 2. Dal 2 al 9 vanno nella classe;
    /// dal 10, quando si sceglie la specializzazione, ai livelli pari nella specializzazione e ai
    /// dispari nella classe. Al livello 50 fanno 28 e 21. Non si salvano: vengono dal livello.
    /// </summary>
    public static class TalentPoints
    {
        /// <summary>L'oro per livello che costa ricominciare l'albero all'altare (D11).</summary>
        public const int ResetGoldPerLevel = 30;

        /// <summary>Il livello da cui si sceglie la specializzazione.</summary>
        public const int SpecializationLevel = 10;

        /// <summary>I punti della classe fino a <paramref name="level"/> compreso.</summary>
        public static int ForClass(int level)
        {
            if (level < SpecializationLevel)
            {
                return Math.Max(0, level - 1);
            }

            return SpecializationLevel - 2 + (level - SpecializationLevel + 1) / 2;
        }

        /// <summary>I punti della specializzazione fino a <paramref name="level"/> compreso.</summary>
        public static int ForSpecialization(int level)
        {
            return level < SpecializationLevel ? 0 : (level - SpecializationLevel) / 2 + 1;
        }

        /// <summary>L'oro che chiede l'altare per ricominciare l'albero: 30 per livello (300 al 10, 1500 al 50).</summary>
        public static int ResetCost(int level)
        {
            return ResetGoldPerLevel * Math.Max(1, level);
        }
    }
}
