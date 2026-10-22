using System.Collections.Generic;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.Progression;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Le difficoltà in Core (passo 14.2): la tabella, le regole di apertura, il mondo nuovo, la morte che costa.</summary>
    public class DifficultyTests
    {
        private static DifficultyTable Table => AssetDatabase.LoadAssetAtPath<DifficultyTable>("Assets/_Project/Data/Progression/Difficulties.asset");

        private static ProgressionSettings Settings => AssetDatabase.LoadAssetAtPath<ProgressionSettings>("Assets/_Project/Data/Progression/Progression.asset");

        [Test, Description("Nove gradini: Crepuscolo, Ombra, Tenebra, Abisso I–VI, con i nomi nelle due lingue (D2)")]
        public void Table_HasNineNamedTiers()
        {
            var table = Table;
            Assert.AreEqual(9, table.Count);
            var strings = StringTable.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv").text);
            var expected = new[] { "dusk", "shadow", "darkness", "abyss1", "abyss2", "abyss3", "abyss4", "abyss5", "abyss6" };
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual("difficulty." + expected[i], table.Tier(i).NameKey);
                Assert.IsTrue(strings.Contains(table.Tier(i).NameKey), table.Tier(i).NameKey);
            }
        }

        [Test, Description("Il Crepuscolo è il gioco di prima: niente in più, nessun moltiplicatore, morire non costa")]
        public void Dusk_IsTheOldGame()
        {
            var dusk = Table.Tier(0);
            Assert.AreEqual(0, dusk.ZoneLevelBonus);
            Assert.AreEqual((1f, 1f, 1f, 1f), (dusk.Life, dusk.Damage, dusk.Experience, dusk.Gold));
            Assert.AreEqual((0, 0, 0), (dusk.ToHit, dusk.Armor, dusk.BossModifiers));
            Assert.AreEqual((0f, 0f), (dusk.DeathGold, dusk.DeathExperience));
            foreach (Rarity rarity in new[] { Rarity.Normal, Rarity.Magic, Rarity.Rare })
            {
                Assert.AreEqual(RarityTable.Default.Probability(rarity), dusk.Rarities.Probability(rarity), 1e-9, rarity.ToString());
            }
        }

        [Test, Description("Ogni gradino è più duro del precedente e rende di più; il re prende 0, 1, 2, 3, 4 e poi 5 modificatori (D5, D9, D11)")]
        public void Tiers_Grow()
        {
            var table = Table;
            CollectionAssert.AreEqual(new[] { 0, 10, 20, 30, 33, 36, 39, 42, 45 }, Tiers(table, t => t.ZoneLevelBonus));
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 5, 5, 5 }, Tiers(table, t => t.BossModifiers));
            CollectionAssert.AreEqual(new[] { 0, 10, 15, 20, 22, 24, 26, 28, 30 }, Tiers(table, t => Mathf.RoundToInt(t.DeathGold * 100f)));
            CollectionAssert.AreEqual(new[] { 0, 5, 10, 15, 17, 19, 21, 23, 25 }, Tiers(table, t => Mathf.RoundToInt(t.DeathExperience * 100f)));
            for (int i = 1; i < table.Count; i++)
            {
                var lower = table.Tier(i - 1);
                var upper = table.Tier(i);
                string where = $"{lower.NameKey} → {upper.NameKey}";
                Assert.Greater(upper.Life, lower.Life, where);
                Assert.Greater(upper.Damage, lower.Damage, where);
                Assert.Greater(upper.ToHit, lower.ToHit, where);
                Assert.Greater(upper.Armor, lower.Armor, where);
                Assert.Greater(upper.Experience, lower.Experience, where);
                Assert.Greater(upper.Gold, lower.Gold, where);
                Assert.Greater(upper.Rarities.Probability(Rarity.Rare), lower.Rarities.Probability(Rarity.Rare), where);
                Assert.Less(upper.Rarities.Probability(Rarity.Normal), lower.Rarities.Probability(Rarity.Normal), where);
            }
        }

        private static List<int> Tiers(DifficultyTable table, System.Func<DifficultyTier, int> value)
        {
            var values = new List<int>();
            for (int i = 0; i < table.Count; i++)
            {
                values.Add(value(table.Tier(i)));
            }

            return values;
        }

        [Test, Description("Fuori misura si prende il gradino più vicino: un salvataggio con più gradini di oggi non rompe niente")]
        public void Tier_OutOfRange_IsClamped()
        {
            var table = Table;
            Assert.AreSame(table.Tier(0), table.Tier(-3));
            Assert.AreSame(table.Tier(table.Count - 1), table.Tier(99));
        }

        [Test, Description("Una partita nuova è al Crepuscolo e può scegliere solo quello; uccidere il re apre la successiva")]
        public void Progress_OpensOnlyTheNext()
        {
            var progress = new DifficultyProgress(9);
            Assert.AreEqual((0, -1, 0), (progress.Current, progress.HighestCompleted, progress.World));
            Assert.IsTrue(progress.CanSelect(0));
            Assert.IsFalse(progress.CanSelect(1), "l'Ombra è chiusa finché il re non cade");
            Assert.IsFalse(progress.Select(1));

            Assert.IsTrue(progress.CompleteCurrent(), "la prima volta");
            Assert.IsFalse(progress.CompleteCurrent(), "la seconda volta niente di nuovo");
            Assert.IsTrue(progress.CanSelect(1));
            Assert.IsFalse(progress.CanSelect(2), "solo la successiva, non due avanti");
            Assert.IsFalse(progress.CanSelect(-1));
        }

        [Test, Description("Si torna a una difficoltà più bassa e si risale; cambiare difficoltà fa un mondo nuovo, sceglierne la stessa no")]
        public void Progress_GoesBackAndForth()
        {
            var progress = new DifficultyProgress(9);
            var worlds = new List<int>();
            int changes = 0;
            progress.WorldChanged += worlds.Add;
            progress.Changed += () => changes++;

            progress.CompleteCurrent();
            Assert.IsTrue(progress.Select(1));
            progress.CompleteCurrent();
            Assert.IsTrue(progress.Select(0), "si torna al Crepuscolo");
            Assert.AreEqual((0, 1), (progress.Current, progress.HighestCompleted));
            Assert.IsTrue(progress.CanSelect(2), "la Tenebra resta aperta anche dal Crepuscolo");
            Assert.IsFalse(progress.Select(0), "è già quella");
            Assert.IsTrue(progress.Select(2));

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, worlds);
            Assert.AreEqual(3, progress.World);
            Assert.AreEqual(5, changes, "due completate e tre mondi");
        }

        [Test, Description("All'ultimo Abisso completato non si apre niente oltre la tabella")]
        public void Progress_StopsAtTheLastTier()
        {
            var progress = new DifficultyProgress(3);
            progress.Restore(2, 1, 4);
            Assert.IsTrue(progress.CompleteCurrent());
            Assert.AreEqual(2, progress.HighestCompleted);
            Assert.IsFalse(progress.CanSelect(3));
        }

        [Test, Description("Rifare il mondo tiene la difficoltà e avanza il numero del mondo")]
        public void Progress_NewWorld_KeepsTheDifficulty()
        {
            var progress = new DifficultyProgress(9);
            progress.Restore(1, 0, 2);
            int world = -1;
            progress.WorldChanged += w => world = w;
            progress.NewWorld();
            Assert.AreEqual((1, 0, 3), (progress.Current, progress.HighestCompleted, progress.World));
            Assert.AreEqual(3, world);
        }

        [Test, Description("Uno stato salvato si rimette; valori fuori misura si correggono, mai una difficoltà chiusa")]
        public void Progress_Restore_Clamps()
        {
            var progress = new DifficultyProgress(9);
            progress.Restore(4, 3, 7);
            Assert.AreEqual((4, 3, 7), (progress.Current, progress.HighestCompleted, progress.World));

            progress.Restore(6, 2, -5);
            Assert.AreEqual((3, 2, 0), (progress.Current, progress.HighestCompleted, progress.World), "la 6 era chiusa: la prima aperta più alta");

            progress.Restore(-2, 40, 1);
            Assert.AreEqual((0, 8, 1), (progress.Current, progress.HighestCompleted, progress.World));
        }

        [Test, Description("Il seme di un mondo nuovo viene dal vecchio: sempre lo stesso, diverso dal vecchio e da un altro numero (D4)")]
        public void WorldSeed_IsStableAndNew()
        {
            const ulong Seed = 12345UL;
            Assert.AreEqual(SeedMixer.ForWorld(Seed, 1), SeedMixer.ForWorld(Seed, 1));
            Assert.AreNotEqual(Seed, SeedMixer.ForWorld(Seed, 1));
            Assert.AreNotEqual(SeedMixer.ForWorld(Seed, 1), SeedMixer.ForWorld(Seed, 2));
            Assert.AreNotEqual(SeedMixer.ForWorld(Seed, 1), SeedMixer.ForWorld(Seed + 1, 1));

            // una catena di mondi non torna mai indietro
            var seen = new HashSet<ulong> { Seed };
            ulong seed = Seed;
            for (int world = 1; world <= 100; world++)
            {
                seed = SeedMixer.ForWorld(seed, world);
                Assert.IsTrue(seen.Add(seed), $"mondo {world}");
            }
        }

        [Test, Description("La difficoltà alza il livello della zona: all'Ombra la profondità 2 vale 13, e un cavaliere di livello 10 non è troppo forte")]
        public void ZoneLevel_AddsTheDifficulty()
        {
            var settings = Settings;
            Assert.AreEqual(3, settings.ZoneLevel(2));
            Assert.AreEqual(13, settings.ZoneLevel(2, Table.Tier(1).ZoneLevelBonus));
            Assert.AreEqual(55, settings.ZoneLevel(8, Table.Tier(8).ZoneLevelBonus), "il livello più alto degli oggetti");

            // 70 × 1,15 = 80,5 alla profondità 2; all'Ombra × 4, senza riduzione per il livello 10
            Assert.AreEqual(67, settings.ExperienceForKill(70, 2, 5), "al Crepuscolo il 5 è sopra la zona 3");
            Assert.AreEqual(322, settings.ExperienceForKill(70, 2, 10, 10, 4f));
            Assert.AreEqual(settings.ExperienceForKill(70, 2, 5), settings.ExperienceForKill(70, 2, 5, 0, 1f), "il Crepuscolo non cambia niente");
        }

        [Test, Description("L'oro perso è la parte di quello addosso; senza oro, o al Crepuscolo, niente")]
        public void DeathPenalty_Gold()
        {
            Assert.AreEqual(100, DeathPenalty.GoldLost(1000, 0.10f));
            Assert.AreEqual(190, DeathPenalty.GoldLost(1000, 0.19f), "0,19f non deve diventare 189");
            Assert.AreEqual(0, DeathPenalty.GoldLost(0, 0.30f));
            Assert.AreEqual(0, DeathPenalty.GoldLost(1000, 0f));
            Assert.AreEqual(0, DeathPenalty.GoldLost(4, 0.10f), "0,4 arrotonda a zero");
            Assert.AreEqual(1, DeathPenalty.GoldLost(1, 1f));
        }

        [Test, Description("L'esperienza persa è la parte di quella del livello, ma si ferma all'inizio del livello; al massimo niente")]
        public void DeathPenalty_Experience()
        {
            Assert.AreEqual(168, DeathPenalty.ExperienceLost(2000, 3360, 0.05f), "il 5% di 3360");
            Assert.AreEqual(40, DeathPenalty.ExperienceLost(40, 3360, 0.05f), "non sotto l'inizio del livello");
            Assert.AreEqual(0, DeathPenalty.ExperienceLost(0, 3360, 0.25f), "appena saliti non si perde niente");
            Assert.AreEqual(0, DeathPenalty.ExperienceLost(0, 0, 0.25f), "al livello massimo niente, e niente divisioni per zero");
            Assert.AreEqual(0, DeathPenalty.ExperienceLost(2000, 3360, 0f), "al Crepuscolo niente");
        }

        [Test, Description("Togliere esperienza non fa mai scendere di livello")]
        public void Remove_NeverLosesALevel()
        {
            var sheet = new StatSheet();
            var progress = new CharacterProgress(Settings, sheet);
            progress.Restore(9, 500, 2);
            int changes = 0;
            progress.Changed += () => changes++;

            Assert.AreEqual(168, progress.Remove(168));
            Assert.AreEqual((9, 332), (progress.Level, progress.Experience));
            Assert.AreEqual(332, progress.Remove(10_000), "si ferma all'inizio del livello");
            Assert.AreEqual((9, 0, 2), (progress.Level, progress.Experience, progress.UnspentPoints));
            Assert.AreEqual(0, progress.Remove(50), "a zero non c'è più niente");
            Assert.AreEqual(0, progress.Remove(-5));
            Assert.AreEqual(2, changes, "solo quando cambia qualcosa");
            Assert.AreEqual(9f, sheet.Get(StatType.Level));
        }
    }
}
