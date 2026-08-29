using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class FactoryHudPresenter : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private StyleSheet _styleSheet;
        [SerializeField] private InteractionPromptPresenter _promptPresenter;
        [SerializeField] private FactoryObjectiveTerminal[] _objectives = System.Array.Empty<FactoryObjectiveTerminal>();
        [SerializeField] private DoubleJumpUpgradeStation _doubleJumpStation;
        [SerializeField] private MonoBehaviour _portalReceiverBehaviour;
        [SerializeField, Min(1f)] private float _tutorialTipDuration = 4f;
        [SerializeField, Min(0f)] private float _tutorialTipGap = 0.35f;
        [SerializeField] private bool _deferStartupTutorial;

        private Label _objectiveLabel;
        private VisualElement _brokenMachinesPanel;
        private Label _brokenMachinesLabel;
        private Label _portalCoreProgressLabel;
        private VisualElement _styledRoot;
        private IFactoryProductionReceiver _portalReceiver;
        private Coroutine _tutorialRoutine;
        private bool _eventsBound;

        private void Awake()
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            if (_promptPresenter == null)
            {
                _promptPresenter = GetComponent<InteractionPromptPresenter>();
            }

            ResolveElements();
            ResolvePortalReceiver();
        }

        private void OnEnable()
        {
            ResolveElements();
            ResolvePortalReceiver();
            BindEvents();
            Refresh();
            if (!_deferStartupTutorial)
            {
                StartTutorialSequence();
            }
        }

        private void OnDisable()
        {
            UnbindEvents();
            if (_tutorialRoutine != null)
            {
                StopCoroutine(_tutorialRoutine);
                _tutorialRoutine = null;
            }
        }

        public void Configure(
            UIDocument document,
            StyleSheet styleSheet,
            InteractionPromptPresenter promptPresenter,
            FactoryObjectiveTerminal[] objectives,
            DoubleJumpUpgradeStation doubleJumpStation,
            MonoBehaviour portalReceiverBehaviour)
        {
            UnbindEvents();
            _document = document;
            _styleSheet = styleSheet;
            _promptPresenter = promptPresenter;
            _objectives = objectives ?? System.Array.Empty<FactoryObjectiveTerminal>();
            _doubleJumpStation = doubleJumpStation;
            _portalReceiverBehaviour = portalReceiverBehaviour;
            ResolveElements();
            ResolvePortalReceiver();
            BindEvents();
            Refresh();

            if (isActiveAndEnabled && !_deferStartupTutorial)
            {
                StartTutorialSequence();
            }
        }

        public void SetStartupTutorialDeferred(bool deferred)
        {
            _deferStartupTutorial = deferred;
            if (!deferred || _tutorialRoutine == null)
            {
                return;
            }

            StopCoroutine(_tutorialRoutine);
            _tutorialRoutine = null;
            _promptPresenter?.HideStatus();
        }

        public void BeginStartupTutorial()
        {
            _deferStartupTutorial = false;
            StartTutorialSequence();
        }

        public void Refresh()
        {
            ResolveElements();
            RefreshObjective();
            RefreshBrokenMachines();
            RefreshPortalProgress();
        }

        private void ResolveElements()
        {
            if (_document == null || _document.rootVisualElement == null)
            {
                return;
            }

            VisualElement root = _document.rootVisualElement;
            if (_styleSheet != null && _styledRoot != root)
            {
                root.styleSheets.Add(_styleSheet);
                _styledRoot = root;
            }

            _objectiveLabel = root.Q<Label>("current-objective");
            _brokenMachinesPanel = root.Q<VisualElement>("broken-machines-panel");
            _brokenMachinesLabel = root.Q<Label>("broken-machines");
            _portalCoreProgressLabel = root.Q<Label>("portal-core-progress");
        }

        private void ResolvePortalReceiver()
        {
            _portalReceiver = _portalReceiverBehaviour as IFactoryProductionReceiver;
        }

        private void BindEvents()
        {
            if (_eventsBound || !isActiveAndEnabled)
            {
                return;
            }

            foreach (FactoryObjectiveTerminal terminal in _objectives)
            {
                if (terminal == null)
                {
                    continue;
                }

                terminal.Activated += HandleObjectiveChanged;
                terminal.MachineStateChanged += HandleMachineStateChanged;
            }

            if (_doubleJumpStation != null)
            {
                _doubleJumpStation.Installed += HandleDoubleJumpInstalled;
            }

            if (_portalReceiver != null)
            {
                _portalReceiver.ProgressChanged += HandlePortalProgressChanged;
            }

            _eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!_eventsBound)
            {
                return;
            }

            foreach (FactoryObjectiveTerminal terminal in _objectives)
            {
                if (terminal == null)
                {
                    continue;
                }

                terminal.Activated -= HandleObjectiveChanged;
                terminal.MachineStateChanged -= HandleMachineStateChanged;
            }

            if (_doubleJumpStation != null)
            {
                _doubleJumpStation.Installed -= HandleDoubleJumpInstalled;
            }

            if (_portalReceiver != null)
            {
                _portalReceiver.ProgressChanged -= HandlePortalProgressChanged;
            }

            _eventsBound = false;
        }

        private void RefreshObjective()
        {
            if (_objectiveLabel == null)
            {
                return;
            }

            for (int index = 0; index < _objectives.Length; index++)
            {
                FactoryObjectiveTerminal terminal = _objectives[index];
                if (terminal != null && !terminal.IsActivated)
                {
                    _objectiveLabel.text = index == 0
                        ? "Restore production: Activate the " + terminal.StationName
                        : "Activate the " + terminal.StationName;
                    return;
                }

                if (index == 1 && _doubleJumpStation != null && !_doubleJumpStation.IsInstalled)
                {
                    _objectiveLabel.text = "Install the Double Jump Module";
                    return;
                }
            }

            if (_doubleJumpStation != null && !_doubleJumpStation.IsInstalled)
            {
                _objectiveLabel.text = "Install the Double Jump Module";
                return;
            }

            if (_portalReceiver == null || _portalReceiver.DeliveredCount < _portalReceiver.RequiredCount)
            {
                _objectiveLabel.text = _portalReceiver != null && _portalReceiver.RequiredCount == 1
                    ? "Produce 1 Portal Core"
                    : "Produce the Portal Cores";
                return;
            }

            _objectiveLabel.text = "Enter the Portal";
        }

        private void RefreshBrokenMachines()
        {
            if (_brokenMachinesPanel == null || _brokenMachinesLabel == null)
            {
                return;
            }

            List<string> brokenMachineNames = new List<string>();
            foreach (FactoryObjectiveTerminal terminal in _objectives)
            {
                if (terminal != null && terminal.IsActivated && terminal.MachineState == FactoryMachineState.Broken)
                {
                    brokenMachineNames.Add(terminal.StationName);
                }
            }

            bool hasBrokenMachines = brokenMachineNames.Count > 0;
            _brokenMachinesPanel.style.display = hasBrokenMachines ? DisplayStyle.Flex : DisplayStyle.None;
            _brokenMachinesLabel.text = hasBrokenMachines
                ? string.Join("  •  ", brokenMachineNames)
                : string.Empty;
        }

        private void RefreshPortalProgress()
        {
            if (_portalCoreProgressLabel == null)
            {
                return;
            }

            int delivered = _portalReceiver != null ? _portalReceiver.DeliveredCount : 0;
            int required = _portalReceiver != null ? Mathf.Max(1, _portalReceiver.RequiredCount) : 3;
            _portalCoreProgressLabel.text = delivered + "/" + required;
        }

        private void HandleObjectiveChanged(FactoryObjectiveTerminal terminal)
        {
            Refresh();
        }

        private void HandleMachineStateChanged(FactoryObjectiveTerminal terminal, FactoryMachineState state)
        {
            Refresh();
        }

        private void HandlePortalProgressChanged(int delivered, int required)
        {
            Refresh();
        }

        private void HandleDoubleJumpInstalled()
        {
            RefreshObjective();
            _promptPresenter?.SetStatus("DOUBLE JUMP ONLINE\nPress Space again while airborne.", _tutorialTipDuration);
        }

        private void StartTutorialSequence()
        {
            if (_promptPresenter == null)
            {
                return;
            }

            if (_tutorialRoutine != null)
            {
                StopCoroutine(_tutorialRoutine);
            }

            _tutorialRoutine = StartCoroutine(ShowStartupTutorial());
        }

        private IEnumerator ShowStartupTutorial()
        {
            _promptPresenter.SetStatus("WASD  MOVE   •   MOUSE  LOOK   •   SHIFT  SPRINT", _tutorialTipDuration);
            yield return new WaitForSecondsRealtime(_tutorialTipDuration + _tutorialTipGap);
            _promptPresenter.SetStatus("SPACE  JUMP   •   LEFT CTRL  DASH", _tutorialTipDuration);
            yield return new WaitForSecondsRealtime(_tutorialTipDuration);
            _tutorialRoutine = null;
        }
    }
}
