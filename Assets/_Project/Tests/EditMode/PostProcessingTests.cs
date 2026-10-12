using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DarkDescent.Tests
{
    /// <summary>Il post-processing di ogni tipo di livello (D3 della M11).</summary>
    public class PostProcessingTests
    {
        private static LevelTileset Tileset(string name) => AssetDatabase.LoadAssetAtPath<LevelTileset>($"Assets/_Project/Data/Levels/{name}.asset");

        private static T Active<T>(VolumeProfile profile) where T : VolumeComponent
        {
            Assert.IsTrue(profile.TryGet(out T component), $"{profile.name}: manca {typeof(T).Name}");
            Assert.IsTrue(component.active, $"{profile.name}: {typeof(T).Name} spento");
            return component;
        }

        [Test, Description("Ogni livello ha tonemapping, bloom solo sulle luci forti, vignetta, grana leggera e un colore suo: la cripta fredda, le caverne verdastre, l'accampamento caldo")]
        public void EveryTileset_HasItsOwnGrade()
        {
            var crypt = Tileset("DungeonTileset").PostProcessing;
            var caves = Tileset("CaveTileset").PostProcessing;
            var town = Tileset("TownTileset").PostProcessing;
            Assert.AreNotSame(crypt, caves, "le caverne hanno un profilo loro");
            Assert.AreNotSame(crypt, town);

            foreach (var profile in new[] { crypt, caves, town })
            {
                Active<Tonemapping>(profile);
                Assert.GreaterOrEqual(Active<Bloom>(profile).threshold.value, 0.9f, $"{profile.name}: il bloom resta sulle luci forti");
                Active<Vignette>(profile);
                var grain = Active<FilmGrain>(profile);
                Assert.IsTrue(grain.intensity.overrideState);
                Assert.That(grain.intensity.value, Is.InRange(0.05f, 0.3f), $"{profile.name}: grana leggera");
                Active<WhiteBalance>(profile);
                Active<ColorAdjustments>(profile);
            }

            Assert.Less(Active<WhiteBalance>(crypt).temperature.value, 0f, "cripta fredda");
            Assert.Less(Active<WhiteBalance>(caves).tint.value, 0f, "caverne verdastre");
            Assert.Greater(Active<WhiteBalance>(town).temperature.value, 0f, "accampamento caldo");
        }
    }
}
