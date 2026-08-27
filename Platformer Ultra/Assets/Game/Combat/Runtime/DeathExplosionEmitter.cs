using UnityEngine;

namespace PlatformerUltra.Combat
{
    [DisallowMultipleComponent]
    public sealed class DeathExplosionEmitter : MonoBehaviour
    {
        [SerializeField] private GameObject _effectPrefab;
        [SerializeField] private Transform _origin;
        [SerializeField, Min(0.1f)] private float _scale = 1f;

        public GameObject EffectPrefab => _effectPrefab;
        public GameObject LastSpawnedEffect { get; private set; }
        public int SpawnCount { get; private set; }

        public void Configure(GameObject effectPrefab, Transform origin, float scale = 1f)
        {
            _effectPrefab = effectPrefab;
            _origin = origin;
            _scale = Mathf.Max(0.1f, scale);
        }

        public bool Play()
        {
            if (_effectPrefab == null)
            {
                return false;
            }

            Transform effectOrigin = _origin != null ? _origin : transform;
            GameObject instance = Instantiate(
                _effectPrefab,
                effectOrigin.position,
                Quaternion.identity);
            instance.name = _effectPrefab.name + " (Death)";
            instance.transform.localScale = Vector3.one * _scale;
            LastSpawnedEffect = instance;
            SpawnCount++;

            DeathExplosionEffect effect = instance.GetComponent<DeathExplosionEffect>();
            if (effect != null)
            {
                effect.Play();
            }
            else
            {
                ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
                for (int index = 0; index < particleSystems.Length; index++)
                {
                    particleSystems[index].Play(true);
                }
            }

            return true;
        }
    }
}
