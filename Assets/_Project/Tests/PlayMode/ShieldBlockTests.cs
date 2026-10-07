using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class ShieldBlockTests : SandboxFixture
    {
        // dentro il raggio di aggro dello scheletro principale, in vista (come in EnemyAITests)
        private static readonly Vector3 InSightPoint = new Vector3(6f, 0f, 1f);

        private ShieldBlock _block;
        private Health _health;
        private PlayerInventory _inventory;
        private Health _skeleton;

        private IEnumerator LoadArena()
        {
            yield return LoadSandbox();
            _block = Player.GetComponent<ShieldBlock>();
            _health = Player.GetComponent<Health>();
            _inventory = Player.GetComponent<PlayerInventory>();
            _skeleton = GameObject.Find("Skeleton").GetComponent<Health>();
        }

        private void EquipShield()
        {
#if UNITY_EDITOR
            var shield = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/BadgeShield.asset");
            Assert.IsTrue(_inventory.Equipment.TryEquip(new ItemInstance(shield), out _));
#endif
        }

        private static IEnumerator WaitFor(System.Func<bool> condition, float timeout)
        {
            float elapsed = 0f;
            while (elapsed < timeout && !condition())
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        [UnityTest, Description("Senza scudo il blocco è 0; con lo scudo con stemma è 10 + Destrezza 20 / 2 = 20%, e il pannello lo mostra")]
        public IEnumerator BlockChance_FollowsTheShield()
        {
            yield return LoadArena();
            var character = Object.FindFirstObjectByType<CharacterPanel>();
            Assert.IsFalse(_block.HasShield);
            Assert.AreEqual(0f, _block.BlockChance);
            StringAssert.EndsWith("\n0%", character.ValuesText);

            EquipShield();

            Assert.AreEqual(20f, _block.BlockChance, 0.001f);
            StringAssert.EndsWith("\n20%", character.ValuesText);

            _inventory.Equipment.Unequip(EquipSlot.Offhand);
            Assert.AreEqual(0f, _block.BlockChance);
        }

        [UnityTest, Description("Con lo scudo e il tiro a 0,0 i colpi dello scheletro sono bloccati: niente danno, \"Blocked\", animazione Block")]
        public IEnumerator Shield_BlocksSkeletonHits()
        {
            yield return LoadArena();
            EquipShield();
            int blocks = 0;
            void Count(DamageInfo info) => blocks++;
            _block.Blocked += Count;

            ClickAt(InSightPoint);
            yield return WaitFor(() => blocks > 0, 8f);
            yield return null;

            _block.Blocked -= Count;
            Assert.Greater(blocks, 0, "lo scheletro deve attaccare e il colpo essere bloccato");
            Assert.AreEqual(_health.Max, _health.Current, "un colpo bloccato non toglie vita");
            var texts = Object.FindFirstObjectByType<DamageNumbers>().GetComponentsInChildren<TextMeshProUGUI>().Select(t => t.text).ToArray();
            CollectionAssert.Contains(texts, "Blocked");
            var animator = Player.GetComponentInChildren<Animator>();
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Block") || animator.GetNextAnimatorStateInfo(0).IsName("Block"),
                "parte l'animazione del blocco");
        }

        [UnityTest, Description("Senza scudo lo stesso tiro a 0,0 non blocca: il colpo arriva")]
        public IEnumerator NoShield_NeverBlocks()
        {
            yield return LoadArena();
            int blocks = 0;
            void Count(DamageInfo info) => blocks++;
            _block.Blocked += Count;

            ClickAt(InSightPoint);
            yield return WaitFor(() => _health.Current < _health.Max, 8f);

            _block.Blocked -= Count;
            Assert.Less(_health.Current, _health.Max, "il colpo dello scheletro arriva");
            Assert.AreEqual(0, blocks);
        }

        [UnityTest, Description("Il blocco non ferma il colpo che il cavaliere stava dando: il danno arriva (prova della M7, lo sciame lo teneva fermo)")]
        public IEnumerator Block_DoesNotStopKnightSwing()
        {
            yield return LoadArena();
            EquipShield();
            var attack = Player.GetComponent<MeleeAttack>();

            ClickAt(_skeleton.transform.position + Vector3.up);
            yield return WaitFor(() => attack.IsSwinging, 6f);
            Assert.IsTrue(attack.IsSwinging, "il cavaliere deve aver cominciato il colpo");

            Assert.IsTrue(_block.TryBlock(new DamageInfo(0f, DamageType.Physical, null), new FixedRandomSource(0.0)));

            Assert.IsTrue(attack.IsSwinging, "il colpo in corso continua");
            Assert.IsFalse(attack.IsInterrupted);
            yield return WaitFor(() => _skeleton.Current < _skeleton.Max, 1f);
            Assert.Less(_skeleton.Current, _skeleton.Max, "il danno del colpo arriva");
        }
    }
}
