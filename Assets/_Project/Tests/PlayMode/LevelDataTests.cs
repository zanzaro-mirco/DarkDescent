using System.Collections;
using System.Collections.Generic;
using System.IO;
using DarkDescent.Interaction;
using DarkDescent.Levels;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class LevelDataTests : SandboxFixture
    {
        private static List<string> LevelScenes()
        {
            var names = new List<string>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (path.Contains("/Scenes/Levels/"))
                {
                    names.Add(Path.GetFileNameWithoutExtension(path));
                }
            }

            return names;
        }

        private static IEnumerator LoadWithCore(string level)
        {
            SceneManager.LoadScene("Core");
            SceneManager.LoadScene(level, LoadSceneMode.Additive);
            yield return WaitForLevel();
        }

        [UnityTest, Description("Ogni livello, sandbox compresa, è al buio: ambiente a colore unico, post-processing, niente sole, una sola luce con le ombre (quella del cavaliere)")]
        public IEnumerator Levels_AreDarkWithOneShadowLight()
        {
            var levels = LevelScenes();
            levels.Add(SandboxScene);
            foreach (var level in levels)
            {
                yield return LoadWithCore(level);

                // le impostazioni di luce valgono solo per la scena attiva (trappola 1)
                Assert.AreEqual(level, SceneManager.GetActiveScene().name);
                Assert.AreEqual(AmbientMode.Flat, RenderSettings.ambientMode, $"{level}: ambiente non a colore unico");
                Assert.IsNull(RenderSettings.skybox, $"{level}: c'è ancora il cielo");

                var volume = Object.FindFirstObjectByType<Volume>();
                Assert.IsNotNull(volume, $"{level}: manca il Volume");
                Assert.IsTrue(volume.isGlobal);
                Assert.IsTrue(volume.sharedProfile.Has<Tonemapping>() && volume.sharedProfile.Has<Bloom>()
                    && volume.sharedProfile.Has<Vignette>(), $"{level}: post-processing incompleto");

                int shadowLights = 0;
                foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    Assert.AreNotEqual(LightType.Directional, light.type, $"{level}: luce direzionale {light.name}");
                    if (light.shadows != LightShadows.None)
                    {
                        shadowLights++;
                        Assert.AreEqual("PlayerLight", light.name, $"{level}: {light.name} fa ombre");
                    }
                }

                Assert.AreEqual(1, shadowLights, $"{level}: le ombre le fa solo la luce del cavaliere (D4)");
            }
        }

        [UnityTest, Description("Ogni livello ha contesto, ingresso e NavMesh; ogni uscita porta a una scena della build e a un suo ingresso")]
        public IEnumerator Levels_AreConsistent()
        {
            var levels = LevelScenes();
            CollectionAssert.IsSupersetOf(levels, new[] { "Level_01", "Level_02" });

            var exits = new List<(string From, string Scene, string Entrance)>();
            foreach (var level in levels)
            {
                yield return LoadWithCore(level);
                var context = Object.FindFirstObjectByType<LevelManager>().CurrentLevel;
                Assert.AreEqual(level, context.gameObject.scene.name);
                Assert.Greater(context.EntranceCount, 0, $"{level}: nessun ingresso");
                Assert.IsNotNull(Object.FindFirstObjectByType<NavMeshSurface>(), $"{level}: manca il NavMesh");
                foreach (var exit in context.Exits)
                {
                    Assert.IsNotEmpty(exit.GetComponent<Interactable>().Label, $"{level}: uscita senza etichetta");
                    Assert.IsNotNull(exit.GetComponent<InteractableHighlight>(), $"{level}: uscita che non si evidenzia");
                    exits.Add((level, exit.TargetScene, exit.TargetEntrance));
                }
            }

            foreach (var exit in exits)
            {
                Assert.Contains(exit.Scene, levels, $"{exit.From}: l'uscita porta a {exit.Scene}, che non è nella build");
                yield return LoadWithCore(exit.Scene);
                Assert.IsTrue(Object.FindFirstObjectByType<LevelManager>().CurrentLevel.HasEntrance(exit.Entrance),
                    $"{exit.From}: {exit.Scene} non ha l'ingresso {exit.Entrance}");
            }
        }
    }
}
