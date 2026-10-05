using System;
using System.Collections.Generic;
using DarkDescent.Core;

namespace DarkDescent.Items
{
    /// <summary>
    /// Fa un oggetto a partire da base, livello e seme (D2, D3, D5 della M5): stesso ingresso, stesso
    /// oggetto, sempre. Prima la rarità, poi gli affissi ammessi (tipo di oggetto, livello, gruppo
    /// libero, limiti di prefissi e suffissi), poi i valori. Logica pura: si prova senza scena.
    /// </summary>
    public sealed class ItemGenerator
    {
        private readonly IReadOnlyList<AffixDefinition> _pool;
        private readonly RarityTable _rarities;

        // riusate a ogni generazione: un drop non deve creare liste nuove
        private readonly List<AffixDefinition> _candidates = new List<AffixDefinition>();
        private readonly List<ItemAffix> _rolled = new List<ItemAffix>();
        private readonly HashSet<string> _usedGroups = new HashSet<string>(StringComparer.Ordinal);

        public ItemGenerator(IReadOnlyList<AffixDefinition> pool, RarityTable rarities)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _rarities = rarities ?? throw new ArgumentNullException(nameof(rarities));
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

        /// <summary>Con la rarità decisa da fuori: per i test e per gli oggetti messi a mano.</summary>
        public ItemInstance Generate(ItemDefinition item, int itemLevel, ulong seed, Rarity rarity)
        {
            return Build(item, itemLevel, seed, rarity, new SplitMix64Source(seed));
        }

        private ItemInstance Build(ItemDefinition item, int itemLevel, ulong seed, Rarity rarity, IRandomSource random)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
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
                foreach (var affix in _pool)
                {
                    bool kindFree = affix.Kind == AffixKind.Prefix ? prefixes < maxPerKind : suffixes < maxPerKind;
                    if (kindFree && affix.AllowedOn(item, itemLevel) && (string.IsNullOrEmpty(affix.Group) || !_usedGroups.Contains(affix.Group)))
                    {
                        _candidates.Add(affix);
                    }
                }

                // se il pool non ha abbastanza affissi, l'oggetto ne ha quanti ce ne sono
                if (_candidates.Count == 0)
                {
                    break;
                }

                var chosen = _candidates[Index(random, _candidates.Count)];
                int value = chosen.Min + Index(random, chosen.Max - chosen.Min + 1);
                _rolled.Add(new ItemAffix(chosen, value));
                if (!string.IsNullOrEmpty(chosen.Group))
                {
                    _usedGroups.Add(chosen.Group);
                }

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
