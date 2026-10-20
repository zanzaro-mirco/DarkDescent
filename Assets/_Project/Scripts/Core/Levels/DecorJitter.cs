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
