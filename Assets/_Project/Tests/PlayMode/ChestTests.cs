using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class ChestTests : SandboxFixture
    {
        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static CompositionRoot Root => Object.FindFirstObjectByType<CompositionRoot>();

        private static string Describe(ItemInstance item)
        {
            return item == null ? "niente"
                : $"{item.Definition.name} {item.Rarity} L{item.ItemLevel} [{string.Join(",", item.Affixes.Select(a => a.AffixId + "=" + a.Value))}]";
        }

        // gli scheletri non disturbano: il test parla della cassa
        private static void ClearEnemies()
        {
            foreach (var enemy in Manager.CurrentLevel.Enemies)
            {
                enemy.GetComponent<Health>().TakeDamage(new DamageInfo(1000f, DamageType.Physical, null));
            }
        }

        [UnityTest, Description("Nella cripta generata le casse sono collegate al loot, guardano la stanza e si raggiungono: il punto d'arrivo sta sul NavMesh")]
        public IEnumerator GeneratedChests_AreBoundAndReachable()
        {
            yield return LoadGeneratedCore();
            var chests = Manager.CurrentLevel.Chests;

            Assert.AreEqual(1, chests.Count, "una cassa al livello 1 (D6)");
            foreach (var chest in chests)
            {
                Assert.IsNotNull(chest.LootTable, "la cassa ha il suo loot");
                Vector3 approach = chest.GetComponent<Interactable>().ApproachPoint;
                Assert.IsTrue(NavMesh.SamplePosition(approach, out _, 0.5f, NavMesh.AllAreas), $"davanti a {chest.name} non si arriva");
                Assert.AreEqual("Chest", chest.GetComponent<Interactable>().GetLabel(Localizer));
            }
        }

        [UnityTest, Description("Un click sulla cassa: il cavaliere ci va, il coperchio si alza e davanti cade l'oggetto previsto dal seme. Un secondo click non fa niente")]
        public IEnumerator Click_OpensOnceAndDropsTheSeededItem()
        {
            yield return LoadGeneratedCore();
            ClearEnemies();
            yield return null;

            var chest = Manager.CurrentLevel.Chests[0];

            // la cassa lascia un oggetto una volta su tre (D13 della M13): un seme con l'oggetto
            ulong seed = 1;
            for (Root.UseLootSeed(seed); chest.Preview() == null; Root.UseLootSeed(++seed))
            {
                Assert.Less(seed, 100UL, "nessun seme con l'oggetto");
            }

            string expected = Describe(chest.Preview());
            GroundItem dropped = null;
            int openings = 0;
            chest.Opened += item =>
            {
                dropped = item;
                openings++;
            };

            PlayerAgent.Warp(chest.GetComponent<Interactable>().ApproachPoint + chest.transform.forward * 1.5f);
            yield return new WaitForSeconds(0.3f);
            var lid = chest.transform.GetComponentsInChildren<Transform>().First(t => t.name == "chest_lid");
            Quaternion closed = lid.localRotation;

            ClickAt(chest.transform.position + Vector3.up * 0.7f);
            float elapsed = 0f;
            while (!chest.IsOpen && elapsed < 8f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(chest.IsOpen, "il cavaliere è arrivato e l'ha aperta");
            Assert.IsNotNull(dropped);
            Assert.AreEqual(expected, Describe(dropped.Item), "l'oggetto previsto dal seme");

            yield return new WaitForSeconds(0.6f);
            Assert.Greater(Quaternion.Angle(closed, lid.localRotation), 90f, "il coperchio si è alzato");
            Assert.IsTrue(lid.TransformPoint(Vector3.forward).y > lid.position.y, "il bordo davanti è salito, non sceso");

            // il secondo click può cadere sull'etichetta di un oggetto appena caduto e raccoglierlo:
            // conta che la cassa non lasci niente di nuovo, non quanti oggetti restano a terra
            var before = new System.Collections.Generic.HashSet<GroundItem>(Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None));
            ClickAt(chest.transform.position + Vector3.up * 0.7f);
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(1, openings, "aperta una volta sola");
            Assert.IsTrue(Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None).All(before.Contains), "nessun oggetto nuovo dalla cassa");
        }
    }
}
