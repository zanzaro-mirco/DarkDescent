using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class LootSeedTests : SandboxFixture
    {
        private static string Describe(ItemInstance item)
        {
            return item == null
                ? "niente"
                : $"{item.Definition.name} {item.Rarity} L{item.ItemLevel} [{string.Join(", ", item.Affixes.Select(a => $"{a.AffixId} {a.Value}"))}]";
        }

        private static CompositionRoot Root => Object.FindFirstObjectByType<CompositionRoot>();

        private static LootDrop[] Skeletons()
        {
            return Object.FindObjectsByType<LootDrop>(FindObjectsSortMode.None).OrderBy(d => d.name).ToArray();
        }

        private static Dictionary<string, string> Previews()
        {
            return Skeletons().ToDictionary(d => d.name, d => Describe(d.Preview()));
        }

        [UnityTest, Description("Con il seme 4711 gli scheletri del livello 1 lasciano gli oggetti previsti anche uccisi in ordine inverso, e un nuovo avvio con lo stesso seme li rifà uguali")]
        public IEnumerator Seed4711_SameDropsInReverseOrder_AndAfterRestart()
        {
            yield return LoadCore();
            Root.UseLootSeed(4711UL);
            var expected = Previews();
            Assert.Greater(expected.Count, 1);
            Assert.IsTrue(expected.Values.Any(v => v != "niente"), "con il seme 4711 qualcuno deve lasciare qualcosa");

            foreach (var drop in Skeletons().Reverse())
            {
                var before = new HashSet<GroundItem>(Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None));
                string name = drop.name;
                drop.GetComponent<Health>().TakeDamage(new DamageInfo(1000f, DamageType.Physical, null));
                yield return null;

                var fresh = Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None).Where(g => !before.Contains(g)).ToArray();
                Assert.AreEqual(expected[name], fresh.Length == 0 ? "niente" : Describe(fresh.Single().Item), name);
            }

            SceneManager.LoadScene("Core");
            yield return WaitForLevel();
            Root.UseLootSeed(4711UL);
            CollectionAssert.AreEquivalent(expected, Previews(), "stesso seme, stessa partita");
        }

        [UnityTest, Description("Un oggetto raro cade con il nome giallo e la luce gialla")]
        public IEnumerator RareDrop_GlowsYellow()
        {
            yield return LoadSandbox(allSkeletons: true);
            var skeleton = GameObject.Find("Skeleton_B");
            var expected = UseLootSeedWithDrop(skeleton.GetComponent<LootDrop>(), item => item.Rarity == Rarity.Rare);

            skeleton.GetComponent<Health>().TakeDamage(new DamageInfo(1000f, DamageType.Physical, null));
            yield return null;

            var dropped = Object.FindFirstObjectByType<GroundItem>();
            Assert.AreEqual(Rarity.Rare, dropped.Item.Rarity);
            Assert.AreEqual(expected.Affixes.Count, dropped.Item.Affixes.Count);
            Assert.AreEqual(RarityColors.Light(Rarity.Rare), dropped.GlowColor);
            StringAssert.Contains(RarityColors.TextHex(Rarity.Rare), dropped.GetComponent<Interactable>().GetLabel(Localizer));
        }

        [UnityTest, Description("Nel livello 2 la profondità è 2 e gli oggetti che cadono sono di livello 2")]
        public IEnumerator Level2_DropsLevel2Items()
        {
            yield return LoadCore();
            var manager = Object.FindFirstObjectByType<LevelManager>();
            Assert.AreEqual(1, manager.CurrentLevel.Depth);

            PlayerAgent.Warp(manager.CurrentLevel.Exits[0].GetComponent<Interactable>().ApproachPoint);
            float elapsed = 0f;
            while (elapsed < 15f && (manager.IsTransitioning || manager.CurrentLevel.gameObject.scene.name != "Level_02"))
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(2, manager.CurrentLevel.Depth);
            var item = UseLootSeedWithDrop(Skeletons()[0]);
            Assert.AreEqual(2, item.ItemLevel);
        }
    }
}
