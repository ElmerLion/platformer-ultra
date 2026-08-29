using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace PlatformerUltra.Gameplay.Tests
{
    public sealed class HoverPlatformPresentationTests
    {
        [Test]
        public void Presentation_IdlesWithinBoundsAndLandingRecoilSettlesWithoutDrift()
        {
            GameObject root = new GameObject("Hover Platform");
            try
            {
                HoverPlatformPresentation presentation = BuildPresentation(root, out Transform visualRoot);
                float idlePeakTime = 0.25f / presentation.IdleFrequency;

                InvokeUpdatePresentation(presentation, idlePeakTime, 0f);
                Assert.That(visualRoot.localPosition.y, Is.EqualTo(presentation.IdleAmplitude).Within(0.0001f));

                presentation.ReactToLanding(50f, root.transform.TransformPoint(Vector3.right));
                InvokeUpdatePresentation(presentation, idlePeakTime, presentation.SettleDuration * 0.25f);

                Assert.That(presentation.LandingStrength, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(
                    Mathf.Abs(visualRoot.localPosition.y),
                    Is.LessThanOrEqualTo(presentation.IdleAmplitude + presentation.MaximumLandingDip + 0.0001f));
                Assert.That(
                    Quaternion.Angle(Quaternion.identity, visualRoot.localRotation),
                    Is.InRange(0.01f, presentation.MaximumTiltDegrees + 0.0001f));

                InvokeUpdatePresentation(presentation, idlePeakTime, presentation.SettleDuration);
                Assert.That(presentation.LandingStrength, Is.Zero);
                Assert.That(visualRoot.localPosition.y, Is.EqualTo(presentation.IdleAmplitude).Within(0.0001f));
                Assert.That(visualRoot.localRotation, Is.EqualTo(Quaternion.identity));

                presentation.ResetPresentation();
                Assert.That(visualRoot.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(visualRoot.localRotation, Is.EqualTo(Quaternion.identity));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Configure_ClampsTuningAndRepeatedLandingsDoNotAccumulateOffsets()
        {
            GameObject root = new GameObject("Hover Platform");
            try
            {
                HoverPlatformPresentation presentation = BuildPresentation(root, out Transform visualRoot);
                presentation.Configure(
                    visualRoot,
                    root.GetComponent<BoxCollider>(),
                    presentation.RepulsorRing,
                    presentation.HoverLight,
                    presentation.HoverParticles,
                    -0.25f,
                    -1f,
                    -2f,
                    5f,
                    4f,
                    -3f,
                    -4f,
                    -1f);

                Assert.That(presentation.IdleAmplitude, Is.Zero);
                Assert.That(presentation.IdleFrequency, Is.Zero);
                Assert.That(presentation.MaximumLandingDip, Is.Zero);
                Assert.That(presentation.MaximumTiltDegrees, Is.Zero);
                Assert.That(presentation.SettleDuration, Is.EqualTo(0.05f).Within(0.0001f));

                presentation.Configure(
                    visualRoot,
                    root.GetComponent<BoxCollider>(),
                    presentation.RepulsorRing,
                    presentation.HoverLight,
                    presentation.HoverParticles,
                    0f);
                presentation.ReactToLanding(12f, root.transform.TransformPoint(Vector3.forward));
                InvokeUpdatePresentation(presentation, 0f, 0.125f);
                presentation.ReactToLanding(12f, root.transform.TransformPoint(Vector3.back));
                InvokeUpdatePresentation(presentation, 0f, 0.125f);

                Assert.That(
                    Mathf.Abs(visualRoot.localPosition.y),
                    Is.LessThanOrEqualTo(presentation.IdleAmplitude + presentation.MaximumLandingDip + 0.0001f));

                InvokeLifecycle(presentation, "OnDisable");
                Assert.That(visualRoot.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(visualRoot.localRotation, Is.EqualTo(Quaternion.identity));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LandingTrigger_TracksPlayerAndUnsubscribesOnExitOrDisable()
        {
            GameObject root = new GameObject("Hover Platform");
            GameObject player = new GameObject("Player");
            try
            {
                HoverPlatformPresentation presentation = BuildPresentation(root, out _);
                CharacterController characterController = player.AddComponent<CharacterController>();
                player.AddComponent<ThirdPersonPlayerController>();

                InvokeTrigger(presentation, "OnTriggerEnter", characterController);
                Assert.That(presentation.IsTrackingPlayer, Is.True);

                InvokeTrigger(presentation, "OnTriggerExit", characterController);
                Assert.That(presentation.IsTrackingPlayer, Is.False);

                InvokeTrigger(presentation, "OnTriggerEnter", characterController);
                InvokeLifecycle(presentation, "OnDisable");
                Assert.That(presentation.IsTrackingPlayer, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(root);
            }
        }

        private static HoverPlatformPresentation BuildPresentation(
            GameObject root,
            out Transform visualRoot)
        {
            visualRoot = new GameObject("Visual Body").transform;
            visualRoot.SetParent(root.transform, false);

            Transform ring = new GameObject("Ring").transform;
            ring.SetParent(visualRoot, false);

            GameObject lightObject = new GameObject("Light");
            lightObject.transform.SetParent(visualRoot, false);
            Light hoverLight = lightObject.AddComponent<Light>();
            hoverLight.intensity = 2f;

            GameObject particlesObject = new GameObject("Particles");
            particlesObject.transform.SetParent(visualRoot, false);
            ParticleSystem particles = particlesObject.AddComponent<ParticleSystem>();

            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            HoverPlatformPresentation presentation = root.AddComponent<HoverPlatformPresentation>();
            presentation.Configure(visualRoot, trigger, ring, hoverLight, particles, 0f);
            return presentation;
        }

        private static void InvokeUpdatePresentation(
            HoverPlatformPresentation presentation,
            float scaledTime,
            float deltaTime)
        {
            MethodInfo method = typeof(HoverPlatformPresentation).GetMethod(
                "UpdatePresentation",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(presentation, new object[] { scaledTime, deltaTime });
        }

        private static void InvokeTrigger(
            HoverPlatformPresentation presentation,
            string methodName,
            Collider collider)
        {
            MethodInfo method = typeof(HoverPlatformPresentation).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(presentation, new object[] { collider });
        }

        private static void InvokeLifecycle(HoverPlatformPresentation presentation, string methodName)
        {
            MethodInfo method = typeof(HoverPlatformPresentation).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(presentation, null);
        }
    }
}
