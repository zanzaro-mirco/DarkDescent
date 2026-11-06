using System.Collections.Generic;
using System.Linq;
using DarkDescent.Editor;
using DarkDescent.Progression;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    /// <summary>La misura delle difficoltà (D10 della M14, passo 14.6): le formule e le fasce con gli asset veri.</summary>
    public class DifficultyCensusTests
    {
        private List<DifficultyCensus.Row> _rows;
        private DifficultyTable _table;
        private DifficultyCensus _census;

        [OneTimeSetUp]
        public void Measure()
        {
            _table = AssetDatabase.LoadAssetAtPath<DifficultyTable>(DifficultyCensus.DifficultiesPath);
            _census = new DifficultyCensus(20, 8);
            _rows = _census.Measure(_table);
        }

        private static CensusFighter Fighter(float life, int min, int max, float dexterity = 0f, float armor = 0f, bool canCrit = false, float block = 0f)
        {
            return new CensusFighter(life, min, max, 0f, 0f, dexterity, armor, 0f, 0f, canCrit, block, 1f);
        }

        [Test, Description("Un'arma da 10 fa 10 a colpo; con Destrezza 50 il critico al 10% porta la media a 11; Forza 50 e livello 10 fanno 10 × 1,5 + 5")]
        public void AverageHit_FollowsTheFormulas()
        {
            Assert.AreEqual(10.0, PowerCensus.AverageHit(Fighter(1f, 10, 10)), 1e-9);
            Assert.AreEqual(11.0, PowerCensus.AverageHit(Fighter(1f, 10, 10, dexterity: 50f, canCrit: true)), 1e-9);
            var strong = new CensusFighter(1f, 10, 10, 50f, 10f, 0f, 0f, 0f, 0f, false, 0f, 2f);
            Assert.AreEqual(40.0, PowerCensus.AverageHit(strong), 1e-9, "(15 + 5) per il moltiplicatore 2");
        }

        [Test, Description("Contro Armatura 25 si colpisce al 50%, e uno scudo al 20% lascia passare l'80%: 40 di vita con colpi da 10 vogliono 10 colpi tirati e 4 arrivati")]
        public void Swings_CountMissesAndBlocks()
        {
            var attacker = Fighter(1f, 10, 10);
            var defender = Fighter(40f, 1, 1, armor: 25f, block: 20f);
            Assert.AreEqual(0.4, PowerCensus.LandChance(attacker, defender), 1e-9);
            Assert.AreEqual(10.0, PowerCensus.SwingsToKill(attacker, defender), 1e-9);
            Assert.AreEqual(4.0, PowerCensus.HitsToDie(attacker, defender), 1e-9);
        }

        [Test, Description("La difficoltà moltiplica vita e danno e aggiunge colpire e Armatura al nemico")]
        public void Foe_TakesTheDifficulty()
        {
            var shadow = _table.Tier(1);
            var foe = PowerCensus.Foe(30f, 4, 6, 10f, 10f, shadow);
            Assert.AreEqual(30f * shadow.Life, foe.Life, 1e-4f);
            Assert.AreEqual(shadow.Damage, foe.DamageFactor, 1e-6f);
            Assert.AreEqual((10f + shadow.Armor, (float)shadow.ToHit), (foe.Armor, foe.ToHit));
        }

        [Test, Description("A ogni difficoltà e profondità: scheletro in 1–5 colpi, bruto in 4–14; il cavaliere regge almeno 3 colpi del bruto e 2 del re; colpisce il bruto almeno 2 volte su 3, e il bruto lo colpisce tra il 5% e il 45% delle volte")]
        public void EveryDepth_StaysInTheBands()
        {
            Assert.AreEqual(_table.Count * 8, _rows.Count);
            MapChecks.Pass("misura delle difficoltà", CensusChecks.EveryDepth(_rows));
        }

        [Test, Description("Dall'Ombra ogni discesa comincia dura (bruto in 9–14 colpi alla profondità 1), non più facile della precedente oltre il 5%")]
        public void EachDifficulty_StartsHarder()
        {
            MapChecks.Pass("misura delle difficoltà", CensusChecks.EachDifficultyStartsHarder(_rows, _table.Count));
        }

        [Test, Description("Il re dura un po' di più a ogni gradino: non meno del 95% dei colpi del precedente, e meno di una volta e mezza")]
        public void King_LastsALittleLonger()
        {
            MapChecks.Pass("misura delle difficoltà", CensusChecks.KingLastsALittleLonger(_rows));
        }

        [Test, Description("Con una discesa per difficoltà il cavaliere comincia ognuna al più due livelli sotto la zona, e arriva al 50 prima dell'Abisso IV")]
        public void Experience_KeepsUpWithTheZone()
        {
            MapChecks.Pass("misura delle difficoltà", CensusChecks.ExperienceKeepsUp(_rows, _table, _census.Settings));
        }

        [Test, Description("Con la specializzazione Difesa al posto di Armi le fasce reggono lo stesso (D14 della M20: Armi è il riferimento, Difesa il confronto)")]
        public void ReferenceTree_Defense_StaysInTheBands()
        {
            var rows = new DifficultyCensus(20, 8) { Reference = DifficultyCensus.LoadTree("WarriorDefense") }.Measure(_table);
            MapChecks.Pass("misura con la Difesa", CensusChecks.EveryDepth(rows)
                ?? CensusChecks.EachDifficultyStartsHarder(rows, _table.Count)
                ?? CensusChecks.KingLastsALittleLonger(rows)
                ?? CensusChecks.ExperienceKeepsUp(rows, _table, _census.Settings));
        }

        [Test, Description("L'albero rende il cavaliere più forte ma non rompe le difficoltà: contro il cavaliere senza albero, mai più del 22% in meno di colpi per uccidere e mai più del 30% in più di colpi retti, in entrambe le specializzazioni")]
        public void ReferenceTree_StrengthensTheKnightALittle()
        {
            var bare = new DifficultyCensus(20, 8) { Reference = null }.Measure(_table);
            foreach (string name in new[] { "WarriorArms", "WarriorDefense" })
            {
                var rows = new DifficultyCensus(20, 8) { Reference = DifficultyCensus.LoadTree(name) }.Measure(_table);
                for (int i = 0; i < rows.Count; i++)
                {
                    string where = $"{name}, {rows[i].TierName} profondità {rows[i].Depth}";
                    Assert.GreaterOrEqual(rows[i].BruteSwings, bare[i].BruteSwings * 0.78, $"{where}: colpi al bruto");
                    Assert.GreaterOrEqual(rows[i].KingSwings, bare[i].KingSwings * 0.78, $"{where}: colpi al re");
                    Assert.LessOrEqual(rows[i].BruteHitsTaken, bare[i].BruteHitsTaken * 1.30, $"{where}: colpi del bruto retti");
                    Assert.LessOrEqual(rows[i].KingHitsTaken, bare[i].KingHitsTaken * 1.30, $"{where}: colpi del re retti");
                }

                Assert.IsTrue(rows.Zip(bare, (r, b) => r.BruteSwings <= b.BruteSwings).All(x => x), $"{name}: l'albero non toglie forza");
            }
        }

        [Test, Description("I colpi al bruto del Guerriero di riferimento (Armi) alla profondità 1 di ogni difficoltà e alla 8 del Crepuscolo sono quelli del passo 20.8, a un centesimo: dà un colpo d'allarme se un'altra classe o un rifacimento della misura sposta il Guerriero dentro le fasce senza che nessuno se ne accorga")]
        public void WarriorReference_KeepsItsNumbers()
        {
            double[] depthOne = { 6.95, 8.97, 8.83, 10.33, 9.92, 10.36, 10.90, 11.43, 11.85 };
            for (int t = 0; t < depthOne.Length; t++)
            {
                Assert.AreEqual(depthOne[t], _rows.Single(r => r.Tier == t && r.Depth == 1).BruteSwings, 0.01, $"difficoltà {t}, profondità 1");
            }

            Assert.AreEqual(3.90, _rows.Single(r => r.Tier == 0 && r.Depth == 8).BruteSwings, 0.01, "Crepuscolo, profondità 8");
        }

        [Test, Description("Il Mago di riferimento (D11 della M21: Fuoco, e Gelo per il confronto, con il dardo arcano, bacchetta e tomo, l'Armatura di classe) sta nelle stesse fasce del cavaliere, con la tolleranza del 5% sul lato corto e quella del Mago sul gradino dell'Abisso II")]
        public void Mage_StaysInTheBands()
        {
            foreach (string spec in new[] { "MageFire", "MageFrost" })
            {
                var rows = DifficultyCensus.ForMage(20, 8, spec).Measure(_table);
                MapChecks.Pass("misura del Mago, " + spec, CensusChecks.EveryDepth(rows)
                    ?? CensusChecks.EachDifficultyStartsHarder(rows, _table.Count, CensusChecks.MageStepTolerance)
                    ?? CensusChecks.KingLastsALittleLonger(rows)
                    ?? CensusChecks.ExperienceKeepsUp(rows, _table, _census.Settings));
            }
        }

        [Test, Description("Il Mago porta bacchetta e tomo e non spada e scudo, anche con tanti oggetti tra cui scegliere; il cavaliere continua a prendere quello che i requisiti permettono")]
        public void HeroSetup_EquipsTheMageWithCasterGear()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<Classes.CharacterClassCatalog>(ClassTools.CatalogPath);
            var mage = catalog.Find("mage");
            var sheet = new Stats.StatSheet();
            sheet.SetBase(Stats.StatType.Strength, mage.Strength);
            sheet.SetBase(Stats.StatType.Dexterity, mage.Dexterity);
            sheet.SetBase(Stats.StatType.Magic, mage.Magic + 100f);
            sheet.SetBase(Stats.StatType.Vitality, mage.Vitality);
            var equipment = new Items.Equipment(sheet);
            var stock = AssetDatabase.LoadAssetAtPath<Items.MerchantTable>("Assets/_Project/Data/Loot/MerchantTable.asset").Stock;
            var generator = new Items.ItemGenerator(AssetDatabase.LoadAssetAtPath<Items.AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset").Affixes, Items.RarityTable.Default);
            HeroSetup.Equip(equipment, stock, generator, 20, 7UL, Items.Rarity.Rare, mage);
            Assert.IsTrue(equipment.Get(Items.EquipSlot.Weapon).Definition is Items.WeaponDefinition { IsCaster: true, TwoHanded: false }, "una bacchetta");
            Assert.IsTrue(equipment.Get(Items.EquipSlot.Offhand).Definition is Items.ArmorDefinition { IsTome: true }, "un tomo");
            Assert.IsTrue(mage.IsCaster);
            Assert.IsFalse(catalog.Warrior.IsCaster);
            Assert.IsTrue(catalog.Warrior.Allows(equipment.Get(Items.EquipSlot.Weapon).Definition), "il cavaliere non ha regole di classe");
        }

        [Test, Description("La crescita degli incantesimi vale 1 con la Magia di partenza del Mago, sale con la Magia sempre più in fretta, e sotto la partenza scende")]
        public void SpellGrowth_StartsAtOne()
        {
            Assert.AreEqual(1f, Combat.CombatFormulas.SpellGrowth(Combat.CombatFormulas.SpellStartMagic), 1e-5f);
            float previous = 0f;
            for (float magic = 0f; magic <= 300f; magic += 10f)
            {
                float growth = Combat.CombatFormulas.SpellGrowth(magic);
                Assert.Greater(growth, previous, $"Magia {magic}");
                previous = growth;
            }

            Assert.Less(Combat.CombatFormulas.SpellGrowth(10f), 1f);
            Assert.Greater(Combat.CombatFormulas.SpellGrowth(200f), 5f, "a fine gioco il Mago fa molte volte i numeri di base");
        }

        // le celle della misura con l'interruttore acceso in cui un cavaliere sta davvero: non più di due livelli sotto la zona
        // il Crepuscolo, l'Ombra e la Tenebra: dall'Abisso I gli oggetti del livello efficace tengono il passo con la crescita
        private const int LastBitingTier = 2;

        private static bool Realistic(DifficultyCensus.Row row, DifficultyTable table, ProgressionSettings settings)
        {
            return row.HeroLevel >= settings.ZoneLevel(row.Depth, table.Tier(row.Tier).ZoneLevelBonus) - 2;
        }

        private static DifficultyCensus[] References()
        {
            return new[] { new DifficultyCensus(20, 8), DifficultyCensus.ForMage(20, 8, "MageFire") };
        }

        [Test, Description("Con l'interruttore spento la misura per livelli scelti è quella di prima, cella per cella (D17 della M24): la griglia 4 × 4 non si muove")]
        public void GrowingWorld_Off_IsTheOldMeasure()
        {
            var off = _census.MeasureGrowing(_table, DifficultyCensus.GrowthHeroLevels, DifficultyCensus.GrowthDepths, false);
            foreach (var row in off)
            {
                var old = _census.MeasureDepth(_table.Tier(row.Tier), row.Tier, row.Depth, row.HeroLevel);
                Assert.AreEqual(1f, row.Growth, $"{row.TierName} {row.Depth} L{row.HeroLevel}: nessuna crescita");
                Assert.AreEqual(old.ItemLevel, row.ItemLevel);
                Assert.AreEqual(old.BruteSwings, row.BruteSwings, 1e-9);
                Assert.AreEqual(old.BruteHitsTaken, row.BruteHitsTaken, 1e-9);
                Assert.AreEqual(old.Experience, row.Experience, 1e-9);
            }
        }

        [Test, Description("Le fasce con l'interruttore acceso (D13 della M24), per il Guerriero e il Mago di riferimento, nelle celle in cui un cavaliere sta davvero: il bruto non muore mai in un colpo solo (almeno 1,4), il cavaliere regge almeno 3 colpi del bruto e 2 del re, la crescita non supera il tetto")]
        public void GrowingWorld_On_StaysInTheNewBands()
        {
            foreach (var census in References())
            {
                foreach (var row in census.MeasureGrowing(_table, DifficultyCensus.GrowthHeroLevels, DifficultyCensus.GrowthDepths, true))
                {
                    if (!Realistic(row, _table, _census.Settings))
                    {
                        continue;
                    }

                    string where = $"{census.Hero?.name ?? "Guerriero"}, {row.TierName} profondità {row.Depth}, livello {row.HeroLevel}";
                    Assert.LessOrEqual(row.Growth, _census.Settings.GrowthCap + 1e-6f, where + ": il tetto");
                    Assert.GreaterOrEqual(row.BruteSwings, 1.4, where + ": colpi al bruto");
                    Assert.GreaterOrEqual(row.BruteHitsTaken, 3.0, where + ": colpi del bruto retti");
                    Assert.GreaterOrEqual(row.KingHitsTaken, 2.0, where + ": colpi del re retti");
                }
            }
        }

        [Test, Description("Dove la crescita morde (almeno ×1,5) fino alla Tenebra, l'interruttore acceso rende il mondo più duro di quello spento: il bruto resiste almeno il 10% di colpi in più e il cavaliere ne regge meno; dall'Abisso, dove gli oggetti da 47 raggiungono la crescita, non lo rende più facile di oltre il 15%; l'esperienza non cala mai")]
        public void GrowingWorld_On_IsHarderWhereItBites()
        {
            foreach (var census in References())
            {
                var on = census.MeasureGrowing(_table, DifficultyCensus.GrowthHeroLevels, DifficultyCensus.GrowthDepths, true);
                var off = census.MeasureGrowing(_table, DifficultyCensus.GrowthHeroLevels, DifficultyCensus.GrowthDepths, false);
                int bites = 0;
                for (int i = 0; i < on.Count; i++)
                {
                    string where = $"{on[i].TierName} profondità {on[i].Depth}, livello {on[i].HeroLevel}";
                    Assert.GreaterOrEqual(on[i].Experience, off[i].Experience - 1e-9, where + ": l'esperienza");
                    if (on[i].Tier > LastBitingTier)
                    {
                        Assert.GreaterOrEqual(on[i].BruteSwings, off[i].BruteSwings * 0.85, where + ": non più facile di oltre il 15%");
                        continue;
                    }

                    if (on[i].Growth < 1.5f)
                    {
                        continue;
                    }

                    bites++;
                    Assert.GreaterOrEqual(on[i].BruteSwings, off[i].BruteSwings * 1.1, where + ": colpi al bruto");
                    Assert.Less(on[i].BruteHitsTaken, off[i].BruteHitsTaken, where + ": colpi del bruto retti");
                }

                Assert.GreaterOrEqual(bites, 6, "la griglia ha celle in cui la crescita morde");
            }
        }

        [Test, Description("Anche con l'interruttore acceso salire di livello si sente: alla profondità 1 del Crepuscolo il bruto muore in meno colpi al 10, al 25 e al 50 (D12: sempre acceso, salire non si sentirebbe più)")]
        public void GrowingWorld_On_LevelingUpIsStillFelt()
        {
            foreach (var census in References())
            {
                var rows = census.MeasureGrowing(_table, DifficultyCensus.GrowthHeroLevels, new[] { 1 }, true).Where(r => r.Tier == 0).OrderBy(r => r.HeroLevel).ToList();
                for (int i = 1; i < rows.Count; i++)
                {
                    Assert.Less(rows[i].BruteSwings, rows[i - 1].BruteSwings, $"livello {rows[i].HeroLevel} contro {rows[i - 1].HeroLevel}");
                }
            }
        }

        [Test, Description("La rarità dell'equipaggiamento segue la difficoltà: normale al Crepuscolo, magica all'Ombra e alla Tenebra, rara dall'Abisso")]
        public void Gear_FollowsTheDifficulty()
        {
            Assert.AreEqual(Items.Rarity.Normal, DifficultyCensus.GearFor(0));
            Assert.AreEqual(Items.Rarity.Magic, DifficultyCensus.GearFor(1));
            Assert.AreEqual(Items.Rarity.Magic, DifficultyCensus.GearFor(2));
            Assert.AreEqual(Items.Rarity.Rare, DifficultyCensus.GearFor(3));
            Assert.AreEqual(Items.Rarity.Rare, DifficultyCensus.GearFor(8));
        }
    }
}
