using System;
using System.Collections.Generic;
using System.Globalization;

namespace DarkDescent.Localization
{
    /// <summary>
    /// La lingua attiva e i testi in quella lingua. Una traduzione mancante ricade sull'inglese; una
    /// chiave che non esiste diventa <c>#chiave</c>, che si vede subito a schermo e nei test. Chi
    /// mostra testo ascolta <see cref="LanguageChanged"/> e si ridisegna: nessuno controlla a ogni frame.
    /// </summary>
    public sealed class Localizer
    {
        public const string DefaultLanguage = "en";

        private readonly StringTable _table;
        private readonly int _fallback;
        private int _current;

        public Localizer(StringTable table, string language = DefaultLanguage)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _fallback = table.IndexOf(DefaultLanguage);
            if (_fallback < 0)
            {
                throw new ArgumentException("La tabella deve avere la colonna dell'inglese", nameof(table));
            }

            _current = _fallback;
            SetLanguage(language);
        }

        public event Action LanguageChanged;

        /// <summary>Il codice ISO della lingua attiva: en, it…</summary>
        public string Language => _table.Languages[_current];

        public IReadOnlyList<string> Languages => _table.Languages;

        /// <summary>Cambia lingua; false, senza cambiare niente, se la tabella non la conosce.</summary>
        public bool SetLanguage(string language)
        {
            int index = language != null ? _table.IndexOf(language) : -1;
            if (index < 0)
            {
                return false;
            }

            if (index != _current)
            {
                _current = index;
                LanguageChanged?.Invoke();
            }

            return true;
        }

        /// <summary>Passa alla lingua successiva della tabella, e dall'ultima alla prima.</summary>
        public void CycleLanguage()
        {
            SetLanguage(_table.Languages[(_current + 1) % _table.Languages.Count]);
        }

        /// <summary>Il testo della chiave: la stringa della tabella, senza copie.</summary>
        public string Get(string key)
        {
            if (_table.TryGet(key, _current, out var value) || _table.TryGet(key, _fallback, out value))
            {
                return value;
            }

            return "#" + key;
        }

        /// <summary>Il testo della chiave con i segnaposto {0}, {1}… riempiti, sempre con le cifre invarianti.</summary>
        public string Format(string key, object arg0)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(key), arg0);
        }

        public string Format(string key, object arg0, object arg1)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(key), arg0, arg1);
        }
    }
}
