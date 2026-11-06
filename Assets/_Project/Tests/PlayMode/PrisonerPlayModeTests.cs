using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Il prigioniero nella cripta vera (25.5 della M25, D6): una scorta che già c'era, il click che libera solo a scorta morta, il premio e il consiglio, la partenza, il ricordo.</summary>
    public class PrisonerPlayModeTests : SandboxFixture
    {
        private const int SeedsToTry = 60;

        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentPrisoner_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            Prisoner.Enabled = false;
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static LorePanel Panel => UnityEngine.Object.FindFirstObjectByType<LorePanel>(FindObjectsInactive.Include);

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

        private IEnumerator LevelWithAPrisoner(Action<int, ulong> found)
        {
            yield return LoadGeneratedCore();
            Prisoner.Enabled = true;
            for (int seed = 1; seed <= SeedsToTry; seed++)
            {
                for (int depth = PrisonerRules.FirstDepth; depth <= PrisonerRules.LastDepth; depth++)
                {
                    if (!PrisonerRules.LevelWantsOne((ulong)seed, depth))
                    {
                        continue;
                    }

                    UseSeed((ulong)seed);
                    yield return Travel("Level_Crypt", "Start", depth);
                    if (Manager.CurrentLevel.Secrets.OfType<Prisoner>().Any())
                    {
                        var health = Player.GetComponent<Health>();
                        health.MultiplyMax(500f);
                        health.Heal(float.MaxValue);
                        found(depth, (ulong)seed);
                        yield break;
                    }
                }
            }

            Assert.Fail($"nessun livello di {SeedsToTry} semi ha un prigioniero");
        }

        private static Prisoner Captive => Manager.CurrentLevel.Secrets.OfType<Prisoner>().First();

        private static void KillTheEscort(Prisoner prisoner, GameObject killer)
        {
            foreach (var enemy in prisoner.Escort.Where(e => e != null))
            {
                var health = enemy.GetComponent<Health>();
                health.TakeDamage(new DamageInfo(health.Max * 10f, DamageType.Physical, killer));
            }
        }

        [UnityTest, Timeout(300000), Description("Il prigioniero (D6): sta seduto contro un muro con la scorta dei nemici che già c'erano, e non cambia i nemici del livello")]
        public IEnumerator ThePrisoner_HasAnEscortThatWasAlreadyThere()
        {
            yield return LevelWithAPrisoner((d, s) => { });
            var level = Manager.CurrentLevel;
            var prisoner = Captive;
            Assert.AreEqual(Prisoner.Phase.Captive, prisoner.State);
            Assert.IsFalse(prisoner.IsDone);
            Assert.Greater(prisoner.Escort.Count, 0, "una scorta");
            Assert.Greater(prisoner.EscortAlive, 0, "viva");
            foreach (var enemy in prisoner.Escort)
            {
                Assert.IsTrue(level.Enemies.Contains(enemy), "la scorta è fatta di nemici del livello, non di nemici in più");
            }

            Assert.AreEqual(Localizer.Get("prisoner.label"), prisoner.GetComponent<Interactable>().GetLabel(Localizer));
        }

        [UnityTest, Timeout(300000), Description("Con la scorta viva un click non lo libera: una riga dice di sistemare prima la scorta")]
        public IEnumerator WhileTheEscortLives_TheClickDoesNothing()
        {
            yield return LevelWithAPrisoner((d, s) => { });
            var prisoner = Captive;
            int refused = 0;
            prisoner.Refused += () => refused++;
            prisoner.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;

            Assert.AreEqual(1, refused);
            Assert.AreEqual(Prisoner.Phase.Captive, prisoner.State);
            Assert.AreEqual(Localizer.Get(DarkDescent.Localization.TextKeys.PrisonerGuarded), UnityEngine.Object.FindFirstObjectByType<HudMessage>().Text);
            Assert.IsFalse(Panel.IsOpen);
        }

        [UnityTest, Timeout(300000), Description("A scorta morta il click lo libera: pozione e oro a terra, il consiglio nel pannello, poi sale verso la scala in su e sparisce, e le catene restano")]
        public IEnumerator WhenTheEscortIsDead_ItIsFreedRewardsAndLeaves()
        {
            yield return LevelWithAPrisoner((d, s) => { });
            var level = Manager.CurrentLevel;
            var prisoner = Captive;
            int itemsBefore = UnityEngine.Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None).Length;
            int enemies = level.Enemies.Count;
            bool left = false;
            prisoner.Left += () => left = true;

            KillTheEscort(prisoner, Player.gameObject);
            yield return null;
            Assert.AreEqual(0, prisoner.EscortAlive);

            Time.timeScale = 1f;
            prisoner.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;
            Assert.AreEqual(Prisoner.Phase.Thanking, prisoner.State);
            Assert.IsTrue(prisoner.IsDone);

            // il premio: una pozione e dell'oro a terra, senza toccare i nemici del livello
            var items = UnityEngine.Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(items.Length - itemsBefore, 2, "pozione e oro");
            Assert.IsTrue(items.Any(i => i.Gold == PrisonerRules.Gold(level.Depth)), "l'oro della profondità");

            // il consiglio nel pannello
            Assert.IsTrue(Panel.IsOpen);
            Assert.AreEqual(Localizer.Get(DarkDescent.Localization.TextKeys.PrisonerTitle), Panel.TitleText);
            Assert.AreEqual(Localizer.Get(PrisonerRules.AdviceKey(prisoner.Advice)), Panel.BodyText);
            Panel.Close();

            // se ne va a piedi verso la scala in su (il tempo corre più in fretta per non aspettare tutto il cammino)
            Time.timeScale = 8f;
            for (float t = 0f; !left && t < 25f; t += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            Time.timeScale = 1f;
            Assert.IsTrue(left, "è salito");
            Assert.AreEqual(Prisoner.Phase.Gone, prisoner.State);
            Assert.IsFalse(prisoner.GetComponentInChildren<Animator>(true).gameObject.activeSelf, "non si vede più");
            Assert.IsNull(prisoner.GetComponentInChildren<Transform>(true).Find("Chains"), "le catene non sono più sue...");
            Assert.IsTrue(level.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Chains" && t.gameObject.activeInHierarchy), "...restano nel livello");
            Assert.AreEqual(enemies, level.Enemies.Count);
        }

        [UnityTest, Timeout(300000), Description("Il ricordo del livello (D13): un prigioniero liberato si ritrova andato, senza click, con le catene vuote; nemici e casse sono gli stessi")]
        public IEnumerator AFreedPrisoner_IsRememberedWhenYouComeBack()
        {
            int depth = 0;
            ulong seed = 0;
            yield return LevelWithAPrisoner((d, s) => { depth = d; seed = s; });
            var level = Manager.CurrentLevel;
            int secrets = level.Secrets.Count;
            int chests = level.Chests.Count;
            int enemies = level.Enemies.Count;
            var prisoner = Captive;
            KillTheEscort(prisoner, Player.gameObject);
            yield return null;
            prisoner.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;
            Panel.Close();

            yield return Travel("Level_Crypt", "Start", depth == 2 ? 3 : 2);
            UseSeed(seed);
            yield return Travel("Level_Crypt", "Start", depth);

            var back = Manager.CurrentLevel;
            Assert.AreEqual(secrets, back.Secrets.Count, "gli stessi segreti, nello stesso ordine");
            Assert.AreEqual(chests, back.Chests.Count);
            Assert.AreEqual(enemies, back.Enemies.Count);
            var again = Captive;
            Assert.IsTrue(again.IsDone, "ricordato libero");
            Assert.AreEqual(Prisoner.Phase.Gone, again.State);
            Assert.IsFalse(again.GetComponentInChildren<Animator>(true).gameObject.activeSelf);
        }

        [UnityTest, Timeout(300000), Description("Il prigioniero non tocca il livello (D14): con l'interruttore acceso e spento le casse e i nemici sono gli stessi, nello stesso posto")]
        public IEnumerator ThePrisoner_DoesNotChangeTheLevel()
        {
            int depth = 0;
            ulong seed = 0;
            yield return LevelWithAPrisoner((d, s) => { depth = d; seed = s; });
            var on = Manager.CurrentLevel;
            var chestsOn = on.Chests.Select(c => c.transform.position).ToList();
            var enemiesOn = on.Enemies.Select(e => e.HomePosition).ToList();

            Prisoner.Enabled = false;
            UseSeed(seed);
            yield return Travel("Level_Crypt", "Start", depth);
            var off = Manager.CurrentLevel;
            Assert.IsFalse(off.Secrets.OfType<Prisoner>().Any(), "spento: niente prigioniero");
            CollectionAssert.AreEqual(chestsOn, off.Chests.Select(c => c.transform.position).ToList());
            CollectionAssert.AreEqual(enemiesOn, off.Enemies.Select(e => e.HomePosition).ToList());
        }
    }
}
