using System.Collections;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Base dei test PlayMode: carica Core con la sandbox come livello e simula il mouse con
    /// InputTestFixture, che isola l'Input System dai dispositivi reali.
    /// </summary>
    public abstract class SandboxFixture : InputTestFixture
    {
        protected const string SandboxScene = "Sandbox_Combat";

        protected Mouse Mouse { get; private set; }
        protected Camera Camera { get; private set; }
        protected Transform Player { get; private set; }
        protected NavMeshAgent PlayerAgent { get; private set; }

        /// <param name="allSkeletons">
        /// Falso (default): resta attivo solo lo scheletro "Skeleton", così i test su un nemico non
        /// vengono disturbati dagli altri. Vero: la stanza com'è nella build.
        /// </param>
        protected IEnumerator LoadSandbox(bool allSkeletons = false)
        {
            Mouse = InputSystem.AddDevice<Mouse>();

            // Core e sandbox nello stesso frame: il LevelManager trova la sandbox già aperta e la usa
            // al posto del primo livello, come nell'editor con le due scene aperte
            SceneManager.LoadScene("Core");
            SceneManager.LoadScene(SandboxScene, LoadSceneMode.Additive);
            yield return WaitForLevel();
            Assert.AreEqual(SandboxScene, Object.FindFirstObjectByType<LevelManager>().CurrentLevel.gameObject.scene.name);

            if (!allSkeletons)
            {
                foreach (var enemy in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
                {
                    if (enemy.name != "Skeleton")
                    {
                        enemy.gameObject.SetActive(false);
                    }
                }
            }

            // un frame perché Start (snap della camera, agent dei nemici) sia passato
            yield return null;
            Camera = Camera.main;
            Player = GameObject.Find("Player").transform;
            PlayerAgent = Player.GetComponent<NavMeshAgent>();
            Assert.IsTrue(PlayerAgent.isOnNavMesh, "il player deve stare sul NavMesh");
        }

        /// <summary>Aspetta che un livello sia caricato e il player sopra, con un limite di tempo.</summary>
        protected static IEnumerator WaitForLevel(float timeout = 10f)
        {
            // LoadScene agisce al frame successivo: senza questa attesa si troverebbe ancora il
            // LevelManager della scena di prima, già pronto
            yield return null;

            float elapsed = 0f;
            while (elapsed < timeout)
            {
                var manager = Object.FindFirstObjectByType<LevelManager>();
                if (manager != null && manager.CurrentLevel != null && !manager.IsTransitioning)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.Fail("il livello non si è caricato");
        }

        protected void PointAt(Vector3 world)
        {
            Move(Mouse.position, (Vector2)Camera.WorldToScreenPoint(world));
        }

        protected void ClickAt(Vector3 world)
        {
            PointAt(world);
            Press(Mouse.leftButton);
            Release(Mouse.leftButton);
        }

        protected static float FlatDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }
    }
}
