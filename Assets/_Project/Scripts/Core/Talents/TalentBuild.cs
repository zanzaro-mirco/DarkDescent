using System;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Magic;
using DarkDescent.Stats;

namespace DarkDescent.Talents
{
    /// <summary>
    /// I nodi presi da un personaggio (D6, D10, D11 e D13 della M20): l'albero della classe, la
    /// specializzazione scelta dal livello 10 e i gradi di ogni nodo. Le modifiche si provano su uno
    /// stato in attesa e valgono solo con <see cref="Apply"/>, come *Applica modifiche* di WoW; prima
    /// si tolgono gratis. Il gioco legge soltanto lo stato applicato. Logica pura: il componente del
    /// cavaliere la crea e la collega alla scheda, al livello e al salvataggio.
    /// </summary>
    public sealed class TalentBuild
    {
        private readonly TalentTree _class;
        private readonly TalentTree[] _specializations;
        private State _applied = new State();
        private State _pending = new State();

        public TalentBuild(TalentTree classTree, IEnumerable<TalentTree> specializations, int level = 1)
        {
            _class = classTree != null ? classTree : throw new ArgumentNullException(nameof(classTree));
            _specializations = (specializations ?? Enumerable.Empty<TalentTree>()).Where(s => s != null).ToArray();
            Level = Math.Max(1, level);
        }

        /// <summary>Cambiato lo stato in attesa, o il livello: il pannello si ridisegna.</summary>
        public event Action Changed;

        /// <summary>Cambiato lo stato applicato: le statistiche e la barra si rifanno.</summary>
        public event Action Applied;

        public int Level { get; private set; }

        public TalentTree ClassTree => _class;

        public IReadOnlyList<TalentTree> Specializations => _specializations;

        /// <summary>La specializzazione nello stato in attesa, o null.</summary>
        public TalentTree Specialization => _pending.Specialization;

        /// <summary>La specializzazione applicata, o null.</summary>
        public TalentTree AppliedSpecialization => _applied.Specialization;

        /// <summary>Ci sono modifiche da applicare o da annullare.</summary>
        public bool HasChanges => !_pending.SameAs(_applied);

        public void SetLevel(int level)
        {
            level = Math.Max(1, level);
            if (level == Level)
            {
                return;
            }

            Level = level;
            Changed?.Invoke();
        }

        /// <summary>I punti che il livello dà a questo albero.</summary>
        public int Budget(TalentTree tree)
        {
            return tree == _class ? TalentPoints.ForClass(Level) : TalentPoints.ForSpecialization(Level);
        }

        /// <summary>I punti spesi nell'albero nello stato in attesa, la radice esclusa.</summary>
        public int Spent(TalentTree tree) => _pending.Spent(tree);

        /// <summary>I punti ancora da spendere nell'albero.</summary>
        public int Available(TalentTree tree) => Math.Max(0, Budget(tree) - Spent(tree));

        /// <summary>I gradi presi nello stato in attesa; la radice di un albero attivo ne ha uno.</summary>
        public int Rank(TalentTree tree, TalentNode node) => _pending.Rank(tree, node, _class);

        /// <summary>I gradi presi nello stato applicato.</summary>
        public int AppliedRank(TalentTree tree, TalentNode node) => _applied.Rank(tree, node, _class);

        /// <summary>L'opzione presa nello stato in attesa; −1 se il nodo non è preso.</summary>
        public int OptionOf(TalentTree tree, TalentNode node)
        {
            if (node == null)
            {
                return -1;
            }

            if (node.IsRoot)
            {
                return _pending.IsActive(tree, _class) ? 0 : -1;
            }

            return _pending.Get(tree, node.Id, out var taken) ? taken.Option : -1;
        }

        /// <summary>Il nodo si può aprire: un legame ha tutti i gradi e i punti sopra la sua riga bastano.</summary>
        public bool IsOpen(TalentTree tree, TalentNode node) => _pending.IsActive(tree, _class) && node != null && _pending.IsOpen(tree, node, _class);

        public bool CanChoose(TalentTree specialization)
        {
            return specialization != null && _pending.Specialization == null && Level >= TalentPoints.SpecializationLevel
                && Array.IndexOf(_specializations, specialization) >= 0;
        }

