using System;
using UnityEngine;

namespace DarkDescent.Talents
{
    /// <summary>
    /// Una linea tratteggiata dell'albero (M20): dalla riga <see cref="Row"/> in giù i nodi si aprono
    /// solo dopo <see cref="Points"/> punti spesi nelle righe sopra dello stesso albero.
    /// </summary>
    [Serializable]
    public struct TalentGate
    {
        [SerializeField, Min(0)] private int _row;

        [SerializeField, Min(0)] private int _points;

        public TalentGate(int row, int points)
        {
            _row = row;
            _points = points;
        }

        public int Row => _row;
        public int Points => _points;
    }
}
