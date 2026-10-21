using System;
using DarkDescent.Core;

namespace DarkDescent.Combat
{
    /// <summary>
    /// I numeri delle trappole (D8 della M13). Gli spuntoni feriscono solo il cavaliere, per una parte
    /// della vita massima che cresce con la profondità: 10% alla 3, due punti in più a profondità, al
    /// più 25%. Il barile esplosivo ferisce tutti nel suo raggio, nemici compresi, con un danno che
    /// cresce con la profondità; dalla profondità 4 un barile dei gruppi su cinque lo è, dal seme della
    /// sua cella. Logica pura.
    /// </summary>
    public static class TrapRules
    {
        public const int FirstExplosiveDepth = 4;

        public const double ExplosiveChance = 0.2;

        /// <summary>La parte della vita massima che toglie uno scatto degli spuntoni.</summary>
        public static float SpikeFraction(int depth)
        {
            return Math.Min(0.25f, Math.Max(0.10f, 0.10f + 0.02f * (depth - 3)));
        }

        /// <summary>Il danno dell'esplosione a chiunque stia nel raggio: 15 + 6 a profondità.</summary>
        public static float ExplosionDamage(int depth)
        {
            return 15f + 6f * Math.Max(1, depth);
        }

        /// <summary>Se il barile di questa cella è esplosivo: mai prima della profondità 4, poi uno su cinque.</summary>
        public static bool IsExplosiveBarrel(ulong runSeed, int depth, int cellX, int cellZ)
        {
            if (depth < FirstExplosiveDepth)
            {
                return false;
            }

            return new SplitMix64Source(SeedMixer.ForExplosive(runSeed, depth, cellX, cellZ)).NextDouble() < ExplosiveChance;
        }
    }
}
