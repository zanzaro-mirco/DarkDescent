using System.Collections;
using System.Linq;
using System.Text;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class LevelBuilderTests : SandboxFixture
    {
        private static LevelBuilder RuntimeBuilder()
        {
#if UNITY_EDITOR
            var tileset = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelTileset>("Assets/_Project/Data/Levels/DungeonTileset.asset");
            return new LevelBuilder(tileset, findItem: name =>
                UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset"));
#else
            return null;
#endif
        }

        private static LevelMap Map(string name)
        {
#if UNITY_EDITOR
            return LevelMap.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/_Project/Levels/{name}.txt").text);
#else
            return null;
#endif
        }

        // quello che conta di un livello costruito: gruppi e quanti figli hanno, dove stanno (somma
        // delle posizioni dei figli), cosa ha raccolto il contesto, dove portano le uscite
        private static string Signature(LevelContext context)
        {
            var sb = new StringBuilder();
            foreach (Transform child in context.transform)
            {
                // i figli diretti: le ossa dei nemici si muovono con l'animazione, e l'agent li alza
                // sul NavMesh, quindi per loro conta solo la pianta
                Vector3 sum = Vector3.zero;
                foreach (Transform t in child)
                {
                    sum += t.position;
                }

                if (child.name == "Enemies")
                {
                    sum.y = 0f;
                }

                sb.Append(child.name).Append(':').Append(child.childCount).Append('@').Append(sum.ToString("F1")).Append('\n');
            }

            sb.Append($"depth={context.Depth} enemies={context.Enemies.Count} exits={context.Exits.Count} entrances={context.EntranceCount}\n");
            foreach (var exit in context.Exits)
            {
                var interactable = exit.GetComponent<Interactable>();
                sb.Append($"exit {exit.TargetScene} {exit.TargetEntrance} {interactable.LabelKey} {interactable.ApproachPoint.ToString("F1")}\n");
            }

            foreach (var entrance in context.GetComponentsInChildren<LevelEntrance>())
            {
                sb.Append($"entrance {entrance.Id} {entrance.transform.position.ToString("F1")}\n");
            }

            return sb.ToString();
        }

        [UnityTest, Description("Il livello 1 costruito a runtime è uguale a quello costruito dall'editor: gruppi, posizioni, uscita, ingresso, oggetto a terra, evidenziazione della scala")]
        public IEnumerator RuntimeBuild_MatchesEditorScene()
        {
            SceneManager.LoadScene("Core");
            SceneManager.LoadScene("Level_01", LoadSceneMode.Additive);
            yield return WaitForLevel();
            var built = SceneManager.GetSceneByName("Level_01").GetRootGameObjects()
                .Select(g => g.GetComponent<LevelContext>()).First(c => c != null);
            string expected = Signature(built);
            Material stairsMaterial = built.transform.Find("StairsDown").GetComponentInChildren<Renderer>().sharedMaterial;

            // il NavMesh dell'editor se ne va con la sua scena: quello che resta lo cuoce il runtime
            yield return SceneManager.UnloadSceneAsync("Level_01");

            var runtime = SceneManager.CreateScene("RuntimeLevel");
            SceneManager.SetActiveScene(runtime);
            var context = RuntimeBuilder().Build(Map("Level_01"));
            yield return null;

            Assert.AreEqual(runtime, context.gameObject.scene, "il livello nasce nella scena attiva");
            Assert.AreEqual(expected, Signature(context));
            Assert.AreEqual(4, context.Enemies.Count, "il contesto, acceso a livello finito, trova i nemici");

            var ground = context.GetComponentInChildren<GroundItem>();
            Assert.AreEqual("BadgeShield", ground.Item.Definition.name);

            // la scala sotto il cursore si accende: l'evidenziazione ha ricevuto i suoi renderer
            var exit = context.Exits[0];
            exit.GetComponent<Interactable>().SetHighlighted(true);
            Assert.IsTrue(exit.GetComponent<InteractableHighlight>().IsShowing);
            var stairs = context.transform.Find("StairsDown").GetComponentInChildren<Renderer>();
            Assert.AreNotEqual(stairsMaterial, stairs.sharedMaterial, "materiale acceso");

            // il NavMesh si cuoce anche a runtime, e copre l'ingresso. Dai collider (D5 della M6): le mesh
            // dei modelli non sono leggibili, e in build il NavMesh verrebbe vuoto
            var surface = context.GetComponentInChildren<NavMeshSurface>();
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            Vector3 entrance = context.GetComponentInChildren<LevelEntrance>().transform.position;
            Assert.IsTrue(NavMesh.SamplePosition(entrance, out _, 0.5f, NavMesh.AllAreas), "l'ingresso sta sul NavMesh cotto a runtime");
        }
    }
}
