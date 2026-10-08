using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class LootAndPickupTests : SandboxFixture
    {
        private PlayerInventory _inventory;
        private ItemInstance _expected;

        private static GroundItem[] GroundItems() => Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None);

        // Skeleton_B e non Skeleton: dalla camera, il cubo della sandbox copre lo scheletro principale
        // e quello che lascia cadere
        private const string SkeletonName = "Skeleton_B";

        private IEnumerator KillSkeleton()
        {
            yield return LoadSandbox(allSkeletons: true);
            _inventory = Player.GetComponent<PlayerInventory>();
            var skeleton = GameObject.Find(SkeletonName);
            // un seme con cui lo scheletro lascia qualcosa: con il 30% non lascerebbe niente
            _expected = UseLootSeedWithDrop(skeleton.GetComponent<LootDrop>());
            skeleton.GetComponent<Health>().TakeDamage(new DamageInfo(1000f, DamageType.Physical, null));
            yield return null;
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            float elapsed = 0f;
            while (elapsed < timeout && !condition())
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private static Bounds ModelBounds(GroundItem drop)
        {
            var renderers = drop.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        [UnityTest, Description("Quello che cade vola dal corpo con una capriola e si posa dov'è il suo click: in volo la luce è spenta, a terra si accende e il modello è disteso sul pavimento")]
        public IEnumerator Drop_FliesFromTheBodyAndLands()
        {
            yield return KillSkeleton();
            var drop = GroundItems().Single();
            var glow = drop.GetComponentInChildren<Light>(true);
            bool landed = false;
            drop.Landed += () => landed = true;

            Assert.IsTrue(drop.IsFlying);
            Assert.IsFalse(glow.enabled, "in volo la luce è spenta");
            Assert.Greater(ModelBounds(drop).center.y, 0.5f, "parte dal petto dello scheletro, non da terra");

            yield return WaitUntil(() => landed, 2f);
            Assert.IsTrue(landed, "deve posarsi in meno di due secondi");
            Assert.IsFalse(drop.IsFlying);
            Assert.IsTrue(glow.enabled, "a terra la luce si accende");
            Bounds bounds = ModelBounds(drop);
            Assert.AreEqual(drop.transform.position.y + 0.02f, bounds.min.y, 0.02f, "appoggiato al pavimento");
            Assert.AreEqual(drop.transform.position.x, bounds.center.x, 0.02f, "centrato sul suo click");
            Assert.AreEqual(drop.transform.position.z, bounds.center.z, 0.02f, "centrato sul suo click");
        }

        [UnityTest, Description("Lo scheletro morto lascia l'oggetto della sua loot table davanti al corpo, sul NavMesh, con il nome nel colore della rarità")]
        public IEnumerator SkeletonDeath_DropsItsLoot()
        {
            yield return KillSkeleton();

            var drops = GroundItems();
            Assert.AreEqual(1, drops.Length);
            Assert.AreSame(_expected.Definition, drops[0].Item.Definition);
            Assert.AreEqual(_expected.Rarity, drops[0].Item.Rarity);
            Assert.AreEqual(_expected.Affixes.Count, drops[0].Item.Affixes.Count);
            string label = drops[0].GetComponent<Interactable>().GetLabel(Localizer);
            StringAssert.Contains(Localizer.Get(_expected.Definition.NameKey), label);
            StringAssert.Contains(RarityColors.TextHex(_expected.Rarity), label);
            Assert.Less(FlatDistance(drops[0].transform.position, GameObject.Find(SkeletonName).transform.position), 2f);
            Assert.IsTrue(NavMesh.SamplePosition(drops[0].transform.position, out _, 0.2f, NavMesh.AllAreas), "deve stare dove il cavaliere arriva");
        }

        [UnityTest, Description("Un click sull'oggetto caduto: il cavaliere lo raggiunge e lo mette nell'inventario")]
        public IEnumerator ClickOnGroundItem_PicksItUp()
        {
            yield return KillSkeleton();
            var drop = GroundItems()[0];
            var item = drop.Item;

            ClickAt(drop.transform.position + Vector3.up * 0.1f);
            yield return WaitUntil(() => drop == null, 8f);

            Assert.IsTrue(drop == null, "raccolto, sparisce da terra");
            Assert.IsTrue(_inventory.Inventory.Grid.TryGetPlacement(item, out var area), "ed entra nella griglia");
            Assert.AreEqual(new Vector2Int(0, 0), area.position);
        }

        [UnityTest, Description("Con l'inventario pieno l'oggetto resta a terra e l'HUD lo dice per un momento al centro; l'etichetta resta il solo nome")]
        public IEnumerator FullInventory_ItemStaysOnGround()
        {
            yield return KillSkeleton();
#if UNITY_EDITOR
            var sword = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/ShortSword.asset");
#else
            ItemDefinition sword = null;
#endif
            // dieci spade riempiono le prime tre righe: nella quarta non entra nessuna base, alte almeno due celle
            var swords = new List<ItemInstance>();
            for (int i = 0; i < 10; i++)
            {
                swords.Add(new ItemInstance(sword));
                Assert.IsTrue(_inventory.TryPickUp(swords[i]));
            }

            var drop = GroundItems()[0];
            ClickAt(drop.transform.position + Vector3.up * 0.1f);
            yield return WaitUntil(() => FlatDistance(Player.position, drop.transform.position) < 1f, 8f);
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(drop != null, "deve restare a terra");
            var message = Object.FindFirstObjectByType<InventoryFullMessage>();
            Assert.AreEqual("No room in the inventory", message.Text);
            Assert.Greater(message.Alpha, 0.9f, "il messaggio si vede");

            // la seconda prova di Mirco: niente scritta che resta sull'oggetto
            StringAssert.DoesNotContain("inventory", drop.GetComponent<Interactable>().GetLabel(Localizer));

            yield return new WaitForSecondsRealtime(2.2f);
            Assert.AreEqual(0f, message.Alpha, 0.01f, "dopo un momento sparisce");
        }

        [UnityTest, Description("Nel livello 1 lo scudo è a terra; scendendo, gli oggetti a terra se ne vanno con il livello")]
        public IEnumerator GroundItems_UnloadWithTheirLevel()
        {
            yield return LoadCore();
            var manager = Object.FindFirstObjectByType<LevelManager>();

            var shield = GroundItems().Single();
            Assert.AreEqual("BadgeShield", shield.Item.Definition.name);
            Assert.AreEqual("Level_01", shield.gameObject.scene.name);

            var exit = manager.CurrentLevel.Exits[0];
            PlayerAgent.Warp(exit.GetComponent<Interactable>().ApproachPoint);
            float elapsed = 0f;
            while (elapsed < 15f && (manager.IsTransitioning || manager.CurrentLevel.gameObject.scene.name != "Level_02"))
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual("Level_02", manager.CurrentLevel.gameObject.scene.name);
            Assert.IsEmpty(GroundItems(), "lo scudo del livello 1 non deve seguire il cavaliere");
        }
    }
}
