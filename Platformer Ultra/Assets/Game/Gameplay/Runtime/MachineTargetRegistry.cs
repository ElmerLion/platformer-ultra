using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MachineTargetRegistry : MonoBehaviour
    {
        private readonly List<IFactoryTarget> _targets = new List<IFactoryTarget>();
        private readonly List<FactoryMachineHealth> _machines = new List<FactoryMachineHealth>();

        public IReadOnlyList<IFactoryTarget> Targets => _targets;
        public IReadOnlyList<FactoryMachineHealth> Machines => _machines;
        public bool HasOperationalMachines => HasEligible(_machines);
        public bool HasEligibleTargets => HasEligible(_targets);

        public event Action Changed;

        public void Register(FactoryMachineHealth machine)
        {
            if (machine == null || _machines.Contains(machine))
            {
                return;
            }

            _machines.Add(machine);
            machine.StateChanged += HandleMachineStateChanged;
            RegisterTarget(machine);
        }

        public void Unregister(FactoryMachineHealth machine)
        {
            if (machine == null || !_machines.Remove(machine))
            {
                return;
            }

            machine.StateChanged -= HandleMachineStateChanged;
            UnregisterTarget(machine);
        }

        public void RegisterTarget(IFactoryTarget target)
        {
            if (target == null || _targets.Contains(target))
            {
                return;
            }

            _targets.Add(target);
            Changed?.Invoke();
        }

        public void UnregisterTarget(IFactoryTarget target)
        {
            if (target == null || !_targets.Remove(target))
            {
                return;
            }

            Changed?.Invoke();
        }

        public void NotifyChanged()
        {
            RemoveMissingTargets();
            Changed?.Invoke();
        }

        public FactoryMachineHealth FindNearestEligible(
            Vector3 origin,
            Predicate<FactoryMachineHealth> predicate = null)
        {
            return FindNearest(origin, _machines, predicate);
        }

        public IFactoryTarget FindNearestEligibleTarget(
            Vector3 origin,
            Predicate<IFactoryTarget> predicate = null)
        {
            return FindNearest(origin, _targets, predicate);
        }

        private void OnDestroy()
        {
            for (int index = 0; index < _machines.Count; index++)
            {
                FactoryMachineHealth machine = _machines[index];
                if (machine != null)
                {
                    machine.StateChanged -= HandleMachineStateChanged;
                }
            }

            _machines.Clear();
            _targets.Clear();
        }

        private static bool HasEligible<T>(IReadOnlyList<T> targets) where T : IFactoryTarget
        {
            for (int index = 0; index < targets.Count; index++)
            {
                T target = targets[index];
                if (target != null && target.IsEligibleTarget)
                {
                    return true;
                }
            }

            return false;
        }

        private static T FindNearest<T>(Vector3 origin, IReadOnlyList<T> targets, Predicate<T> predicate)
            where T : IFactoryTarget
        {
            T nearest = default;
            float nearestSqrDistance = float.PositiveInfinity;
            for (int index = 0; index < targets.Count; index++)
            {
                T target = targets[index];
                if (target == null || !target.IsEligibleTarget || target.Targetable == null ||
                    (predicate != null && !predicate(target)))
                {
                    continue;
                }

                float sqrDistance = (target.Targetable.TargetPoint.position - origin).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance)
                {
                    continue;
                }

                nearest = target;
                nearestSqrDistance = sqrDistance;
            }

            return nearest;
        }

        private void HandleMachineStateChanged(FactoryMachineHealth machine, FactoryMachineState state)
        {
            NotifyChanged();
        }

        private void RemoveMissingTargets()
        {
            for (int index = _machines.Count - 1; index >= 0; index--)
            {
                if (_machines[index] == null)
                {
                    _machines.RemoveAt(index);
                }
            }

            for (int index = _targets.Count - 1; index >= 0; index--)
            {
                IFactoryTarget target = _targets[index];
                if (target == null || target.Targetable == null)
                {
                    _targets.RemoveAt(index);
                }
            }
        }
    }
}
