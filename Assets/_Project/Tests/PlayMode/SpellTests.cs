using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Magic;
using DarkDescent.Stats;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class SpellTests : SandboxFixture
    {
        private static T Asset<T>(string path) where T : Object => UnityEditor.AssetDatabase.LoadAssetAtPath<T>("Assets/_Project/" + path);
        private static SpellDefinition Spell(string name) => Asset<SpellDefinition>($"Data/Spells/{name}.asset");

        private static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                yield return null;
            }
        }

        // un punto sul NavMesh a <distance> dal cavaliere, in pianta
        private Vector3 PointAround(float distance, float startAngle)
        {
            for (int i = 0; i < 24; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, startAngle + i * 15f, 0f) * Vector3.forward * distance;
                if (NavMesh.SamplePosition(Player.position + offset, out var hit, 0.3f, NavMesh.AllAreas)
                    && Mathf.Abs(FlatDistance(hit.position, Player.position) - distance) < 0.3f)
                {
                    return hit.position;
                }
            }

            Assert.Fail($"nessun punto a {distance} m dal cavaliere");
            return Vector3.zero;
        }

        [UnityTest, Description("L'Anello di fuoco colpisce il nemico a 3 m con 11–18 di danno (8–14 + Magia 10 / 3) e non quello a 6 m; spende 15 di mana e parte la ricarica di 3 s; l'effetto si accende e poi si spegne")]
        public IEnumerator FireRing_HitsOnlyEnemiesInRadius()
        {
            yield return LoadSandbox(allSkeletons: true);
            var enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Where(e => e.isActiveAndEnabled).ToArray();
            Assert.GreaterOrEqual(enemies.Length, 2, "servono due nemici");
            foreach (var extra in enemies.Skip(2))
            {
                extra.gameObject.SetActive(false);
            }

            var near = enemies[0].GetComponent<Health>();
            var far = enemies[1].GetComponent<Health>();
            enemies[0].GetComponent<NavMeshAgent>().Warp(PointAround(3f, 0f));
            enemies[1].GetComponent<NavMeshAgent>().Warp(PointAround(6f, 180f));
            yield return null;

            var magic = Player.GetComponent<PlayerMagic>();
            Assert.IsTrue(magic.Learn(Spell("FireRing")));
            Assert.AreSame(Spell("FireRing"), magic.Book.SelectedSpell, "imparato, è scelto");
            Assert.AreEqual(CastResult.Ready, magic.TryCastAt(Player.position));
            Assert.AreEqual(15f, magic.Mana.Max - magic.Mana.Current, 0.5f);
            Assert.AreEqual(near.Max, near.Current, "il colpo aspetta la mano");

            yield return Wait(0.6f);
            float damage = near.Max - near.Current;
            Assert.That(damage, Is.InRange(11f, 18f), "danno dell'anello");
            Assert.AreEqual(far.Max, far.Current, "a 6 m non arriva");
            var effect = Object.FindFirstObjectByType<AreaEffect>();
            Assert.IsNotNull(effect, "il cerchio di fiamme");
            Assert.AreEqual(CastResult.Cooldown, magic.Caster.Check(magic.Book.Selected));

            yield return Wait(1.2f);
            Assert.IsFalse(effect.IsPlaying, "le fiamme si spengono");
        }

        [UnityTest, Description("La Pelle di pietra alza l'Armatura del 50% e accende l'alone; finiti i 20 s (a tempo accelerato) l'Armatura torna com'era e l'alone si spegne; alla morte finisce subito")]
        public IEnumerator StoneSkin_RaisesArmorForItsDuration()
        {
            yield return LoadSandbox();
            var stats = Player.GetComponent<CharacterStats>();
            stats.Sheet.SetBase(StatType.Armor, 20f);
            var magic = Player.GetComponent<PlayerMagic>();
            var spells = Player.GetComponent<PlayerSpells>();
            Assert.IsTrue(magic.Learn(Spell("StoneSkin")));
            Assert.AreEqual(CastResult.Ready, magic.TryCastAt(Player.position));
            yield return Wait(0.5f);

            Assert.AreEqual(30f, stats.Armor, 0.01f, "+50% di 20");
            var aura = Player.GetComponentInChildren<AuraEffect>();
            Assert.IsNotNull(aura);
            Assert.IsTrue(aura.IsPlaying);
            Assert.That(spells.BuffRemaining, Is.InRange(19f, 20f));

            Time.timeScale = 20f;
            try
            {
                for (float t = 0f; t < 3f && spells.BuffRemaining > 0f; t += Time.unscaledDeltaTime)
                {
                    yield return null;
                }
            }
            finally
            {
                Time.timeScale = 1f;
            }

            Assert.AreEqual(0f, spells.BuffRemaining);
            Assert.AreEqual(20f, stats.Armor, 0.01f, "finito, l'Armatura torna com'era");
            Assert.IsFalse(aura.IsPlaying);

            // ricarica di 30 s: si riparte da capo con un cavaliere nuovo di mana e ricarica
            magic.Caster.Advance(30f);
            magic.Mana.Fill();
            Assert.AreEqual(CastResult.Ready, magic.TryCastAt(Player.position));
            yield return Wait(0.5f);
            Assert.AreEqual(30f, stats.Armor, 0.01f);
            Player.GetComponent<Health>().TakeDamage(new DamageInfo(10000f, DamageType.Fire, null));
            yield return null;
            Assert.AreEqual(20f, stats.Armor, 0.01f, "alla morte finisce");
            Assert.IsFalse(aura.IsPlaying);
        }

        [UnityTest, Description("Una pergamena rimasta da prima della M20 non si legge più: il click destro non insegna niente e la lascia nello zaino (D4)")]
        public IEnumerator OldScroll_TeachesNothing()
        {
            yield return LoadSandbox();
            var inventory = Player.GetComponent<PlayerInventory>();
            var magic = Player.GetComponent<PlayerMagic>();
            var scroll = Asset<ScrollDefinition>("Data/Items/ScrollStoneSkin.asset");
            var item = new ItemInstance(scroll);
            Assert.IsTrue(inventory.TryPickUp(item));
            Assert.IsTrue(inventory.Inventory.Grid.TryGetPlacement(item, out var area));

            var panel = Object.FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            panel.SetOpen(true);
            yield return null;
            panel.UseCell(area.position);

            Assert.IsFalse(magic.Book.Knows(scroll.Spell));
            Assert.IsTrue(inventory.Inventory.Grid.TryGetPlacement(item, out _), "resta nello zaino");
        }

        [UnityTest, Description("La pozione di mana va nella cintura e rende metà del mana massimo; a mana pieno non si beve; la pozione di cura non tocca il mana")]
        public IEnumerator ManaPotion_RestoresMana()
        {
            yield return LoadSandbox();
            var inventory = Player.GetComponent<PlayerInventory>();
            var magic = Player.GetComponent<PlayerMagic>();
            var mana = Asset<PotionDefinition>("Data/Items/ManaPotion.asset");
            for (int i = 0; i < Belt.Size; i++)
            {
                inventory.Belt.Take(i);
            }

            Assert.IsTrue(inventory.TryPickUp(new ItemInstance(mana)));
            Assert.AreSame(mana, inventory.Belt.Get(0).Definition, "nella cintura");
            Assert.IsFalse(inventory.DrinkFromBelt(0), "a mana pieno resta");

            magic.Mana.SetCurrent(3f);
            Assert.IsTrue(inventory.DrinkFromBelt(0));
            Assert.AreEqual(18f, magic.Mana.Current, 0.01f, "3 + metà di 30");
            Assert.IsNull(inventory.Belt.Get(0));
        }
    }
}
