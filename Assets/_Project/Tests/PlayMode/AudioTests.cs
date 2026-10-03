using System.Collections;
using DarkDescent.Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class AudioTests : SandboxFixture
    {
        [UnityTest, Description("Con Core e un livello caricati c'è un solo AudioListener")]
        public IEnumerator Scene_HasSingleAudioListener()
        {
            yield return LoadSandbox(allSkeletons: true);

            Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length);
        }

        [UnityTest, Description("Il listener segue la testa del cavaliere, orientato come la camera")]
        public IEnumerator Listener_FollowsPlayerHead()
        {
            yield return LoadSandbox();

            PlayerAgent.Warp(new Vector3(6f, 0f, 4f));
            yield return null;

            var listener = Object.FindFirstObjectByType<AudioListener>().transform;
            Assert.Less(Vector3.Distance(Player.position + Vector3.up * 1.6f, listener.position), 0.01f);
            Assert.AreEqual(Camera.transform.eulerAngles.y, listener.eulerAngles.y, 0.1f, "destra dello schermo = destra delle orecchie");
        }

        [UnityTest, Description("I suoni dei personaggi sono 3D, si attenuano entro 25 m e passano dal gruppo SFX")]
        public IEnumerator CharacterSounds_AreSpatial()
        {
            yield return LoadSandbox(allSkeletons: true);

            var sounds = Object.FindObjectsByType<CharacterAudio>(FindObjectsSortMode.None);
            Assert.AreEqual(4, sounds.Length, "cavaliere più tre scheletri");
            foreach (var sound in sounds)
            {
                var source = sound.GetComponent<AudioSource>();
                Assert.AreEqual(1f, source.spatialBlend, $"{sound.name}: deve essere 3D");
                Assert.AreEqual(25f, source.maxDistance, 0.01f);
                Assert.IsNotNull(source.outputAudioMixerGroup, $"{sound.name}: manca il gruppo del mixer");
                Assert.AreEqual("SFX", source.outputAudioMixerGroup.name);
            }
        }
    }
}
