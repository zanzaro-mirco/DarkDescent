using System.Collections.Generic;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>I gradi degli affissi (D7 e D8 della M14): due gradi aperti, gruppi con lo stesso peso.</summary>
    public class AffixGradeTests
    {
        private static readonly int[] GradeLevels = { 1, 6, 15, 25, 35, 45 };

        private readonly List<AffixDefinition> _created = new List<AffixDefinition>();

        private static AffixDatabase Affixes => AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");
        private static ItemDefinition Sword => AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/ShortSword.asset");

        [TearDown]
        public void DestroyCreated()
        {
            foreach (var affix in _created)
            {
                Object.DestroyImmediate(affix);
            }

            _created.Clear();
        }

        private AffixDefinition Affix(string group, int grade, int minItemLevel, AffixKind kind = AffixKind.Suffix)
        {
            var affix = ScriptableObject.CreateInstance<AffixDefinition>();
            affix.name = $"{group}_{grade}";
            var serialized = new SerializedObject(affix);
            serialized.FindProperty("_id").stringValue = $"{group}-{grade}";
            serialized.FindProperty("_group").stringValue = group;
            serialized.FindProperty("_grade").intValue = grade;
            serialized.FindProperty("_minItemLevel").intValue = minItemLevel;
            serialized.FindProperty("_kind").enumValueIndex = (int)kind;
            serialized.FindProperty("_effect").enumValueIndex = (int)AffixEffect.Strength;
            serialized.FindProperty("_min").intValue = grade * 10;
            serialized.FindProperty("_max").intValue = grade * 10 + 5;
            serialized.FindProperty("_targets").intValue = (int)AffixTargets.All;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _created.Add(affix);
            return affix;
        }

        private List<AffixDefinition> SixGrades(string group, AffixKind kind = AffixKind.Suffix)
        {
            // in disordine apposta: il generatore li mette in fila da solo
            var grades = new List<AffixDefinition>();
            foreach (int grade in new[] { 4, 1, 6, 2, 5, 3 })
            {
                grades.Add(Affix(group, grade, GradeLevels[grade - 1], kind));
            }

            return grades;
        }

        [Test, Description("Un oggetto tira solo i due gradi più alti che il suo livello ammette, mai uno sopra il livello")]
        public void Item_RollsOnlyTheTwoHighestOpenGrades()
        {
            var generator = new ItemGenerator(SixGrades("strength"), RarityTable.Default);
            foreach (int level in new[] { 1, 5, 6, 14, 15, 30, 44, 45, 55 })
            {
                int highest = GradeLevels.Count(min => min <= level);
                var seen = new HashSet<int>();
                for (ulong seed = 1; seed <= 200; seed++)
                {
                    var item = generator.Generate(Sword, level, seed, Rarity.Magic);
                    Assert.AreEqual(1, item.Affixes.Count, $"livello {level}, seme {seed}: un gruppo solo, un affisso solo");
                    int grade = item.Affixes[0].Definition.Grade;
                    Assert.LessOrEqual(item.Affixes[0].Definition.MinItemLevel, level, $"livello {level}, seme {seed}");
                    Assert.That(grade, Is.InRange(highest - 1, highest), $"livello {level}, seme {seed}");
                    seen.Add(grade);
                }

                Assert.AreEqual(highest == 1 ? 1 : 2, seen.Count, $"livello {level}: escono tutti e due i gradi aperti");
            }
        }

        [Test, Description("Ogni gruppo esce con lo stesso peso, anche se ha più gradi degli altri")]
        public void Groups_HaveTheSameWeight()
        {
            var pool = new List<AffixDefinition>(SixGrades("strength"));
            pool.Add(Affix("dexterity", 1, 1));
            var generator = new ItemGenerator(pool, RarityTable.Default);

            int strength = 0;
            const int Rolls = 10000;
            for (ulong seed = 1; seed <= Rolls; seed++)
            {
                if (generator.Generate(Sword, 50, seed, Rarity.Magic).Affixes[0].Definition.Group == "strength")
                {
                    strength++;
                }
            }

            Assert.That(strength / (double)Rolls, Is.InRange(0.47, 0.53), "metà Forza e metà Destrezza");
        }

        [Test, Description("Due gradi dello stesso gruppo non stanno mai sullo stesso oggetto")]
        public void Grades_OfOneGroup_ExcludeEachOther()
        {
            var pool = new List<AffixDefinition>(SixGrades("strength"));
            pool.AddRange(SixGrades("life"));
            pool.Add(Affix("damage", 1, 1, AffixKind.Prefix));
            pool.Add(Affix("damage", 2, 6, AffixKind.Prefix));
            var generator = new ItemGenerator(pool, RarityTable.Default);
            for (ulong seed = 1; seed <= 500; seed++)
            {
                var item = generator.Generate(Sword, 20, seed, Rarity.Rare);
                var groups = item.Affixes.Select(a => a.Definition.Group).ToList();
                Assert.AreEqual(groups.Count, groups.Distinct().Count(), $"seme {seed}: {string.Join(", ", groups)}");
            }
        }

        [Test, Description("Stesso seme, stesso oggetto: anche con i gradi")]
        public void SameSeed_SameItem()
        {
            var pool = new List<AffixDefinition>(SixGrades("strength"));
            pool.AddRange(SixGrades("life"));
            var first = new ItemGenerator(pool, RarityTable.Default);
            var second = new ItemGenerator(pool, RarityTable.Default);
            for (ulong seed = 1; seed <= 100; seed++)
            {
                var a = first.Generate(Sword, 30, seed);
                var b = second.Generate(Sword, 30, seed);
                Assert.AreEqual(a.Affixes.Select(x => x.AffixId + x.Value), b.Affixes.Select(x => x.AffixId + x.Value), $"seme {seed}");
            }
        }

        [Test, Description("Negli asset, i gradi di un gruppo sono in fila: stesso tipo, stesso effetto, stessi oggetti, livello e valori che crescono")]
        public void Assets_GradesOfAGroup_Grow()
        {
            foreach (var group in Affixes.Affixes.GroupBy(a => a.Group))
            {
                var grades = group.OrderBy(a => a.Grade).ToList();
                Assert.AreEqual(grades.Count, grades.Select(a => a.Grade).Distinct().Count(), $"{group.Key}: due affissi con lo stesso grado");
                Assert.AreEqual(1, grades[0].Grade, $"{group.Key}: il primo grado è l'1");
                for (int i = 1; i < grades.Count; i++)
                {
                    var lower = grades[i - 1];
                    var upper = grades[i];
                    string where = $"{group.Key}: {lower.name} → {upper.name}";
                    Assert.AreEqual(lower.Grade + 1, upper.Grade, where);
                    Assert.AreEqual(lower.Kind, upper.Kind, where);
                    Assert.AreEqual(lower.Effect, upper.Effect, where);
                    Assert.AreEqual(lower.Targets, upper.Targets, where);
                    Assert.Greater(upper.MinItemLevel, lower.MinItemLevel, where);
                    Assert.Greater(upper.Min, lower.Max, where);
                }
            }
        }

        [Test, Description("Dodici gruppi da sei gradi, tutti nel database, ognuno dal suo livello: 1, 6, 15, 25, 35, 45 (il grado I può partire più in alto)")]
        public void Assets_TwelveGroupsOfSixGrades()
        {
            var inFolder = AssetDatabase.FindAssets("t:AffixDefinition", new[] { "Assets/_Project/Data/Affixes" });
            Assert.AreEqual(72, inFolder.Length);
            Assert.AreEqual(72, Affixes.Affixes.Count, "ogni asset della cartella è nel database");

            var groups = Affixes.Affixes.GroupBy(a => a.Group).ToList();
            Assert.AreEqual(12, groups.Count);
            foreach (var group in groups)
            {
                var grades = group.OrderBy(a => a.Grade).ToList();
                Assert.AreEqual(AffixDefinition.MaxGrade, grades.Count, group.Key);
                Assert.That(grades[0].MinItemLevel, Is.InRange(1, GradeLevels[1] - 1), $"{group.Key}: grado I");
                for (int i = 1; i < grades.Count; i++)
                {
                    Assert.AreEqual(GradeLevels[i], grades[i].MinItemLevel, $"{group.Key}: grado {i + 1}");
                }
            }
        }

        [Test, Description("Gli id degli affissi sono tutti diversi: due gradi con lo stesso id rovinerebbero i salvataggi")]
        public void Assets_IdsAreUnique()
        {
            var ids = Affixes.Affixes.Select(a => a.Id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count());
        }

        [Test, Description("Il tooltip dice il grado in piccolo dopo il valore, in numeri romani")]
        public void Tooltip_ShowsTheGrade()
        {
            Assert.AreEqual("I", ItemDescription.RomanGrade(1));
            Assert.AreEqual("IV", ItemDescription.RomanGrade(4));
            Assert.AreEqual("VI", ItemDescription.RomanGrade(AffixDefinition.MaxGrade));
            Assert.AreEqual(string.Empty, ItemDescription.RomanGrade(0));
        }
    }
}
