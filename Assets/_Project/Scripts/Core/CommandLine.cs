using System;

namespace DarkDescent.Core
{
    /// <summary>Le opzioni passate all'avvio della build, come <c>-lang it</c> o <c>-seed 4711</c>.</summary>
    public static class CommandLine
    {
        /// <summary>Il valore che segue <paramref name="option"/>; false se l'opzione non c'è o non ha valore.</summary>
        public static bool TryGetValue(string[] args, string option, out string value)
        {
            value = null;
            if (args == null)
            {
                return false;
            }

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], option, StringComparison.OrdinalIgnoreCase))
                {
                    value = args[i + 1];
                    return true;
                }
            }

            return false;
        }

        /// <summary>Se c'è l'opzione, senza valore: <c>-newgame</c>.</summary>
        public static bool HasFlag(string[] args, string option)
        {
            if (args == null)
            {
                return false;
            }

            foreach (string arg in args)
            {
                if (string.Equals(arg, option, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
