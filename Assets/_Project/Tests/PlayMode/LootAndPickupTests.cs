using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class LootAndPickupTests : SandboxFixture
    {
        private PlayerInventory _inventory;

        private static GroundItem[] GroundItems() => Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None);

        // Skeleton_B e non Skeleton: dalla camera, il cubo della sandbox copre lo scheletro principale
        // e quello che lascia cadere
        private const string SkeletonName = "Skeleton_B";

        private IEnumerator KillSkeleton()
        {
            yield return LoadSandbox(allSkeletons: true);
            _inventory = Player.GetComponent<PlayerInventory>();
            GameObject.Find(SkeletonName).GetComponent<Health>().TakeDamage(new DamageInfo(1000f, DamageType.Physical, null));
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

        [UnityTest, Description("Lo scheletro morto lascia la sua lama davanti al corpo, sul NavMesh")]
        public IEnumerator SkeletonDeath_DropsBlade()
        {
            yield return KillSkeleton();

            var drops = GroundItems();
            Assert.AreEqual(1, drops.Length);
            Assert.AreEqual("SkeletonBlade", drops[0].Item.Definition.name);
            Assert.AreEqual("Lama dello scheletro", drops[0].GetComponent<Interactable>().Label);
            Assert.Less(FlatDistance(drops[0].transform.position, GameObject.Find(SkeletonName).transform.position), 2f);
            Assert.IsTrue(NavMesh.SamplePosition(drops[0].transform.position, out _, 0.2f, NavMesh.AllAreas), "deve stare dove il cavaliere arriva");
        }

        [UnityTest, Description("Un click sulla lama: il cavaliere la raggiunge e la mette nell'inventario")]
        public IEnumerator ClickOnGroundItem_PicksItUp()
        {
            yield return KillSkeleton();
            var drop = GroundItems()[0];
            var item = drop.Item;

            ClickAt(drop.transform.position + Vector3.up * 0.1f);
            yield return WaitUntil(() => drop == null, 8f);

            Assert.IsTrue(drop == null, "raccolta, la lama sparisce da terra");
            Assert.IsTrue(_inventory.Inventory.Grid.TryGetPlacement(item, out var area), "ed entra nella griglia");
            Assert.AreEqual(new Vector2Int(0, 0), area.position);
        }

        [UnityTest, Description("Con l'inventario pieno la lama resta a terra e l'etichetta lo dice")]
        public IEnumerator FullInventory_ItemStaysOnGround()
        {
            yield return KillSkeleton();
#if UNITY_EDITOR
            var sword = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/ShortSword.asset");
#else
            ItemDefinition sword = null;
#endif
            // dieci spade riempiono le prime tre righe: una lama da tre celle non entra più
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(_inventory.TryPickUp(new ItemInstance(sword)));
            }

            var drop = GroundItems()[0];
            ClickAt(drop.transform.position + Vector3.up * 0.1f);
            yield return WaitUntil(() => FlatDistance(Player.position, drop.transform.position) < 1f, 8f);
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(drop != null, "deve restare a terra");
            StringAssert.Contains("inventario pieno", drop.GetComponent<Interactable>().Label);
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
