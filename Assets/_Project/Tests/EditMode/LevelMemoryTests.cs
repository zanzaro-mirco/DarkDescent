using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>I ricordi dei livelli visitati (10.9 della M10).</summary>
    public class LevelMemoryTests
    {
        [Test, Description("Un ricordo per scena e profondità: quello nuovo prende il posto del vecchio, e scene diverse alla stessa profondità non si confondono")]
        public void Memory_KeepsOneRecordPerLevel()
        {
            var memory = new LevelMemory();
            var first = new LevelRecord("Level_Crypt", 1, 10, 2);
            first.AddKilled(3);
            memory.Remember(first);
            memory.Remember(new LevelRecord("Level_Test", 1, 4, 0));
            Assert.AreEqual(2, memory.Count);

            var second = new LevelRecord("Level_Crypt", 1, 10, 2);
            second.AddKilled(3);
            second.AddKilled(5);
            memory.Remember(second);
            Assert.AreEqual(2, memory.Count);
            Assert.IsTrue(memory.TryGet("Level_Crypt", 1, out var read));
            Assert.AreSame(second, read);
            Assert.IsFalse(memory.TryGet("Level_Crypt", 2, out _));
        }

        [Test, Description("Importare un salvataggio sostituisce i ricordi di adesso; senza ricordi la memoria si svuota")]
        public void Import_ReplacesTheRecords()
        {
            var memory = new LevelMemory();
            memory.Remember(new LevelRecord("Level_Caves", 6, 20, 3));
            var saved = new LevelRecord("Level_Crypt", 2, 12, 1);
            saved.AddItem(new GroundItemRecord(15, Vector3.one));
            memory.Import(new[] { saved });
            Assert.AreEqual(1, memory.Count);
            Assert.IsFalse(memory.TryGet("Level_Caves", 6, out _));
            Assert.IsTrue(memory.TryGet("Level_Crypt", 2, out var read));
            Assert.AreEqual(15, read.Items[0].Gold);

            memory.Import(null);
            Assert.AreEqual(0, memory.Count);
        }
    }
}
