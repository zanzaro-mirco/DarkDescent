using System.Collections;
using DarkDescent.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class TorchFlickerTests : SandboxFixture
    {
        [UnityTest, Description("La luce delle torce trema attorno al valore di partenza, senza mai uscire dal margine")]
        public IEnumerator Torch_FlickersWithinRange()
        {
            yield return LoadCore();
            var torches = Object.FindObjectsByType<TorchFlicker>(FindObjectsSortMode.None);
            Assert.IsNotEmpty(torches, "il livello 1 ha delle torce");

            var torch = torches[0];
            var light = torch.GetComponent<Light>();
            float low = torch.BaseIntensity * (1f - torch.Amount) - 0.001f;
            float high = torch.BaseIntensity * (1f + torch.Amount) + 0.001f;
            float min = float.MaxValue;
            float max = float.MinValue;
            float elapsed = 0f;
            while (elapsed < 1.5f)
            {
                min = Mathf.Min(min, light.intensity);
                max = Mathf.Max(max, light.intensity);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.GreaterOrEqual(min, low);
            Assert.LessOrEqual(max, high);
            Assert.Greater(max - min, torch.BaseIntensity * 0.05f, "la fiamma deve tremare");
        }

        [UnityTest, Description("Due torce non tremano all'unisono")]
        public IEnumerator Torches_AreNotInSync()
        {
            yield return LoadCore();
            var torches = Object.FindObjectsByType<TorchFlicker>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(torches.Length, 2);
            yield return null;

            var a = torches[0].GetComponent<Light>();
            var b = torches[1].GetComponent<Light>();
            float difference = 0f;
            for (int i = 0; i < 20; i++)
            {
                difference += Mathf.Abs(a.intensity / torches[0].BaseIntensity - b.intensity / torches[1].BaseIntensity);
                yield return null;
            }

            Assert.Greater(difference, 0.01f);
        }
    }
}
