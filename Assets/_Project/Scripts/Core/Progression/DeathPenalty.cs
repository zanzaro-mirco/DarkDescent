using System;

namespace DarkDescent.Progression
{
    /// <summary>
    /// Quanto costa morire (D9 della M14): una parte dell'oro addosso e una parte dell'esperienza del
    /// livello, che si ferma all'inizio del livello attuale. Non si scende mai di livello, e al
    /// livello massimo non c'è esperienza da perdere. Logica pura.
    /// </summary>
    public static class DeathPenalty
    {
        /// <summary>L'oro perso: la parte dell'oro addosso, arrotondata.</summary>
        public static int GoldLost(int gold, float fraction)
        {
            if (gold <= 0 || fraction <= 0f)
            {
                return 0;
            }

            return Math.Min(gold, Round(gold * (double)fraction));
        }

        /// <summary>
        /// L'esperienza persa: la parte di quella che serve per il livello successivo, ma non più di
        /// quella già fatta nel livello. Al livello massimo (<paramref name="experienceToNext"/> 0) niente.
        /// </summary>
        public static int ExperienceLost(int experience, int experienceToNext, float fraction)
        {
            if (experience <= 0 || experienceToNext <= 0 || fraction <= 0f)
            {
                return 0;
            }

            return Math.Min(experience, Round(experienceToNext * (double)fraction));
        }

        // al più vicino e non per difetto: 0,19f vale 0,18999… e 1000 × 0,19 deve fare 190
        private static int Round(double value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
