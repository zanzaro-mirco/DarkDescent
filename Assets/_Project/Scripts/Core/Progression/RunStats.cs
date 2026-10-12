namespace DarkDescent.Progression
{
    /// <summary>
    /// I contatori della partita per la schermata di vittoria (D9 e D11 della M10): tempo di gioco,
    /// nemici uccisi, morti, oro raccolto e se il boss è già stato sconfitto. Logica pura; li
    /// aggiorna il CompositionRoot e passano dal salvataggio.
    /// </summary>
    public sealed class RunStats
    {
        public float PlayTime { get; private set; }

        public int Kills { get; private set; }

        public int Deaths { get; private set; }

        public int GoldCollected { get; private set; }

        /// <summary>Il boss è morto: tornando alla sua profondità non rinasce (trappola 4 della M10).</summary>
        public bool BossDefeated { get; private set; }

        public void AddTime(float seconds)
        {
            if (seconds > 0f)
            {
                PlayTime += seconds;
            }
        }

        public void AddKill()
        {
            Kills++;
        }

        public void AddDeath()
        {
            Deaths++;
        }

        public void AddGold(int amount)
        {
            if (amount > 0)
            {
                GoldCollected += amount;
            }
        }

        public void MarkBossDefeated()
        {
            BossDefeated = true;
        }

        /// <summary>Il tempo di gioco come si legge: "12:05" sotto l'ora, "1:02:05" da un'ora in su.</summary>
        public static string FormatTime(float seconds)
        {
            int total = seconds > 0f ? (int)seconds : 0;
            int hours = total / 3600;
            int minutes = total / 60 % 60;
            int rest = total % 60;
            return hours > 0 ? $"{hours}:{minutes:00}:{rest:00}" : $"{minutes}:{rest:00}";
        }

        /// <summary>I contatori di un salvataggio.</summary>
        public void Restore(float playTime, int kills, int deaths, int goldCollected, bool bossDefeated)
        {
            PlayTime = playTime < 0f ? 0f : playTime;
            Kills = kills < 0 ? 0 : kills;
            Deaths = deaths < 0 ? 0 : deaths;
            GoldCollected = goldCollected < 0 ? 0 : goldCollected;
            BossDefeated = bossDefeated;
        }
    }
}
