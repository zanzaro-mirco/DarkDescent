using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Magic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Gli incantesimi rompono i barili e il barile del crollo è un barile vero (25.6 della M25, D11).</summary>
    public class BarrelSpellsPlayModeTests : SandboxFixture
    {
        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentBarrelSpells_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static SpellDefinition Spell(string name) => UnityEditor.AssetDatabase.LoadAssetAtPath<SpellDefinition>($"Assets/_Project/Data/Spells/{name}.asset");

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

        // la cripta al livello 1 del primo seme con dei barili e nessun barile esplosivo: l'esplosione ferirebbe il cavaliere
        private IEnumerator GoToCryptWithBarrels()
        {
            yield return LoadGeneratedCore();
            for (ulong seed = 1; seed <= 12; seed++)
            {
                Manager.RunSeed = seed;
                yield return Travel("Level_Crypt", "Start", 1);
                if (Manager.CurrentLevel.Breakables.Count > 0)
                {
                    foreach (var enemy in Manager.CurrentLevel.Enemies)
                    {
                        if (enemy != null && enemy.TryGetComponent(out Health health) && !health.IsDead)
                        {
                            health.TakeDamage(new DamageInfo(health.Max, DamageType.Physical, null));
                        }
                    }

                    yield break;
                }
            }

            Assert.Fail("nessun seme da 1 a 12 dà dei barili al livello 1");
        }

        // un punto del NavMesh a una certa distanza dal barile, da cui ci si arriva
        private static Vector3 StandAt(Breakable barrel, float distance)
        {
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI / 16f;
                Vector3 probe = barrel.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                // senza muri in mezzo: il proiettile si ferma sul NavMesh (il buco attorno al barile non è un muro)
                if (NavMesh.SamplePosition(probe, out var hit, 0.3f, NavMesh.AllAreas) && (!NavMesh.Raycast(hit.position, barrel.transform.position, out var edge, NavMesh.AllAreas) || Vector3.Distance(edge.position, barrel.transform.position) < barrel.Radius + 1.5f))
                {
                    return hit.position;
                }
            }

            Assert.Fail($"attorno a {barrel.name} non c'è posto a {distance} m");
            return Vector3.zero;
        }

        private PlayerMagic LearnedMagic(string spell)
        {
            var magic = Player.GetComponent<PlayerMagic>();
            Assert.IsTrue(magic.Learn(Spell(spell)), spell);
            return magic;
        }

        [UnityTest, Timeout(300000), Description("L'Anello di fuoco rompe i barili dentro il suo raggio e non quelli lontani")]
        public IEnumerator FireRing_BreaksTheBarrelsAround()
        {
            yield return GoToCryptWithBarrels();
            var barrel = Manager.CurrentLevel.Breakables[0];
            var magic = LearnedMagic("FireRing");
            PlayerAgent.Warp(StandAt(barrel, 2f));
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(barrel.IsBroken);

            Assert.AreEqual(CastResult.Ready, magic.TryCastAt(Spell("FireRing"), Player.position));
            for (float t = 0f; !barrel.IsBroken && t < 3f; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(barrel.IsBroken, "l'anello rompe il barile a 2 m");
            var far = Manager.CurrentLevel.Breakables.FirstOrDefault(b => b != barrel && !b.IsBroken && Vector3.Distance(b.transform.position, Player.position) > 8f);
            if (far != null)
            {
                Assert.IsFalse(far.IsBroken, "quello lontano resta intero");
            }
        }

        [UnityTest, Timeout(300000), Description("Il Dardo di fuoco che incontra un barile lo rompe e scoppia lì: non passa oltre")]
        public IEnumerator Firebolt_BreaksTheBarrelOnItsWay()
        {
            yield return GoToCryptWithBarrels();
            var barrel = Manager.CurrentLevel.Breakables[0];
            var magic = LearnedMagic("Firebolt");
            PlayerAgent.Warp(StandAt(barrel, 5f));
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(barrel.IsBroken);

            Assert.AreEqual(CastResult.Ready, magic.TryCastAt(Spell("Firebolt"), barrel.transform.position));
            for (float t = 0f; !barrel.IsBroken && t < 4f; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(barrel.IsBroken, "il dardo rompe il barile");
        }

        [UnityTest, Timeout(300000), Description("Un barile esplosivo colpito da un incantesimo accende la miccia e poi esplode, come con un colpo")]
        public IEnumerator AnExplosiveBarrel_FusesWhenASpellHitsIt()
        {
            yield return LoadGeneratedCore();
            Breakable barrel = null;
            for (ulong seed = 1; seed <= 60 && barrel == null; seed++)
            {
                Manager.RunSeed = seed;
                yield return Travel("Level_Crypt", "Start", 4);
                barrel = Manager.CurrentLevel.Breakables.FirstOrDefault(b => b.IsExplosive);
            }

            Assert.IsNotNull(barrel, "nessun seme da 1 a 60 dà un barile esplosivo alla profondità 4");
            foreach (var enemy in Manager.CurrentLevel.Enemies)
            {
                if (enemy != null && enemy.TryGetComponent(out Health health) && !health.IsDead)
                {
                    health.TakeDamage(new DamageInfo(health.Max, DamageType.Physical, null));
                }
            }

            var exploded = false;
            barrel.Exploded += _ => exploded = true;
            var magic = LearnedMagic("Firebolt");
            PlayerAgent.Warp(StandAt(barrel, 7f));
            yield return new WaitForSeconds(0.5f);
            magic.TryCastAt(Spell("Firebolt"), barrel.transform.position);
            for (float t = 0f; !barrel.IsBroken && t < 4f; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(barrel.IsBroken);
            Assert.IsTrue(barrel.IsFusing || exploded, "la miccia è accesa");
            for (float t = 0f; !exploded && t < 3f; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(exploded, "dopo la miccia scoppia");
        }

        [UnityTest, Timeout(300000), Description("Il barile del crollo delle caverne è un barile vero: si rompe con un colpo, non lascia niente e il suo posto si libera")]
        public IEnumerator TheRockfallBarrel_IsARealBarrel()
        {
            yield return LoadGeneratedCore();
            Breakable barrel = null;
            for (ulong seed = 1; seed <= 20 && barrel == null; seed++)
            {
                Manager.RunSeed = seed;
                yield return Travel("Level_Caves", "Start", 5);
                barrel = Manager.CurrentLevel.Breakables.FirstOrDefault(b => b.name == "RockfallBarrel");
            }

            Assert.IsNotNull(barrel, "nessuna caverna da 1 a 20 ha un crollo con il barile");
            Assert.IsNull(barrel.LootTable, "il barile del crollo è vuoto");
            Assert.AreEqual(LayerMask.NameToLayer("Enemy"), barrel.gameObject.layer, "si colpisce come gli altri barili");
            Assert.IsFalse(barrel.IsBroken);
            var intact = barrel.transform.Find("Intact");
            Assert.IsTrue(intact.gameObject.activeSelf);

            // poggia a terra: il piede del modello sta sul pavimento
            var bounds = intact.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Assert.That(bounds.min.y, Is.InRange(-0.15f, 0.15f), "il barile poggia a terra");

            var before = UnityEngine.Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None).Length;
            barrel.TakeDamage(new DamageInfo(10f, DamageType.Physical, Player.gameObject));
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(barrel.IsBroken);
            Assert.IsFalse(intact.gameObject.activeSelf);
            Assert.IsFalse(barrel.GetComponent<NavMeshObstacle>().enabled, "il passaggio si apre");
            Assert.AreEqual(before, UnityEngine.Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None).Length, "niente a terra");
        }
    }
}
