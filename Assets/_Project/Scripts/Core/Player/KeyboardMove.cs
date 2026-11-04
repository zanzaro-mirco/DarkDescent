using UnityEngine;

namespace DarkDescent.Player
{
    /// <summary>
    /// Dove porta il movimento da tastiera (WASD e frecce, D2 e D3 della M23): i tasti si leggono sullo schermo, W va
    /// verso l'alto, S verso il basso, A a sinistra, D a destra, qualunque sia la rotazione della visuale (Z e X, ADR-052).
    /// Si ricalcola a ogni frame dalla camera di quel momento, non una volta. Logica pura.
    /// </summary>
    public static class KeyboardMove
    {
        private const float Epsilon = 0.0001f;

        /// <summary>Sotto questa lunghezza i tasti contano come rilasciati.</summary>
        public const float Deadzone = 0.1f;

        /// <summary>Qualche tasto di movimento è premuto.</summary>
        public static bool IsPressed(Vector2 keys)
        {
            return keys.sqrMagnitude > Deadzone * Deadzone;
        }

        /// <summary>
        /// La direzione nel mondo, in piano e di lunghezza 1 (la diagonale non è più veloce), o zero se nessun tasto è
        /// premuto. <paramref name="forward"/>, <paramref name="up"/> e <paramref name="right"/> sono quelli della camera:
        /// di una camera quasi a picco, il cui avanti in piano sparisce, si usa il suo su.
        /// </summary>
        public static Vector3 WorldDirection(Vector2 keys, Vector3 forward, Vector3 up, Vector3 right)
        {
            if (!IsPressed(keys))
            {
                return Vector3.zero;
            }

            Vector3 ahead = Flat(forward);
            if (ahead.sqrMagnitude < Epsilon)
            {
                ahead = Flat(up);
            }

            Vector3 side = Flat(right);
            if (ahead.sqrMagnitude < Epsilon || side.sqrMagnitude < Epsilon)
            {
                return Vector3.zero;
            }

            Vector3 direction = ahead.normalized * keys.y + side.normalized * keys.x;
            return direction.sqrMagnitude < Epsilon ? Vector3.zero : direction.normalized;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
