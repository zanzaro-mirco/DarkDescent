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

        [Test, Description("Una profondità che le impostazioni non coprono non ha un livello di sopra: il controllo non chiede niente")]
        public void WithoutALevelAbove_NothingIsExpected()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");
            var layout = settings.CreateGenerator().Generate(SeedMixer.ForLevel(1UL, 3), 3);
            Assert.IsNull(MapChecks.UpExit(settings, layout, 99), "una profondità che le impostazioni non coprono");
        }
    }
}
