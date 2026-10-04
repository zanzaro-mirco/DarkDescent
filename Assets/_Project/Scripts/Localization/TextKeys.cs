namespace DarkDescent.Localization
{
    /// <summary>
    /// Le chiavi della tabella usate dal codice, in un posto solo: un test controlla che esistano
    /// tutte. Le chiavi delle etichette fisse stanno sui componenti <c>LocalizedText</c> delle scene,
    /// quelle degli oggetti sulle loro definizioni.
    /// </summary>
    public static class TextKeys
    {
        public const string Miss = "combat.miss";
        public const string Blocked = "combat.blocked";
        public const string ExitDescend = "exit.descend";
        public const string InventoryFull = "ground.inventory_full";
        public const string TooltipDamage = "tooltip.damage";
        public const string TooltipArmor = "tooltip.armor";
        public const string TooltipRequiredStrength = "tooltip.required_strength";
        public const string TooltipBlock = "tooltip.block";
        public const string TooltipEquipped = "tooltip.equipped";

        // le righe degli affissi nel tooltip, una per effetto
        public const string EffectWeaponDamagePercent = "affix.effect.weapon_damage_percent";
        public const string EffectArmorPercent = "affix.effect.armor_percent";
        public const string EffectBlockChance = "affix.effect.block_chance";
        public const string EffectArmor = "affix.effect.armor";
        public const string EffectToHit = "affix.effect.to_hit";
        public const string EffectStrength = "affix.effect.strength";
        public const string EffectDexterity = "affix.effect.dexterity";
        public const string EffectVitality = "affix.effect.vitality";
        public const string EffectLife = "affix.effect.life";
    }
}
