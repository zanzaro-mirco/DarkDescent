using System.Collections;
using DarkDescent.Characters;
using DarkDescent.Levels;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Dalla prova della build M7: i passi del cavaliere, con il suono del pavimento del livello, e la
    /// scritta del livello in cui si è.
    /// </summary>
    public class FootstepsAndTitleTests : SandboxFixture
    {
        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static LevelTileset Tileset(string name)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<LevelTileset>($"Assets/_Project/Data/Levels/{name}.asset");
        }

        private static IEnumerator Enter(string scene, int depth)
        {
            Manager.LoadLevel(scene, "FromAbove", depth);
            for (float time = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel.Depth != depth; time += Time.unscaledDeltaTime)
            {
                Assert.Less(time, 15f, $"la profondità {depth} non si è caricata");
                yield return null;
            }
        }

        [UnityTest, Description("Correndo il cavaliere fa un passo quando un piede tocca terra nell'animazione, con i suoni della pietra della cripta; fermo, nessun passo; nelle caverne i passi sono di terra")]
        public IEnumerator Steps_FollowTheFeetAndTheFloor()
        {
            yield return LoadGeneratedCore();
            var feet = Player.GetComponentInChildren<Footsteps>();
            CollectionAssert.AreEqual(Tileset("DungeonTileset").Footsteps, feet.Surface, "la pietra della cripta");

            int steps = 0;
            float lastStep = -1f;
            float shortestGap = float.MaxValue;
            float worstPhase = 0f;
            void OnStep(AudioClip clip)
            {
                steps++;
                CollectionAssert.Contains(feet.Surface, clip);
                if (lastStep >= 0f)
                {
                    shortestGap = Mathf.Min(shortestGap, Time.time - lastStep);
                }

                lastStep = Time.time;

                // quanto il ciclo della corsa è lontano dal punto in cui un piede tocca terra
                float cycle = Mathf.Repeat(feet.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);
                float nearest = 1f;
                foreach (float footfall in feet.Footfalls)
                {
                    float gap = Mathf.Repeat(cycle - footfall, 1f);
                    nearest = Mathf.Min(nearest, Mathf.Min(gap, 1f - gap));
                }

                worstPhase = Mathf.Max(worstPhase, nearest);
            }

            feet.StepPlayed += OnStep;
            try
            {
                yield return new WaitForSeconds(0.5f);
                Assert.AreEqual(0, steps, "fermo non fa passi");

                // verso un punto del NavMesh lontano dall'ingresso
                Vector3 start = Player.position;
                Vector3 goal = Manager.CurrentLevel.Exits[0].transform.position;
                PlayerAgent.SetDestination(goal);
                yield return new WaitForSeconds(2f);
                float walked = FlatDistance(start, Player.position);
                Assert.Greater(walked, 4f, "il cavaliere deve aver camminato");
                // il ciclo della corsa dura 0,8 s con due passi: in 2 s di corsa, partenza compresa, 3–5 passi
                Assert.That(steps, Is.InRange(3, 6), $"{walked:0.0} m");
                Assert.GreaterOrEqual(shortestGap, 0.3f, "mai più svelti dei piedi: un passo ogni 0,4 s a piena corsa");
                Assert.Less(worstPhase, 0.1f, "ogni passo suona quando un piede tocca terra");
            }
            finally
            {
                feet.StepPlayed -= OnStep;
            }

            yield return Enter("Level_Caves", 5);
            CollectionAssert.AreEqual(Tileset("CaveTileset").Footsteps, feet.Surface, "la terra delle caverne");
            CollectionAssert.AreNotEqual(Tileset("DungeonTileset").Footsteps, feet.Surface);
        }

        [UnityTest, Description("Entrando in un livello la scritta dice tipo e profondità, nella lingua scelta; grande, poi sfuma, e piccola resta")]
        public IEnumerator Title_SaysWhereYouAre()
        {
            yield return LoadGeneratedCore();
            var title = Object.FindFirstObjectByType<LevelTitle>();
            Assert.AreEqual("Crypt – Level 1", title.Text);
            var banner = title.GetComponentInChildren<CanvasGroup>();
            Assert.AreEqual(1f, banner.alpha, 0.01f, "appena entrati si vede grande");

            yield return Enter("Level_Caves", 6);
            Assert.AreEqual("Caves – Level 6", title.Text);
            Localizer.SetLanguage("it");
            Assert.AreEqual("Caverne – Livello 6", title.Text, "cambia con la lingua");

            yield return new WaitForSecondsRealtime(4.3f);
            Assert.AreEqual(0f, banner.alpha, 0.01f, "dopo qualche secondo la scritta grande è sparita");
            Assert.AreEqual("Caverne – Livello 6", title.Text, "la piccola resta");
        }
    }
}
