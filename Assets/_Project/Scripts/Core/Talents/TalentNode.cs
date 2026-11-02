using System;
using UnityEngine;

namespace DarkDescent.Talents
{
    /// <summary>
    /// Un nodo dell'albero (D13 della M20): il posto nella griglia, i gradi, i nodi della riga sopra a
    /// cui è legato e quello che dà. Si apre quando uno dei nodi a cui è legato ha tutti i suoi gradi;
    /// il nodo senza legami è la radice dell'albero, presa gratis.
    /// </summary>
    [Serializable]
    public sealed class TalentNode
    {
        [Tooltip("Mai più cambiato una volta pubblicato: è quello che salva il salvataggio.")]
        [SerializeField] private string _id;

        [SerializeField, Min(0)] private int _row;

        [SerializeField, Min(0)] private int _column;

        [SerializeField, Min(1)] private int _ranks = 1;

        [Tooltip("Gli id dei nodi della riga sopra a cui è legato; vuoto per la radice.")]
        [SerializeField] private string[] _links = Array.Empty<string>();

        [SerializeField] private TalentOption[] _options = Array.Empty<TalentOption>();

        /// <summary>Per il serializzatore di Unity.</summary>
        public TalentNode()
        {
        }

        public TalentNode(string id, int row, int column, int ranks, string[] links, params TalentOption[] options)
        {
            _id = id;
            _row = row;
            _column = column;
            _ranks = ranks;
            _links = links ?? Array.Empty<string>();
            _options = options ?? Array.Empty<TalentOption>();
        }

        public string Id => _id;
        public int Row => _row;
        public int Column => _column;
        public int Ranks => _ranks;
        public string[] Links => _links ?? Array.Empty<string>();
        public TalentOption[] Options => _options ?? Array.Empty<TalentOption>();

        public bool IsRoot => Links.Length == 0;

        public TalentNodeKind Kind => Options.Length > 1
            ? TalentNodeKind.Choice
            : Options.Length == 1 && Options[0].Ability != null ? TalentNodeKind.Active : TalentNodeKind.Passive;
    }
}
