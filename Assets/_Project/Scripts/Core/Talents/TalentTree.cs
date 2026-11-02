using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DarkDescent.Talents
{
    /// <summary>
    /// Un albero delle abilità (D13 della M20): quello di una classe, a sinistra nel pannello, o
    /// quello di una sua specializzazione, a destra. Nodi in una griglia, legati alla riga sopra, e
    /// le linee tratteggiate dopo cui servono più punti. Lo costruisce lo strumento dell'editor dal
    /// disegno approvato; le regole di cosa si può prendere stanno in <see cref="TalentBuild"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "TalentTree", menuName = "DarkDescent/Talent tree")]
    public sealed class TalentTree : ScriptableObject
    {
        [Tooltip("Mai più cambiato una volta pubblicato: è quello che salva il salvataggio.")]
        [SerializeField] private string _id;

        [Tooltip("La chiave del nome nella tabella delle stringhe, come talent.tree.warrior.")]
        [SerializeField] private string _nameKey;

        [Tooltip("Vero per una specializzazione, che si sceglie dal livello 10; falso per l'albero della classe.")]
        [SerializeField] private bool _specialization;

        [SerializeField] private TalentNode[] _nodes = Array.Empty<TalentNode>();

        [SerializeField] private TalentGate[] _gates = Array.Empty<TalentGate>();

        public string Id => _id;
        public string NameKey => _nameKey;
        public bool IsSpecialization => _specialization;
        public IReadOnlyList<TalentNode> Nodes => _nodes ?? Array.Empty<TalentNode>();
        public IReadOnlyList<TalentGate> Gates => _gates ?? Array.Empty<TalentGate>();

        /// <summary>Il nodo senza legami, preso gratis; null in un albero sbagliato.</summary>
        public TalentNode Root => Nodes.FirstOrDefault(n => n.IsRoot);

        /// <summary>Quanti gradi ha l'albero, la radice compresa.</summary>
        public int TotalRanks => Nodes.Sum(n => n.Ranks);

        /// <summary>Il nodo con questo id, o null.</summary>
        public TalentNode Find(string id)
        {
            foreach (var node in Nodes)
            {
                if (node.Id == id)
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>I punti da spendere nelle righe sopra per aprire la riga <paramref name="row"/>.</summary>
        public int PointsFor(int row)
        {
            int points = 0;
            foreach (var gate in Gates)
            {
                if (gate.Row <= row)
                {
                    points = Math.Max(points, gate.Points);
                }
            }

            return points;
        }

        /// <summary>Riempie l'albero: per lo strumento dell'editor e per i test.</summary>
        public void Define(string id, string nameKey, bool specialization, TalentNode[] nodes, TalentGate[] gates)
        {
            _id = id;
            _nameKey = nameKey;
            _specialization = specialization;
            _nodes = nodes ?? Array.Empty<TalentNode>();
            _gates = gates ?? Array.Empty<TalentGate>();
        }

        /// <summary>
        /// Cosa non va nella forma dell'albero, vuoto se niente: una radice sola e di un grado, id e
        /// posti unici, legami solo verso la riga sopra, una o due opzioni, i nodi a scelta di un grado.
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrEmpty(_id))
            {
                errors.Add("l'albero non ha un id");
            }

            var roots = Nodes.Where(n => n.IsRoot).ToList();
            if (roots.Count != 1)
            {
                errors.Add($"{roots.Count} radici invece di una");
            }
            else if (roots[0].Ranks != 1 || roots[0].Kind == TalentNodeKind.Choice)
            {
                errors.Add($"la radice {roots[0].Id} deve avere un grado e un'opzione");
            }

            var ids = new HashSet<string>();
            var cells = new HashSet<(int, int)>();
            foreach (var node in Nodes)
            {
                if (string.IsNullOrEmpty(node.Id) || !ids.Add(node.Id))
                {
                    errors.Add($"id vuoto o ripetuto: '{node.Id}'");
                }

                if (!cells.Add((node.Row, node.Column)))
                {
                    errors.Add($"{node.Id}: il posto {node.Row},{node.Column} è già occupato");
                }

                if (node.Options.Length < 1 || node.Options.Length > 2)
                {
                    errors.Add($"{node.Id}: {node.Options.Length} opzioni");
                }

                if (node.Kind == TalentNodeKind.Choice && node.Ranks != 1)
                {
                    errors.Add($"{node.Id}: un nodo a scelta ha un grado solo");
                }

                foreach (string link in node.Links)
                {
                    var parent = Find(link);
                    if (parent == null)
                    {
                        errors.Add($"{node.Id}: il legame {link} non c'è");
                    }
                    else if (parent.Row != node.Row - 1)
                    {
                        errors.Add($"{node.Id}: il legame {link} non è nella riga sopra");
                    }
                }
            }

            return errors;
        }
    }
}
