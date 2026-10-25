using System;
using System.Collections.Generic;
using DarkDescent.Core;

namespace DarkDescent.Items
{
    /// <summary>
    /// Fa un oggetto a partire da base, livello e seme (D2, D3, D5 della M5): stesso ingresso, stesso
    /// oggetto, sempre. Prima la rarità, poi per ogni affisso il gruppo, con lo stesso peso per tutti
    /// i gruppi ammessi (tipo di oggetto, livello, gruppo libero, limiti di prefissi e suffissi), poi
    /// il grado tra i due più alti che il livello ammette (D7 della M14), poi il valore. Logica pura:
    /// si prova senza scena.
    /// </summary>
    public sealed class ItemGenerator
    {
        /// <summary>Quanti gradi di un gruppo può tirare un oggetto: i più alti che il suo livello ammette.</summary>
        public const int OpenGrades = 2;

        /// <summary>Di quanto il tiro sposta i valori di un unico o di un pezzo di set, in più o in meno (D6 della M16).</summary>
        public const double NamedSpread = 0.1;

        private readonly RarityTable _rarities;

        // i gradi di ogni gruppo in ordine crescente, nell'ordine in cui i gruppi compaiono nel pool
        private readonly List<List<AffixDefinition>> _groups = new List<List<AffixDefinition>>();

        // riusate a ogni generazione: un drop non deve creare liste nuove
        private readonly List<int> _candidates = new List<int>();
        private readonly List<AffixDefinition> _grades = new List<AffixDefinition>();
        private readonly List<ItemAffix> _rolled = new List<ItemAffix>();
        private readonly HashSet<int> _usedGroups = new HashSet<int>();

        public ItemGenerator(IReadOnlyList<AffixDefinition> pool, RarityTable rarities)
        {
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            _rarities = rarities ?? throw new ArgumentNullException(nameof(rarities));

            // un affisso senza gruppo è un gruppo da solo
            var byKey = new Dictionary<string, List<AffixDefinition>>(StringComparer.Ordinal);
            foreach (var affix in pool)
            {
                if (affix == null)
                {
                    continue;
                }

                string key = string.IsNullOrEmpty(affix.Group) ? "#" + affix.Id : affix.Group;
                if (!byKey.TryGetValue(key, out var grades))
                {
                    grades = new List<AffixDefinition>();
                    byKey.Add(key, grades);
                    _groups.Add(grades);
                }

                grades.Add(affix);
            }

            foreach (var grades in _groups)
            {
                grades.Sort((a, b) => a.Grade != b.Grade ? a.Grade.CompareTo(b.Grade) : a.MinItemLevel.CompareTo(b.MinItemLevel));
            }
        }

        /// <summary>Quanti prefissi può avere al più un oggetto di questa rarità; lo stesso vale per i suffissi.</summary>
        public static int MaxPerKind(Rarity rarity)
        {
            return rarity == Rarity.Magic ? 1 : rarity == Rarity.Rare ? 2 : 0;
        }

        public ItemInstance Generate(ItemDefinition item, int itemLevel, ulong seed)
        {
            var random = new SplitMix64Source(seed);
            return Build(item, itemLevel, seed, _rarities.Roll(random), random);
        }

        /// <summary>Le probabilità delle rarità di <see cref="Generate(ItemDefinition, int, ulong)"/>.</summary>
        public RarityTable Rarities => _rarities;

        /// <summary>Con un'altra tabella delle rarità, come quella spostata dei campioni (D7 della M12).</summary>
        public ItemInstance Generate(ItemDefinition item, int itemLevel, ulong seed, RarityTable rarities)
        {
            if (rarities == null)
            {
                throw new ArgumentNullException(nameof(rarities));
            }

            var random = new SplitMix64Source(seed);
            return Build(item, itemLevel, seed, rarities.Roll(random), random);
        }

        /// <summary>Con la rarità decisa da fuori: per i test e per gli oggetti messi a mano.</summary>
        public ItemInstance Generate(ItemDefinition item, int itemLevel, ulong seed, Rarity rarity)
        {
            return Build(item, itemLevel, seed, rarity, new SplitMix64Source(seed));
        }

