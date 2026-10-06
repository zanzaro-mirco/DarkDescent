using System.Collections;
using System.Collections.Generic;
using DarkDescent.Audio;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class AmbienceTests : SandboxFixture
    {
        private const string CryptScene = "Level_Crypt";

        private static AmbiencePlayer Ambience => Object.FindFirstObjectByType<AmbiencePlayer>();

        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static T Load<T>(string path) where T : Object
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static AmbienceProfile CryptProfile => Load<LevelTileset>("Assets/_Project/Data/Levels/DungeonTileset.asset").Ambience;

        private static AmbienceProfile CavesProfile => Load<AmbienceProfile>("Assets/_Project/Data/Audio/AmbienceCaves.asset");

        private static IEnumerator WaitUnscaled(float seconds)
        {
            for (float time = 0f; time < seconds; time += Time.unscaledDeltaTime)
            {
                yield return null;
            }
        }

        [Test, Description("I due profili hanno musica, fondo e almeno sei versi nel buio, tutti presenti")]
        public void Profiles_AreComplete()
        {
            foreach (var profile in new[] { CryptProfile, CavesProfile })
            {
                Assert.IsNotNull(profile);
                Assert.IsNotNull(profile.Music, $"{profile.name}: la musica");
                Assert.IsNotNull(profile.Bed, $"{profile.name}: il fondo");
                Assert.GreaterOrEqual(profile.StingerCount, 6, profile.name);
                for (int i = 0; i < profile.StingerCount; i++)
                {
                    Assert.IsNotNull(profile.GetStinger(i), $"{profile.name}: verso {i}");
                }
            }

            Assert.AreNotEqual(CryptProfile.Music, CavesProfile.Music, "cripta e caverne non suonano uguali");
        }

        [UnityTest, Description("Nella cripta suonano la sua musica e il fondo, in loop, in 2D, dal gruppo Music, e salgono al loro volume")]
        public IEnumerator Crypt_PlaysItsMusicAndBed()
        {
            yield return LoadGeneratedCore();
            var profile = CryptProfile;

            Assert.AreSame(profile, Ambience.Current);
            foreach (var (source, clip) in new[] { (Ambience.Music, profile.Music), (Ambience.Bed, profile.Bed) })
            {
                Assert.AreSame(clip, source.clip);
                Assert.IsTrue(source.isPlaying, $"{clip.name} non suona");
                Assert.IsTrue(source.loop);
                Assert.AreEqual(0f, source.spatialBlend, "musica e fondo non vengono da un punto");
                Assert.AreEqual("Music", source.outputAudioMixerGroup.name);
            }

            yield return WaitUnscaled(3f);
            Assert.AreEqual(profile.MusicVolume, Ambience.Music.volume, 1e-3f);
            Assert.AreEqual(profile.BedVolume, Ambience.Bed.volume, 1e-3f);
        }

        [UnityTest, Description("Scendendo nella cripta la musica non ricomincia: stessa sorgente, sempre in corsa")]
        public IEnumerator SameProfile_MusicCarriesOn()
        {
            yield return LoadGeneratedCore();
            var music = Ambience.Music;
            yield return WaitUnscaled(1f);
            float before = music.time;

            Manager.LoadLevel(CryptScene, "FromAbove", 2);
            for (float time = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel.Depth != 2; time += Time.unscaledDeltaTime)
            {
                Assert.Less(time, 15f, "il livello 2 non si è caricato");
                yield return null;
            }

            Assert.AreSame(music, Ambience.Music);
            Assert.IsTrue(music.isPlaying);
            Assert.Greater(music.time, before, "la musica va avanti da dove era");
        }

        [UnityTest, Description("Un altro profilo sfuma nell'altro: la musica nuova sale, la vecchia scende e si ferma")]
        public IEnumerator OtherProfile_Crossfades()
        {
            yield return LoadGeneratedCore();
            yield return WaitUnscaled(3f);
            var oldMusic = Ambience.Music;

            Ambience.Play(CavesProfile);
            var newMusic = Ambience.Music;
            Assert.AreNotSame(oldMusic, newMusic);
            Assert.AreSame(CavesProfile.Music, newMusic.clip);

            yield return WaitUnscaled(1f);
            Assert.That(oldMusic.volume, Is.LessThan(CryptProfile.MusicVolume).And.GreaterThan(0f), "la vecchia scende");
            Assert.That(newMusic.volume, Is.GreaterThan(0f).And.LessThan(CavesProfile.MusicVolume), "la nuova sale");

            yield return WaitUnscaled(2f);
            Assert.IsFalse(oldMusic.isPlaying, "finita la dissolvenza la vecchia si ferma");
            Assert.AreEqual(CavesProfile.MusicVolume, newMusic.volume, 1e-3f);

            // lo stesso profilo una seconda volta non cambia niente
            Ambience.Play(CavesProfile);
            Assert.AreSame(newMusic, Ambience.Music);
        }

        [UnityTest, Description("I versi nel buio partono a intervalli, in 3D dal gruppo SFX, attorno al cavaliere alla distanza del profilo, mai lo stesso due volte di fila")]
        public IEnumerator Stingers_PlayAroundThePlayer()
        {
            yield return LoadGeneratedCore();

            // il profilo della cripta, con versi ogni pochi centesimi di secondo
            var quick = Object.Instantiate(CryptProfile);
            JsonUtility.FromJsonOverwrite("{\"_stingerInterval\":{\"x\":0.05,\"y\":0.1}}", quick);
            Ambience.Play(quick);

            var played = new List<(AudioClip clip, Vector3 offset)>();
            void Record(AudioSource source)
            {
                Assert.IsTrue(source.isPlaying);
                Assert.AreEqual(1f, source.spatialBlend, "il verso viene da un punto");
                Assert.AreEqual("SFX", source.outputAudioMixerGroup.name);
                Assert.IsNotNull(source.GetComponent<AudioLowPassFilter>(), "suona lontano");
                Assert.That(source.pitch, Is.InRange(quick.StingerPitch.x, quick.StingerPitch.y));
                played.Add((source.clip, source.transform.position - Player.position));
            }

            Ambience.StingerPlayed += Record;
            try
            {
                for (float time = 0f; played.Count < 8; time += Time.unscaledDeltaTime)
                {
                    Assert.Less(time, 5f, $"solo {played.Count} versi in 5 s");
                    yield return null;
                }
            }
            finally
            {
                Ambience.StingerPlayed -= Record;
                Object.Destroy(quick);
            }

            for (int i = 0; i < played.Count; i++)
            {
                var flat = new Vector2(played[i].offset.x, played[i].offset.z);
                Assert.That(flat.magnitude, Is.InRange(quick.StingerDistance.x - 0.01f, quick.StingerDistance.y + 0.01f), $"verso {i}");
                if (i > 0)
                {
                    Assert.AreNotSame(played[i - 1].clip, played[i].clip, $"verso {i} uguale al precedente");
                }
            }
        }
    }
}
