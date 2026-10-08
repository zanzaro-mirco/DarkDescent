using System;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La mappa scoperta di una profondità, come si salva (formato 2, D9 della M8): misura della
    /// mappa e una cella per bit, riga per riga, in base64. Con la misura si riconosce un salvataggio
    /// che non corrisponde più al livello generato, e lo si ignora.
    /// </summary>
    [Serializable]
    public sealed class ExploredLevel
    {
        [SerializeField] private int _depth;
        [SerializeField] private int _width;
        [SerializeField] private int _height;
        [SerializeField] private string _cells;

        public ExploredLevel(int depth, int width, int height, byte[] cells)
        {
            _depth = depth;
            _width = width;
            _height = height;
            _cells = Convert.ToBase64String(cells ?? Array.Empty<byte>());
        }

        public int Depth => _depth;

        public int Width => _width;

        public int Height => _height;

        /// <summary>Le celle in bit; vuote se il testo non è base64 valido.</summary>
        public byte[] Cells()
        {
            try
            {
                return string.IsNullOrEmpty(_cells) ? Array.Empty<byte>() : Convert.FromBase64String(_cells);
            }
            catch (FormatException)
            {
                return Array.Empty<byte>();
            }
        }
    }
}
