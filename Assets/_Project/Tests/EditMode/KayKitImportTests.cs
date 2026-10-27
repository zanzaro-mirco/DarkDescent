using System.IO;
using System.Linq;
using DarkDescent.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>I pezzi KayKit del borgo, i personaggi degli abitanti e le animazioni nuove arrivano con le impostazioni giuste (M18, 18.1).</summary>
    public class KayKitImportTests
    {
        private static readonly string[] Folders = { "Village", "Graveyard", "Nature" };
        private static readonly string[] Animations = { "Rig_Medium_Simulation.fbx", "Rig_Medium_Special.fbx", "Rig_Medium_Tools.fbx" };

        private static string[] Props => Folders
            .SelectMany(folder => Directory.GetFiles(KayKitImporter.ModelFolder + folder, "*.fbx"))
            .Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray();

        private static AnimationClip Clip(string file, string name)
        {
            return AssetDatabase.LoadAllAssetsAtPath(KayKitImporter.AnimationFolder + file)
                .OfType<AnimationClip>().FirstOrDefault(clip => clip.name == name);
        }

        [Test, Description("Ogni pezzo del borgo si importa come i KayKit della cripta: niente materiali né animazioni")]
        public void Props_ImportLikeTheDungeon()
        {
            Assert.That(Props.Length, Is.GreaterThan(150), "case, cimitero e natura ci sono tutti");
            foreach (string path in Props)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                Assert.AreEqual(ModelImporterMaterialImportMode.None, importer.materialImportMode, path);
                Assert.IsFalse(importer.importAnimation, path);
                Assert.AreEqual(ModelImporterAnimationType.None, importer.animationType, path);
            }
        }

        [Test, Description("Ogni atlante dei pezzi ha il suo materiale, con la texture giusta")]
        public void Atlases_HaveTheirMaterials()
        {
            foreach (var (name, texture, _) in VillageTools.Materials)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(VillageTools.MaterialFolder + name + ".mat");
                Assert.IsNotNull(material, name);
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(texture), material.GetTexture("_BaseMap"), name);
            }
        }

        [Test, Description("Le animazioni nuove usano lo scheletro del cavaliere, come quelle della M1")]
        public void Animations_UseTheKnightSkeleton()
        {
            var knight = AssetDatabase.LoadAssetAtPath<Avatar>(KayKitImporter.KnightPath);
            foreach (string file in Animations)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(KayKitImporter.AnimationFolder + file);
                Assert.AreEqual(ModelImporterAnimationType.Generic, importer.animationType, file);
                Assert.AreEqual(ModelImporterAvatarSetup.CopyFromOther, importer.avatarSetup, file);
                Assert.AreEqual(knight, importer.sourceAvatar, file);
            }
        }

        [Test, Description("Sedersi, salutare e battere il martello girano in loop; alzarsi e risvegliarsi no")]
        public void Animations_LoopOnlyWhatLasts()
        {
            Assert.IsTrue(Clip("Rig_Medium_Simulation.fbx", "Sit_Floor_Idle").isLooping);
            Assert.IsTrue(Clip("Rig_Medium_Simulation.fbx", "Waving").isLooping);
            Assert.IsTrue(Clip("Rig_Medium_Tools.fbx", "Hammering").isLooping);
            Assert.IsFalse(Clip("Rig_Medium_Simulation.fbx", "Sit_Floor_StandUp").isLooping);
            Assert.IsFalse(Clip("Rig_Medium_Special.fbx", "Skeletons_Awaken_Floor").isLooping);
            Assert.IsFalse(Clip("Rig_Medium_Special.fbx", "Skeletons_Death").isLooping);
        }

        [Test, Description("I personaggi degli abitanti hanno il loro scheletro e niente materiali dal file")]
        public void Characters_HaveTheirAvatars()
        {
            foreach (string name in new[] { "Mage", "Ranger", "Rogue" })
            {
                string path = KayKitImporter.ModelFolder + name + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                Assert.AreEqual(ModelImporterAnimationType.Generic, importer.animationType, path);
                Assert.AreEqual(ModelImporterMaterialImportMode.None, importer.materialImportMode, path);
                Assert.IsNotNull(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault(), path);
            }
        }
    }
}
