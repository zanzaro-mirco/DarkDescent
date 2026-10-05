using System.Collections;
using DarkDescent.Characters;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class DeathAndRestartTests : SandboxFixture
    {
        private const float ShowDelay = 2f;

        private static DamageInfo Damage(float amount)
        {
            return new DamageInfo(amount, DamageType.Physical, null);
        }

        private static DeathScreen FindDeathScreen()
        {
            return Object.FindFirstObjectByType<DeathScreen>();
        }

        private IEnumerator KillPlayerAndWaitForScreen()
        {
            Player.GetComponent<Health>().TakeDamage(Damage(1000f));
            float elapsed = 0f;
            while (elapsed < ShowDelay + 1f && !FindDeathScreen().IsShown)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(FindDeathScreen().IsShown, "la schermata di morte deve comparire");
        }

        [UnityTest, Description("Morto il player: input e agent spenti, animazione di morte, schermata solo dopo la caduta")]
        public IEnumerator PlayerDies_StopsEverythingThenShowsScreen()
        {
            yield return LoadSandbox();
            var start = Player.position;

            Player.GetComponent<Health>().TakeDamage(Damage(1000f));

            Assert.IsFalse(Player.GetComponent<PlayerController>().enabled, "il controller va spento");
            Assert.IsFalse(Player.GetComponent<PlayerInputReader>().enabled, "l'input va spento");
            Assert.IsFalse(PlayerAgent.enabled, "l'agent va spento");
            Assert.IsTrue(Player.GetComponentInChildren<CharacterAnimatorDriver>().IsDead);
            Assert.IsFalse(FindDeathScreen().IsShown, "la schermata aspetta la fine della caduta");

            ClickAt(new Vector3(-4f, 0f, 0f));
            yield return new WaitForSeconds(0.5f);
            Assert.Less(Vector3.Distance(start, Player.position), 0.01f, "un morto non cammina");

            yield return new WaitForSeconds(ShowDelay);
            Assert.IsTrue(FindDeathScreen().IsShown);
        }

        [UnityTest, Description("Ricomincia riporta all'ingresso del livello in cui si è morti: stessa cripta e profondità, nemici e casse rimessi, vita piena, inventario com'era entrando, timeScale a 1 (D13 della M6)")]
        public IEnumerator Restart_ReloadsTheSameLevelWithTheEntryInventory()
        {
            yield return LoadGeneratedCore();
            var manager = Object.FindFirstObjectByType<LevelManager>();
            manager.LoadLevel("Level_Crypt", "FromAbove", 2);
            yield return WaitForDepth(manager, 2);

            // l'istantanea si prende un frame dopo l'ingresso
            yield return null;
            yield return null;
            string map = manager.CurrentLevel.Map.ToText();
            Vector3 entrance = Player.position;
            var inventory = Player.GetComponent<PlayerInventory>();
            var health = Player.GetComponent<Health>();
            var sword = inventory.Equipment.Get(EquipSlot.Weapon).Definition;
            int potions = inventory.Belt.Count;

            // dopo l'ingresso: una pozione bevuta, un pugnale raccolto, la cassa aperta
            health.TakeDamage(Damage(Mathf.Round(health.Max * 0.6f)));
            Assert.IsTrue(inventory.DrinkFromBelt(0));
#if UNITY_EDITOR
            inventory.TryPickUp(new ItemInstance(UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/Dagger.asset")));
#endif
            Assert.AreEqual(1, inventory.Inventory.Grid.Placements.Count);
            var chest = manager.CurrentLevel.Chests[0];
            chest.GetComponent<Interactable>().Use(Player.gameObject);
            Assert.IsTrue(chest.IsOpen);
            var oldLevel = manager.CurrentLevel;

            yield return KillPlayerAndWaitForScreen();

            // un hit stop rimasto a metà non deve sopravvivere al riavvio (trappola 6)
            Time.timeScale = 0.3f;
            var button = GameObject.Find("RestartButton").GetComponent<RectTransform>();
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center));
            Move(Mouse.position, screen);
            yield return null;
            yield return null;
            Press(Mouse.leftButton);
            Release(Mouse.leftButton);

            float elapsed = 0f;
            while ((manager.CurrentLevel == oldLevel || manager.CurrentLevel == null || manager.IsTransitioning) && elapsed < 10f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreNotSame(oldLevel, manager.CurrentLevel, "il livello si è ricaricato");
            Assert.AreEqual(1f, Time.timeScale, "timeScale va rimesso a 1");
            Assert.AreEqual("Level_Crypt", manager.CurrentLevel.gameObject.scene.name);
            Assert.AreEqual(2, manager.CurrentLevel.Depth, "la profondità in cui si è morti, non la prima");
            Assert.AreEqual(map, manager.CurrentLevel.Map.ToText(), "la stessa cripta: stesso seme");
            Assert.Less(FlatDistance(entrance, Player.position), 0.1f, "dallo stesso ingresso");

            Assert.IsFalse(health.IsDead);
            Assert.AreEqual(health.Max, health.Current);
            Assert.IsTrue(Player.GetComponent<PlayerController>().enabled);
            Assert.IsTrue(Player.GetComponent<PlayerInputReader>().enabled);
            Assert.IsTrue(Player.GetComponent<MeleeAttack>().enabled);
            Assert.IsTrue(PlayerAgent.isOnNavMesh);
            Assert.IsFalse(Player.GetComponentInChildren<CharacterAnimatorDriver>().IsDead, "di nuovo in piedi");
            Assert.IsFalse(FindDeathScreen().IsShown, "la schermata sparisce");
            Assert.AreEqual(1f, Object.FindFirstObjectByType<HealthOrb>().FillAmount, 0.001f);

            Assert.AreEqual(potions, inventory.Belt.Count, "la pozione bevuta è tornata");
            Assert.AreEqual(0, inventory.Inventory.Grid.Placements.Count, "il pugnale raccolto dopo l'ingresso no");
            Assert.AreEqual(sword, inventory.Equipment.Get(EquipSlot.Weapon).Definition);
            Assert.IsFalse(manager.CurrentLevel.Chests[0].IsOpen, "la cassa è di nuovo chiusa");
            foreach (var enemy in manager.CurrentLevel.Enemies)
            {
                var enemyHealth = enemy.GetComponent<Health>();
                Assert.AreEqual(enemyHealth.Max, enemyHealth.Current, $"{enemy.name} riparte con la vita piena");
                Assert.AreEqual(EnemyState.Idle, enemy.State);
            }

            // si può morire di nuovo
            health.TakeDamage(Damage(1000f));
            Assert.IsTrue(Player.GetComponentInChildren<CharacterAnimatorDriver>().IsDead);
            Assert.IsFalse(Player.GetComponent<PlayerController>().enabled);
        }

        private static IEnumerator WaitForDepth(LevelManager manager, int depth)
        {
            float elapsed = 0f;
            while (elapsed < 10f && (manager.IsTransitioning || manager.CurrentLevel == null || manager.CurrentLevel.Depth != depth))
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(depth, manager.CurrentLevel.Depth);
        }

        [UnityTest, Description("Il corpo dello scheletro resta qualche secondo, poi sparisce")]
        public IEnumerator SkeletonCorpse_DisappearsAfterDelay()
        {
            yield return LoadSandbox();
            var skeleton = GameObject.Find("Skeleton");
            skeleton.GetComponent<Health>().TakeDamage(Damage(1000f));

            yield return new WaitForSeconds(2f);
            Assert.IsTrue(skeleton != null, "il corpo deve restare per qualche secondo");

            yield return new WaitForSeconds(4f);
            Assert.IsTrue(skeleton == null, "il corpo deve sparire");
        }
    }
}
