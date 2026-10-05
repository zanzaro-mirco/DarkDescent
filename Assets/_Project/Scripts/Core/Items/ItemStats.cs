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
        /// <summary>La somma dei valori degli affissi con questo effetto; quelli non ancora risolti non contano.</summary>
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

            return total;
        }

        /// <summary>Danno minimo e massimo dell'arma con il suo "+% danno", prima della Forza.</summary>
        public static (int min, int max) WeaponDamage(ItemInstance item)
        {
            var weapon = (WeaponDefinition)item.Definition;
            int percent = Sum(item, AffixEffect.WeaponDamagePercent);
            return (CombatFormulas.ApplyPercent(weapon.MinDamage, percent), CombatFormulas.ApplyPercent(weapon.MaxDamage, percent));
        }

        /// <summary>L'Armatura dello scudo con il suo "+% Armatura".</summary>
        public static int ShieldArmor(ItemInstance item)
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

        /// <summary>La statistica del personaggio toccata da un effetto; false per gli effetti dell'oggetto.</summary>
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
                default: stat = default; return false;
            }
        }
    }
}
