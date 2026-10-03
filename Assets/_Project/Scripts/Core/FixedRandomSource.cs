using System;

namespace DarkDescent.Core
{
    /// <summary>
    /// <see cref="IRandomSource"/> che restituisce i valori dati, in ciclo. Per i test e per
    /// riprodurre a mano un caso preciso: con 0 ogni colpo va a segno con il danno minimo.
    /// </summary>
    public sealed class FixedRandomSource : IRandomSource
    {
        private readonly double[] _values;
        private int _next;

        public FixedRandomSource(params double[] values)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("Serve almeno un valore", nameof(values));
            }

            foreach (double value in values)
            {
                // fuori da [0, 1) i tiri uscirebbero dai loro intervalli
                if (!(value >= 0.0 && value < 1.0))
                {
                    throw new ArgumentOutOfRangeException(nameof(values), value, "I valori devono stare in [0, 1)");
                }
            }

            _values = (double[])values.Clone();
        }

        public double NextDouble()
        {
            double value = _values[_next];
            _next = (_next + 1) % _values.Length;
            return value;
        }
    }
}
