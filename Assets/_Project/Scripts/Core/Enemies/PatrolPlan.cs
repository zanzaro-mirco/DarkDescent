using DarkDescent.Core;
using UnityEngine;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// La ronda di un nemico (D7 della M24), decisa dall'hash della cella e della profondità: né un tiro del seme né un
    /// generatore condiviso, quindi i livelli, gli arredi e il loot dei semi provati non cambiano, e lo stesso nemico nello
    /// stesso posto fa sempre la stessa ronda. Dice solo <i>se</i> gira, <i>quanti</i> punti ha e dove stanno rispetto a
    /// casa (angolo e distanza) e quanto sta fermo a ciascuno; il corpo li porta sul NavMesh.
    /// </summary>
    public sealed class PatrolPlan
    {
        public const int MinPoints = 2;
        public const int MaxPoints = 3;

        /// <summary>Più vicino di così a casa un punto non è una ronda: è un passo.</summary>
        public const float MinRadius = 3f;

        public const float MinPause = 2f;
        public const float MaxPause = 4f;

        private readonly float[] _angles = new float[MaxPoints];
        private readonly float[] _radii = new float[MaxPoints];
        private readonly float[] _pauses = new float[MaxPoints];

        private PatrolPlan()
        {
        }

        /// <summary>Il nemico di questa cella gira.</summary>
        public bool Patrols { get; private set; }

        /// <summary>Quanti punti ha: 2 o 3. Zero se non gira.</summary>
        public int Count { get; private set; }

        /// <param name="share">La parte dei nemici di questo tipo che girano (0,25: uno su quattro).</param>
        /// <param name="maxRadius">I metri da casa del punto più lontano.</param>
        public static PatrolPlan For(int depth, int cellX, int cellZ, float share, float maxRadius)
        {
            var plan = new PatrolPlan();
            var hash = new SplitMix64Source(SeedMixer.ForPatrol(depth, cellX, cellZ));

            // l'ordine dei numeri è fisso e li prende tutti anche chi non gira: cambiare la quota non sposta i punti
            double roll = hash.NextDouble();
            double count = hash.NextDouble();
            for (int i = 0; i < MaxPoints; i++)
            {
                plan._angles[i] = (float)(hash.NextDouble() * 360.0);
                plan._radii[i] = Mathf.Lerp(MinRadius, Mathf.Max(MinRadius, maxRadius), (float)hash.NextDouble());
                plan._pauses[i] = Mathf.Lerp(MinPause, MaxPause, (float)hash.NextDouble());
            }

            plan.Patrols = roll < share;
            plan.Count = plan.Patrols ? (count < 0.5 ? MinPoints : MaxPoints) : 0;
            return plan;
        }

        /// <summary>Dove sta il punto, rispetto a casa, sul piano (x, z). Il raggio più corto è il fallback se il primo non è sul NavMesh.</summary>
        public Vector2 Offset(int index, float scale = 1f)
        {
            float angle = _angles[index] * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (_radii[index] * scale);
        }

        public float Radius(int index) => _radii[index];

        /// <summary>I secondi che sta fermo al punto: tra 2 e 4.</summary>
        public float Pause(int index) => _pauses[index];
    }
}
