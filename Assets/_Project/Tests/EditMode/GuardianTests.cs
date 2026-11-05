using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Vorrak il Custode (24.4 della M24, D8–D11): le fasi, il posto nella sala della scala, i dati, il prefab e il tesoro.</summary>
    public class GuardianTests
    {
        private const int Seeds = 400;

        private DungeonSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<DungeonSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, path);
            return asset;
        }

        private static string RoomText(DungeonLayout layout, RectInt room)
        {
            var text = new System.Text.StringBuilder();
            for (int y = room.yMin; y < room.yMax; y++)
            {
                for (int x = room.xMin; x < room.xMax; x++)
                {
                    text.Append(layout[x, y]);
                }

                text.AppendLine();
            }

            return text.ToString();
        }

        // --- le fasi ---

        [Test, Description("Sopra la metà della vita il colpo caricato, sotto la metà la seconda fase; esattamente a metà è ancora la prima")]
        public void Phases_SwitchBelowHalf()
        {
            Assert.AreEqual(GuardianPhase.Strike, GuardianPhases.For(240f, 240f));
            Assert.AreEqual(GuardianPhase.Strike, GuardianPhases.For(121f, 240f));
            Assert.AreEqual(GuardianPhase.Strike, GuardianPhases.For(120f, 240f), "a metà esatta");
            Assert.AreEqual(GuardianPhase.Wrath, GuardianPhases.For(119.9f, 240f));
            Assert.AreEqual(GuardianPhase.Wrath, GuardianPhases.For(1f, 240f));
            Assert.AreEqual(GuardianPhase.Wrath, GuardianPhases.For(0f, 0f), "senza vita massima non c'è una prima fase");
        }

        // --- il posto ---

        // la stanza che contiene un punto (null se nessuna)
        private static int RoomOf(DungeonLayout layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                if (layout.Rooms[i].Contains(cell))
                {
                    return i;
                }
            }

            return -1;
        }

        [Test, Description("Alla profondità 4 il Custode e il suo tesoro stanno nella stessa stanza, quasi sempre quella della scala giù (le altre volte la sala era una di tre celle per tre, troppo piena); mai alle altre profondità")]
        public void Guardian_StandsInTheStairsRoom_OnlyAtDepth4()
        {
            var generator = new DungeonGenerator(_settings);
            int inStairsRoom = 0;
            for (int depth = 1; depth <= 8; depth++)
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var layout = generator.Generate(seed, depth);
                    var guardians = MapChecks.Find(layout, c => c == DungeonPopulator.GuardianSymbol);
                    var treasures = MapChecks.Find(layout, c => c == DungeonPopulator.GuardianTreasureSymbol);
                    if (depth != DungeonPopulator.GuardianDepth)
                    {
                        Assert.AreEqual(0, guardians.Count + treasures.Count, $"seme {seed}, profondità {depth}: niente Custode");
                        continue;
                    }

                    Assert.AreEqual(1, guardians.Count, $"seme {seed}: un Custode");
                    Assert.AreEqual(1, treasures.Count, $"seme {seed}: un tesoro");
                    int room = RoomOf(layout, guardians[0]);
                    Assert.GreaterOrEqual(room, 0, $"seme {seed}: il Custode in una stanza");
                    Assert.AreEqual(room, RoomOf(layout, treasures[0]), $"seme {seed}: il tesoro nella stessa stanza");
                    Assert.AreNotEqual(layout.StartRoom, room, $"seme {seed}: non nella stanza di partenza");
                    inStairsRoom += room == layout.ExitRoom ? 1 : 0;
                }
            }

            Assert.GreaterOrEqual(inStairsRoom, Seeds * 0.97, "nella stanza della scala (D9) nel 97% dei semi almeno");
        }

        [Test, Description("Il Custode è sulla cella libera più vicina al centro della sua stanza, e non sulle celle davanti alla scala")]
        public void Guardian_IsNearTheCentre_AndKeepsTheStairsFree()
        {
            var generator = new DungeonGenerator(_settings);
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = generator.Generate(seed, DungeonPopulator.GuardianDepth);
                var guardian = MapChecks.Find(layout, c => c == DungeonPopulator.GuardianSymbol).Single();
                RectInt room = layout.Rooms[RoomOf(layout, guardian)];
                var stairsFront = new[] { layout.Stairs + Vector2Int.up, layout.Stairs + Vector2Int.left, layout.Stairs + Vector2Int.right };
                Vector2 center = new Vector2(room.xMin + (room.width - 1) * 0.5f, room.yMin + (room.height - 1) * 0.5f);
                float distance = (new Vector2(guardian.x, guardian.y) - center).sqrMagnitude;
                foreach (var cell in stairsFront)
                {
                    Assert.AreNotEqual(cell, guardian, $"seme {seed}: la scala resta libera");
                }

                // nessuna cella libera della stanza è più vicina al centro (le libere sono Floor; l'occupata è lui)
                for (int y = room.yMin; y < room.yMax; y++)
                {
                    for (int x = room.xMin; x < room.xMax; x++)
                    {
                        if (layout[x, y] == LevelMap.Floor && !stairsFront.Contains(new Vector2Int(x, y)))
                        {
                            float other = (new Vector2(x, y) - center).sqrMagnitude;
                            Assert.GreaterOrEqual(other, distance - 1e-4f, $"seme {seed}: ({x}, {y}) è più vicina al centro");
                        }
                    }
                }
            }
        }

        [Test, Description("La scala giù si raggiunge a piedi dall'ingresso con il Custode e il suo tesoro in sala: niente porta, e il tesoro non chiude il passaggio")]
        public void TheStairs_StayReachable()
        {
            var generator = new DungeonGenerator(_settings);
            for (ulong seed = 1; seed <= Seeds; seed++)
            {
                var layout = generator.Generate(seed, DungeonPopulator.GuardianDepth);
                Assert.IsNull(MapChecks.Reachable(layout, stairs: true), $"seme {seed}");
                Assert.IsNull(MapChecks.ObstaclesLeavePath(layout, stairs: true), $"seme {seed}");
            }
        }

        [Test, Description("Alla profondità 4 ci sono esattamente due segni nuovi (il Custode e il suo tesoro), e alla 3 e alla 5 nessuno: il passo ha il suo flusso del seme e riguarda solo la 4")]
        public void OnlyDepth4_HasTheTwoSigns()
        {
            var generator = new DungeonGenerator(_settings);
            for (ulong seed = 1; seed <= 50; seed++)
            {
                var with = generator.Generate(seed, DungeonPopulator.GuardianDepth);
                int guardianCells = 0;
                for (int y = 0; y < with.Height; y++)
                {
                    for (int x = 0; x < with.Width; x++)
                    {
                        guardianCells += with[x, y] == DungeonPopulator.GuardianSymbol || with[x, y] == DungeonPopulator.GuardianTreasureSymbol ? 1 : 0;
                    }
                }

                Assert.AreEqual(2, guardianCells, $"seme {seed}");

                // le stesse celle, senza i due segni, alla 3 e alla 5 non hanno niente da togliere: il passo è solo della 4
                Assert.AreEqual(0, MapChecks.Count(generator.Generate(seed, 3), DungeonPopulator.GuardianSymbol));
                Assert.AreEqual(0, MapChecks.Count(generator.Generate(seed, 5), DungeonPopulator.GuardianTreasureSymbol));
            }
        }

        [Test, Description("Lo stesso seme, lo stesso posto: il Custode non si sposta tra due generazioni")]
        public void SameSeed_SamePlace()
        {
            var generator = new DungeonGenerator(_settings);
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var a = generator.Generate(seed, DungeonPopulator.GuardianDepth);
                var b = generator.Generate(seed, DungeonPopulator.GuardianDepth);
                Assert.AreEqual(MapChecks.Find(a, c => c == DungeonPopulator.GuardianSymbol), MapChecks.Find(b, c => c == DungeonPopulator.GuardianSymbol));
                Assert.AreEqual(MapChecks.Find(a, c => c == DungeonPopulator.GuardianTreasureSymbol), MapChecks.Find(b, c => c == DungeonPopulator.GuardianTreasureSymbol));
            }
        }

        // --- i dati ---

        [Test, Description("L'archetipo del Custode: colpo caricato come il bruto, nessuna fuga né ronda, il suo nome")]
        public void Archetype_IsATelegraphedBigOne()
        {
            var archetype = Load<EnemyArchetype>("Assets/_Project/Data/Enemies/Guardian.asset");
            Assert.AreEqual("enemy.guardian", archetype.NameKey);
            Assert.IsTrue(archetype.IsTelegraphed, "il colpo caricato");
            Assert.IsFalse(archetype.CanFlee, "i grandi non scappano (D2)");
            Assert.IsFalse(archetype.CanPatrol, "né girano (D7)");
            Assert.IsFalse(archetype.IsRanged);
            Assert.Less(archetype.LeashRange, 25f);
        }

        [Test, Description("I numeri di D10: otto scheletri di vita, i colpi del bruto per 1,3")]
        public void Numbers_AreEightSkeletonsAndTheBruteTimesOnePointThree()
        {
            var skeleton = Load<GameObject>("Assets/_Project/Prefabs/Skeleton.prefab").GetComponent<Health>();
            var guardian = Load<GameObject>("Assets/_Project/Prefabs/Guardian.prefab").GetComponent<Health>();
            Assert.AreEqual(skeleton.Max * 8f, guardian.Max, 0.01f, "otto scheletri");

            var strike = Load<WeaponDefinition>("Assets/_Project/Data/Attacks/GuardianStrike.asset");
            var bruteStrike = Load<WeaponDefinition>("Assets/_Project/Data/Attacks/BruteStrike.asset");
            Assert.AreEqual(bruteStrike.MinDamage * 1.3f, strike.MinDamage, 1f, "il colpo caricato è del bruto per 1,3");
            Assert.AreEqual(bruteStrike.MaxDamage * 1.3f, strike.MaxDamage, 1f);
            var quick = Load<WeaponDefinition>("Assets/_Project/Data/Attacks/GuardianQuick.asset");
            var bruteQuick = Load<WeaponDefinition>("Assets/_Project/Data/Attacks/BruteQuick.asset");
            Assert.AreEqual(bruteQuick.MinDamage * 1.3f, quick.MinDamage, 1f);
            Assert.AreEqual(bruteQuick.MaxDamage * 1.3f, quick.MaxDamage, 1f);
            Assert.AreNotEqual(Load<WeaponDefinition>("Assets/_Project/Data/Attacks/BoneKingStrike.asset").Id, strike.Id, "un id suo, non quello del re");
        }

        [Test, Description("Il prefab è del Custode e non del re: ha il suo controllo, non quello del re, ed è più grande di un nemico normale")]
        public void Prefab_IsTheGuardianNotTheKing()
        {
            var prefab = Load<GameObject>("Assets/_Project/Prefabs/Guardian.prefab");
            Assert.IsNotNull(prefab.GetComponent<GuardianController>());
            Assert.IsNull(prefab.GetComponent<BossController>(), "niente porta, anello né portale della vittoria");
            Assert.IsNotNull(prefab.GetComponent<EnemyAI>());
            Assert.IsNotNull(prefab.GetComponent<TelegraphLine>(), "la striscia della carica");
            Assert.IsNotNull(prefab.GetComponent<LootDrop>());
            Assert.AreEqual("enemy.guardian", prefab.GetComponent<EnemyAI>().Archetype.NameKey);
            Assert.AreEqual(1.25f, prefab.transform.localScale.x, 1e-4f);
            Assert.IsNull(prefab.GetComponentInChildren<TelegraphCircle>(true), "l'anello è del re");
        }

        [Test, Description("Gli evocati sono scheletri normali del livello")]
        public void Summons_AreSkeletons()
        {
            var prefab = Load<GameObject>("Assets/_Project/Prefabs/Guardian.prefab").GetComponent<GuardianController>();
            var so = new SerializedObject(prefab);
            var summon = (EnemyAI)so.FindProperty("_summonPrefab").objectReferenceValue;
            Assert.IsNotNull(summon);
            Assert.AreEqual("enemy.skeleton", summon.Archetype.NameKey);
            Assert.AreEqual(3, so.FindProperty("_summonCount").intValue, "tre scheletri");
        }

        [Test, Description("Il tesoro del Custode: una cassa chiusa finché lui è vivo, non quella del re, con tre oggetti e un unico possibile")]
        public void Treasure_OpensOnlyWithTheGuardian()
        {
            var chest = Load<GameObject>("Assets/_Project/Prefabs/Dungeon/GuardianTreasure.prefab").GetComponentInChildren<Chest>(true);
            Assert.IsTrue(chest.OpensWithGuardian);
            Assert.IsFalse(chest.OpensWithBoss, "non si apre con il re");
            Assert.AreEqual(2, chest.Bonus.ExtraRolls, "tre oggetti in tutto");
            Assert.Greater(chest.Bonus.MagicFactor, 1f, "i magici spostati");
            Assert.Greater(chest.Bonus.RareFactor, 1f, "e i rari");
            Assert.Greater(chest.Bonus.NamedFactor, 1f, "un unico possibile");
            Assert.Greater(chest.Bonus.NamedGuarantee, 0f);

            var king = Load<GameObject>("Assets/_Project/Prefabs/Dungeon/KingsTreasure.prefab").GetComponentInChildren<Chest>(true);
            Assert.IsFalse(king.OpensWithGuardian, "il tesoro del re resta del re");
            Assert.IsTrue(king.OpensWithBoss);
            Assert.Less(chest.Bonus.RareFactor, king.Bonus.RareFactor, "e meno ricco di quello del re");
        }

        [Test, Description("Il set della cripta ha i due segni, e quello delle caverne e il tesoro del re non sono cambiati")]
        public void Tileset_MapsTheTwoSymbols()
        {
            var crypt = Load<LevelTileset>("Assets/_Project/Data/Levels/DungeonTileset.asset");
            Assert.AreEqual("Guardian", crypt.GetMarkerPrefab(DungeonPopulator.GuardianSymbol).name);
            Assert.AreEqual("GuardianTreasure", crypt.GetMarkerPrefab(DungeonPopulator.GuardianTreasureSymbol).name);
            Assert.AreEqual("KingsTreasure", crypt.GetMarkerPrefab(SpecialEnemyPlanner.TreasureSymbol).name, "la t del re resta la sua");
            var caves = Load<LevelTileset>("Assets/_Project/Data/Levels/CaveTileset.asset");
            Assert.AreEqual("BoneKing", caves.GetMarkerPrefab(CavePopulator.BossSymbol).name, "il boss delle caverne resta il re");
        }
    }
}
