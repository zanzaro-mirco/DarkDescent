using System;
using System.IO;

namespace DarkDescent.Save
{
    /// <summary>
    /// Il file del salvataggio su disco (D7 della M8). Si scrive in un file accanto e poi lo si mette
    /// al posto del vecchio: un crash a metà scrittura lascia il salvataggio di prima, mai uno a metà.
    /// </summary>
    public static class SaveFile
    {
        public const string TemporarySuffix = ".tmp";

        public static void Write(string path, SaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporary = path + TemporarySuffix;
            File.WriteAllText(temporary, data.ToJson());
            if (File.Exists(path))
            {
                File.Replace(temporary, path, null);
            }
            else
            {
                File.Move(temporary, path);
            }
        }

        /// <summary>
        /// Legge il salvataggio, portandolo al formato corrente se è più vecchio (D9 della M8).
        /// <see cref="SaveReadResult.TooNew"/> se l'ha scritto una versione del gioco più nuova di
        /// questa: non si carica, per non perdere quello che non capiamo.
        /// </summary>
        public static SaveReadResult TryRead(string path, out SaveData data)
        {
            data = null;
            if (!File.Exists(path))
            {
                return SaveReadResult.Missing;
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (IOException)
            {
                return SaveReadResult.Corrupt;
            }

            var read = SaveData.FromJson(json);
            if (read == null)
            {
                return SaveReadResult.Corrupt;
            }

            if (read.Version > SaveData.CurrentVersion)
            {
                return SaveReadResult.TooNew;
            }

            data = SaveMigrator.Migrate(read);
            return SaveReadResult.Ok;
        }
    }
}
