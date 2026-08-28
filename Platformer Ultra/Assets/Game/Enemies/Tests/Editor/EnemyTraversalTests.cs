using NUnit.Framework;
using PlatformerUltra.Combat;
using PlatformerUltra.Gameplay;
using Unity.AI.Navigation;
using UnityEngine;

namespace PlatformerUltra.Enemies.Tests
{
    public sealed class EnemyTraversalTests
    {
        private const float ArmoredEnemyDiameter = 2.1f;

        [Test]
        public void LadderMotion_IsBidirectionalAndReachesBothEndpoints()
        {
            Vector3 lower = new Vector3(-2f, 1f, 3f);
            Vector3 upper = new Vector3(-1f, 7f, 4f);

            AssertVector(EnemyTraversalMotion.EvaluateLadder(lower, upper, 0f), lower);
            AssertVector(EnemyTraversalMotion.EvaluateLadder(lower, upper, 1f), upper);
            AssertVector(EnemyTraversalMotion.EvaluateLadder(upper, lower, 0f), upper);
            AssertVector(EnemyTraversalMotion.EvaluateLadder(upper, lower, 1f), lower);

            Vector3 ascendingMidpoint = EnemyTraversalMotion.EvaluateLadder(lower, upper, 0.5f);
            Vector3 descendingMidpoint = EnemyTraversalMotion.EvaluateLadder(upper, lower, 0.5f);
            AssertVector(ascendingMidpoint, descendingMidpoint);
        }

        [Test]
        public void JumpMotion_UsesConfiguredArcAndReturnsToLandingHeight()
        {
            Vector3 start = new Vector3(0f, 2f, 0f);
            Vector3 end = new Vector3(4f, 2f, 0f);

            AssertVector(EnemyTraversalMotion.EvaluateJump(start, end, 0f, 0.8f), start);
            AssertVector(EnemyTraversalMotion.EvaluateJump(start, end, 1f, 0.8f), end);
            AssertVector(
                EnemyTraversalMotion.EvaluateJump(start, end, 0.5f, 0.8f),
                new Vector3(2f, 2.8f, 0f));
        }

        [Test]
        public void InterruptedTraversal_RecoversToNearestEndpoint()
        {
            Vector3 start = Vector3.zero;
            Vector3 end = new Vector3(0f, 6f, 0f);

            AssertVector(
                EnemyTraversalMotion.GetNearestEndpoint(new Vector3(0f, 1f, 0f), start, end),
                start);
            AssertVector(
                EnemyTraversalMotion.GetNearestEndpoint(new Vector3(0f, 5f, 0f), start, end),
                end);
            AssertVector(
                EnemyTraversalMotion.GetNearestEndpoint(new Vector3(0f, 3f, 0f), start, end),
                start);
        }

