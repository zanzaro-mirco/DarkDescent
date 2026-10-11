using System;
using System.IO;

namespace DarkDescent.Save
{
    /// <summary>
    /// I tre posti di salvataggio (D2 della M10): un file per posto, <c>save_1.json</c>…<c>save_3.json</c>,
    /// nella cartella dei salvataggi. Il riepilogo di un posto si legge dal salvataggio stesso;
    /// l'ultimo giocato è quello scritto per ultimo. Il <c>save.json</c> di prima della M10 diventa il
    /// posto 1: si sposta, non si cancella. I posti vanno da 1 a <see cref="Count"/>.
    /// </summary>
    public sealed class SaveSlots
    {
        public const int Count = 3;

        /// <summary>Il file unico di prima della M10.</summary>
        public const string LegacyFile = "save.json";

        private readonly string _folder;

        public SaveSlots(string folder)
        {
            _folder = folder ?? throw new ArgumentNullException(nameof(folder));
        }

        public string Folder => _folder;

        public string PathOf(int slot)
        {
            if (slot < 1 || slot > Count)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            return Path.Combine(_folder, $"save_{slot}.json");
        }

        public bool IsUsed(int slot)
        {
            return File.Exists(PathOf(slot));
        }

        /// <summary>Il salvataggio di un posto, già migrato; false se il posto è vuoto o il file non si legge.</summary>
        public bool TryRead(int slot, out SaveData data)
        {
            data = null;
            return IsUsed(slot) && SaveFile.TryRead(PathOf(slot), out data) == SaveReadResult.Ok;
        }

        /// <summary>Quando il posto è stato scritto l'ultima volta, in UTC; null se è vuoto.</summary>
        public DateTime? LastWrite(int slot)
        {
            return IsUsed(slot) ? File.GetLastWriteTimeUtc(PathOf(slot)) : (DateTime?)null;
        }

        /// <summary>Il posto scritto per ultimo tra quelli che si leggono; 0 se non ce n'è nessuno.</summary>
        public int LastPlayed()
        {
            int best = 0;
            DateTime newest = DateTime.MinValue;
            for (int slot = 1; slot <= Count; slot++)
            {
                DateTime? written = LastWrite(slot);
                if (written.HasValue && written.Value > newest && TryRead(slot, out _))
                {
                    newest = written.Value;
                    best = slot;
                }
            }

            return best;
        }

        /// <summary>Il primo posto vuoto; 0 se sono tutti occupati.</summary>
        public int FirstFree()
        {
            for (int slot = 1; slot <= Count; slot++)
            {
                if (!IsUsed(slot))
                {
                    return slot;
                }
            }

            return 0;
        }

        /// <summary>Cancella un posto, chiesto dal menu dopo la conferma.</summary>
        public void Delete(int slot)
        {
            string path = PathOf(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Sposta il <c>save.json</c> di prima nel posto 1, se c'è e il posto 1 è vuoto; true se l'ha
        /// spostato. Lo spostamento è una rinomina: il file non si perde nemmeno a metà.
        /// </summary>
        public bool MigrateLegacy()
        {
            string legacy = Path.Combine(_folder, LegacyFile);
            string first = PathOf(1);
            if (!File.Exists(legacy) || File.Exists(first))
            {
                return false;
            }

            File.Move(legacy, first);
            return true;
        }
    }
}
