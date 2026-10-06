using DarkDescent.Core;
using UnityEngine;

namespace DarkDescent.Audio
{
    /// <summary>
    /// Quando, dove e quale verso nel buio (passo 7.0 della M7). Logica pura, con la sorgente di
    /// numeri casuali passata da fuori: nei test è fissa.
    /// </summary>
    public sealed class StingerSchedule
    {
        private readonly IRandomSource _random;
        private int _last = -1;

        public StingerSchedule(IRandomSource random)
        {
            _random = random ?? throw new System.ArgumentNullException(nameof(random));
        }

        /// <summary>Secondi fino al prossimo verso, tra x e y dell'intervallo.</summary>
        public float NextDelay(Vector2 interval)
        {
            return Range(interval);
        }

        /// <summary>
        /// Da dove viene: in piano, in una direzione qualsiasi, a una distanza tra x e y. Sopra o
        /// sotto non serve: il listener è all'altezza della testa e la camera guarda dall'alto.
        /// </summary>
        public Vector3 NextOffset(Vector2 distance)
        {
            float angle = (float)(_random.NextDouble() * Mathf.PI * 2.0);
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Range(distance);
        }

        public float NextPitch(Vector2 range)
        {
            return Range(range);
        }

        /// <summary>Un indice tra 0 e count escluso, mai uguale al precedente se ce n'è più di uno.</summary>
        public int NextIndex(int count)
        {
            // un precedente fuori misura viene da un profilo con più versi: qui non conta
            if (_last >= count)
            {
                _last = -1;
            }

            if (count <= 1)
            {
                _last = 0;
                return 0;
            }

            // Senza un precedente si tira tra tutti. Con un precedente si tira tra gli altri count - 1 e
            // lo si salta: un tiro solo, nessun ciclo.
            int index;
            if (_last < 0)
            {
                index = System.Math.Min(count - 1, (int)(_random.NextDouble() * count));
            }
            else
            {
                index = System.Math.Min(count - 2, (int)(_random.NextDouble() * (count - 1)));
                if (index >= _last)
                {
                    index++;
                }
            }

            _last = index;
            return index;
        }

        private float Range(Vector2 range)
        {
            return Mathf.Lerp(range.x, range.y, (float)_random.NextDouble());
        }
    }
}