        [Test]
        public void AccessRoute_StaysLockedUntilDeploymentCompletes()
        {
            RouteFixture fixture = CreateRouteFixture();
            try
            {
                Assert.That(fixture.Route.DeploymentProgress, Is.Zero);
                Assert.That(fixture.Link.enabled, Is.False);
                AssertVector(fixture.DeploymentPart.localPosition, Vector3.zero);

                fixture.Terminal.Activate();
                Assert.That(fixture.Link.enabled, Is.False);
                fixture.Route.Tick(0.675f);
                Assert.That(fixture.Route.DeploymentProgress, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(fixture.Link.enabled, Is.False);
                Assert.That(fixture.DeploymentPart.localPosition.y, Is.LessThan(0f));

                fixture.Route.Tick(0.675f);
                Assert.That(fixture.Route.IsDeployed, Is.True);
                Assert.That(fixture.Link.enabled, Is.True);
                AssertVector(fixture.DeploymentPart.localPosition, new Vector3(0f, -4f, 0f));
            }
            finally
            {
                Object.DestroyImmediate(fixture.Root);
            }
        }

        [Test]
        public void AccessRoute_RemainsDeployedAfterActivatedMachineBreaks()
        {
            RouteFixture fixture = CreateRouteFixture(createMachine: true);
            try
            {
                fixture.Terminal.Activate();
                fixture.Route.Tick(1.35f);
                Assert.That(fixture.Route.IsDeployed, Is.True);

                bool damaged = fixture.Machine.TakeDamage(
                    new DamageInfo(10, null, Faction.Enemy, fixture.Machine.transform.position));

                Assert.That(damaged, Is.True);
                Assert.That(fixture.Machine.State, Is.EqualTo(FactoryMachineState.Broken));
                Assert.That(fixture.Route.IsDeployed, Is.True);
                Assert.That(fixture.Link.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(fixture.Root);
            }
        }

        [Test]
        public void SharedLadderLink_ClearsArmoredEnemyWidth()
        {
            RouteFixture fixture = CreateRouteFixture();
            try
            {
                Assert.That(fixture.Link.width, Is.EqualTo(2.6f).Within(0.0001f));
                Assert.That(fixture.Link.width, Is.GreaterThanOrEqualTo(ArmoredEnemyDiameter));
                Assert.That(fixture.Link.bidirectional, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(fixture.Root);
            }
        }

        private static RouteFixture CreateRouteFixture(bool createMachine = false)
        {
            GameObject root = new GameObject("Enemy Route Test Root");
            FactoryMachineHealth machine = null;
            if (createMachine)
            {
                GameObject machineObject = new GameObject("Machine");
                machineObject.transform.SetParent(root.transform, false);
                Health health = machineObject.AddComponent<Health>();
                FactionMember factionMember = machineObject.AddComponent<FactionMember>();
                Targetable targetable = machineObject.AddComponent<Targetable>();
                machine = machineObject.AddComponent<FactoryMachineHealth>();
                machine.Configure(
                    "Test Machine",
                    10,
                    5f,
                    health,
                    factionMember,
                    targetable);
            }

            GameObject terminalObject = new GameObject("Terminal");
            terminalObject.transform.SetParent(root.transform, false);
            FactoryObjectiveTerminal terminal = terminalObject.AddComponent<FactoryObjectiveTerminal>();
            terminal.Configure(
                "Test Station",
                null,
                null,
                System.Array.Empty<GameObject>(),
                System.Array.Empty<Light>(),
                System.Array.Empty<PlatformerUltra.Factory.Conveyors.ConveyorBelt>(),
                false,
                machine);

            GameObject routeObject = new GameObject("Route");
            routeObject.transform.SetParent(root.transform, false);
            EnemyAccessRoute route = routeObject.AddComponent<EnemyAccessRoute>();

            GameObject linkObject = new GameObject("Ladder Link");
            linkObject.transform.SetParent(routeObject.transform, false);
            NavMeshLink link = linkObject.AddComponent<NavMeshLink>();
            link.width = 2.6f;
            link.bidirectional = true;
            EnemyTraversalLink traversal = linkObject.AddComponent<EnemyTraversalLink>();
            traversal.Configure(link, EnemyTraversalKind.Ladder, Vector3.forward);

            GameObject partObject = new GameObject("Deploying Ladder");
            partObject.transform.SetParent(routeObject.transform, false);
            partObject.transform.localPosition = new Vector3(0f, -4f, 0f);
            route.Configure(
                terminal,
                new[] { traversal },
                new[] { partObject.transform },
                new[] { Vector3.zero },
                1.35f);

            return new RouteFixture(
                root,
                terminal,
                machine,
                route,
                link,
                partObject.transform);
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThanOrEqualTo(0.0001f));
        }

        private sealed class RouteFixture
        {
            public RouteFixture(
                GameObject root,
                FactoryObjectiveTerminal terminal,
                FactoryMachineHealth machine,
                EnemyAccessRoute route,
                NavMeshLink link,
                Transform deploymentPart)
            {
                Root = root;
                Terminal = terminal;
                Machine = machine;
                Route = route;
                Link = link;
                DeploymentPart = deploymentPart;
            }

            public GameObject Root { get; }
            public FactoryObjectiveTerminal Terminal { get; }
            public FactoryMachineHealth Machine { get; }
            public EnemyAccessRoute Route { get; }
            public NavMeshLink Link { get; }
            public Transform DeploymentPart { get; }
        }
    }
}