        /// <summary>Sceglie la specializzazione, in attesa come i nodi; la sua radice è gratis.</summary>
        public bool TryChoose(TalentTree specialization)
        {
            if (!CanChoose(specialization))
            {
                return false;
            }

            _pending.Specialization = specialization;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Una specializzazione scelta e non ancora applicata, senza nodi presi, si lascia.</summary>
        public bool TryUnchoose()
        {
            var specialization = _pending.Specialization;
            if (specialization == null || _applied.Specialization != null || _pending.Spent(specialization) > 0)
            {
                return false;
            }

            _pending.Specialization = null;
            Changed?.Invoke();
            return true;
        }

        public bool CanAdd(TalentTree tree, TalentNode node, int option = 0) => _pending.CanAdd(tree, node, option, Budget(tree), _class);

        /// <summary>Un grado in più al nodo, nello stato in attesa; per un nodo a scelta, con l'opzione data.</summary>
        public bool TryAdd(TalentTree tree, TalentNode node, int option = 0)
        {
            if (!CanAdd(tree, node, option))
            {
                return false;
            }

            _pending.Add(tree, node.Id, option);
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Si toglie solo un grado preso dopo l'ultimo <see cref="Apply"/>, e solo se nessun altro nodo
        /// preso resta senza il suo legame o i suoi punti.
        /// </summary>
        public bool CanRemove(TalentTree tree, TalentNode node)
        {
            if (node == null || node.IsRoot || Rank(tree, node) <= AppliedRank(tree, node))
            {
                return false;
            }

            var trial = _pending.Clone();
            trial.Remove(tree, node.Id);
            return IsValid(trial);
        }

        public bool TryRemove(TalentTree tree, TalentNode node)
        {
            if (!CanRemove(tree, node))
            {
                return false;
            }

            _pending.Remove(tree, node.Id);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Le modifiche in attesa diventano vere.</summary>
        public void Apply()
        {
            if (!HasChanges)
            {
                return;
            }

            _applied = _pending.Clone();
            Applied?.Invoke();
            Changed?.Invoke();
        }

        /// <summary>Si torna allo stato applicato.</summary>
        public void Cancel()
        {
            if (!HasChanges)
            {
                return;
            }

            _pending = _applied.Clone();
            Changed?.Invoke();
        }

        /// <summary>Si riparte da zero, specializzazione compresa: l'altare (D11).</summary>
        public void Reset()
        {
            _applied = new State();
            _pending = new State();
            Applied?.Invoke();
            Changed?.Invoke();
        }

        /// <summary>I nodi applicati, per il salvataggio; le radici no, sono gratis.</summary>
        public List<TalentRecord> Export()
        {
            return _applied.Records().ToList();
        }

        /// <summary>
        /// Rimette uno stato salvato al livello dato, che va rimesso prima. Non lancia mai: gli id
        /// sconosciuti si ignorano, e un nodo che con gli alberi di adesso non si potrebbe prendere
        /// (legame mancante, punti finiti, linea non passata) si lascia, con i suoi punti da spendere.
        /// I nodi si riprendono riga per riga, così i legami arrivano prima di chi li usa.
        /// </summary>
        public void Restore(int level, string specialization, IEnumerable<TalentRecord> records)
        {
            Level = Math.Max(1, level);
            var state = new State();
            var chosen = _specializations.FirstOrDefault(s => s.Id == specialization);
            if (chosen != null && Level >= TalentPoints.SpecializationLevel)
            {
                state.Specialization = chosen;
            }

            var saved = (records ?? Enumerable.Empty<TalentRecord>()).ToList();
            foreach (var tree in state.ActiveTrees(_class))
            {
                foreach (var node in tree.Nodes.Where(n => !n.IsRoot).OrderBy(n => n.Row).ThenBy(n => n.Column))
                {
                    var record = saved.FirstOrDefault(r => r.Tree == tree.Id && r.Node == node.Id);
                    for (int rank = 0; rank < Math.Min(record.Rank, node.Ranks); rank++)
                    {
                        if (!state.CanAdd(tree, node, record.Option, Budget(tree), _class))
                        {
                            break;
                        }

                        state.Add(tree, node.Id, record.Option);
                    }
                }
            }

            _applied = state;
            _pending = state.Clone();
            Applied?.Invoke();
            Changed?.Invoke();
        }

        /// <summary>Le abilità dei nodi applicati, radici comprese, nell'ordine degli alberi.</summary>
        public List<SpellDefinition> Abilities()
        {
            return _applied.Options(_class).Select(t => t.option.Ability).Where(a => a != null).ToList();
        }

        /// <summary>
        /// I modificatori della scheda dei nodi applicati la cui condizione vale secondo
        /// <paramref name="holds"/>, con <paramref name="source"/> come sorgente, per toglierli insieme.
        /// </summary>
        public void CollectModifiers(List<StatModifier> into, object source, Func<TalentCondition, bool> holds)
        {
            foreach (var (effect, rank) in AppliedEffects(TalentEffectKind.Stat))
            {
                if (holds == null ? effect.Condition == TalentCondition.None : holds(effect.Condition))
                {
                    into.Add(new StatModifier(effect.Stat, effect.Modifier, effect.Value(rank), source));
                }
            }
        }

        /// <summary>La somma di una statistica data dai nodi applicati con questa condizione: per quelle che si guardano al colpo.</summary>
        public float StatBonus(StatType stat, TalentCondition condition)
        {
            return AppliedEffects(TalentEffectKind.Stat).Where(e => e.effect.Stat == stat && e.effect.Condition == condition).Sum(e => e.effect.Value(e.rank));
        }

        /// <summary>Quanto i nodi applicati aggiungono a un numero di un'abilità, nella sua unità.</summary>
        public float AbilityBonus(SpellDefinition ability, AbilityParam param)
        {
            return AppliedEffects(TalentEffectKind.Ability).Where(e => e.effect.Ability == ability && e.effect.Param == param).Sum(e => e.effect.Value(e.rank));
        }

        /// <summary>
        /// Un numero di un'abilità con i nodi applicati: quello dell'asset più i loro bonus, mai sotto
        /// zero. La ricarica della Carica con Slancio, la durata della Pelle di pietra con Seconda pelle.
        /// </summary>
        public float AbilityValue(SpellDefinition ability, AbilityParam param)
        {
            return Math.Max(0f, BaseValue(ability, param) + AbilityBonus(ability, param));
        }

        /// <summary>Il numero di un'abilità come lo dice il suo asset, senza l'albero.</summary>
        public static float BaseValue(SpellDefinition ability, AbilityParam param)
        {
            if (ability == null)
            {
                return 0f;
            }

            switch (param)
            {
                case AbilityParam.Cooldown:
                    return ability.Cooldown;
                case AbilityParam.ManaCost:
                    return ability.ManaCost;
                case AbilityParam.Damage:
                    return ability.WeaponPercent;
                case AbilityParam.Duration:
                    return ability.Duration;
                case AbilityParam.Radius:
                    return ability.Radius;
                case AbilityParam.Stun:
                    return ability.Stun;
                case AbilityParam.Threshold:
                    return ability.Threshold;
                default:
                    return 0f;
            }
        }

        /// <summary>Gli effetti dei nodi applicati che scattano a quel momento, con i loro gradi.</summary>
        public List<(TalentEffect effect, int rank)> Triggers(TalentTrigger trigger)
        {
            return AppliedEffects(TalentEffectKind.Trigger).Where(e => e.effect.Trigger == trigger).ToList();
        }

        private IEnumerable<(TalentEffect effect, int rank)> AppliedEffects(TalentEffectKind kind)
        {
            foreach (var (option, rank) in _applied.Options(_class))
            {
                foreach (var effect in option.Effects)
                {
                    if (effect != null && effect.Kind == kind)
                    {
                        yield return (effect, rank);
                    }
                }
            }
        }

        private bool IsValid(State state)
        {
            foreach (var tree in state.ActiveTrees(_class))
            {
                if (state.Spent(tree) > Budget(tree))
                {
                    return false;
                }

                foreach (var node in tree.Nodes)
                {
                    if (!node.IsRoot && state.Get(tree, node.Id, out _) && !state.IsOpen(tree, node, _class))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // un nodo preso: i gradi e l'opzione
        private struct Taken
        {
            public int Rank;
            public int Option;
        }

        // la specializzazione e i nodi presi (le radici no: sono prese con l'albero)
        private sealed class State
        {
            private readonly Dictionary<TalentTree, Dictionary<string, Taken>> _trees = new Dictionary<TalentTree, Dictionary<string, Taken>>();

            public TalentTree Specialization;

            public IEnumerable<TalentTree> ActiveTrees(TalentTree classTree)
            {
                yield return classTree;
                if (Specialization != null)
                {
                    yield return Specialization;
                }
            }

            public bool IsActive(TalentTree tree, TalentTree classTree) => tree != null && (tree == classTree || tree == Specialization);

            public bool Get(TalentTree tree, string node, out Taken taken)
            {
                taken = default;
                return tree != null && _trees.TryGetValue(tree, out var nodes) && nodes.TryGetValue(node, out taken);
            }

            public int Rank(TalentTree tree, TalentNode node, TalentTree classTree)
            {
                if (node == null || !IsActive(tree, classTree))
                {
                    return 0;
                }

                return node.IsRoot ? 1 : Get(tree, node.Id, out var taken) ? taken.Rank : 0;
            }

            public int Spent(TalentTree tree)
            {
                return tree != null && _trees.TryGetValue(tree, out var nodes) ? nodes.Values.Sum(t => t.Rank) : 0;
            }

            // i punti spesi nelle righe sopra quella data: quelli che contano per le linee
            public int SpentAbove(TalentTree tree, int row)
            {
                if (tree == null || !_trees.TryGetValue(tree, out var nodes))
                {
                    return 0;
                }

                int spent = 0;
                foreach (var pair in nodes)
                {
                    var node = tree.Find(pair.Key);
                    if (node != null && node.Row < row)
                    {
                        spent += pair.Value.Rank;
                    }
                }

                return spent;
            }

            public bool IsOpen(TalentTree tree, TalentNode node, TalentTree classTree)
            {
                if (node.IsRoot)
                {
                    return true;
                }

                bool linked = node.Links.Any(link =>
                {
                    var parent = tree.Find(link);
                    return parent != null && Rank(tree, parent, classTree) >= parent.Ranks;
                });
                return linked && SpentAbove(tree, node.Row) >= tree.PointsFor(node.Row);
            }

            public bool CanAdd(TalentTree tree, TalentNode node, int option, int budget, TalentTree classTree)
            {
                if (!IsActive(tree, classTree) || node == null || node.IsRoot || tree.Find(node.Id) != node)
                {
                    return false;
                }

                if (option < 0 || option >= node.Options.Length)
                {
                    return false;
                }

                bool taken = Get(tree, node.Id, out var current);
                if (taken && (current.Rank >= node.Ranks || current.Option != option))
                {
                    return false;
                }

                return Spent(tree) < budget && IsOpen(tree, node, classTree);
            }

            public void Add(TalentTree tree, string node, int option)
            {
                if (!_trees.TryGetValue(tree, out var nodes))
                {
                    nodes = new Dictionary<string, Taken>();
                    _trees[tree] = nodes;
                }

                nodes.TryGetValue(node, out var taken);
                nodes[node] = new Taken { Rank = taken.Rank + 1, Option = option };
            }

            public void Remove(TalentTree tree, string node)
            {
                if (!_trees.TryGetValue(tree, out var nodes) || !nodes.TryGetValue(node, out var taken))
                {
                    return;
                }

                if (taken.Rank <= 1)
                {
                    nodes.Remove(node);
                }
                else
                {
                    nodes[node] = new Taken { Rank = taken.Rank - 1, Option = taken.Option };
                }
            }

            // le opzioni prese con i loro gradi, radici comprese, albero per albero e riga per riga
            public IEnumerable<(TalentOption option, int rank)> Options(TalentTree classTree)
            {
                foreach (var tree in ActiveTrees(classTree))
                {
                    foreach (var node in tree.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Column))
                    {
                        if (node.IsRoot)
                        {
                            if (node.Options.Length > 0)
                            {
                                yield return (node.Options[0], 1);
                            }
                        }
                        else if (Get(tree, node.Id, out var taken) && taken.Option < node.Options.Length)
                        {
                            yield return (node.Options[taken.Option], taken.Rank);
                        }
                    }
                }
            }

            public IEnumerable<TalentRecord> Records()
            {
                foreach (var tree in _trees)
                {
                    foreach (var node in tree.Value)
                    {
                        yield return new TalentRecord(tree.Key.Id, node.Key, node.Value.Rank, node.Value.Option);
                    }
                }
            }

            public State Clone()
            {
                var copy = new State { Specialization = Specialization };
                foreach (var tree in _trees)
                {
                    copy._trees[tree.Key] = new Dictionary<string, Taken>(tree.Value);
                }

                return copy;
            }

            public bool SameAs(State other)
            {
                if (other.Specialization != Specialization)
                {
                    return false;
                }

                var mine = Records().OrderBy(r => r.Tree).ThenBy(r => r.Node).ToList();
                var theirs = other.Records().OrderBy(r => r.Tree).ThenBy(r => r.Node).ToList();
                return mine.SequenceEqual(theirs);
            }
        }
    }
}
