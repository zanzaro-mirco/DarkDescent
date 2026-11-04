using System.Linq;
using DarkDescent.Core;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Le stanze più grandi della cripta (22.2 della M22, D11): le impostazioni vere del gioco, non quelle di partenza della classe.</summary>
    public class CryptRoomsTests
    {
        private const string CryptSettingsPath = "Assets/_Project/Data/Levels/CryptSettings.asset";
        private const string CaveSettingsPath = "Assets/_Project/Data/Levels/CaveSettings.asset";
        private const int Seeds = 300;

        [Test, Description("La cripta ha la griglia da 32 celle e le stanze da 4 a 8 celle per lato; le caverne restano da 28")]
        public void Settings_AreTheBiggerRooms()
        {
            var crypt = AssetDatabase.LoadAssetAtPath<DungeonSettings>(CryptSettingsPath);
            Assert.AreEqual((32, 32), (crypt.Width, crypt.Height));
            Assert.AreEqual((7, 12), (crypt.MinLeaf, crypt.MaxLeaf));
            Assert.AreEqual((4, 8), (crypt.MinRoom, crypt.MaxRoom));

            var caves = AssetDatabase.LoadAssetAtPath<CaveSettings>(CaveSettingsPath);
            Assert.AreEqual((28, 28), (caves.Width, caves.Height), "le caverne non hanno stanze: la loro forma non cambia");
        }

        [Test, Description("Su 300 semi e quattro profondità nessuna stanza della cripta ha un lato di meno di 4 celle (16 m) né più di 8, e i livelli hanno da 7 a 16 stanze")]
        public void Rooms_AreAtLeastFourCellsWide()
        {
            var crypt = AssetDatabase.LoadAssetAtPath<DungeonSettings>(CryptSettingsPath);
            var generator = crypt.CreateGenerator();
            for (int depth = 1; depth <= 4; depth++)
            {
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    var layout = generator.Generate(SeedMixer.ForLevel(seed, depth), depth);
                    Assert.AreEqual(32, layout.Width);
                    Assert.That(layout.Rooms.Count, Is.InRange(7, 16), $"seme {seed}, profondità {depth}: stanze");
                    foreach (var room in layout.Rooms)
                    {
                        Assert.That(room.width, Is.InRange(4, 8), $"seme {seed}, profondità {depth}: {room}");
                        Assert.That(room.height, Is.InRange(4, 8), $"seme {seed}, profondità {depth}: {room}");
                    }
                }
            }
        }

        [Test, Description("La stanza mediana è più grande di prima: almeno 24 celle su 300 semi (con le stanze da 3 a 7 erano 16)")]
        public void MedianRoom_IsBigger()
        {
            var crypt = AssetDatabase.LoadAssetAtPath<DungeonSettings>(CryptSettingsPath);
            var generator = crypt.CreateGenerator();
            var areas = Enumerable.Range(1, Seeds)
                .SelectMany(i => generator.Generate(SeedMixer.ForLevel((ulong)i, 3), 3).Rooms.Select(r => r.width * r.height))
                .OrderBy(a => a).ToList();
            Assert.GreaterOrEqual(areas[areas.Count / 2], 24);
        }
    }
}
