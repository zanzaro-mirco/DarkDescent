namespace DarkDescent.Save
{
    /// <summary>
    /// Porta un salvataggio di un formato vecchio a quello corrente, un passo alla volta (D9 della
    /// M8). Il codice del gioco legge un solo formato: tutto quello che è più vecchio passa di qui.
    /// </summary>
    public static class SaveMigrator
    {
        public static SaveData Migrate(SaveData data)
        {
            // formato 1 → 2: arriva la mappa scoperta di ogni profondità; da un salvataggio vecchio
            // non ce n'è, e i livelli ripartono coperti
            if (data.Version == 1)
            {
                data.UpgradeTo(2);
            }

            return data;
        }
    }
}
