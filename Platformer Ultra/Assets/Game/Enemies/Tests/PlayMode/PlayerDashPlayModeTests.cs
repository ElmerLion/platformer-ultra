using System.Collections;
using NUnit.Framework;
using PlatformerUltra.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlatformerUltra.Enemies.Tests.PlayMode
{
    public sealed class PlayerDashPlayModeTests
    {
        [UnityTest]
        public IEnumerator Dash_WorksOnGroundAndInAirWithStableDistanceAndCooldown()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject player = new GameObject("Dash Test Player");
            GameObject cameraObject = new GameObject("Dash Test Camera");
            PlayerMovementSettings settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
            try
            {
                floor.transform.position = new Vector3(0f, -0.5f, 0f);
                floor.transform.localScale = new Vector3(20f, 1f, 20f);

                CharacterController characterController = player.AddComponent<CharacterController>();
                characterController.height = 2f;
                characterController.center = Vector3.up;
                ThirdPersonPlayerController controller = player.AddComponent<ThirdPersonPlayerController>();
                controller.Configure(
                    characterController,
                    cameraObject.transform,
                    null,
                    settings,
                    null,
                    null,
                    null,
                    null);

                bool wasAirborne = true;
                controller.Dashed += (_, airborne) => wasAirborne = airborne;
                yield return null;

                Assert.That(controller.IsGrounded, Is.True);
                Vector3 groundStart = player.transform.position;
                Assert.That(controller.TryStartDash(Vector3.forward), Is.True);
                yield return new WaitForSeconds(settings.DashDuration + 0.05f);

                float groundDistance = Vector3.ProjectOnPlane(
                    player.transform.position - groundStart,
                    Vector3.up).magnitude;
                Assert.That(wasAirborne, Is.False);
                Assert.That(groundDistance, Is.EqualTo(settings.DashDistance).Within(0.2f));
                Assert.That(controller.TryStartDash(Vector3.forward), Is.False, "Dash ignored its cooldown.");

                yield return new WaitForSeconds(settings.DashCooldown);
                characterController.enabled = false;
                player.transform.position = new Vector3(0f, 3f, 0f);
                characterController.enabled = true;
                yield return null;

                Assert.That(controller.IsGrounded, Is.False);
                Assert.That(controller.TryStartDash(Vector3.right), Is.True);
                Assert.That(wasAirborne, Is.True);
            }
            finally
            {
                Object.Destroy(settings);
                Object.Destroy(cameraObject);
                Object.Destroy(player);
                Object.Destroy(floor);
            }
        }
    }
}
