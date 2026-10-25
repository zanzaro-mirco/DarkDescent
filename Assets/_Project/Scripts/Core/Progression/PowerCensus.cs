using System;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Stats;

namespace DarkDescent.Progression
{
    /// <summary>
    /// La misura delle difficoltà (D10 della M14): quanti colpi servono al cavaliere per uccidere un
    /// nemico e quanti ne regge prima di morire, con le formule del gioco. Valori attesi, non tiri,
    /// come il <c>LootCensus</c> della M13: due misure uguali danno lo stesso numero. Logica pura;
    /// lo strumento dell'editor ci mette i livelli generati e gli asset veri.
    /// </summary>
    public static class PowerCensus
    {
        /// <summary>
        /// Il cavaliere con la sua scheda e quello che porta: l'arma (o i pugni, 1–2), lo scudo con il
        /// blocco e la Destrezza, la vita di Vitalità, livelli e affissi; dalla M16 il danno in più, la
        /// velocità d'attacco, il danno ridotto e il rubavita.
        /// </summary>
        public static CensusFighter Hero(StatSheet sheet, Equipment equipment)
        {
            if (sheet == null)
            {
                throw new ArgumentNullException(nameof(sheet));
            }

            if (equipment == null)
            {
                throw new ArgumentNullException(nameof(equipment));
            }

            var weapon = equipment.Get(EquipSlot.Weapon);
            var (min, max) = weapon?.Definition is WeaponDefinition ? ItemStats.WeaponDamage(weapon) : (1, 2);
            var shield = equipment.Get(EquipSlot.Offhand);
            float dexterity = sheet.Get(StatType.Dexterity);
            float block = shield?.Definition is ArmorDefinition ? CombatFormulas.BlockChance(ItemStats.ShieldBlock(shield), dexterity) : 0f;
            return new CensusFighter(
                CombatFormulas.MaxLife(sheet.Get(StatType.Vitality)) + sheet.Get(StatType.Life),
                min, max, sheet.Get(StatType.Strength), sheet.Get(StatType.Level), dexterity,
                sheet.Get(StatType.Armor), sheet.Get(StatType.ToHit), sheet.Get(StatType.CritChance),
                true, block, CombatFormulas.WithBonus(1f, sheet.Get(StatType.DamageBonus)),
                sheet.Get(StatType.AttackSpeed), sheet.Get(StatType.DamageReduction), sheet.Get(StatType.LifeSteal));
        }

        /// <summary>
        /// Un nemico alla difficoltà data: la vita e il danno moltiplicati, il colpire e l'Armatura in
        /// più, come <c>CompositionRoot.ApplyDifficulty</c>. Niente Forza né livello, niente scudo.
        /// </summary>
        public static CensusFighter Foe(float life, int minDamage, int maxDamage, float dexterity, float armor, DifficultyTier tier)
        {
            if (tier == null)
            {
                throw new ArgumentNullException(nameof(tier));
            }

            return new CensusFighter(life * tier.Life, minDamage, maxDamage, 0f, 0f, dexterity,
                armor + tier.Armor, tier.ToHit, 0f, false, 0f, tier.Damage);
        }

        /// <summary>
        /// Il danno medio di un colpo arrivato: la media dei tiri interi dell'arma con Forza e livello,
        /// arrotondati come nel gioco, per il moltiplicatore e, per chi può, per il critico atteso.
        /// </summary>
        public static double AverageHit(in CensusFighter attacker)
        {
            int min = Math.Min(attacker.MinDamage, attacker.MaxDamage);
            int max = Math.Max(attacker.MinDamage, attacker.MaxDamage);
            double sum = 0.0;
            for (int roll = min; roll <= max; roll++)
            {
                sum += CombatFormulas.Damage(roll, attacker.Strength, attacker.Level);
            }

            double hit = sum / (max - min + 1) * attacker.DamageFactor;
            if (attacker.CanCrit)
            {
                double crit = CombatFormulas.CritChance(attacker.Dexterity, attacker.CritBonus) / 100.0;
                hit *= 1.0 + crit * (CombatFormulas.CritMultiplier - 1f);
            }

            return hit;
        }

        /// <summary>La probabilità che un colpo faccia danno: colpito e non bloccato, da 0 a 1.</summary>
        public static double LandChance(in CensusFighter attacker, in CensusFighter defender)
        {
            double hit = CombatFormulas.HitChance(attacker.Dexterity, defender.Armor, attacker.ToHit) / 100.0;
            return hit * (1.0 - defender.Block / 100.0);
        }

        /// <summary>Quanti colpi tira, in media, chi attacca per uccidere chi difende: mancati e bloccati compresi.</summary>
        public static double SwingsToKill(in CensusFighter attacker, in CensusFighter defender)
        {
            double perSwing = LandChance(attacker, defender) * AverageHit(attacker);
            return perSwing > 0.0 ? defender.Life / perSwing : double.PositiveInfinity;
        }

        /// <summary>
        /// Quanti colpi arrivati regge chi difende: il "colpi presi prima di morire" della D10. Il danno
        /// ridotto toglie i suoi punti al colpo medio, mai sotto 1.
        /// </summary>
        public static double HitsToDie(in CensusFighter attacker, in CensusFighter defender)
        {
            double hit = AverageHit(attacker);
            hit = defender.DamageReduction > 0f ? CombatFormulas.ReduceDamage((float)hit, defender.DamageReduction) : hit;
            return hit > 0.0 ? defender.Life / hit : double.PositiveInfinity;
        }

        /// <summary>
        /// I colpi per uccidere contati nel tempo di un colpo normale: con la velocità d'attacco in più
        /// ogni colpo dura meno (D5 della M16).
        /// </summary>
        public static double TimeToKill(in CensusFighter attacker, in CensusFighter defender)
        {
            return SwingsToKill(attacker, defender) / CombatFormulas.AttackSpeedMultiplier(attacker.AttackSpeed);
        }

        /// <summary>
        /// La forza di un combattente contro un nemico, in un numero solo (D11 della M16): quanti colpi
        /// regge diviso il tempo per ucciderlo. Il doppio dei colpi retti o la metà del tempo la raddoppiano.
        /// Il rubavita allunga la vita di quello che torna uccidendo il nemico.
        /// </summary>
        public static double Power(in CensusFighter hero, in CensusFighter foe)
        {
            double time = TimeToKill(hero, foe);
            double hits = HitsToDie(foe, hero) * (1.0 + CombatFormulas.LifeStolen(foe.Life, hero.LifeSteal) / Math.Max(1f, hero.Life));
            return time > 0.0 && !double.IsInfinity(time) ? hits / time : 0.0;
        }
    }
}
