using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>I segreti nel ricordo di un livello (25.4 della M25, D13): niente formato nuovo del salvataggio, e un ricordo di prima combacia ancora.</summary>
    public class LevelRecordSecretsTests
    {
        [Test, Description("Una porta aperta passa dal salvataggio: il ricordo la ritrova, e sa quanti segreti aveva il livello")]
        public void ASolvedSecret_TravelsThroughTheSave()
        {
            var record = new LevelRecord("Level_Crypt", 3, 12, 4, 6, 2, secretCount: 1);
            record.AddSecretDone(0);

            var back = JsonUtility.FromJson<LevelRecord>(JsonUtility.ToJson(record));

            Assert.IsTrue(back.MatchesSecrets(1));
            CollectionAssert.AreEqual(new[] { 0 }, back.SecretsDone);
            Assert.IsTrue(back.Matches(12, 4), "nemici e casse come prima");
        }

        [Test, Description("Un ricordo di prima della M25 non ha segreti: combacia con un livello senza, i suoi nemici e le sue casse si rimettono com'erano, e un livello con la porta non ritrova niente di inventato")]
        public void ARecordFromBeforeTheSecrets_StillMatches()
        {
            var old = JsonUtility.FromJson<LevelRecord>("{\"_scene\":\"Level_Crypt\",\"_depth\":3,\"_enemyCount\":12,\"_chestCount\":4,\"_killed\":[0,3],\"_opened\":[1]}");

            Assert.IsTrue(old.Matches(12, 4), "nemici e casse combaciano");
            CollectionAssert.AreEqual(new[] { 0, 3 }, old.Killed, "i nemici morti restano morti");
            CollectionAssert.AreEqual(new[] { 1 }, old.Opened, "la cassa aperta resta aperta");
            Assert.IsTrue(old.MatchesSecrets(0), "un livello senza porta");
            Assert.IsFalse(old.MatchesSecrets(1), "un livello con la porta non prende segreti da un ricordo che non ne ha");
            Assert.AreEqual(0, old.SecretsDone.Count);
        }
    }
}
