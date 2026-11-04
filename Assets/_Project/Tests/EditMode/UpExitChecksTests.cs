using DarkDescent.Core;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    /// <summary>La scala in su su molti semi, con il generatore vero (D14 della M22): i controlli della ronda notturna, a ogni push su 64 semi.</summary>
    public class UpExitChecksTests
    {
        private const int Seeds = 64;

        private static void EveryLevel(string assetName, int firstDepth, int lastDepth)
        {
            var settings = AssetDatabase.LoadAssetAtPath<DungeonSettings>($"Assets/_Project/Data/Levels/{assetName}.asset");
            var generator = settings.CreateGenerator();
            for (int depth = firstDepth; depth <= lastDepth; depth++)
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var layout = generator.Generate(SeedMixer.ForLevel(seed, depth), depth);
                    MapChecks.Pass($"{assetName}, seme {seed}, profondità {depth}", MapChecks.UpExit(settings, layout, depth));
                }
            }
        }

        [Test, Description("Cripta, profondità 1–4, 64 semi: la scala in su c'è, sta su un pavimento libero, il davanti si raggiunge e non chiude nessun passaggio")]
        public void Crypt_HasAValidStairsUp() => EveryLevel("CryptSettings", 1, 4);

        [Test, Description("Caverne, profondità 5–8, 64 semi: la stessa cosa, attorno all'ingresso (la sala del re all'8 compresa)")]
        public void Caves_HaveAValidStairsUp() => EveryLevel("CaveSettings", 5, 8);

        // i dieci livelli su 40000 della ronda del 4 nov 2026 senza posto: nella cripta una stanza da quattro righe con i barili ai lati, nelle
        // caverne un posto oltre i 14 passi dall'ingresso
        private static readonly (string asset, ulong seed, int depth)[] RoundOfNovemberFourth =
        {
            ("CryptSettings", 15350509948729063921UL, 2),
            ("CryptSettings", 4188725880966964667UL, 1),
            ("CryptSettings", 4089383737949874156UL, 1),
            ("CryptSettings", 11130291107631566839UL, 3),
            ("CaveSettings", 280633663985087738UL, 8),
            ("CaveSettings", 8609991402389985406UL, 7),
            ("CaveSettings", 16473295633914758376UL, 6),
            ("CaveSettings", 15922359730270781759UL, 7),
            ("CaveSettings", 3453924801427519583UL, 6),
            ("CaveSettings", 9234089153769734514UL, 8),
        };

        [Test, Description("I livelli che la ronda del 4 nov 2026 aveva trovato senza posto per la scala in su ora ce l'hanno: nelle stanze da quattro righe con le tasche, e nelle caverne oltre il raggio, si cerca in tutto il livello")]
        public void TheRoundsLevelsWithoutASpot_NowHaveOne()
        {
            foreach (var (asset, seed, depth) in RoundOfNovemberFourth)
            {
                var settings = AssetDatabase.LoadAssetAtPath<DungeonSettings>($"Assets/_Project/Data/Levels/{asset}.asset");
                var layout = settings.CreateGenerator().Generate(SeedMixer.ForLevel(seed, depth), depth);
                Assert.IsTrue(TunnelSpot.TryChoose(layout, out _, out _), $"{asset}, partita {seed}, profondità {depth}");
                MapChecks.Pass($"{asset}, partita {seed}, profondità {depth}", MapChecks.UpExit(settings, layout, depth));
            }
        }

        [Test, Description("Una profondità che le impostazioni non coprono non ha un livello di sopra: il controllo non chiede niente")]
        public void WithoutALevelAbove_NothingIsExpected()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");
            var layout = settings.CreateGenerator().Generate(SeedMixer.ForLevel(1UL, 3), 3);
            Assert.IsNull(MapChecks.UpExit(settings, layout, 99), "una profondità che le impostazioni non coprono");
        }
    }
}
