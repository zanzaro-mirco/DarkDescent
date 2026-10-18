using System;
using DarkDescent.Core;

namespace DarkDescent.Items
{
    /// <summary>
    /// Le probabilità delle rarità al drop, con un'estrazione pesata (D2 della M5): normale 65,
    /// magico 28, raro 7. I pesi non devono sommare a 100: conta il rapporto.
    /// </summary>
    public sealed class RarityTable
    {
        /// <summary>Le probabilità della M5, generose perché i livelli fatti a mano hanno pochi nemici.</summary>
        public static readonly RarityTable Default = new RarityTable(65, 28, 7);

        private readonly int[] _weights;
        private readonly int _total;

        public RarityTable(int normal, int magic, int rare)
        {
            _weights = new[] { normal, magic, rare };
            foreach (int weight in _weights)
            {
                if (weight < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(normal), "I pesi non possono essere negativi");
                }

                _total += weight;
            }

            if (_total == 0)
            {
                throw new ArgumentException("Serve almeno un peso positivo");
            }
        }

        /// <summary>
        /// Una tabella nuova con i pesi di magici e rari moltiplicati, arrotondati (D7 della M12): magico
        /// ×2 e raro ×3 danno 65, 56, 21. Questa resta com'è.
        /// </summary>
        public RarityTable Shifted(float magicFactor, float rareFactor)
        {
            if (magicFactor < 0f || rareFactor < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(magicFactor), "I fattori non possono essere negativi");
            }

            return new RarityTable(_weights[0], (int)Math.Round(_weights[1] * (double)magicFactor, MidpointRounding.AwayFromZero),
                (int)Math.Round(_weights[2] * (double)rareFactor, MidpointRounding.AwayFromZero));
        }

        /// <summary>La probabilità di una rarità, tra 0 e 1.</summary>
        public double Probability(Rarity rarity)
        {
            return (double)_weights[(int)rarity] / _total;
        }

        public Rarity Roll(IRandomSource random)
        {
            double roll = random.NextDouble() * _total;
            for (int i = 0; i < _weights.Length; i++)
            {
                roll -= _weights[i];
                if (roll < 0.0)
                {
                    return (Rarity)i;
                }
            }

            // un NextDouble vicinissimo a 1 con l'arrotondamento: l'ultima con peso
            for (int i = _weights.Length - 1; i >= 0; i--)
            {
                if (_weights[i] > 0)
                {
                    return (Rarity)i;
                }
            }

            return Rarity.Normal;
        }
    }
}
