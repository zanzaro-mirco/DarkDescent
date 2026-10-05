using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Localization;
using DarkDescent.Stats;
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
        /// <summary>La lingua del gioco, creata dal CompositionRoot.</summary>
        protected static Localizer Localizer => Object.FindFirstObjectByType<CompositionRoot>().Localizer;

        // ogni test parte in inglese, la lingua di default: una scelta salvata da un test o da una
        // prova nell'editor non deve cambiare i testi attesi
        private static void ForgetLanguage()
        {
            PlayerPrefs.DeleteKey(CompositionRoot.LanguagePreference);
        }

        protected IEnumerator LoadSandbox(bool allSkeletons = false)
        {
            ForgetLanguage();
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
            FindPlayer();
        }

        /// <summary>
        /// Core con il livello 1 fatto a mano, caricato nello stesso frame: il LevelManager usa quello
        /// invece della cripta generata. Per i test che cercano una stanza o uno scheletro precisi
        /// (trappola 10 della M6).
        /// </summary>
        protected IEnumerator LoadCore()
        {
            ForgetLanguage();
            Mouse = InputSystem.AddDevice<Mouse>();
            SceneManager.LoadScene("Core");
            SceneManager.LoadScene("Level_01", LoadSceneMode.Additive);
            yield return WaitForLevel();
            yield return null;
            FindPlayer();
        }

        /// <summary>Core da sola, come nella build: il LevelManager genera il primo livello della cripta.</summary>
        protected IEnumerator LoadGeneratedCore()
        {
            ForgetLanguage();
            Mouse = InputSystem.AddDevice<Mouse>();
            SceneManager.LoadScene("Core");
            yield return WaitForLevel();
            yield return null;
            FindPlayer();
        }

        private void FindPlayer()
        {
            Camera = Camera.main;
            Player = GameObject.Find("Player").transform;
            PlayerAgent = Player.GetComponent<NavMeshAgent>();
            Assert.IsTrue(PlayerAgent.isOnNavMesh, "il player deve stare sul NavMesh");

            // tiri fissi (D3 della M4): ogni colpo va a segno con il danno minimo, come i colpi
            // sempre uguali della M2 che questi test verificano
            UseRandom(0.0);
        }

        /// <summary>
        /// Cerca dal seme 1 in su il primo seme della partita con cui il nemico lascia un oggetto che va
        /// bene a <paramref name="wanted"/> (qualsiasi, se null), lo usa e restituisce l'oggetto atteso.
        /// </summary>
        protected static ItemInstance UseLootSeedWithDrop(LootDrop drop, System.Func<ItemInstance, bool> wanted = null)
        {
            var root = Object.FindFirstObjectByType<CompositionRoot>();
            for (ulong seed = 1; seed < 5000; seed++)
            {
                root.UseLootSeed(seed);
                var item = drop.Preview();
                if (item != null && (wanted == null || wanted(item)))
                {
                    return item;
                }
            }

            Assert.Fail($"nessun seme fa cadere l'oggetto cercato da {drop.name}");
            return null;
        }

        /// <summary>I tiri del combattimento restituiscono questi valori, in ciclo.</summary>
        protected static void UseRandom(params double[] values)
        {
            Object.FindFirstObjectByType<CompositionRoot>().UseRandomSource(new FixedRandomSource(values));
        }

        /// <summary>Il danno di un colpo a segno con il tiro minimo: danno minimo dell'arma per la Forza.</summary>
        protected static float MinHitDamage(MeleeAttack attack)
        {
            float strength = CharacterStats.ValueOf(attack.GetComponent<CharacterStats>(), StatType.Strength);
            return CombatFormulas.RollDamage(attack.MinDamage, attack.MinDamage, strength, new FixedRandomSource(0.0));
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
