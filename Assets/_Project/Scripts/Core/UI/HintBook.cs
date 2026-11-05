using System.Collections.Generic;

namespace DarkDescent.UI
{
    /// <summary>
    /// La memoria dei suggerimenti (D12 e D13 della M23), senza Unity: quali sono già stati mostrati, quali aspettano
    /// il loro turno, e se i suggerimenti sono accesi. Un suggerimento si mostra una volta sola, uno alla volta, nell'ordine
    /// di <see cref="Hints"/>; spenti, nessuno entra in coda. Si salva come una riga di id separati da virgole.
    /// </summary>
    public sealed class HintBook
    {
        private readonly HashSet<string> _seen = new HashSet<string>();
        private readonly List<string> _queue = new List<string>();
        private bool _enabled = true;

        /// <summary>Accesi di default; spenti non entra niente in coda e la coda si svuota.</summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (!value)
                {
                    _queue.Clear();
                }
            }
        }

        public int QueuedCount => _queue.Count;

        public int SeenCount => _seen.Count;

        public bool HasSeen(string id) => _seen.Contains(id);

        public bool IsQueued(string id) => _queue.Contains(id);

        /// <summary>
        /// Un momento è arrivato: il suggerimento va in coda se è acceso, non è già stato visto e non è già in attesa.
        /// Restituisce vero solo se è entrato.
        /// </summary>
        public bool Raise(string id)
        {
            if (!_enabled || string.IsNullOrEmpty(id) || _seen.Contains(id) || _queue.Contains(id))
            {
                return false;
            }

            // in ordine di precedenza, a parità dopo chi c'era già
            int at = _queue.Count;
            int order = Hints.OrderOf(id);
            while (at > 0 && Hints.OrderOf(_queue[at - 1]) > order)
            {
                at--;
            }

            _queue.Insert(at, id);
            return true;
        }

        /// <summary>Il prossimo da mostrare: esce dalla coda e conta come visto. Falso se non c'è niente o è spento.</summary>
        public bool TryNext(out string id)
        {
            id = null;
            if (!_enabled || _queue.Count == 0)
            {
                return false;
            }

            id = _queue[0];
            _queue.RemoveAt(0);
            _seen.Add(id);
            return true;
        }

        /// <summary>«Rivedi i suggerimenti»: tutti tornano nuovi, la coda si svuota.</summary>
        public void Reset()
        {
            _seen.Clear();
            _queue.Clear();
        }

        /// <summary>Gli id già visti, separati da virgole, in ordine di precedenza (stabile tra un salvataggio e l'altro).</summary>
        public string Serialize()
        {
            var ids = new List<string>(_seen);
            ids.Sort((a, b) =>
            {
                int byOrder = Hints.OrderOf(a).CompareTo(Hints.OrderOf(b));
                return byOrder != 0 ? byOrder : string.CompareOrdinal(a, b);
            });
            return string.Join(",", ids);
        }

        /// <summary>Rimette gli id letti da <see cref="Serialize"/>; righe vuote o sporche non fanno danno.</summary>
        public void Load(string serialized)
        {
            _seen.Clear();
            if (string.IsNullOrWhiteSpace(serialized))
            {
                return;
            }

            foreach (string part in serialized.Split(','))
            {
                string id = part.Trim();
                if (id.Length > 0)
                {
                    _seen.Add(id);
                }
            }
        }
    }
}
