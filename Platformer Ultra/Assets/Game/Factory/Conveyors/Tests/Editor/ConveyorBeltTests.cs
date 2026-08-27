using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace PlatformerUltra.Factory.Conveyors.Tests
{
    public sealed class ConveyorBeltTests
    {
        private GameObject _root;
        private ConveyorEndpoint _start;
        private ConveyorEndpoint _end;
        private ConveyorBelt _belt;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Test Conveyor");
            _start = CreateEndpoint("Start", Vector3.zero, ConveyorEndpointKind.Output);
            _end = CreateEndpoint("End", new Vector3(3f, 4f, 0f), ConveyorEndpointKind.Input);
            _belt = _root.AddComponent<ConveyorBelt>();
            _belt.SetEndpoints(_start, _end);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void Rebuild_UsesArbitraryThreeDimensionalSpan()
        {
            Assert.That(_belt.SpanLength, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(_belt.Direction, Is.EqualTo(new Vector3(0.6f, 0.8f, 0f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(_root.transform.Find("Generated Conveyor/Conveyor Assembly"), Is.Not.Null);
        }

        [Test]
        public void Rebuild_CreatesOwnedSurfaceZone()
        {
            ConveyorSurfaceZone zone = _root.GetComponentInChildren<ConveyorSurfaceZone>();

            Assert.That(zone, Is.Not.Null);
            Assert.That(zone.Owner, Is.SameAs(_belt));
            Assert.That(zone.GetComponent<BoxCollider>().isTrigger, Is.True);
        }

        [Test]
        public void SurfaceVelocity_RespectsStateSpeedAndDirection()
        {
            _belt.SetSpeed(4f);
            _belt.SetOperatingState(ConveyorOperatingState.Online);

            Assert.That(_belt.SurfaceVelocity, Is.EqualTo(_belt.Direction * 4f).Using(Vector3ComparerWithEqualsOperator.Instance));

            _belt.SetReversed(true);
            Assert.That(_belt.SurfaceVelocity, Is.EqualTo(_belt.Direction * -4f).Using(Vector3ComparerWithEqualsOperator.Instance));

            _belt.SetOperatingState(ConveyorOperatingState.Sabotaged);
            Assert.That(_belt.SurfaceVelocity, Is.EqualTo(Vector3.zero).Using(Vector3ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void GetPathPosition_ReturnsPointBetweenEndpoints()
        {
            Vector3 midpoint = _belt.GetPathPosition(0.5f);
            Assert.That(midpoint, Is.EqualTo(new Vector3(1.5f, 2f, 0f)).Using(Vector3ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void EndpointKinds_ValidateOutputToInputFlow()
        {
            Assert.That(_start.CanFeed(_end), Is.True);
            Assert.That(_end.CanFeed(_start), Is.False);
        }

        private ConveyorEndpoint CreateEndpoint(string objectName, Vector3 position, ConveyorEndpointKind kind)
        {
            GameObject endpointObject = new GameObject(objectName);
            endpointObject.transform.SetParent(_root.transform, false);
            endpointObject.transform.position = position;
            ConveyorEndpoint endpoint = endpointObject.AddComponent<ConveyorEndpoint>();
            endpoint.Configure(kind);
            return endpoint;
        }
    }
}
