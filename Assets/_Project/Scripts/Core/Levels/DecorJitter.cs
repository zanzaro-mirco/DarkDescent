using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Rotazione e piccolo scarto degli oggetti in mezzo alla stanza (D3 della M13), presi da un hash
    /// della cella e non dal flusso del seme: due mucchietti uguali non sembrano più copie, e lo
    /// stesso livello si ricostruisce sempre uguale senza consumare tiri. Logica pura.
    /// </summary>
    public static class DecorJitter
    {
        /// <summary>Lo scarto massimo dal centro della cella, in metri per asse.</summary>
        public const float MaxOffset = 0.6f;

        /// <summary>Un angolo in gradi, da 0 a 360, a passi di 15 gradi.</summary>
        public static float Angle(int x, int y)
        {
            return 15f * (Hash(x, y, 0x9E37u) % 24u);
        }

        /// <summary>Uno scarto sul piano (x, z) dentro ±<see cref="MaxOffset"/>.</summary>
        public static Vector2 Offset(int x, int y)
        {
            return new Vector2(Unit(Hash(x, y, 0x85EBu)), Unit(Hash(x, y, 0xC2B2u))) * MaxOffset;
        }

        /// <summary>Un numero tra 0 (compreso) e 1 (escluso) per la cella, diverso per ogni <paramref name="salt"/>: l'erba e i sassi sparsi (M18).</summary>
        public static float Chance(int x, int y, uint salt)
        {
            return (Hash(x, y, salt) & 0xFFFFu) / 65536f;
        }

        /// <summary>Quale tra <paramref name="count"/> varianti va sulla cella.</summary>
        public static int Pick(int x, int y, int count)
        {
            return (int)(Hash(x, y, 0x5A17u) % (uint)count);
        }

        private static float Unit(uint hash)
        {
            return (hash & 0xFFFFu) / 32767.5f - 1f;
        }

        // Un mescolamento di interi senza stato (stile murmur): celle vicine danno valori lontani
        private static uint Hash(int x, int y, uint salt)
        {
            unchecked
            {
                uint h = (uint)x * 0x27D4EB2Du ^ (uint)y * 0x165667B1u ^ salt * 0x9E3779B9u;
                h ^= h >> 15;
                h *= 0x85EBCA6Bu;
                h ^= h >> 13;
                h *= 0xC2B2AE35u;
                h ^= h >> 16;
                return h;
            }
        }
    }
}
