using NUnit.Framework;
using PlatformerUltra.Combat;
using UnityEditor;
using UnityEngine;

namespace PlatformerUltra.Gameplay.Tests
{
    public sealed class FactoryMachinePresentationTests
    {
        private const string PrefabFolder = "Assets/Game/Factory/Prefabs/";

        [Test]
        public void Presentation_ReplaysStartupAfterRepairAndStopsWhenBroken()
        {
            GameObject root = new GameObject("Machine Presentation Test");
            try
            {
                Health health = root.AddComponent<Health>();
                FactionMember faction = root.AddComponent<FactionMember>();
                Targetable targetable = root.AddComponent<Targetable>();
                FactoryMachineHealth machineHealth = root.AddComponent<FactoryMachineHealth>();
                machineHealth.Configure("Test Machine", 10, 3f, health, faction, targetable);

                GameObject rotorObject = new GameObject("Rotor");
                rotorObject.transform.SetParent(root.transform, false);
                FactoryMachinePresentation presentation = root.AddComponent<FactoryMachinePresentation>();
                presentation.Configure(
                    FactoryMachinePresentationKind.Generator,
                    machineHealth,
                    rotorObject.transform,
                    new[] { rotorObject.transform },
                    null,
                    null,
                    null,
                    null,
                    Color.cyan,
                    null,
                    null,
                    null,
                    null,
                    Vector3.up,
                    Vector3.up,
                    10f,
                    120f,
                    0f,
                    0f,
                    0.5f);

                Assert.That(presentation.State, Is.EqualTo(FactoryMachinePresentationState.Offline));
                machineHealth.SetProgressionActivated();
                Assert.That(presentation.State, Is.EqualTo(FactoryMachinePresentationState.Starting));

                presentation.SetWorkload(1f);
                presentation.AdvancePresentation(0.6f);
                Assert.That(presentation.State, Is.EqualTo(FactoryMachinePresentationState.Working));
                Assert.That(presentation.Workload, Is.EqualTo(1f));

                machineHealth.TakeDamage(new DamageInfo(10, null, Faction.Enemy, root.transform.position));
                Assert.That(presentation.State, Is.EqualTo(FactoryMachinePresentationState.Broken));

                Assert.That(machineHealth.TryRepair(), Is.True);
                Assert.That(presentation.State, Is.EqualTo(FactoryMachinePresentationState.Starting));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase("PF_Factory_Mine.prefab", 6.2f, 5.2f, true)]
        [TestCase("PF_Factory_Smelter.prefab", 5.4f, 4.2f, true)]
        [TestCase("PF_Factory_Generator.prefab", 5.6f, 4.4f, true)]
        [TestCase("PF_Factory_Assembler.prefab", 5.5f, 4.3f, true)]
        [TestCase("PF_Factory_Crusher.prefab", 4.5f, 3.7f, false)]
        public void GeneratedMachinePrefab_PreservesFootprintAndRequiredPresentation(
            string prefabName,
            float expectedWidth,
            float expectedDepth,
            bool expectsSharedPresentation)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + prefabName);
            Assert.That(prefab, Is.Not.Null, prefabName);

            BoxCollider[] colliders = prefab.GetComponents<BoxCollider>();
            Assert.That(colliders, Is.Not.Empty, prefabName + " needs root gameplay colliders");
            Assert.That(ContainsFootprint(colliders, expectedWidth, expectedDepth), Is.True, prefabName);

            if (expectsSharedPresentation)
            {
                Assert.That(prefab.transform.Find("Target Point"), Is.Not.Null, prefabName);
                Assert.That(prefab.GetComponent<FactoryMachinePresentation>(), Is.Not.Null, prefabName);
                Assert.That(prefab.GetComponent<FactoryMachineHealth>(), Is.Not.Null, prefabName);
            }
            else
            {
                Assert.That(prefab.GetComponent("FactoryCrusherVisual"), Is.Not.Null);
            }
        }

        [Test]
        public void GeneratedPortalPrefab_PreservesColliderConfigurationAndVisualDriver()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "PF_Factory_Portal.prefab");
            Assert.That(prefab, Is.Not.Null);
            BoxCollider[] colliders = prefab.GetComponents<BoxCollider>();
            Assert.That(colliders, Has.Length.EqualTo(2));
            Assert.That(colliders[0].size, Is.EqualTo(new Vector3(0.8f, 5.5f, 1.4f)));
            Assert.That(colliders[1].size, Is.EqualTo(new Vector3(0.8f, 5.5f, 1.4f)));
            Assert.That(prefab.GetComponent("FactoryPortalVisual"), Is.Not.Null);
        }

        private static bool ContainsFootprint(BoxCollider[] colliders, float width, float depth)
        {
            foreach (BoxCollider collider in colliders)
            {
                if (Mathf.Approximately(collider.size.x, width) &&
                    Mathf.Approximately(collider.size.z, depth))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
