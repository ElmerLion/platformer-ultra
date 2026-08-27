using NUnit.Framework;
using PlatformerUltra.Combat;
using UnityEngine;

namespace PlatformerUltra.Gameplay.Tests
{
    public sealed class GameplayFeedbackEffectsTests
    {
        [Test]
        public void CameraShake_NearImpulseProducesMotionAndDistantImpulseIsRejected()
        {
            GameObject cameraObject = new GameObject("Camera Shake");
            try
            {
                CameraShakeController shake = cameraObject.AddComponent<CameraShakeController>();
                shake.PlayAt(Vector3.zero, 0.2f, 0.3f, 24f, 10f);
                shake.Sample(0.01f, out Vector3 position, out Vector3 rotation);

                Assert.That(shake.ImpulseCount, Is.EqualTo(1));
                Assert.That(position.sqrMagnitude + rotation.sqrMagnitude, Is.GreaterThan(0f));

                shake.PlayAt(Vector3.right * 100f, 0.5f, 0.5f, 20f, 10f);
                Assert.That(shake.ImpulseCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void MachineBreakPresentation_FiresOnceFromActualMachineDeath()
        {
            GameObject machineObject = new GameObject("Machine");
            GameObject cameraObject = new GameObject("Camera");
            try
            {
                Health health = machineObject.AddComponent<Health>();
                FactionMember faction = machineObject.AddComponent<FactionMember>();
                Targetable targetable = machineObject.AddComponent<Targetable>();
                FactoryMachineHealth machine = machineObject.AddComponent<FactoryMachineHealth>();
                GameObject targetPointObject = new GameObject("Target Point");
                targetPointObject.transform.SetParent(machineObject.transform, false);
                TargetPoint targetPoint = targetPointObject.AddComponent<TargetPoint>();
                targetable.Configure(faction, targetPoint, machine, true);
                machine.Configure("Machine", 10, 5f, health, faction, targetable);
                machine.SetProgressionActivated();

                CameraShakeController shake = cameraObject.AddComponent<CameraShakeController>();
                MachineBreakPresentation presentation = machineObject.AddComponent<MachineBreakPresentation>();
                presentation.Configure(machine, null, null, null, shake);

                Assert.That(machine.TakeDamage(new DamageInfo(
                    10,
                    null,
                    Faction.Enemy,
                    targetPoint.transform.position)), Is.True);
                Assert.That(machine.TakeDamage(new DamageInfo(
                    1,
                    null,
                    Faction.Enemy,
                    targetPoint.transform.position)), Is.False);
                Assert.That(presentation.BreakPresentationCount, Is.EqualTo(1));
                Assert.That(shake.ImpulseCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(machineObject);
            }
        }
    }
}
