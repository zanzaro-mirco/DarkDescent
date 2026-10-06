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
        public const string Chest = "chest";
        public const string TooltipEquipped = "tooltip.equipped";
        public const string TooltipHeal = "tooltip.heal";
        public const string TooltipDrink = "tooltip.drink";

        // le spiegazioni delle righe del pannello del personaggio (D16 della M6), con i titoli dalle sue righe
        public const string StatNames = "hud.stat_names";
        public const string TipStrength = "stat.tip.strength";
        public const string TipDexterity = "stat.tip.dexterity";
        public const string TipMagic = "stat.tip.magic";
        public const string TipVitality = "stat.tip.vitality";
        public const string TipLife = "stat.tip.life";
        public const string TipArmor = "stat.tip.armor";
        public const string TipDamage = "stat.tip.damage";
        public const string TipHitChance = "stat.tip.hit_chance";
        public const string TipBlock = "stat.tip.block";
        public const string TipCritical = "stat.tip.critical";

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
