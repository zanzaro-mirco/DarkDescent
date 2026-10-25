using DarkDescent.Combat;
using DarkDescent.Stats;

namespace DarkDescent.Items
{
    /// <summary>
    /// I numeri di un oggetto con i suoi affissi (D3 della M5): danno dell'arma, Armatura e blocco
    /// dello scudo, e quali affissi vanno invece sul personaggio. Logica pura, in un posto solo:
    /// la usano equipaggiamento, attacco, pannello e tooltip.
    /// </summary>
    public static class ItemStats
    {
        // i quattro di "tutti gli attributi" (D5 della M16)
        private static readonly StatType[] Attributes = { StatType.Strength, StatType.Dexterity, StatType.Magic, StatType.Vitality };

        /// <summary>
        /// La somma dei valori degli affissi con questo effetto, e delle proprietà di un unico o di un
        /// pezzo di set (M16); quelli non ancora risolti non contano.
        /// </summary>
        public static int Sum(ItemInstance item, AffixEffect effect)
        {
            int total = 0;
            if (item == null)
            {
                return total;
            }

            foreach (var affix in item.Affixes)
            {
                if (affix.Definition != null && affix.Definition.Effect == effect)
                {
                    total += affix.Value;
                }
            }

            if (item.Named != null)
            {
                var properties = item.Named.Properties;
                for (int i = 0; i < properties.Count; i++)
                {
                    if (properties[i].Effect == effect)
                    {
                        total += item.NamedValues[i];
                    }
                }
            }

            return total;
        }

        /// <summary>
        /// Mette sulla scheda i modificatori di un effetto del personaggio, con questa sorgente: uno, o
        /// quattro per tutti gli attributi (D5 della M16). False, senza toccare niente, per gli effetti
        /// dell'oggetto.
        /// </summary>
        public static bool AddCharacterModifiers(StatSheet stats, AffixEffect effect, int value, object source)
        {
            if (effect == AffixEffect.AllAttributes)
            {
                foreach (var attribute in Attributes)
                {
                    stats.AddModifier(new StatModifier(attribute, ModifierKind.Flat, value, source));
                }

                return true;
            }

            if (!TryGetCharacterStat(effect, out StatType stat))
            {
                return false;
            }

            stats.AddModifier(new StatModifier(stat, ModifierKind.Flat, value, source));
            return true;
        }

        /// <summary>Danno minimo e massimo dell'arma con il suo "+% danno", prima della Forza.</summary>
        public static (int min, int max) WeaponDamage(ItemInstance item)
        {
            var weapon = (WeaponDefinition)item.Definition;
            int percent = Sum(item, AffixEffect.WeaponDamagePercent);
            return (CombatFormulas.ApplyPercent(weapon.MinDamage, percent), CombatFormulas.ApplyPercent(weapon.MaxDamage, percent));
        }

        /// <summary>L'Armatura di uno scudo o di un pezzo d'armatura, con il suo "+% Armatura".</summary>
        public static int ItemArmor(ItemInstance item)
        {
            var armor = (ArmorDefinition)item.Definition;
            return CombatFormulas.ApplyPercent(armor.Armor, Sum(item, AffixEffect.ArmorPercent));
        }

        /// <summary>Il blocco dello scudo più quello dei suoi affissi, prima della Destrezza.</summary>
        public static int ShieldBlock(ItemInstance item)
        {
            var armor = (ArmorDefinition)item.Definition;
            return armor.BlockChance + Sum(item, AffixEffect.BlockChance);
        }

        /// <summary>
        /// La statistica del personaggio toccata da un effetto; false per gli effetti dell'oggetto e per
        /// tutti gli attributi, che ne toccano quattro (<see cref="AddCharacterModifiers"/>).
        /// </summary>
        public static bool TryGetCharacterStat(AffixEffect effect, out StatType stat)
        {
            switch (effect)
            {
                case AffixEffect.Armor: stat = StatType.Armor; return true;
                case AffixEffect.ToHit: stat = StatType.ToHit; return true;
                case AffixEffect.Strength: stat = StatType.Strength; return true;
                case AffixEffect.Dexterity: stat = StatType.Dexterity; return true;
                case AffixEffect.Vitality: stat = StatType.Vitality; return true;
                case AffixEffect.Life: stat = StatType.Life; return true;
                case AffixEffect.CritChance: stat = StatType.CritChance; return true;
                case AffixEffect.Magic: stat = StatType.Magic; return true;
                case AffixEffect.Mana: stat = StatType.Mana; return true;
                case AffixEffect.AttackSpeed: stat = StatType.AttackSpeed; return true;
                case AffixEffect.LifeSteal: stat = StatType.LifeSteal; return true;
                case AffixEffect.DamageReduction: stat = StatType.DamageReduction; return true;
                case AffixEffect.DamageBonus: stat = StatType.DamageBonus; return true;
                case AffixEffect.ExperienceBonus: stat = StatType.ExperienceBonus; return true;
                default: stat = default; return false;
            }
        }
    }
}
