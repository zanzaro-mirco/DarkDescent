using System;

namespace DarkDescent.Progression
{
    /// <summary>
    /// A che difficoltà si gioca e fin dove si è arrivati (D3 e D4 della M14): la difficoltà attuale,
    /// la più alta completata (−1 se nessuna) e il numero del mondo. Si sceglie tra quelle completate
    /// e quella subito dopo; cambiare difficoltà o rifare il mondo ne fa uno nuovo. Logica pura.
    /// </summary>
    public sealed class DifficultyProgress
    {
        private readonly int _count;

        public DifficultyProgress(int count)
        {
            _count = count > 0 ? count : throw new ArgumentOutOfRangeException(nameof(count), "Serve almeno una difficoltà");
        }

        /// <summary>Cambiati difficoltà, più alta completata o mondo: chi li mostra li rilegge.</summary>
        public event Action Changed;

        /// <summary>Il mondo è cambiato, per una difficoltà nuova o per "rifai il mondo": il dungeon va rifatto.</summary>
        public event Action<int> WorldChanged;

        public int Current { get; private set; }

        public int HighestCompleted { get; private set; } = -1;

        /// <summary>Quanti mondi nuovi ci sono stati: 0 è quello della partita nuova.</summary>
        public int World { get; private set; }

        /// <summary>Si può scegliere: completata, o la prima non ancora completata.</summary>
        public bool CanSelect(int index)
        {
            return index >= 0 && index < _count && index <= HighestCompleted + 1;
        }

        /// <summary>Passa a un'altra difficoltà aperta, in un mondo nuovo; false se è chiusa o è già quella.</summary>
        public bool Select(int index)
        {
            if (!CanSelect(index) || index == Current)
            {
                return false;
            }

            Current = index;
            AdvanceWorld();
            return true;
        }

        /// <summary>Rifà il mondo alla stessa difficoltà.</summary>
        public void NewWorld()
        {
            AdvanceWorld();
        }

        /// <summary>
        /// Il re è caduto alla difficoltà attuale: true se è la prima volta, e allora si apre la
        /// successiva (se c'è).
        /// </summary>
        public bool CompleteCurrent()
        {
            if (Current <= HighestCompleted)
            {
                return false;
            }

            HighestCompleted = Current;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Rimette uno stato salvato; valori fuori misura si correggono.</summary>
        public void Restore(int current, int highestCompleted, int world)
        {
            HighestCompleted = Math.Max(-1, Math.Min(highestCompleted, _count - 1));
            Current = Math.Max(0, Math.Min(current, Math.Min(_count - 1, HighestCompleted + 1)));
            World = Math.Max(0, world);
            Changed?.Invoke();
        }

        // prima Changed: chi rifà il mondo trova già pronti i numeri della difficoltà nuova (passo 14.7)
        private void AdvanceWorld()
        {
            World++;
            Changed?.Invoke();
            WorldChanged?.Invoke(World);
        }
    }
}
