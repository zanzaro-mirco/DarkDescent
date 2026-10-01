using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Base dei test PlayMode sulla scena Sandbox_Combat: carica la scena e simula il mouse
    /// con InputTestFixture, che isola l'Input System dai dispositivi reali.
    /// </summary>
    public abstract class SandboxFixture : InputTestFixture
    {
        protected Mouse Mouse { get; private set; }
        protected Camera Camera { get; private set; }
        protected Transform Player { get; private set; }
        protected NavMeshAgent PlayerAgent { get; private set; }

        protected IEnumerator LoadSandbox()
        {
            Mouse = InputSystem.AddDevice<Mouse>();
            SceneManager.LoadScene("Sandbox_Combat");
            // due frame: uno per il caricamento, uno perché Start (snap della camera) sia passato
            yield return null;
            yield return null;
            Camera = Camera.main;
            Player = GameObject.Find("Player").transform;
            PlayerAgent = Player.GetComponent<NavMeshAgent>();
            Assert.IsTrue(PlayerAgent.isOnNavMesh, "il player deve stare sul NavMesh");
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
