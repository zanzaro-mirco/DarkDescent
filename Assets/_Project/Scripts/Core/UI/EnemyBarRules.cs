using System.Collections.Generic;

namespace DarkDescent.UI
{
    /// <summary>
    /// Le regole delle barre sopra i nemici (D9 e D10 della M23), senza Unity: chi ne ha una e quante se ne
    /// mostrano. Una barra spetta a chi è ferito e vivo; ne bastano le più vicine al cavaliere, dentro un tetto.
    /// </summary>
    public static class EnemyBarRules
    {
        /// <summary>Il tetto delle barre in scena: lo sciame ne ha dieci, il resto è fuori vista.</summary>
        public const int MaxBars = 24;

        /// <summary>Un nemico ha la barra se è vivo e ha perso della vita: intatto o morto no.</summary>
        public static bool IsWounded(float current, float max)
        {
            return max > 0f && current > 0f && current < max;
        }

        /// <summary>La parte piena della barra, tra 0 e 1.</summary>
        public static float Fill(float current, float max)
        {
            if (max <= 0f || current <= 0f)
            {
                return 0f;
            }

            return current >= max ? 1f : current / max;
        }

        /// <summary>
        /// Gli indici dei candidati più vicini (distanze al quadrato, una per candidato), al più
        /// <paramref name="limit"/>, dal più vicino al più lontano. Riempie <paramref name="picked"/> senza
        /// allocare; a parità vince chi sta prima nella lista.
        /// </summary>
        public static void PickNearest(IReadOnlyList<float> distances, int limit, List<int> picked)
        {
            picked.Clear();
            if (limit <= 0)
            {
                return;
            }

            for (int i = 0; i < distances.Count; i++)
            {
                float distance = distances[i];

                // il posto in cui entra nella lista ordinata: dopo tutti quelli che non sono più lontani
                int at = picked.Count;
                while (at > 0 && distances[picked[at - 1]] > distance)
                {
                    at--;
                }

                if (at >= limit)
                {
                    continue;
                }

                picked.Insert(at, i);
                if (picked.Count > limit)
                {
                    picked.RemoveAt(picked.Count - 1);
                }
            }
        }
    }
}
