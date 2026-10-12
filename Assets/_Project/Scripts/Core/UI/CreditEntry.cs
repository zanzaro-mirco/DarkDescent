namespace DarkDescent.UI
{
    /// <summary>Una riga dei riconoscimenti: l'opera, chi l'ha fatta e la licenza, già senza markdown.</summary>
    public readonly struct CreditEntry
    {
        public readonly string Work;
        public readonly string Author;
        public readonly string License;

        public CreditEntry(string work, string author, string license)
        {
            Work = work;
            Author = author;
            License = license;
        }
    }
}
