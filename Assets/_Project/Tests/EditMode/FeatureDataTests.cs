using System.Collections.Generic;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Interaction;
using DarkDescent.Levels;
using DarkDescent.Localization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Pozzi e santuari (D6, D7 e D10 della M13): generatore, prefab, tipi, nomi e ricordi.</summary>
    public class FeatureDataTests
    {
        private static StringTable Table => StringTable.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv").text);

        private static ShrineDefinition Shrine(string name) => AssetDatabase.LoadAssetAtPath<ShrineDefinition>($"Assets/_Project/Data/Shrines/Shrine_{name}.asset");

        [Test, Description("Furia danno +25%, Vento velocità +25%, Pietra armatura +50%, Sapienza esperienza +50%, per 60 s, con nome, nome da vuoto e messaggio nelle due lingue")]
        public void Shrines_GiveTheirBonus()
        {
            var expected = new (string name, ShrineEffect effect, float percent)[]
            {
                ("Fury", ShrineEffect.Damage, 25f), ("Wind", ShrineEffect.Speed, 25f),
                ("Stone", ShrineEffect.Armor, 50f), ("Wisdom", ShrineEffect.Experience, 50f),
            };
            var table = Table;
            var pitches = new HashSet<float>();
            foreach (var (name, effect, percent) in expected)
            {
                var shrine = Shrine(name);
                Assert.IsNotNull(shrine, name);
                Assert.AreEqual(effect, shrine.Effect, name);
                Assert.AreEqual(percent, shrine.Percent, name);
                Assert.AreEqual(60f, shrine.Duration, name);
                Assert.IsNotNull(shrine.Crystal, name);
                Assert.IsTrue(shrine.Crystal.IsKeywordEnabled("_EMISSION"), name + ": il cristallo brilla");
                pitches.Add(shrine.ChimePitch);
                foreach (var key in new[] { shrine.LabelKey, shrine.SpentLabelKey, shrine.MessageKey })
                {
                    Assert.IsTrue(table.Contains(key), $"{name}: chiave {key} mancante");
                }
            }

            Assert.AreEqual(4, pitches.Count, "ogni santuario suona un po' diverso");
            foreach (var key in new[] { "well.life", "well.life.spent", "well.mana", "well.mana.spent" })
            {
                Assert.IsTrue(table.Contains(key), key);
            }
        }

        [Test, Description("Nel tileset della cripta i pozzi e i santuari hanno il loro prefab: ostacolo, click, punto d'arrivo davanti, cristallo acceso con il materiale spento pronto, luce del colore, suono")]
        public void FeaturePrefabs_AreUsable()
        {
            var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>("Assets/_Project/Data/Levels/DungeonTileset.asset");
            var symbols = DungeonPopulator.WellSymbols + DungeonPopulator.ShrineSymbols;
            var shrines = new HashSet<ShrineDefinition>();
            foreach (char symbol in symbols)
            {
                var prefab = tileset.GetMarkerPrefab(symbol);
                Assert.IsNotNull(prefab, $"'{symbol}'");
                var feature = prefab.GetComponent<OneUseFeature>();
                Assert.IsNotNull(feature, prefab.name);
                Assert.AreEqual(LayerMask.NameToLayer("Obstacle"), prefab.layer, prefab.name);
                Assert.IsTrue(prefab.GetComponentsInChildren<Collider>().Any(c => c.gameObject.layer == LayerMask.NameToLayer("Interactable")), prefab.name + ": click");
                Assert.Greater(prefab.transform.Find("ApproachPoint").localPosition.z, 1f, prefab.name + ": si arriva davanti");

                var so = new SerializedObject(feature);
                var crystal = (Renderer)so.FindProperty("_crystal").objectReferenceValue;
                Assert.IsNotNull(crystal, prefab.name);
                Assert.IsTrue(crystal.sharedMaterial.IsKeywordEnabled("_EMISSION"), prefab.name + ": cristallo acceso");
                var spent = (Material)so.FindProperty("_spentCrystal").objectReferenceValue;
                Assert.IsFalse(spent.IsKeywordEnabled("_EMISSION"), prefab.name + ": cristallo spento");
                var light = (Light)so.FindProperty("_light").objectReferenceValue;
                Assert.IsNotNull(light, prefab.name);
                Assert.IsNotNull(so.FindProperty("_useClip").objectReferenceValue, prefab.name);
                Assert.IsNotNull(so.FindProperty("_audio").objectReferenceValue, prefab.name);

                if (feature is Shrine shrine)
                {
                    Assert.IsTrue(shrines.Add(shrine.Definition), prefab.name + ": un tipo per santuario");
                    Assert.AreSame(shrine.Definition.Crystal, crystal.sharedMaterial, prefab.name);
                    Assert.AreEqual(shrine.Definition.Color, light.color, prefab.name);
                }
                else
                {
                    var well = (Well)feature;
                    Assert.AreEqual(symbol == ')', well.IsMana, prefab.name);
                    Assert.IsNotNull(well.Liquid, prefab.name);
                }
            }

            Assert.AreEqual(4, shrines.Count);
        }

        [Test, Description("Nella cripta al più un pozzo e un santuario per livello, al centro di una stanza grande; in 200 livelli escono tutti e due i pozzi e tutti e quattro i santuari, e mai nella stanza di partenza")]
        public void Generator_PlacesWellsAndShrines()
        {
            var crypt = AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");
            var generator = crypt.CreateGenerator();
            var seen = new HashSet<char>();
            int wells = 0;
            int shrines = 0;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var layout = generator.Generate(seed, 1 + (int)(seed % 4));
                int levelWells = 0;
                int levelShrines = 0;
                for (int y = 0; y < layout.Height; y++)
                {
                    for (int x = 0; x < layout.Width; x++)
                    {
                        char c = layout[x, y];
                        if (!DungeonPopulator.IsFeature(c))
                        {
                            continue;
                        }

                        seen.Add(c);
                        bool well = DungeonPopulator.WellSymbols.IndexOf(c) >= 0;
                        levelWells += well ? 1 : 0;
                        levelShrines += well ? 0 : 1;
                        Assert.IsFalse(layout.Rooms[layout.StartRoom].Contains(new Vector2Int(x, y)), $"seme {seed}: nella stanza di partenza");
                        foreach (var step in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
                        {
                            Assert.IsTrue(layout.IsFloor(x + step.x, y + step.y), $"seme {seed}: ({x}, {y}) contro un muro");
                        }
                    }
                }

                Assert.LessOrEqual(levelWells, 1, $"seme {seed}: un pozzo solo");
                Assert.LessOrEqual(levelShrines, 1, $"seme {seed}: un santuario solo");
                wells += levelWells;
                shrines += levelShrines;
            }

            CollectionAssert.IsSubsetOf((DungeonPopulator.WellSymbols + DungeonPopulator.ShrineSymbols).ToCharArray(), seen.ToArray(),
                "ogni pozzo e ogni santuario esce almeno una volta");
            Assert.Greater(wells, 20, "pozzi in 200 livelli");
            Assert.Greater(shrines, 20, "santuari in 200 livelli");
        }

        [Test, Description("Se ogni stanza grande vuole un pozzo, ogni livello con una stanza grande oltre a quella di partenza ne ha uno: il tiro della stanza di partenza, che non lo prende, non conta")]
        public void StartRoomRoll_DoesNotUseTheWell()
        {
            var settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset"));
            try
            {
                var so = new SerializedObject(settings);
                so.FindProperty("_barrelsChance").floatValue = 0f;
                so.FindProperty("_wellChance").floatValue = 1f;
                so.FindProperty("_shrineChance").floatValue = 0f;
                so.FindProperty("_trapsChance").floatValue = 0f;
                so.ApplyModifiedPropertiesWithoutUndo();
                var generator = settings.CreateGenerator();
                int levels = 0;
                int withWell = 0;
                for (ulong seed = 1; seed <= 60; seed++)
                {
                    var layout = generator.Generate(seed, 2);
                    bool bigRoom = layout.Rooms.Where((room, i) => i != layout.StartRoom
                        && room.width >= DungeonPopulator.MinFeatureRoom && room.height >= DungeonPopulator.MinFeatureRoom).Any();
                    if (!bigRoom)
                    {
                        continue;
                    }

                    levels++;
                    withWell += layout.Features.Count(f => f.Kind == RoomFeatureKind.Well) == 1 ? 1 : 0;
                }

                Assert.Greater(levels, 20);
                Assert.GreaterOrEqual(withWell, levels - 1, "un pozzo in ogni livello che ha posto");
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test, Description("Il ricordo tiene i pozzi e i santuari usati e quanti erano; uno di prima non ne ha, e nemici, casse e barili restano validi")]
        public void LevelRecord_RemembersSpentFeatures()
        {
            var record = new LevelRecord("Level_Crypt", 2, 10, 2, 5, 2);
            record.AddSpent(1);
            Assert.IsTrue(record.MatchesFeatures(2));
            Assert.IsFalse(record.MatchesFeatures(1));
            var read = JsonUtility.FromJson<LevelRecord>(JsonUtility.ToJson(record));
            CollectionAssert.AreEqual(new[] { 1 }, read.Spent);
            Assert.IsTrue(read.MatchesBreakables(5));

            var before = JsonUtility.FromJson<LevelRecord>("{\"_scene\":\"Level_Crypt\",\"_depth\":2,\"_enemyCount\":10,\"_chestCount\":2,\"_killed\":[],\"_opened\":[],\"_items\":[]}");
            Assert.IsTrue(before.Matches(10, 2));
            Assert.AreEqual(0, before.Spent.Count);
            Assert.IsFalse(before.MatchesFeatures(1), "i pozzi del livello ripartono pieni");
        }

        [Test, Description("Il bonus in punti percentuali: 25 porta al 125%, senza bonus resta uguale")]
        public void WithBonus_AddsPercentPoints()
        {
            Assert.AreEqual(125f, CombatFormulas.WithBonus(100f, 25f), 1e-4f);
            Assert.AreEqual(15f, CombatFormulas.WithBonus(10f, 50f), 1e-4f);
            Assert.AreEqual(7f, CombatFormulas.WithBonus(7f, 0f), 1e-4f);
        }
    }
}
