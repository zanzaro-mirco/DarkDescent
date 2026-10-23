using System.Collections;
using System.Linq;
using DarkDescent.Interaction;
using DarkDescent.Levels;
using DarkDescent.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Quello che il gioco crea a runtime se ne va con chi l'ha creato: la scena non distrugge gli
    /// oggetti fatti con new, e il gioco non chiama mai UnloadUnusedAssets. Trovato dalla partita
    /// lunga della ronda notturna (passo 15.6b della M15): sei mondi, 11 NavMesh e 10–20 mesh in più a
    /// ogni mondo.
    /// </summary>
    public class LeftBehindTests : SandboxFixture
    {
        // il seme della partita lunga della notte 20261024
        private const ulong RunSeed = 15342256791277115959UL;

        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static int NavMeshes()
        {
            return Resources.FindObjectsOfTypeAll<NavMeshData>().Length;
        }

        private static int TelegraphMeshes()
        {
            return Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name == "TelegraphSector");
        }

        private static GameObject Load(string path)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static IEnumerator WaitForDepth(int depth)
        {
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel.Depth != depth; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, $"la profondità {depth} non arriva");
                yield return null;
            }

            yield return null;
        }

        [UnityTest, Description("Scendendo per le scale il NavMesh del livello lasciato sparisce con la sua scena: a ogni profondità ce ne sono quanti alla prima")]
        public IEnumerator LevelChanges_LeaveNoNavMeshBehind()
        {
            yield return LoadGeneratedCore();
            Manager.RunSeed = RunSeed;
            int first = NavMeshes();
            for (int depth = 2; depth <= 4; depth++)
            {
                var interactable = Manager.CurrentLevel.Exits.First().GetComponent<Interactable>();
                PlayerAgent.Warp(interactable.ApproachPoint);
                Player.GetComponent<PlayerController>().Use(interactable);
                yield return WaitForDepth(depth);
                Assert.AreEqual(first, NavMeshes(), $"i NavMesh alla profondità {depth}");
            }
        }

        [UnityTest, Description("Il bruto e il re si portano via le mesh dei loro telegrafi, i ventagli e il cerchio, quando spariscono")]
        public IEnumerator Telegraphs_TakeTheirMeshWithThem()
        {
            yield return LoadCore();
            int before = TelegraphMeshes();
            var brute = Object.Instantiate(Load("Assets/_Project/Prefabs/Brute.prefab"), Player.position + Vector3.right * 2f, Quaternion.identity);
            var king = Object.Instantiate(Load("Assets/_Project/Prefabs/BoneKing.prefab"), Player.position + Vector3.left * 2f, Quaternion.identity);
            yield return null;
            Assert.AreEqual(before + 3, TelegraphMeshes(), "un ventaglio per il bruto, un ventaglio e un cerchio per il re");

            Object.Destroy(brute);
            Object.Destroy(king);
            yield return null;
            Assert.AreEqual(before, TelegraphMeshes(), "spariti il bruto e il re");
        }
    }
}
