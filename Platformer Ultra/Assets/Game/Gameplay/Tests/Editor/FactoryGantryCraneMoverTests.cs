using NUnit.Framework;
using UnityEngine;

namespace PlatformerUltra.Gameplay.Tests
{
    public sealed class FactoryGantryCraneMoverTests
    {
        [Test]
        public void AdvanceMovement_WaitsForGeneratorThenTraversesAndReverses()
        {
            GameObject craneObject = new GameObject("Crane");
            GameObject terminalObject = new GameObject("Generator Terminal");
            try
            {
                FactoryObjectiveTerminal generatorTerminal = terminalObject.AddComponent<FactoryObjectiveTerminal>();
                generatorTerminal.Configure("Generator", null, null, null, null, null);

                FactoryGantryCraneMover mover = craneObject.AddComponent<FactoryGantryCraneMover>();
                mover.Configure(
                    craneObject.transform,
                    generatorTerminal,
                    Vector3.left,
                    Vector3.right,
                    1f,
                    0.25f);

                mover.AdvanceMovement(0.5f);
                Assert.That(craneObject.transform.position, Is.EqualTo(Vector3.zero));

                generatorTerminal.Activate();
                mover.AdvanceMovement(1f);
                Assert.That(craneObject.transform.position, Is.EqualTo(Vector3.right));

                mover.AdvanceMovement(1.25f);
                Assert.That(craneObject.transform.position, Is.EqualTo(Vector3.zero));
            }
            finally
            {
                Object.DestroyImmediate(terminalObject);
                Object.DestroyImmediate(craneObject);
            }
        }
    }
}
