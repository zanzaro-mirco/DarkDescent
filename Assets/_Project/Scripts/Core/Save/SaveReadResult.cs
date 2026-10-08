namespace DarkDescent.Save
{
    /// <summary>Com'è andata la lettura del salvataggio.</summary>
    public enum SaveReadResult
    {
        Ok,

        /// <summary>Nessun salvataggio: si comincia una partita nuova.</summary>
        Missing,

        /// <summary>Il file c'è ma non si legge: vuoto, troncato o non nostro.</summary>
        Corrupt,

        /// <summary>L'ha scritto una versione più nuova del gioco.</summary>
        TooNew,
    }
}
