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

        [UnityTest, Description("Continua riporta in vita all'ingresso del livello in cui si è morti, senza ricaricarlo: inventario, pozione bevuta, cassa aperta, nemici uccisi e mappa scoperta restano; i nemici vivi tornano fermi dove li ha messi il livello, con la vita che avevano; timeScale a 1 (D15 della M7)")]
        public IEnumerator Continue_KeepsTheLevelAsItWas()
        {
            yield return LoadGeneratedCore();
            var manager = Object.FindFirstObjectByType<LevelManager>();
            manager.LoadLevel("Level_Crypt", "FromAbove", 2);
            yield return WaitForDepth(manager, 2);
            yield return null;

            var level = manager.CurrentLevel;
            Vector3 entrance = Player.position;
            var inventory = Player.GetComponent<PlayerInventory>();
            var health = Player.GetComponent<Health>();
            var sword = inventory.Equipment.Get(EquipSlot.Weapon).Definition;
            int potions = inventory.Belt.Count;

            // prima di morire: una pozione bevuta, un pugnale raccolto, la cassa aperta
            health.TakeDamage(Damage(Mathf.Round(health.Max * 0.6f)));
            Assert.IsTrue(inventory.DrinkFromBelt(0));
#if UNITY_EDITOR
            inventory.TryPickUp(new ItemInstance(UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/Dagger.asset")));
#endif
            Assert.AreEqual(1, inventory.Inventory.Grid.Placements.Count);
            var chest = level.Chests[0];
            chest.GetComponent<Interactable>().Use(Player.gameObject);
            Assert.IsTrue(chest.IsOpen);

            // un nemico ucciso, uno ferito e portato lontano da dove l'ha messo il livello
            Assert.GreaterOrEqual(level.Enemies.Count, 2);
            var killed = level.Enemies[0];
            killed.GetComponent<Health>().TakeDamage(Damage(1000f));
            var wounded = level.Enemies[1];
            var woundedHealth = wounded.GetComponent<Health>();
            Vector3 home = wounded.transform.position;
            woundedHealth.TakeDamage(Damage(Mathf.Round(woundedHealth.Max * 0.5f)));
            float woundedLife = woundedHealth.Current;
            wounded.GetComponent<NavMeshAgent>().Warp(entrance + Vector3.right * 2f);
            wounded.Alert();

            // e un giro lontano dall'ingresso: la mappa scoperta resta
            var exploration = Object.FindFirstObjectByType<ExplorationTracker>();
            var far = FarUnexploredCell(exploration.Exploration);
            PlayerAgent.Warp(LevelMap.CellCenter(far.x, far.y));
            yield return null;
            yield return null;
            Assert.IsTrue(exploration.Exploration.IsExplored(far.x, far.y));
            int explored = exploration.Exploration.ExploredCount;

            yield return KillPlayerAndWaitForScreen();

            // un hit stop rimasto a metà non deve sopravvivere al ritorno (trappola 6 della M6)
            Time.timeScale = 0.3f;
            var button = GameObject.Find("RestartButton").GetComponent<RectTransform>();
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center));
            Move(Mouse.position, screen);
            yield return null;
            yield return null;
            Press(Mouse.leftButton);
            Release(Mouse.leftButton);
            yield return null;

            float elapsed = 0f;
            while ((manager.IsTransitioning || health.IsDead) && elapsed < 10f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreSame(level, manager.CurrentLevel, "il livello non si è ricaricato");
            Assert.AreEqual(1f, Time.timeScale, "timeScale va rimesso a 1");
            Assert.Less(FlatDistance(entrance, Player.position), 0.1f, "all'ingresso da cui si è entrati");

            Assert.IsFalse(health.IsDead);
            Assert.AreEqual(health.Max, health.Current);
            Assert.IsTrue(Player.GetComponent<PlayerController>().enabled);
            Assert.IsTrue(Player.GetComponent<PlayerInputReader>().enabled);
            Assert.IsTrue(Player.GetComponent<MeleeAttack>().enabled);
            Assert.IsTrue(PlayerAgent.isOnNavMesh);
            Assert.IsFalse(Player.GetComponentInChildren<CharacterAnimatorDriver>().IsDead, "di nuovo in piedi");
            Assert.IsFalse(FindDeathScreen().IsShown, "la schermata sparisce");
            Assert.AreEqual(1f, Object.FindFirstObjectByType<HealthOrb>().FillAmount, 0.001f);

            Assert.AreEqual(potions - 1, inventory.Belt.Count, "la pozione bevuta resta bevuta");
            Assert.AreEqual(1, inventory.Inventory.Grid.Placements.Count, "il pugnale raccolto resta");
            Assert.AreEqual(sword, inventory.Equipment.Get(EquipSlot.Weapon).Definition);
            Assert.IsTrue(chest.IsOpen, "la cassa resta aperta");
            Assert.IsTrue(killed == null || killed.State == EnemyState.Dead, "il nemico ucciso non rinasce");
            Assert.AreEqual(woundedLife, woundedHealth.Current, "il ferito resta ferito");
            Assert.Less(FlatDistance(home, wounded.transform.position), 0.1f, "ed è tornato dove l'ha messo il livello");
            Assert.AreEqual(EnemyState.Idle, wounded.State, "fermo");
            Assert.IsTrue(exploration.Exploration.IsExplored(far.x, far.y), "la mappa scoperta resta");
            Assert.AreEqual(explored, exploration.Exploration.ExploredCount);

            // si può morire di nuovo
            health.TakeDamage(Damage(1000f));
            Assert.IsTrue(Player.GetComponentInChildren<CharacterAnimatorDriver>().IsDead);
            Assert.IsFalse(Player.GetComponent<PlayerController>().enabled);
        }

        // una cella di pavimento libero ancora da scoprire, lontana dalla scala e dal suo trigger
        private static Vector2Int FarUnexploredCell(Exploration exploration)
        {
            var map = exploration.Map;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    bool nearStairs = false;
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            nearStairs |= map.GetSymbol(x + dx, y + dy) == DungeonGenerator.StairsSymbol;
                        }
                    }

                    if (map.GetSymbol(x, y) == LevelMap.Floor && !exploration.IsExplored(x, y) && !nearStairs)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            Assert.Fail("nessuna cella da scoprire");
            return default;
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
