using System.Collections.Generic;
using DarkDescent.Enemies;
using DarkDescent.Progression;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Esperienza, livelli e punti attributo (D1–D3 della M8), con i numeri dell'asset del gioco.
    /// </summary>
    public class ExperienceTests
    {
        private ProgressionSettings _settings;
        private StatSheet _sheet;
        private CharacterProgress _progress;

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<ProgressionSettings>("Assets/_Project/Data/Progression/Progression.asset");
            _sheet = new StatSheet();
            _sheet.SetBase(StatType.Strength, 30f);
            _progress = new CharacterProgress(_settings, _sheet);
        }

        [Test, Description("La curva: 100 × L^1,6 arrotondata a 10; al livello 20, il massimo, non serve più niente")]
        public void Curve_MatchesTheScheda()
        {
            Assert.AreEqual(100, _settings.ExperienceToNext(1));
            Assert.AreEqual(3360, _settings.ExperienceToNext(9));
            Assert.AreEqual(11120, _settings.ExperienceToNext(19));
            Assert.AreEqual(0, _settings.ExperienceToNext(20));
            Assert.AreEqual(20, _settings.MaxLevel);
        }

        [Test, Description("L'esperienza di un nemico cresce del 15% a profondità e cala quando il cavaliere supera il livello della zona")]
        public void KillExperience_GrowsWithDepthAndShrinksWhenOverLevel()
        {
            Assert.AreEqual(70, _settings.ExperienceForKill(70, 1, 1));
            Assert.AreEqual(144, _settings.ExperienceForKill(70, 8, 1), "70 × (1 + 0,15 × 7)");

            // la profondità 2 vale il livello 3 (2 × 1,25 arrotondato): al 3 niente riduzione, al 5 si divide per 1,2
            Assert.AreEqual(3, _settings.ZoneLevel(2));
            Assert.AreEqual(_settings.ExperienceForKill(70, 2, 1), _settings.ExperienceForKill(70, 2, 3));
            Assert.AreEqual(67, _settings.ExperienceForKill(70, 2, 5), "80,5 / 1,2");

            Assert.AreEqual(0, _settings.ExperienceForKill(0, 5, 1), "chi non vale niente non dà niente");
            Assert.AreEqual(1, _settings.ExperienceForKill(1, 1, 20), "ma chi vale qualcosa dà almeno 1");
        }

        [Test, Description("Scheletro, sciame e bruto valgono 70, 30 e 230 (D2, tarati al passo 8.1)")]
        public void Enemies_HaveTheirExperience()
        {
            int Experience(string name) => AssetDatabase.LoadAssetAtPath<EnemyArchetype>($"Assets/_Project/Data/Enemies/{name}.asset").Experience;
            Assert.AreEqual(70, Experience("Skeleton"));
            Assert.AreEqual(30, Experience("Swarm"));
            Assert.AreEqual(230, Experience("Brute"));
        }

        [Test, Description("Salendo di livello: l'esperienza avanza, ogni livello dà 5 punti e 2 di vita; più livelli in un colpo solo si annunciano uno per uno")]
        public void Add_LevelsUpWithPointsAndLife()
        {
            var levels = new List<int>();
            int gained = 0, changes = 0;
            _progress.LeveledUp += levels.Add;
            _progress.ExperienceGained += amount => gained += amount;
            _progress.Changed += () => changes++;

            Assert.AreEqual(0, _progress.Add(60));
            Assert.AreEqual((1, 60, 0), (_progress.Level, _progress.Experience, _progress.UnspentPoints));

            // 60 + 360 = 420: 100 per il 2, 300 per il 3, ne restano 20
            Assert.AreEqual(2, _progress.Add(360));
            Assert.AreEqual((3, 20, 10), (_progress.Level, _progress.Experience, _progress.UnspentPoints));
            CollectionAssert.AreEqual(new[] { 2, 3 }, levels);
            Assert.AreEqual(4f, _sheet.Get(StatType.Life), "2 di vita per ognuno dei due livelli");
            Assert.AreEqual(420, gained);
            Assert.AreEqual(2, changes);
        }

        [Test, Description("Al livello massimo l'esperienza si ferma a zero e non si sale oltre")]
        public void MaxLevel_Stops()
        {
            _progress.Add(10_000_000);
            Assert.AreEqual(20, _progress.Level);
            Assert.IsTrue(_progress.IsMaxLevel);
            Assert.AreEqual(0, _progress.Experience);
            Assert.AreEqual(19 * 5, _progress.UnspentPoints);
            Assert.AreEqual(0, _progress.Add(1000), "oltre il massimo niente");
            Assert.AreEqual(38f, _sheet.Get(StatType.Life));
        }

        [Test, Description("Un punto speso alza di uno il valore base di un attributo; senza punti, o su ciò che non è un attributo, no")]
        public void Spend_RaisesTheAttribute()
        {
            Assert.IsFalse(_progress.TrySpend(StatType.Strength), "senza punti");
            _progress.Add(100);
            Assert.AreEqual(5, _progress.UnspentPoints);

            Assert.IsTrue(_progress.TrySpend(StatType.Strength));
            Assert.AreEqual(31f, _sheet.GetBase(StatType.Strength));
            Assert.AreEqual(4, _progress.UnspentPoints);

            Assert.IsFalse(_progress.TrySpend(StatType.Armor), "l'Armatura non si compra con i punti");
            Assert.IsFalse(_progress.TrySpend(StatType.Life));
            Assert.AreEqual(4, _progress.UnspentPoints);
        }

        [Test, Description("Uno stato salvato si rimette con la vita dei livelli; valori fuori misura si correggono")]
        public void Restore_PutsBackLevelExperienceAndLife()
        {
            _progress.Restore(7, 500, 3);
            Assert.AreEqual((7, 500, 3), (_progress.Level, _progress.Experience, _progress.UnspentPoints));
            Assert.AreEqual(12f, _sheet.Get(StatType.Life));

            _progress.Restore(99, 99_999, -4);
            Assert.AreEqual((20, 0, 0), (_progress.Level, _progress.Experience, _progress.UnspentPoints));
        }
    }
}
