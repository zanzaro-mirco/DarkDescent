using DarkDescent.Localization;

namespace DarkDescent.Rendering
{
    /// <summary>Il giro delle scelte del filtro retrò e il loro nome nelle opzioni. Logica pura.</summary>
    public static class RetroModes
    {
        /// <summary>Quello di una partita nuova, e di chi non l'ha mai cambiato.</summary>
        public const RetroMode Default = RetroMode.Light;

        /// <summary>Spento, leggero, pieno, e di nuovo spento.</summary>
        public static RetroMode Next(RetroMode mode)
        {
            return mode switch
            {
                RetroMode.Off => RetroMode.Light,
                RetroMode.Light => RetroMode.Full,
                _ => RetroMode.Off,
            };
        }

        /// <summary>Il valore delle preferenze; uno che non è una scelta vale quella predefinita.</summary>
        public static RetroMode FromPreference(int value)
        {
            return value >= (int)RetroMode.Off && value <= (int)RetroMode.Full ? (RetroMode)value : Default;
        }

        public static string NameKey(RetroMode mode)
        {
            return mode switch
            {
                RetroMode.Off => TextKeys.RetroOff,
                RetroMode.Full => TextKeys.RetroFull,
                _ => TextKeys.RetroLight,
            };
        }
    }
}
