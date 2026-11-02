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
    }
}
