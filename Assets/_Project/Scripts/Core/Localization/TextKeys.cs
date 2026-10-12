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
        public const string InventoryFull = "hud.inventory_full";

        // la crescita del cavaliere (M8): barra dell'esperienza e testata del pannello
        public const string ExperienceLevel = "hud.experience";
        public const string ExperienceMaxLevel = "hud.experience_max";
        public const string CharacterLevel = "hud.character_level";
        public const string CharacterPoints = "hud.character_points";
        public const string TooltipDamage = "tooltip.damage";
        public const string TooltipSpeed = "tooltip.speed";
        public const string SpeedFast = "speed.fast";
        public const string SpeedNormal = "speed.normal";
        public const string SpeedSlow = "speed.slow";
        public const string TooltipArmor = "tooltip.armor";
        public const string TooltipRequiredStrength = "tooltip.required_strength";
        public const string TooltipBlock = "tooltip.block";
        public const string Chest = "chest";
        public const string TooltipEquipped = "tooltip.equipped";
        public const string TooltipEquippedRing1 = "tooltip.equipped_ring1";
        public const string TooltipEquippedRing2 = "tooltip.equipped_ring2";
        public const string TooltipHeal = "tooltip.heal";
        public const string TooltipDrink = "tooltip.drink";
        public const string TooltipRestoreMana = "tooltip.restore_mana";
        public const string TooltipTeaches = "tooltip.teaches";
        public const string TooltipRead = "tooltip.read";
        public const string TooltipReturn = "tooltip.return";
        public const string TooltipEquip = "tooltip.equip";

        // l'oro (D4 della M10): la pila a terra e il totale nell'inventario
        public const string GoldAmount = "gold.amount";
        public const string InventoryGold = "inventory.gold";

        // dove si trova il cavaliere (prova della M7): "{tipo} – Livello {profondità}"
        public const string LevelTitle = "hud.level_title";
        public const string LevelCrypt = "level.crypt";
        public const string LevelCaves = "level.caves";

        // il menu iniziale, i posti di salvataggio, la pausa e le opzioni (D2 e D10 della M10)
        public const string MenuNewTitle = "menu.new_title";
        public const string MenuLoadTitle = "menu.load_title";
        public const string SlotEmpty = "slot.empty";
        public const string SlotSummary = "slot.summary";
        public const string ConfirmOverwrite = "confirm.overwrite";
        public const string ConfirmDelete = "confirm.delete";
        public const string OptionsLanguage = "options.language";
        public const string OptionsRetro = "options.retro";
        public const string RetroOff = "retro.off";
        public const string RetroLight = "retro.light";
        public const string RetroFull = "retro.full";
        public const string LanguageName = "language.name";
        public const string LevelTown = "level.town";

        // il mercante (D5 della M10)
        public const string MerchantName = "merchant.name";
        public const string ShopPrice = "shop.price";
        public const string ShopSellPrice = "shop.sell_price";
        public const string ShopBuyHint = "shop.buy_hint";
        public const string ShopSellHint = "shop.sell_hint";
        public const string ShopNoGold = "shop.no_gold";
        public const string ShopNoRoom = "shop.no_room";
        public const string ShopHelp = "shop.help";

        // i portali della pergamena del ritorno (D6 della M10)
        public const string PortalToTown = "portal.to_town";
        public const string PortalToDepth = "portal.to_depth";
        public const string PortalVictory = "portal.victory";

        // la vittoria e i riconoscimenti (D9 della M10)
        public const string VictoryTime = "victory.time";
        public const string VictoryLevel = "victory.level";
        public const string VictoryKills = "victory.kills";
        public const string VictoryDeaths = "victory.deaths";
        public const string VictoryGold = "victory.gold";
        public const string CreditsTitle = "credits.title";
        public const string CreditsAuthor = "credits.author";
        public const string CreditsAssets = "credits.assets";
        public const string CreditsOwn = "credits.own";
        public const string CreditsThanks = "credits.thanks";

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
        public const string TipMana = "stat.tip.mana";
        public const string TipBonus = "stat.tip.bonus";
        public const string TipCritical = "stat.tip.critical";

        // le righe degli affissi nel tooltip, una per effetto
        public const string EffectWeaponDamagePercent = "affix.effect.weapon_damage_percent";
        public const string EffectArmorPercent = "affix.effect.armor_percent";
        public const string EffectBlockChance = "affix.effect.block_chance";
        public const string EffectArmor = "affix.effect.armor";
        public const string EffectToHit = "affix.effect.to_hit";
        public const string EffectStrength = "affix.effect.strength";
        public const string EffectDexterity = "affix.effect.dexterity";
        public const string EffectCritChance = "affix.effect.crit_chance";
        public const string EffectMagic = "affix.effect.magic";
        public const string EffectMana = "affix.effect.mana";
        public const string EffectVitality = "affix.effect.vitality";
        public const string EffectLife = "affix.effect.life";
    }
}
