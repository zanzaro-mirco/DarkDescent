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

            // formato 2 → 3 (D11 della M9): arrivano incantesimi e mana; un salvataggio vecchio conosce
            // solo il Dardo di fuoco, che è noto dall'inizio, e riparte con il mana pieno
            if (data.Version == 2)
            {
                data.UpgradeTo(3);
            }

            // formato 3 → 4 (D11 della M10): arrivano oro, città e contatori; un salvataggio vecchio
            // riparte senza oro, nel dungeon dov'era, con i contatori a zero
            if (data.Version == 3)
            {
                data.UpgradeTo(4);
            }

            // formato 4 → 5 (10.9 della M10): arrivano il punto del portale e i ricordi dei livelli; un
            // salvataggio vecchio riporta all'ingresso e rigenera i livelli come nuovi
            if (data.Version == 4)
            {
                data.UpgradeTo(5);
            }

            // formato 5 → 6 (D14 della M14): arriva la difficoltà; un salvataggio vecchio è al Crepuscolo,
            // completato se il re era già battuto
            if (data.Version == 5)
            {
                data.UpgradeTo(6);
            }

            // formato 6 → 7 (D13 della M16): ogni oggetto può avere l'id di un unico e i suoi valori; un
            // oggetto vecchio non li ha, e resta quello che era
            if (data.Version == 6)
            {
                data.UpgradeTo(7);
            }

            // formato 7 → 8 (D4 della M20): arriva l'albero delle abilità; un salvataggio vecchio non ha
            // nodi presi, e riparte con tutti i punti del suo livello da spendere
            if (data.Version == 7)
            {
                data.UpgradeTo(8);
            }

            // formato 8 → 9 (D9 della M21): arrivano la classe e il nome; un salvataggio vecchio è un
            // Guerriero senza nome, che la schermata chiama con il nome di partenza
            if (data.Version == 8)
            {
                data.UpgradeTo(9);
            }

            return data;
        }
    }
}
