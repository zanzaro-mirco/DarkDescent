using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Il mimic e il baule maledetto nella cripta vera (25.3 della M25, D3, D4, D8 e D14): quando si svegliano, che cosa lasciano, e che il livello resta com'è.</summary>
    public class SecretsPlayModeTests : SandboxFixture
    {
        private const int FirstSeed = 1;
        private const int SeedsToTry = 60;

        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentSecrets_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            ChestSecret.Enabled = false;
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        // il seme dei livelli e quello del loot e dei segreti insieme, come nel gioco
        private static void UseSeed(ulong seed)
        {
            Manager.RunSeed = seed;
            UnityEngine.Object.FindFirstObjectByType<CompositionRoot>().UseLootSeed(seed);
        }

        private static IEnumerator Travel(string scene, string entrance, int depth)
        {
            var before = Manager.CurrentLevel;
            Manager.LoadLevel(scene, entrance, depth);
            yield return null;
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel == before; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, scene + " non arriva");
                yield return null;
            }

            yield return null;
            yield return null;
        }

        private static IEnumerator WaitFor(Func<bool> condition, float timeout)
        {
            float elapsed = 0f;
            while (elapsed < timeout && !condition())
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // il cavaliere regge tutto, e i nemici del livello sono spenti: i test parlano dei segreti
        private void Fortify(LevelContext level)
        {
            foreach (var enemy in level.Enemies)
            {
                if (enemy != null)
                {
                    enemy.gameObject.SetActive(false);
                }
            }

            var health = Player.GetComponent<Health>();
            health.MultiplyMax(500f);
            health.Heal(float.MaxValue);
        }

        private static Chest Find(LevelContext level, ChestSecretKind kind)
        {
            return level.Chests.FirstOrDefault(c => c != null && c.Secret == kind);
        }

        // carica livelli di semi diversi alla profondità data finché una cassa ha il segreto cercato: le regole lo danno a poche casse
        private IEnumerator LevelWith(ChestSecretKind kind, int depth, Action<Chest> found)
        {
            yield return LoadGeneratedCore();
            ChestSecret.Enabled = true;
            for (int seed = FirstSeed; seed < FirstSeed + SeedsToTry; seed++)
            {
                UseSeed((ulong)seed);
                yield return Travel("Level_Crypt", "Start", depth);
                var chest = Find(Manager.CurrentLevel, kind);
                if (chest != null)
                {
                    Fortify(Manager.CurrentLevel);
                    found(chest);
                    yield break;
                }
            }

            Assert.Fail($"nessun livello di {SeedsToTry} semi alla profondità {depth} ha una cassa {kind}");
        }

        private static void Kill(EnemyAI enemy)
        {
            enemy.GetComponent<Health>().TakeDamage(new DamageInfo(1_000_000f, DamageType.Physical, null));
        }

        [UnityTest, Timeout(300000), Description("Un mimic (D3): un click sulla cassa non la apre, sveglia un nemico al suo posto, e alla sua morte la cassa si apre e lascia il suo loot di sempre")]
        public IEnumerator Mimic_WakesUpAndTheChestOpensWhenItDies()
        {
            Chest chest = null;
            yield return LevelWith(ChestSecretKind.Mimic, 3, c => chest = c);
            var secret = chest.GetComponent<ChestSecret>();
            var expected = chest.Preview();
            GroundItem dropped = null;
            chest.Opened += item => dropped = item;

            chest.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;

            Assert.IsFalse(chest.IsOpen, "il click sveglia il mimic, non apre la cassa");
            Assert.AreEqual(1, secret.Alive.Count, "un mimic in piedi");
            var mimic = secret.Alive[0];
            Assert.IsTrue(mimic.gameObject.activeInHierarchy);
            Assert.AreEqual("enemy.mimic", mimic.Archetype.NameKey, "è un mimic");
            Assert.IsNull(mimic.GetComponent<LootDrop>(), "il suo premio è la cassa, non un loot suo");

            Kill(mimic);
            yield return WaitFor(() => chest.IsOpen, 5f);

            Assert.IsTrue(chest.IsOpen, "morto il mimic, la cassa si apre");
            Assert.AreEqual(0, secret.Alive.Count);
            if (expected != null)
            {
                Assert.IsNotNull(dropped, "lascia l'oggetto che la cassa avrebbe lasciato");
                Assert.AreEqual(expected.Definition, dropped.Item.Definition, "lo stesso oggetto della cassa di sempre");
            }
        }

        [UnityTest, Timeout(300000), Description("Un baule maledetto (D4): il suo nome lo dice, un click chiama da tre a cinque nemici e non apre, a ondata battuta si apre da solo, con un premio in più")]
        public IEnumerator Cursed_CallsAWaveAndOpensWhenItIsBeaten()
        {
            Chest chest = null;
            yield return LevelWith(ChestSecretKind.Cursed, 3, c => chest = c);
            var secret = chest.GetComponent<ChestSecret>();
            Assert.AreEqual("chest.cursed", chest.GetComponent<Interactable>().LabelKey, "l'etichetta dice che è maledetto");
            Assert.AreEqual(ChestSecret.CursedBonus, chest.Bonus, "ha il premio del baule maledetto");

            chest.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;

            Assert.IsFalse(chest.IsOpen, "non si apre al click");
            Assert.That(secret.Alive.Count, Is.InRange(SecretRules.WaveMin, SecretRules.WaveMax), "l'ondata");
            foreach (var enemy in secret.Alive)
            {
                Assert.IsTrue(enemy.gameObject.activeInHierarchy);
            }

            var wave = secret.Alive.ToList();
            for (int i = 0; i < wave.Count - 1; i++)
            {
                Kill(wave[i]);
            }

            yield return null;
            yield return null;
            Assert.IsFalse(chest.IsOpen, "finché ne resta uno non si apre");

            Kill(wave[wave.Count - 1]);
            yield return WaitFor(() => chest.IsOpen, 5f);
            Assert.IsTrue(chest.IsOpen, "a ondata battuta si apre da solo");
            Assert.AreEqual(0, secret.Alive.Count);
        }

        [UnityTest, Timeout(300000), Description("Il ricordo del livello (D13): il mimic battuto lascia la cassa aperta quando si torna, e senza il suo segreto")]
        public IEnumerator Mimic_Beaten_StaysBeatenWhenYouComeBack()
        {
            Chest chest = null;
            yield return LevelWith(ChestSecretKind.Mimic, 3, c => chest = c);
            int index = Manager.CurrentLevel.Chests.ToList().IndexOf(chest);
            int chests = Manager.CurrentLevel.Chests.Count;
            int enemies = Manager.CurrentLevel.Enemies.Count;
            var seed = Manager.RunSeed;

            chest.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;
            Kill(chest.GetComponent<ChestSecret>().Alive[0]);
            yield return WaitFor(() => chest.IsOpen, 5f);
            Assert.IsTrue(chest.IsOpen);

            yield return Travel("Level_Crypt", "Start", 2);
            UseSeed(seed);
            yield return Travel("Level_Crypt", "Start", 3);
            var back = Manager.CurrentLevel;

            Assert.AreEqual(chests, back.Chests.Count, "lo stesso numero di casse: i segreti non toccano il livello");
            Assert.AreEqual(enemies, back.Enemies.Count, "lo stesso numero di nemici");
            Assert.IsTrue(back.Chests[index].IsOpen, "la cassa del mimic battuto è aperta");
            Assert.AreEqual(ChestSecretKind.None, back.Chests[index].Secret, "e non ha più il segreto");
        }

        [UnityTest, Timeout(300000), Description("Lasciare il livello a metà (D3): il mimic sveglio non è nel ricordo, e quando si torna la cassa è chiusa di nuovo e ancora con il suo segreto")]
        public IEnumerator Mimic_LeftHalfway_IsAClosedChestAgain()
        {
            Chest chest = null;
            yield return LevelWith(ChestSecretKind.Mimic, 3, c => chest = c);
            int index = Manager.CurrentLevel.Chests.ToList().IndexOf(chest);
            var seed = Manager.RunSeed;

            chest.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;
            Assert.AreEqual(1, chest.GetComponent<ChestSecret>().Alive.Count);

            yield return Travel("Level_Crypt", "Start", 2);
            UseSeed(seed);
            yield return Travel("Level_Crypt", "Start", 3);
            var again = Manager.CurrentLevel.Chests[index];

            Assert.IsFalse(again.IsOpen, "chiusa");
            Assert.AreEqual(ChestSecretKind.Mimic, again.Secret, "è ancora un mimic: lo stesso mondo dà gli stessi segreti");
            Assert.AreEqual(0, again.GetComponent<ChestSecret>().Alive.Count, "nessun nemico rimasto del giro di prima");
        }

        [UnityTest, Timeout(300000), Description("I segreti non toccano il livello (D14): con l'interruttore acceso e spento le casse e i nemici sono gli stessi, nello stesso posto, e alla profondità 1 nessuna cassa ha un segreto")]
        public IEnumerator Secrets_DoNotChangeTheLevel()
        {
            yield return LoadGeneratedCore();
            UseSeed(7);

            ChestSecret.Enabled = false;
            yield return Travel("Level_Crypt", "Start", 3);
            var off = Manager.CurrentLevel;
            var chestsOff = off.Chests.Select(c => c.transform.position).ToList();
            var enemiesOff = off.Enemies.Select(e => e.transform.position).ToList();
            Assert.IsTrue(off.Chests.All(c => c.Secret == ChestSecretKind.None), "spento: nessun segreto");

            ChestSecret.Enabled = true;
            yield return Travel("Level_Crypt", "Start", 3);
            var on = Manager.CurrentLevel;
            CollectionAssert.AreEqual(chestsOff, on.Chests.Select(c => c.transform.position).ToList(), "le stesse casse, nello stesso posto");
            CollectionAssert.AreEqual(enemiesOff, on.Enemies.Select(e => e.transform.position).ToList(), "gli stessi nemici, nello stesso posto");

            yield return Travel("Level_Crypt", "Start", 1);
            Assert.IsTrue(Manager.CurrentLevel.Chests.All(c => c.Secret == ChestSecretKind.None), "alla profondità 1 nessun segreto");
        }

        [UnityTest, Timeout(300000), Description("Il tesoro del Custode (D3 e D4): una cassa sigillata non è mai un mimic né un baule maledetto")]
        public IEnumerator SealedChests_NeverHaveASecret()
        {
            yield return LoadGeneratedCore();
            ChestSecret.Enabled = true;
            for (int seed = FirstSeed; seed < FirstSeed + 8; seed++)
            {
                UseSeed((ulong)seed);
                yield return Travel("Level_Crypt", "Start", 4);
                foreach (var chest in Manager.CurrentLevel.Chests)
                {
                    if (chest.OpensWithGuardian || chest.OpensWithBoss)
                    {
                        Assert.AreEqual(ChestSecretKind.None, chest.Secret, $"seme {seed}: una cassa sigillata resta com'è");
                    }
                }
            }
        }
    }
}