        /// <summary>
        /// Un unico o un pezzo di set (D6 della M16): ogni proprietà al valore del livello, spostato da
        /// un tiro di al più ±<see cref="NamedSpread"/>. Stesso seme e livello, stessi valori.
        /// </summary>
        public static ItemInstance GenerateNamed(UniqueDefinition named, int itemLevel, ulong seed)
        {
            if (named == null)
            {
                throw new ArgumentNullException(nameof(named));
            }

            var random = new SplitMix64Source(seed);
            var values = new int[named.Properties.Count];
            for (int i = 0; i < values.Length; i++)
            {
                double factor = 1.0 - NamedSpread + 2.0 * NamedSpread * random.NextDouble();
                values[i] = named.Properties[i].ValueAt(itemLevel, named.MinLevel, factor);
            }

            return new ItemInstance(named, itemLevel, seed, values);
        }

        private ItemInstance Build(ItemDefinition item, int itemLevel, ulong seed, Rarity rarity, IRandomSource random)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            // unici e set nascono solo da una definizione (GenerateNamed): chiesti qui, sono rari
            if (rarity > Rarity.Rare)
            {
                rarity = Rarity.Rare;
            }

            // un anello normale non avrebbe niente: la rarità tirata sale fino a quella minima della base
            if (rarity < item.MinRarity)
            {
                rarity = item.MinRarity;
            }

            // magico 1–2, raro 3–4: il secondo tiro decide se c'è l'affisso in più
            int count = rarity == Rarity.Magic ? 1 + Coin(random) : rarity == Rarity.Rare ? 3 + Coin(random) : 0;
            int maxPerKind = MaxPerKind(rarity);
            int prefixes = 0;
            int suffixes = 0;
            _rolled.Clear();
            _usedGroups.Clear();

            for (int i = 0; i < count; i++)
            {
                _candidates.Clear();
                for (int g = 0; g < _groups.Count; g++)
                {
                    if (!_usedGroups.Contains(g) && HasAllowedGrade(_groups[g], item, itemLevel, prefixes < maxPerKind, suffixes < maxPerKind))
                    {
                        _candidates.Add(g);
                    }
                }

                // se il pool non ha abbastanza affissi, l'oggetto ne ha quanti ce ne sono
                if (_candidates.Count == 0)
                {
                    break;
                }

                int group = _candidates[Index(random, _candidates.Count)];
                _grades.Clear();
                foreach (var grade in _groups[group])
                {
                    if (grade.AllowedOn(item, itemLevel))
                    {
                        _grades.Add(grade);
                    }
                }

                // i gradi sono in ordine crescente: si tira tra gli ultimi due
                int lowest = Math.Max(0, _grades.Count - OpenGrades);
                var chosen = _grades[lowest + Index(random, _grades.Count - lowest)];
                int value = chosen.Min + Index(random, chosen.Max - chosen.Min + 1);
                _rolled.Add(new ItemAffix(chosen, value));
                _usedGroups.Add(group);

                if (chosen.Kind == AffixKind.Prefix)
                {
                    prefixes++;
                }
                else
                {
                    suffixes++;
                }
            }

            return new ItemInstance(item, rarity, itemLevel, seed, _rolled);
        }

        private static bool HasAllowedGrade(List<AffixDefinition> grades, ItemDefinition item, int itemLevel, bool prefixFree, bool suffixFree)
        {
            foreach (var grade in grades)
            {
                bool kindFree = grade.Kind == AffixKind.Prefix ? prefixFree : suffixFree;
                if (kindFree && grade.AllowedOn(item, itemLevel))
                {
                    return true;
                }
            }

            return false;
        }

        private static int Coin(IRandomSource random)
        {
            return random.NextDouble() < 0.5 ? 1 : 0;
        }

        private static int Index(IRandomSource random, int count)
        {
            // il Min protegge dall'arrotondamento di un NextDouble vicinissimo a 1
            return Math.Min(count - 1, (int)(random.NextDouble() * count));
        }
    }
}
