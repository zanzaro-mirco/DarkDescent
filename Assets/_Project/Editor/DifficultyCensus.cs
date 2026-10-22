using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Progression;
using DarkDescent.Stats;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// La misura delle nove difficoltà (D10 della M14), con gli asset veri. Un cavaliere fa una
    /// discesa per difficoltà, dal Crepuscolo all'ultimo Abisso, e sale di livello con l'esperienza
    /// dei livelli generati come nel gioco. A ogni profondità porta, in ogni slot, la base migliore
    /// del mercante al livello degli oggetti di lì, della rarità che la difficoltà fa trovare
    /// (<see cref="GearFor"/>). Con <see cref="PowerCensus"/> conta i colpi per uccidere scheletro,
    /// bruto e re, quelli che regge e le probabilità di colpire. Campioni, unici e modificatori del re
    /// non entrano nei colpi, solo nell'esperienza.
    /// </summary>
    public sealed class DifficultyCensus
    {
        public const string DifficultiesPath = "Assets/_Project/Data/Progression/Difficulties.asset";
        public const string OutputVariable = "DARKDESCENT_CENSUS";

        /// <summary>Dalla prima difficoltà che fa trovare magici e rari più spesso dei normali, l'equipaggiamento è magico.</summary>
        public const int FirstMagicTier = 1;

        /// <summary>Dall'Abisso I, con quasi un raro ogni cinque oggetti, l'equipaggiamento è raro.</summary>
        public const int FirstRareTier = 3;

        private const string ProgressionPath = "Assets/_Project/Data/Progression/Progression.asset";
        private const string MerchantPath = "Assets/_Project/Data/Loot/MerchantTable.asset";
        private const string AffixesPath = "Assets/_Project/Data/AffixDatabase.asset";
        private const string ChampionsPath = "Assets/_Project/Data/Enemies/ChampionSettings.asset";
        private const string PlayerPath = "Assets/_Project/Prefabs/Player.prefab";
        private const string SkeletonPath = "Assets/_Project/Prefabs/Skeleton.prefab";
        private const string BrutePath = "Assets/_Project/Prefabs/Brute.prefab";
        private const string KingPath = "Assets/_Project/Prefabs/BoneKing.prefab";

        private readonly ProgressionSettings _settings;
        private readonly IReadOnlyList<LootEntry> _stock;
        private readonly ItemGenerator _generator;
        private readonly Dictionary<int, List<int>> _experience;
        private readonly int _mapSeeds;
        private readonly int _gearSeeds;
        private readonly Numbers _player;
        private readonly Numbers _skeleton;
        private readonly Numbers _brute;
        private readonly Numbers _king;

        /// <summary>
        /// Carica gli asset e genera, una volta, <paramref name="mapSeeds"/> livelli per profondità per
        /// l'esperienza; i colpi sono la media di <paramref name="gearSeeds"/> equipaggiamenti.
        /// </summary>
        public DifficultyCensus(int mapSeeds, int gearSeeds)
        {
            _mapSeeds = Math.Max(1, mapSeeds);
            _gearSeeds = Math.Max(1, gearSeeds);
            _settings = Load<ProgressionSettings>(ProgressionPath);
            _stock = Load<MerchantTable>(MerchantPath).Stock;
            _generator = new ItemGenerator(Load<AffixDatabase>(AffixesPath).Affixes, RarityTable.Default);
            _experience = ExperienceByDepth(_mapSeeds);
            _player = Read(PlayerPath);
            _skeleton = Read(SkeletonPath);
            _brute = Read(BrutePath);
            _king = Read(KingPath);
        }

        public ProgressionSettings Settings => _settings;

        /// <summary>Una profondità di una difficoltà, con il cavaliere che ci arriva.</summary>
        public sealed class Row
        {
            public int Tier { get; set; }

            public string TierName { get; set; }

            public int Depth { get; set; }

            public int HeroLevel { get; set; }

            public int ItemLevel { get; set; }

            public Rarity Gear { get; set; }

            public double HeroLife { get; set; }

            public double HeroArmor { get; set; }

            public double HeroDexterity { get; set; }

            public double HeroToHit { get; set; }

            /// <summary>La probabilità di bloccare del cavaliere, in punti percentuali.</summary>
            public double HeroBlock { get; set; }

            /// <summary>La probabilità che un colpo del cavaliere arrivi al bruto, da 0 a 1.</summary>
            public double HeroLandsOnBrute { get; set; }

            /// <summary>La probabilità che un colpo forte del bruto arrivi al cavaliere, scudo compreso.</summary>
            public double BruteLandsOnHero { get; set; }

            public double SkeletonSwings { get; set; }

            public double BruteSwings { get; set; }

            public double KingSwings { get; set; }

            /// <summary>Quanti colpi dello scheletro arrivati regge il cavaliere.</summary>
            public double SkeletonHitsTaken { get; set; }

            public double BruteHitsTaken { get; set; }

            public double KingHitsTaken { get; set; }

            /// <summary>L'esperienza attesa della profondità, al livello con cui ci si arriva.</summary>
            public double Experience { get; set; }
        }

        /// <summary>La rarità dell'equipaggiamento che si porta alla difficoltà numero <paramref name="tier"/>.</summary>
        public static Rarity GearFor(int tier)
        {
            return tier >= FirstRareTier ? Rarity.Rare : tier >= FirstMagicTier ? Rarity.Magic : Rarity.Normal;
        }

        /// <summary>Tutte le difficoltà, una discesa ciascuna: ognuna parte dal livello della fine della precedente.</summary>
        public List<Row> Measure(DifficultyTable table)
        {
            var progress = new CharacterProgress(_settings, new StatSheet());
            var rows = new List<Row>();
            for (int t = 0; t < table.Count; t++)
            {
                var tier = table.Tier(t);
                for (int depth = 1; depth <= 8; depth++)
                {
                    var row = MeasureDepth(tier, t, depth, progress.Level);
                    progress.Add((int)Math.Round(row.Experience));
                    rows.Add(row);
                }
            }

            return rows;
        }

        /// <summary>Una profondità di una difficoltà, per un cavaliere arrivato lì al livello dato.</summary>
        public Row MeasureDepth(DifficultyTier tier, int tierIndex, int depth, int heroLevel)
        {
            var row = new Row
            {
                Tier = tierIndex,
                TierName = tier.NameKey,
                Depth = depth,
                HeroLevel = heroLevel,
                ItemLevel = _settings.ZoneLevel(depth, tier.ZoneLevelBonus),
                Gear = GearFor(tierIndex),
                Experience = ExperienceAt(tier, depth, heroLevel),
            };
            var skeleton = _skeleton.Foe(tier);
            var brute = _brute.Foe(tier);
            var king = _king.Foe(tier);

            // la media su più equipaggiamenti dello stesso livello: un solo seme dice poco
            double n = _gearSeeds;
            for (int g = 1; g <= _gearSeeds; g++)
            {
                var sheet = new StatSheet();
                sheet.SetBase(StatType.Strength, _player.Strength);
                sheet.SetBase(StatType.Dexterity, _player.Dexterity);
                sheet.SetBase(StatType.Magic, _player.Magic);
                sheet.SetBase(StatType.Vitality, _player.Vitality);
                HeroSetup.RaiseTo(new CharacterProgress(_settings, sheet), heroLevel);
                var equipment = new Equipment(sheet);
                HeroSetup.Equip(equipment, _stock, _generator, row.ItemLevel, (ulong)g * 104729UL, row.Gear);
                var hero = PowerCensus.Hero(sheet, equipment);

                row.HeroLife += hero.Life / n;
                row.HeroArmor += hero.Armor / n;
                row.HeroDexterity += hero.Dexterity / n;
                row.HeroToHit += hero.ToHit / n;
                row.HeroBlock += hero.Block / n;
                row.HeroLandsOnBrute += PowerCensus.LandChance(hero, brute) / n;
                row.BruteLandsOnHero += PowerCensus.LandChance(brute, hero) / n;
                row.SkeletonSwings += PowerCensus.SwingsToKill(hero, skeleton) / n;
                row.BruteSwings += PowerCensus.SwingsToKill(hero, brute) / n;
                row.KingSwings += PowerCensus.SwingsToKill(hero, king) / n;
                row.SkeletonHitsTaken += PowerCensus.HitsToDie(skeleton, hero) / n;
                row.BruteHitsTaken += PowerCensus.HitsToDie(brute, hero) / n;
                row.KingHitsTaken += PowerCensus.HitsToDie(king, hero) / n;
            }

            return row;
        }

        /// <summary>L'esperienza attesa di un livello a quella profondità, per un cavaliere di quel livello.</summary>
        public double ExperienceAt(DifficultyTier tier, int depth, int heroLevel)
        {
            return _experience[depth].Sum(b => (double)_settings.ExperienceForKill(b, depth, heroLevel, tier.ZoneLevelBonus, tier.Experience)) / _mapSeeds;
        }

        [MenuItem("DarkDescent/Misura le difficoltà")]
        public static void Report()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Logs", "difficulty_census.csv");
            Write(path);
            Debug.Log($"[DifficultyCensus] tabella scritta in {path}");
        }

        /// <summary>Da riga di comando (<c>-executeMethod</c>): scrive nel file della variabile <see cref="OutputVariable"/>.</summary>
        public static void WriteFromCommandLine()
        {
            string path = Environment.GetEnvironmentVariable(OutputVariable);
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException($"{OutputVariable} non dice dove scrivere la tabella");
            }

            Write(path);
        }

        /// <summary>La tabella in CSV, nel formato del foglio di calcolo italiano: punto e virgola, virgola decimale.</summary>
        public static void Write(string path)
        {
            var it = CultureInfo.GetCultureInfo("it-IT");
            var text = new StringBuilder("difficolta;profondita;livello;livello_oggetti;rarita;vita;armatura;blocco;colpisce_bruto;bruto_colpisce;"
                + "colpi_scheletro;colpi_bruto;colpi_re;regge_scheletro;regge_bruto;regge_re;esperienza\n");
            foreach (var row in new DifficultyCensus(20, 8).Measure(Load<DifficultyTable>(DifficultiesPath)))
            {
                text.Append(string.Join(";", row.TierName, row.Depth, row.HeroLevel, row.ItemLevel, row.Gear,
                    row.HeroLife.ToString("0", it), row.HeroArmor.ToString("0", it), (row.HeroBlock / 100.0).ToString("0%", it),
                    row.HeroLandsOnBrute.ToString("0%", it), row.BruteLandsOnHero.ToString("0%", it),
                    row.SkeletonSwings.ToString("0.0", it), row.BruteSwings.ToString("0.0", it), row.KingSwings.ToString("0", it),
                    row.SkeletonHitsTaken.ToString("0.0", it), row.BruteHitsTaken.ToString("0.0", it), row.KingHitsTaken.ToString("0.0", it),
                    row.Experience.ToString("0", it)));
                text.Append('\n');
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(true));
        }

        // l'esperienza di base di ogni nemico dei livelli generati, per profondità, già moltiplicata
        // per il grado di campioni e unici come in CompositionRoot.ExperienceOf
        private static Dictionary<int, List<int>> ExperienceByDepth(int seeds)
        {
            var champions = Load<ChampionSettings>(ChampionsPath);
            var byDepth = new Dictionary<int, List<int>>();
            foreach (var (settingsPath, tilesetPath) in new[] { (LootCensus.CryptSettings, LootCensus.CryptTileset), (LootCensus.CaveSettings, LootCensus.CaveTileset) })
            {
                var dungeon = Load<DungeonSettings>(settingsPath);
                var tileset = Load<LevelTileset>(tilesetPath);
                for (int depth = dungeon.FirstDepth; depth <= dungeon.LastDepth; depth++)
                {
                    var list = new List<int>();
                    for (ulong seed = 1; seed <= (ulong)seeds; seed++)
                    {
                        var map = DungeonLevel.CreateMap(dungeon, seed * 7919UL, depth, out _);
                        var leaders = new HashSet<Vector2Int>(ChampionSpawn.FromMap(map).Select(c => new Vector2Int(c.X, c.Y)));
                        foreach (var marker in map.Markers)
                        {
                            var prefab = tileset.GetMarkerPrefab(marker.Symbol);
                            if (prefab == null || !prefab.TryGetComponent(out EnemyAI enemy) || enemy.Archetype == null)
                            {
                                continue;
                            }

                            EnemyRank? rank = prefab.TryGetComponent(out SpecialEnemy special) ? special.Rank : (EnemyRank?)null;
                            if (leaders.Contains(new Vector2Int(marker.X, marker.Y)))
                            {
                                rank = EnemyRank.Champion;
                            }

                            var numbers = rank.HasValue ? champions.For(rank.Value) : null;
                            list.Add(Mathf.RoundToInt(enemy.Archetype.Experience * (numbers != null ? numbers.Experience : 1f)));
                        }
                    }

                    byDepth[depth] = list;
                }
            }

            return byDepth;
        }

        private static Numbers Read(string prefabPath)
        {
            var prefab = Load<GameObject>(prefabPath);
            var stats = new SerializedObject(prefab.GetComponent<CharacterStats>());
            var attack = prefab.GetComponent<MeleeAttack>();
            float life = prefab.TryGetComponent(out Health health) ? new SerializedObject(health).FindProperty("_maxHealth").floatValue : 0f;
            return new Numbers(life, attack != null ? attack.Weapon : null,
                stats.FindProperty("_strength").floatValue, stats.FindProperty("_dexterity").floatValue,
                stats.FindProperty("_magic").floatValue, stats.FindProperty("_vitality").floatValue,
                stats.FindProperty("_armor").floatValue);
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new InvalidOperationException($"manca {path}");
        }

        // i numeri del prefab di un personaggio, come stanno nell'Inspector
        private readonly struct Numbers
        {
            private readonly float _life;
            private readonly WeaponDefinition _weapon;

            public Numbers(float life, WeaponDefinition weapon, float strength, float dexterity, float magic, float vitality, float armor)
            {
                _life = life;
                _weapon = weapon;
                Strength = strength;
                Dexterity = dexterity;
                Magic = magic;
                Vitality = vitality;
                Armor = armor;
            }

            public float Strength { get; }

            public float Dexterity { get; }

            public float Magic { get; }

            public float Vitality { get; }

            public float Armor { get; }

            public CensusFighter Foe(DifficultyTier tier)
            {
                return PowerCensus.Foe(_life, _weapon.MinDamage, _weapon.MaxDamage, Dexterity, Armor, tier);
            }
        }
    }
}
