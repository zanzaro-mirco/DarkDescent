using DarkDescent.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>«Il mondo cresce con te» (24.5 della M24, D12–D15): il livello efficace, la curva di vita e danno, l'esperienza e il tetto.</summary>
    public class GrowingWorldTests
    {
        private ProgressionSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<ProgressionSettings>("Assets/_Project/Data/Progression/Progression.asset");
        }

        [Test, Description("I numeri di partenza (D13): margine 3 livelli, 4% per livello, tetto ×3")]
        public void Settings_MatchTheScheda()
        {
            Assert.AreEqual(3, _settings.GrowthMargin);
            Assert.AreEqual(0.04f, _settings.GrowthPerLevel, 1e-6f);
            Assert.AreEqual(3f, _settings.GrowthCap, 1e-6f);
        }

        [Test, Description("Spento, il livello efficace è la zona e basta, e la crescita è 1, per ogni profondità, livello del cavaliere e bonus di difficoltà (D17: i numeri della misura non si muovono)")]
        public void Off_ChangesNothing()
        {
            for (int depth = 1; depth <= 8; depth++)
            {
                for (int bonus = 0; bonus <= 24; bonus += 4)
                {
                    for (int hero = 1; hero <= 50; hero++)
                    {
                        Assert.AreEqual(_settings.ZoneLevel(depth, bonus), _settings.EffectiveLevel(depth, hero, bonus, false), $"profondità {depth}, +{bonus}, livello {hero}");
                        Assert.AreEqual(1f, _settings.EnemyGrowth(depth, hero, bonus, false));
                        Assert.AreEqual(
                            _settings.ExperienceForKill(100, depth, hero, bonus, 1.5f),
                            _settings.ExperienceForKill(100, depth, hero, bonus, 1.5f, false),
                            "l'esperienza di prima");
                    }
                }
            }
        }

        [Test, Description("Acceso, il livello efficace è il più alto tra la zona e il livello del cavaliere meno 3; sotto la zona non scende")]
        public void On_EffectiveLevelFollowsTheHeroWithAMargin()
        {
            Assert.AreEqual(1, _settings.EffectiveLevel(1, 1, 0, true));
            Assert.AreEqual(1, _settings.EffectiveLevel(1, 4, 0, true), "un cavaliere entro il margine non cambia niente");
            Assert.AreEqual(2, _settings.EffectiveLevel(1, 5, 0, true));
            Assert.AreEqual(17, _settings.EffectiveLevel(1, 20, 0, true));
            Assert.AreEqual(47, _settings.EffectiveLevel(1, 50, 0, true));
            Assert.AreEqual(_settings.ZoneLevel(8, 0), _settings.EffectiveLevel(8, 5, 0, true), "un cavaliere debole alla 8 trova la zona");
            Assert.AreEqual(_settings.ZoneLevel(3, 12), _settings.EffectiveLevel(3, 1, 12, true), "la difficoltà alza la zona");
        }

        [Test, Description("La curva su carta della scheda (D13): cavaliere 1/10/25/50 contro le profondità 1/3/5/8 al Crepuscolo, da ×1,00 a ×2,84")]
        public void On_TheCurveOfTheScheda()
        {
            Assert.AreEqual(1f, _settings.EnemyGrowth(1, 1, 0, true), 1e-4f);
            Assert.AreEqual(1f, _settings.EnemyGrowth(1, 4, 0, true), 1e-4f);
            Assert.AreEqual(1.64f, _settings.EnemyGrowth(1, 20, 0, true), 1e-4f, "livello 20 alla 1: oggetti da 17, nemici ×1,64");
            Assert.AreEqual(2.84f, _settings.EnemyGrowth(1, 50, 0, true), 1e-4f, "il massimo di oggi: livello 50 alla 1");
            Assert.AreEqual(1.24f, _settings.EnemyGrowth(1, 10, 0, true), 1e-4f, "livello 10: oggetti da 7, zona 1");

            // più si scende, meno distanza c'è da colmare: a pari cavaliere la crescita cala con la profondità
            float previous = float.MaxValue;
            foreach (int depth in new[] { 1, 3, 5, 8 })
            {
                float growth = _settings.EnemyGrowth(depth, 50, 0, true);
                Assert.Less(growth, previous, $"profondità {depth}");
                Assert.LessOrEqual(growth, _settings.GrowthCap);
                previous = growth;
            }
        }

        [Test, Description("Il tetto tiene la crescita sotto ×3 anche se la curva la spingesse oltre (una curva più ripida, un cavaliere oltre il 50)")]
        public void On_TheCapHolds()
        {
            var steep = ScriptableObject.CreateInstance<ProgressionSettings>();
            try
            {
                var so = new SerializedObject(steep);
                so.FindProperty("_growthPerLevel").floatValue = 0.5f;
                so.ApplyModifiedPropertiesWithoutUndo();
                Assert.AreEqual(3f, steep.EnemyGrowth(1, 50, 0, true), 1e-6f);
                Assert.AreEqual(3f, steep.EnemyGrowth(1, 200, 0, true), 1e-6f);
                Assert.Less(steep.EnemyGrowth(1, 6, 0, true), 3f, "sotto il tetto la curva si vede");
            }
            finally
            {
                Object.DestroyImmediate(steep);
            }
        }

        [Test, Description("L'esperienza (D12): acceso, il calo oltre la zona parte dal margine, quindi un cavaliere di livello 20 alla 1 guadagna più che a mondo spento; un cavaliere dentro il margine guadagna come prima")]
        public void On_ExperienceDropsFromTheMargin()
        {
            int off = _settings.ExperienceForKill(100, 1, 20);
            int on = _settings.ExperienceForKill(100, 1, 20, 0, 1f, true);
            Assert.AreEqual(Mathf.RoundToInt(100f / (1f + 0.1f * 19)), off, "spento: oltre la zona di 19 livelli");
            Assert.AreEqual(Mathf.RoundToInt(100f / (1f + 0.1f * 3)), on, "acceso: oltre il livello efficace di 3");
            Assert.Greater(on, off);

            Assert.AreEqual(_settings.ExperienceForKill(100, 1, 3), _settings.ExperienceForKill(100, 1, 3, 0, 1f, true), "dentro il margine niente cambia");
            Assert.AreEqual(_settings.ExperienceForKill(100, 8, 5), _settings.ExperienceForKill(100, 8, 5, 0, 1f, true), "un cavaliere sotto la zona niente");
        }
    }
}
