using System;
using DarkDescent.Core;

namespace DarkDescent.Combat
{
    /// <summary>
    /// L'alternanza dei colpi del bruto (prova della M7): un colpo forte, poi da
    /// <c>minQuick</c> a <c>maxQuick</c> colpi leggeri, poi di nuovo uno forte. Il primo è forte: il
    /// bruto si presenta con il colpo da cui ci si deve spostare. Logica pura, con i tiri da fuori.
    /// </summary>
    public sealed class SwingPattern
    {
        private readonly int _minQuick;
        private readonly int _maxQuick;
        private int _quickLeft;

        public SwingPattern(int minQuick, int maxQuick)
        {
            if (minQuick < 0 || maxQuick < minQuick)
            {
                throw new ArgumentOutOfRangeException(nameof(maxQuick), $"colpi leggeri da {minQuick} a {maxQuick}");
            }

            _minQuick = minQuick;
            _maxQuick = maxQuick;
        }

        /// <summary>Il prossimo colpo è quello forte? Tira quanti leggeri seguono solo dopo un colpo forte.</summary>
        public bool NextIsHeavy(IRandomSource random)
        {
            if (_quickLeft > 0)
            {
                _quickLeft--;
                return false;
            }

            _quickLeft = Math.Min(_maxQuick, _minQuick + (int)(random.NextDouble() * (_maxQuick - _minQuick + 1)));
            return true;
        }
    }
}
