using System;
using System.Collections;
using System.IO;
using System.Linq;
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
    /// <summary>La lore nel gioco vero (25.5 della M25, D9 e D16): il leggio contro un muro, il pannello con il testo, il ricordo, e le caverne; e la riga della leva (D12).</summary>
    public class LorePlayModeTests : SandboxFixture
    {
        private const int SeedsToTry = 40;

        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentLore_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            LoreSpot.Enabled = false;
            SealedRoom.Enabled = false;
            ChestSecret.Enabled = false;
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

        private static string SceneOf(int depth) => depth <= LoreRules.LastCryptDepth ? "Level_Crypt" : "Level_Caves";

        private static string EntranceOf(int depth) => depth == 1 ? "Start" : depth <= LoreRules.LastCryptDepth ? "Start" : "FromAbove";

        // il primo livello a quella profondità con un testo, dal seme 1 in su
        private IEnumerator LevelWithALore(int depth, Action<ulong> found)
        {
            yield return LoadGeneratedCore();
            LoreSpot.Enabled = true;
            for (int seed = 1; seed <= SeedsToTry; seed++)
            {
                if (!LoreRules.Has((ulong)seed, depth))
                {
                    continue;
                }

                UseSeed((ulong)seed);
                yield return Travel(SceneOf(depth), EntranceOf(depth), depth);
                if (Manager.CurrentLevel.Secrets.OfType<LoreSpot>().Any())
                {
                    found((ulong)seed);
                    yield break;
                }
            }

            Assert.Fail($"nessun livello di profondità {depth} ha un testo in {SeedsToTry} semi");
        }

        private static LoreSpot Lore => Manager.CurrentLevel.Secrets.OfType<LoreSpot>().First();

        [UnityTest, Timeout(300000), Description("Il leggio (D9): contro un muro, con il suo testo e il bagliore; un click apre il pannello con titolo e testo, e si chiude")]
        public IEnumerator TheLore_OpensThePanelWithItsText()
        {
            ulong seed = 0;
            yield return LevelWithALore(1, s => seed = s);
            var lore = Lore;
            Assert.AreEqual(LoreRules.TextFor(seed, 1), lore.TextId);
            Assert.IsFalse(lore.IsDone, "non letto");
            Assert.IsTrue(lore.GetComponentInChildren<Light>().enabled, "il bagliore dei testi non letti");
            Assert.IsFalse(Panel.IsOpen);

            lore.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;

            Assert.IsTrue(Panel.IsOpen, "il pannello si apre");
            Assert.AreEqual(Localizer.Get(LoreRules.TitleKey(lore.TextId)), Panel.TitleText);
            Assert.AreEqual(Localizer.Get(LoreRules.BodyKey(lore.TextId)), Panel.BodyText);
            Assert.IsTrue(lore.IsDone);
            Assert.IsFalse(lore.GetComponentInChildren<Light>().enabled, "letto: il bagliore si spegne");

            Panel.CloseButton.onClick.Invoke();
            Assert.IsFalse(Panel.IsOpen);
        }

        [UnityTest, Timeout(300000), Description("Il ricordo (D13): un testo letto si ritrova letto tornando nel livello, stesso testo e stesso posto")]
        public IEnumerator AReadText_IsRememberedWhenYouComeBack()
        {
            ulong seed = 0;
            yield return LevelWithALore(2, s => seed = s);
            var first = Lore;
            int text = first.TextId;
            Vector3 place = first.transform.position;
            first.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;
            Panel.Close();

            yield return Travel("Level_Crypt", "Start", 3);
            UseSeed(seed);
            yield return Travel("Level_Crypt", "Start", 2);

            var back = Lore;
            Assert.AreEqual(text, back.TextId, "lo stesso testo");
            Assert.AreEqual(place.x, back.transform.position.x, 0.01f, "nello stesso posto");
            Assert.AreEqual(place.z, back.transform.position.z, 0.01f);
            Assert.IsTrue(back.IsDone, "letto");
            Assert.IsFalse(back.GetComponentInChildren<Light>().enabled);
        }

        [UnityTest, Timeout(300000), Description("Le caverne (D16): hanno i loro testi, e il pannello li legge come nella cripta")]
        public IEnumerator TheCaves_HaveTheirOwnTexts()
        {
            ulong seed = 0;
            yield return LevelWithALore(5, s => seed = s);
            var lore = Lore;
            Assert.AreEqual(LoreRules.TextFor(seed, 5), lore.TextId);
            Assert.GreaterOrEqual(lore.TextId, 7, "un testo delle caverne o di ovunque");

            lore.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;
            Assert.IsTrue(Panel.IsOpen);
            Assert.AreEqual(Localizer.Get(LoreRules.TitleKey(lore.TextId)), Panel.TitleText);
        }

        [UnityTest, Timeout(300000), Description("La lore non tocca il livello (D14): con l'interruttore acceso e spento le casse e i nemici sono gli stessi, nello stesso posto")]
        public IEnumerator TheLore_DoesNotChangeTheLevel()
        {
            ulong seed = 0;
            yield return LevelWithALore(1, s => seed = s);
            var on = Manager.CurrentLevel;
            var chestsOn = on.Chests.Select(c => c.transform.position).ToList();
            var enemiesOn = on.Enemies.Select(e => e.transform.position).ToList();

            LoreSpot.Enabled = false;
            UseSeed(seed);
            yield return Travel("Level_Crypt", "Start", 1);
            var off = Manager.CurrentLevel;
            Assert.IsFalse(off.Secrets.OfType<LoreSpot>().Any(), "spento: niente leggio");
            CollectionAssert.AreEqual(chestsOn, off.Chests.Select(c => c.transform.position).ToList());
            CollectionAssert.AreEqual(enemiesOn, off.Enemies.Select(e => e.transform.position).ToList());
        }

        [UnityTest, Timeout(300000), Description("La leva (D12): tirata, il gioco dice «hai sentito scattare qualcosa»; e tintinna quando ci si avvicina a 6 m, una volta per avvicinamento")]
        public IEnumerator TheLever_SaysItAndTinkles()
        {
            yield return LoadGeneratedCore();
            SealedRoom.Enabled = true;
            SealedRoom room = null;
            for (int seed = 1; seed <= 60 && room == null; seed++)
            {
                for (int depth = SealedRoomPlanner.FirstDepth; depth <= SealedRoomPlanner.LastDepth && room == null; depth++)
                {
                    UseSeed((ulong)seed);
                    yield return Travel("Level_Crypt", "Start", depth);
                    room = Manager.CurrentLevel.Secrets.OfType<SealedRoom>().FirstOrDefault();
                }
            }

            Assert.IsNotNull(room, "nessuna stanza sigillata in 60 semi");

            // il tintinnio: un ascoltatore finto, lontano e poi vicino e poi lontano e poi di nuovo vicino
            int tinkles = 0;
            room.Lever.Tinkled += () => tinkles++;
            var listener = new GameObject("Listener").transform;
            listener.position = room.Lever.transform.position + Vector3.right * 40f;
            room.Lever.BindListener(listener);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(0, tinkles, "lontano: niente");
            listener.position = room.Lever.transform.position + Vector3.right * 3f;
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(1, tinkles, "a 6 m: una volta");
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(1, tinkles, "restando vicino non ripete");
            listener.position = room.Lever.transform.position + Vector3.right * 40f;
            yield return new WaitForSeconds(0.6f);
            listener.position = room.Lever.transform.position + Vector3.right * 3f;
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(2, tinkles, "tornando: di nuovo");
            UnityEngine.Object.Destroy(listener.gameObject);

            // la riga
            var message = UnityEngine.Object.FindFirstObjectByType<HudMessage>();
            room.Lever.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;
            Assert.AreEqual(Localizer.Get(DarkDescent.Localization.TextKeys.SecretHeard), message.Text);
            Assert.Greater(message.Alpha, 0.5f);
        }
    }
}
