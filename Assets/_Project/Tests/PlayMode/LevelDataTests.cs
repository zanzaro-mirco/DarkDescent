using System.Collections;
using System.Collections.Generic;
using System.IO;
using DarkDescent.Levels;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
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
