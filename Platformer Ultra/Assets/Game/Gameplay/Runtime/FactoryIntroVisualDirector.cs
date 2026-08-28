using UnityEngine;
using UnityEngine.Playables;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FactoryIntroVisualDirector : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _director;
        [SerializeField] private MonoBehaviour _saboteurAnimatorBehaviour;
        [SerializeField] private ParticleSystem _sabotageSparks;
        [SerializeField] private Light _warningLight;
        [SerializeField] private Renderer _networkDisplay;
        [SerializeField] private Renderer[] _hologramNodes = System.Array.Empty<Renderer>();
        [SerializeField] private Color _networkHealthy = new Color(0.1f, 1.5f, 2.2f);
        [SerializeField] private Color _networkCorrupted = new Color(2.1f, 0.12f, 3.2f);

        private MaterialPropertyBlock _propertyBlock;
        private ICinematicAttackPerformer _saboteurAnimator;
        private int _lastStrike = -1;
        private int _lastImpact = -1;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            ResolveAnimator();
        }

        private void Update()
        {
            double time = _director != null ? _director.time : 0d;
            UpdateNetwork((float)time);
            UpdateSabotage((float)time);
            UpdateHologram((float)time);
        }

        private void OnDisable()
        {
            _saboteurAnimator?.StopCinematicAttack();
        }

        public void Configure(
            PlayableDirector director,
            MonoBehaviour saboteurAnimatorBehaviour,
            ParticleSystem sabotageSparks,
            Light warningLight,
            Renderer networkDisplay,
            Renderer[] hologramNodes)
        {
            _director = director;
            _saboteurAnimatorBehaviour = saboteurAnimatorBehaviour;
            _sabotageSparks = sabotageSparks;
            _warningLight = warningLight;
            _networkDisplay = networkDisplay;
            _hologramNodes = hologramNodes ?? System.Array.Empty<Renderer>();
            ResolveAnimator();
        }

        private void UpdateNetwork(float time)
        {
            float corruption = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5f, 7.4f, time));
            Color color = Color.Lerp(_networkHealthy, _networkCorrupted, corruption);
            SetRendererColor(_networkDisplay, color);
            if (_warningLight != null)
            {
                float enabled = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5f, 5.5f, time));
                float pulse = 0.6f + Mathf.Abs(Mathf.Sin(time * 5.6f)) * 1.4f;
                _warningLight.intensity = enabled * pulse * 4f;
                _warningLight.color = Color.Lerp(new Color(0.2f, 0.9f, 1f), new Color(0.9f, 0.08f, 1f), corruption);
            }
        }

        private void UpdateSabotage(float time)
        {
            if (time < 8f || time >= 14f)
            {
                _lastStrike = -1;
                _lastImpact = -1;
                return;
            }

            float localTime = time - 8f;
            const float cycleDuration = 1.35f;
            int strike = Mathf.FloorToInt(localTime / cycleDuration);
            if (strike != _lastStrike)
            {
                _lastStrike = strike;
                _saboteurAnimator?.PlayCinematicAttack(0.92f);
            }

            float cycle = Mathf.Repeat(localTime, cycleDuration);
            if (cycle >= 0.47f && strike != _lastImpact)
            {
                _lastImpact = strike;
                if (_sabotageSparks != null)
                {
                    _sabotageSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    _sabotageSparks.Play(true);
                }
            }
        }

        private void UpdateHologram(float time)
        {
            for (int index = 0; index < _hologramNodes.Length; index++)
            {
                float activationTime = 14.1f + index * 0.62f;
                float activation = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(activationTime, activationTime + 0.35f, time));
                Color color = index == _hologramNodes.Length - 1
                    ? Color.Lerp(new Color(0.05f, 0.15f, 0.2f), new Color(1.4f, 0.15f, 2.4f), activation)
                    : Color.Lerp(new Color(0.04f, 0.14f, 0.18f), new Color(0.1f, 1.4f, 2.2f), activation);
                SetRendererColor(_hologramNodes[index], color);
            }
        }

        private void SetRendererColor(Renderer target, Color color)
        {
            if (target == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();

            target.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(EmissionColorId, color * 1.6f);
            target.SetPropertyBlock(_propertyBlock);
        }

        private void ResolveAnimator()
        {
            _saboteurAnimator = _saboteurAnimatorBehaviour as ICinematicAttackPerformer;
        }
    }
}
