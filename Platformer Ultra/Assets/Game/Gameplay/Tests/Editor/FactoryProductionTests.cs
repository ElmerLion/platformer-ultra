using NUnit.Framework;
using PlatformerUltra.Factory.Conveyors;
using UnityEngine;

namespace PlatformerUltra.Gameplay.Tests
{
    public sealed class FactoryProductionTests
    {
        [Test]
        public void ConveyorConnection_SourceThenDestinationBuildsAndStartsBelt()
        {
            GameObject root = new GameObject("Production Route");
            GameObject sourceTerminalObject = new GameObject("Source Terminal");
            GameObject destinationTerminalObject = new GameObject("Destination Terminal");
            try
            {
                FactoryObjectiveTerminal sourceTerminal = CreateActiveTerminal(sourceTerminalObject, "Mine");
                FactoryObjectiveTerminal destinationTerminal = CreateActiveTerminal(destinationTerminalObject, "Smelter");
                ConveyorBelt belt = CreateBelt(root.transform);
                GameObject marker = new GameObject("Destination Arrow");
                marker.transform.SetParent(root.transform, false);

                FactoryConveyorConnection connection = root.AddComponent<FactoryConveyorConnection>();
                connection.Configure(
                    "Mine to Smelter",
                    sourceTerminal,
                    destinationTerminal,
                    new[] { belt },
                    null,
                    null,
                    marker);

                GameObject sourcePointObject = new GameObject("Source Socket");
                sourcePointObject.transform.SetParent(root.transform, false);
                ConveyorConnectionPoint sourcePoint = sourcePointObject.AddComponent<ConveyorConnectionPoint>();
                sourcePoint.Configure(connection, true);

                GameObject destinationPointObject = new GameObject("Destination Socket");
                destinationPointObject.transform.SetParent(root.transform, false);
                ConveyorConnectionPoint destinationPoint = destinationPointObject.AddComponent<ConveyorConnectionPoint>();
                destinationPoint.Configure(connection, false);

                Assert.That(belt.gameObject.activeSelf, Is.False);
                Assert.That(marker.activeSelf, Is.False);
                Assert.That(destinationPoint.CanInteract(null), Is.False);

                sourcePoint.Interact(null);

                Assert.That(connection.State, Is.EqualTo(FactoryConveyorConnectionState.AwaitingDestination));
                Assert.That(marker.activeSelf, Is.True);
                Assert.That(destinationPoint.CanInteract(null), Is.True);

                destinationPoint.Interact(null);

                Assert.That(connection.IsBuilt, Is.True);
                Assert.That(connection.IsOperational, Is.True);
                Assert.That(belt.gameObject.activeSelf, Is.True);
                Assert.That(belt.OperatingState, Is.EqualTo(ConveyorOperatingState.Online));
                Assert.That(marker.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(destinationTerminalObject);
                Object.DestroyImmediate(sourceTerminalObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ProductionLine_ConvertsOreToPortalComponentAcrossBuiltRoutes()
        {
            GameObject root = new GameObject("Production Test");
            GameObject mineTerminalObject = new GameObject("Mine Terminal");
            GameObject smelterTerminalObject = new GameObject("Smelter Terminal");
            GameObject assemblerTerminalObject = new GameObject("Assembler Terminal");
            try
            {
                FactoryObjectiveTerminal mine = CreateActiveTerminal(mineTerminalObject, "Mine");
                FactoryObjectiveTerminal smelter = CreateActiveTerminal(smelterTerminalObject, "Smelter");
                FactoryObjectiveTerminal assembler = CreateActiveTerminal(assemblerTerminalObject, "Assembler");
                FactoryConveyorConnection mineToSmelter = CreateBuiltConnection(root.transform, mine, smelter);
                FactoryConveyorConnection smelterToAssembler = CreateBuiltConnection(root.transform, smelter, assembler);
                FactoryConveyorConnection assemblerToPortal = CreateBuiltConnection(root.transform, assembler, null);
                ProductionReceiverStub receiver = root.AddComponent<ProductionReceiverStub>();

                FactoryProductionLine productionLine = root.AddComponent<FactoryProductionLine>();
                productionLine.Configure(
                    mine,
                    smelter,
                    assembler,
                    mineToSmelter,
                    smelterToAssembler,
                    assemblerToPortal,
                    null,
                    null,
                    null,
                    root.transform,
                    receiver,
                    0.1f,
                    0.1f,
                    0.1f);

                productionLine.AdvanceProduction(0.11f);

                Assert.That(productionLine.StoredOre, Is.Zero);
                Assert.That(productionLine.StoredIngots, Is.Zero);
                Assert.That(productionLine.DeliveredPortalComponents, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(assemblerTerminalObject);
                Object.DestroyImmediate(smelterTerminalObject);
                Object.DestroyImmediate(mineTerminalObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ProductionLine_DrivesMineAndGeneratorWorkloadWhileMineTimerAdvances()
        {
            GameObject root = new GameObject("Production Presentation Test");
            GameObject mineTerminalObject = new GameObject("Mine Terminal");
            GameObject smelterTerminalObject = new GameObject("Smelter Terminal");
            GameObject assemblerTerminalObject = new GameObject("Assembler Terminal");
            try
            {
                FactoryObjectiveTerminal mine = CreateActiveTerminal(mineTerminalObject, "Mine");
                FactoryObjectiveTerminal smelter = CreateActiveTerminal(smelterTerminalObject, "Smelter");
                FactoryObjectiveTerminal assembler = CreateActiveTerminal(assemblerTerminalObject, "Assembler");
                FactoryConveyorConnection mineToSmelter = CreateBuiltConnection(root.transform, mine, smelter);
                FactoryConveyorConnection smelterToAssembler = CreateBuiltConnection(root.transform, smelter, assembler);
                FactoryConveyorConnection assemblerToPortal = CreateBuiltConnection(root.transform, assembler, null);

                FactoryMachinePresentation minePresentation = new GameObject("Mine Presentation").AddComponent<FactoryMachinePresentation>();
                FactoryMachinePresentation smelterPresentation = new GameObject("Smelter Presentation").AddComponent<FactoryMachinePresentation>();
                FactoryMachinePresentation generatorPresentation = new GameObject("Generator Presentation").AddComponent<FactoryMachinePresentation>();
                FactoryMachinePresentation assemblerPresentation = new GameObject("Assembler Presentation").AddComponent<FactoryMachinePresentation>();
                minePresentation.transform.SetParent(root.transform, false);
                smelterPresentation.transform.SetParent(root.transform, false);
                generatorPresentation.transform.SetParent(root.transform, false);
                assemblerPresentation.transform.SetParent(root.transform, false);

                FactoryProductionLine productionLine = root.AddComponent<FactoryProductionLine>();
                productionLine.Configure(
                    mine,
                    smelter,
                    assembler,
                    mineToSmelter,
                    smelterToAssembler,
                    assemblerToPortal,
                    null,
                    null,
                    null,
                    root.transform,
                    null,
                    1f,
                    1f,
                    1f);
                productionLine.BindPresentation(
                    minePresentation,
                    smelterPresentation,
                    generatorPresentation,
                    assemblerPresentation);

                productionLine.AdvanceProduction(0.25f);

                Assert.That(minePresentation.Workload, Is.GreaterThan(0.45f));
                Assert.That(generatorPresentation.Workload, Is.EqualTo(minePresentation.Workload).Within(0.001f));
                Assert.That(smelterPresentation.Workload, Is.Zero);
                Assert.That(assemblerPresentation.Workload, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(assemblerTerminalObject);
                Object.DestroyImmediate(smelterTerminalObject);
                Object.DestroyImmediate(mineTerminalObject);
                Object.DestroyImmediate(root);
            }
        }

        private static FactoryObjectiveTerminal CreateActiveTerminal(GameObject target, string name)
        {
            FactoryObjectiveTerminal terminal = target.AddComponent<FactoryObjectiveTerminal>();
            terminal.Configure(name, null, null, null, null, null);
            terminal.Activate();
            return terminal;
        }

        private static FactoryConveyorConnection CreateBuiltConnection(
            Transform parent,
            FactoryObjectiveTerminal source,
            FactoryObjectiveTerminal destination)
        {
            GameObject target = new GameObject("Built Connection");
            target.transform.SetParent(parent, false);
            FactoryConveyorConnection connection = target.AddComponent<FactoryConveyorConnection>();
            connection.Configure("Test Route", source, destination, null, null, null, null);
            connection.SelectSource(out _);
            connection.BuildFromDestination(out _);
            return connection;
        }

        private static ConveyorBelt CreateBelt(Transform parent)
        {
            GameObject beltObject = new GameObject("Conveyor Belt");
            beltObject.transform.SetParent(parent, false);
            ConveyorEndpoint start = CreateEndpoint(beltObject.transform, "Start", Vector3.zero, ConveyorEndpointKind.Output);
            ConveyorEndpoint end = CreateEndpoint(beltObject.transform, "End", Vector3.forward * 3f, ConveyorEndpointKind.Input);
            ConveyorBelt belt = beltObject.AddComponent<ConveyorBelt>();
            belt.SetEndpoints(start, end);
            return belt;
        }

        private static ConveyorEndpoint CreateEndpoint(
            Transform parent,
            string name,
            Vector3 position,
            ConveyorEndpointKind kind)
        {
            GameObject target = new GameObject(name);
            target.transform.SetParent(parent, false);
            target.transform.position = position;
            ConveyorEndpoint endpoint = target.AddComponent<ConveyorEndpoint>();
            endpoint.Configure(kind);
            return endpoint;
        }
    }

    public sealed class ProductionReceiverStub : MonoBehaviour, IFactoryProductionReceiver
    {
        public event System.Action<int, int> ProgressChanged;

        public int DeliveredCount { get; private set; }
        public int RequiredCount => 3;

        public void ReceivePortalComponent()
        {
            DeliveredCount++;
            ProgressChanged?.Invoke(DeliveredCount, RequiredCount);
        }
    }
}
