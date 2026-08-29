using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using PlatformerUltra.Enemies;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace PlatformerUltra.Audio.Editor
{
    public static class GameAudioAssetFactory
    {
        public const string MixerPath = "Assets/Game/Audio/AM_Game.mixer";
        public const string MusicGroupName = "Music";
        public const string SfxGroupName = "SFX";
        public const string DialogueGroupName = "Dialogue";

        private const string LegacyMixerPath = "Assets/Game/Audio/AM_FactoryIntro.mixer";
        private const string SfxRoot = "Assets/Game/Audio/SFX";
        private const int SampleRate = 48000;

        public static readonly string[] PlayerFootstepPaths = Paths("Player/Footstep", 4);
        public static readonly string[] PlayerJumpPaths = Paths("Player/Jump", 2);
        public static readonly string[] PlayerDoubleJumpPaths = Paths("Player/DoubleJump", 2);
        public static readonly string[] PlayerDashPaths = Paths("Player/Dash", 2);
        public static readonly string[] PlayerLightLandingPaths = Paths("Player/LandLight", 2);
        public static readonly string[] PlayerHeavyLandingPaths = Paths("Player/LandHeavy", 2);
        public static readonly string[] SaboteurFootstepPaths = Paths("Enemies/SaboteurFootstep", 4);
        public static readonly string[] ArmoredFootstepPaths = Paths("Enemies/ArmoredFootstep", 4);
        public static readonly string[] SaboteurAttackStartPaths = Paths("Enemies/SaboteurCutter", 2);
        public static readonly string[] SaboteurAttackImpactPaths = Paths("Enemies/SaboteurImpact", 2);
        public static readonly string[] ArmoredAttackStartPaths = Paths("Enemies/ArmoredWindup", 2);
        public static readonly string[] ArmoredAttackImpactPaths = Paths("Enemies/ArmoredStrike", 2);
        public static readonly string[] ArmoredSpecialStartPaths = Paths("Enemies/ArmoredLeap", 2);
        public static readonly string[] ArmoredSpecialImpactPaths = Paths("Enemies/ArmoredSlam", 2);
        public static readonly string[] DroneAttackStartPaths = Paths("Enemies/DroneCharge", 2);
        public static readonly string[] DroneAttackImpactPaths = Paths("Enemies/DroneDischarge", 2);
        public const string DroneLoopPath = SfxRoot + "/Enemies/DronePropulsion.wav";

        private enum SoundKind
        {
            PlayerStep,
            Jump,
            DoubleJump,
            Dash,
            LightLanding,
            HeavyLanding,
            SaboteurStep,
            ArmoredStep,
            SaboteurCutter,
            SaboteurImpact,
            ArmoredWindup,
            ArmoredStrike,
            ArmoredLeap,
            ArmoredSlam,
            DroneCharge,
            DroneDischarge,
            DroneLoop
        }

        [MenuItem("Tools/Platformer Ultra/Build Shared Audio Assets")]
        public static void BuildAll()
        {
            GenerateMechanicalClips();
            BuildOrUpdateMixer();
            WireEnemyPrefabs();
            RouteGamePrefabSources();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built the shared mixer, original mechanical SFX, and enemy audio wiring.");
        }

        [MenuItem("Tools/Platformer Ultra/Audio/Regenerate Saboteur Blade SFX")]
        public static void RegenerateSaboteurBladeSfx()
        {
            EnsureFolder(SfxRoot);
            EnsureFolder(SfxRoot + "/Enemies");
            GeneratePool(SaboteurAttackStartPaths, SoundKind.SaboteurCutter, 0.26f, 901);
            GeneratePool(SaboteurAttackImpactPaths, SoundKind.SaboteurImpact, 0.18f, 1001);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (string path in SaboteurAttackStartPaths.Concat(SaboteurAttackImpactPaths))
            {
                ConfigureImporter(path, false);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Regenerated the Saboteur knife-hand slice and impact SFX.");
        }

        public static AudioMixer BuildOrUpdateMixer()
        {
            EnsureFolder("Assets/Game/Audio");
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
            {
                AudioMixer legacy = AssetDatabase.LoadAssetAtPath<AudioMixer>(LegacyMixerPath);
                if (legacy != null)
                {
                    string moveError = AssetDatabase.MoveAsset(LegacyMixerPath, MixerPath);
                    if (!string.IsNullOrEmpty(moveError))
                    {
                        throw new InvalidOperationException(moveError);
                    }

                    mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
                }
            }

            if (mixer == null)
            {
                Type controllerType = FindEditorType("UnityEditor.Audio.AudioMixerController");
                MethodInfo createMixer = controllerType?.GetMethod(
                    "CreateMixerControllerAtPath",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                mixer = createMixer?.Invoke(null, new object[] { MixerPath }) as AudioMixer;
            }

            if (mixer == null)
            {
                throw new InvalidOperationException("Unity could not create the shared game AudioMixer.");
            }

            object controller = mixer;
            AudioMixerGroup master = mixer.FindMatchingGroups("Master").First();
            AudioMixerGroup music = EnsureGroup(controller, master, MusicGroupName);
            AudioMixerGroup sfx = EnsureGroup(controller, master, SfxGroupName);
            AudioMixerGroup dialogue = mixer.FindMatchingGroups(DialogueGroupName).FirstOrDefault();
            if (dialogue == null)
            {
                dialogue = EnsureGroup(controller, sfx, DialogueGroupName);
            }
            else
            {
                ReparentGroup(master, sfx, dialogue);
            }

            EnsureDialogueCompressor(mixer, dialogue);
            SetExposedVolumes(controller, master, music, sfx);
            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
            return mixer;
        }

        public static AudioMixerGroup GetGroup(string groupName)
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath) ?? BuildOrUpdateMixer();
            return mixer.FindMatchingGroups(groupName).FirstOrDefault();
        }

        public static AudioClip[] LoadClips(IEnumerable<string> paths)
        {
            return paths.Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(clip => clip != null).ToArray();
        }

        public static void ConfigureEnemyAudio(
            GameObject root,
            EnemyDefinition definition,
            MonoBehaviour motorBehaviour,
            ProceduralEnemyAnimator proceduralAnimator,
            EnemyAttackController attackController)
        {
            if (root == null || definition == null)
            {
                return;
            }

            EnemyAudioPresentation presentation = root.GetComponent<EnemyAudioPresentation>();
            if (presentation == null)
            {
                presentation = root.AddComponent<EnemyAudioPresentation>();
            }

            AudioSource oneShot = presentation.OneShotSource;
            if (oneShot == null)
            {
                oneShot = root.AddComponent<AudioSource>();
            }

            AudioSource loop = presentation.LoopSource;
            if (definition.Archetype == EnemyArchetype.Drone && loop == null)
            {
                loop = root.AddComponent<AudioSource>();
            }

            AudioMixerGroup sfxGroup = GetGroup(SfxGroupName);
            oneShot.outputAudioMixerGroup = sfxGroup;
            if (loop != null)
            {
                loop.outputAudioMixerGroup = sfxGroup;
            }

            AudioClip[] footsteps = Array.Empty<AudioClip>();
            AudioClip[] normalStart;
            AudioClip[] normalImpact;
            AudioClip[] specialStart = Array.Empty<AudioClip>();
            AudioClip[] specialImpact = Array.Empty<AudioClip>();
            AudioClip movementLoop = null;
            switch (definition.Archetype)
            {
                case EnemyArchetype.Saboteur:
                    footsteps = LoadClips(SaboteurFootstepPaths);
                    normalStart = LoadClips(SaboteurAttackStartPaths);
                    normalImpact = LoadClips(SaboteurAttackImpactPaths);
                    break;
                case EnemyArchetype.Armored:
                    footsteps = LoadClips(ArmoredFootstepPaths);
                    normalStart = LoadClips(ArmoredAttackStartPaths);
                    normalImpact = LoadClips(ArmoredAttackImpactPaths);
                    specialStart = LoadClips(ArmoredSpecialStartPaths);
                    specialImpact = LoadClips(ArmoredSpecialImpactPaths);
                    break;
                default:
                    normalStart = LoadClips(DroneAttackStartPaths);
                    normalImpact = LoadClips(DroneAttackImpactPaths);
                    movementLoop = AssetDatabase.LoadAssetAtPath<AudioClip>(DroneLoopPath);
                    break;
            }

            presentation.Configure(
                definition,
                motorBehaviour,
                proceduralAnimator,
                attackController,
                oneShot,
                loop,
                footsteps,
                normalStart,
                normalImpact,
                specialStart,
                specialImpact,
                movementLoop);
            EditorUtility.SetDirty(presentation);
        }

        public static void WireEnemyPrefabs()
        {
            string[] paths =
            {
                "Assets/Game/Enemies/Prefabs/PF_Enemy_Drone.prefab",
                "Assets/Game/Enemies/Prefabs/PF_Enemy_Saboteur.prefab",
                "Assets/Game/Enemies/Prefabs/PF_Enemy_Armored.prefab"
            };

            foreach (string path in paths)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    EnemyHealth health = root.GetComponent<EnemyHealth>();
                    MonoBehaviour motor = root.GetComponents<MonoBehaviour>().FirstOrDefault(item => item is IEnemyMotor);
                    ConfigureEnemyAudio(
                        root,
                        health != null ? health.Definition : null,
                        motor,
                        root.GetComponentInChildren<ProceduralEnemyAnimator>(true),
                        root.GetComponent<EnemyAttackController>());
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        public static void RouteGamePrefabSources()
        {
            AudioMixerGroup sfxGroup = GetGroup(SfxGroupName);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                try
                {
                    foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
                    {
                        if (source.outputAudioMixerGroup == null)
                        {
                            source.outputAudioMixerGroup = sfxGroup;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static void GenerateMechanicalClips()
        {
            EnsureFolder(SfxRoot);
            EnsureFolder(SfxRoot + "/Player");
            EnsureFolder(SfxRoot + "/Enemies");
            GeneratePool(PlayerFootstepPaths, SoundKind.PlayerStep, 0.2f, 101);
            GeneratePool(PlayerJumpPaths, SoundKind.Jump, 0.42f, 201);
            GeneratePool(PlayerDoubleJumpPaths, SoundKind.DoubleJump, 0.5f, 301);
            GeneratePool(PlayerDashPaths, SoundKind.Dash, 0.58f, 401);
            GeneratePool(PlayerLightLandingPaths, SoundKind.LightLanding, 0.28f, 501);
            GeneratePool(PlayerHeavyLandingPaths, SoundKind.HeavyLanding, 0.52f, 601);
            GeneratePool(SaboteurFootstepPaths, SoundKind.SaboteurStep, 0.18f, 701);
            GeneratePool(ArmoredFootstepPaths, SoundKind.ArmoredStep, 0.46f, 801);
            GeneratePool(SaboteurAttackStartPaths, SoundKind.SaboteurCutter, 0.26f, 901);
            GeneratePool(SaboteurAttackImpactPaths, SoundKind.SaboteurImpact, 0.18f, 1001);
            GeneratePool(ArmoredAttackStartPaths, SoundKind.ArmoredWindup, 0.62f, 1101);
            GeneratePool(ArmoredAttackImpactPaths, SoundKind.ArmoredStrike, 0.5f, 1201);
            GeneratePool(ArmoredSpecialStartPaths, SoundKind.ArmoredLeap, 0.75f, 1301);
            GeneratePool(ArmoredSpecialImpactPaths, SoundKind.ArmoredSlam, 0.9f, 1401);
            GeneratePool(DroneAttackStartPaths, SoundKind.DroneCharge, 0.55f, 1501);
            GeneratePool(DroneAttackImpactPaths, SoundKind.DroneDischarge, 0.42f, 1601);
            GenerateWav(DroneLoopPath, SoundKind.DroneLoop, 2f, 1701);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string path in EnumerateAllClipPaths())
            {
                ConfigureImporter(path, path == DroneLoopPath);
            }
        }

        private static IEnumerable<string> EnumerateAllClipPaths()
        {
            return PlayerFootstepPaths.Concat(PlayerJumpPaths).Concat(PlayerDoubleJumpPaths)
                .Concat(PlayerDashPaths).Concat(PlayerLightLandingPaths).Concat(PlayerHeavyLandingPaths)
                .Concat(SaboteurFootstepPaths).Concat(ArmoredFootstepPaths)
                .Concat(SaboteurAttackStartPaths).Concat(SaboteurAttackImpactPaths)
                .Concat(ArmoredAttackStartPaths).Concat(ArmoredAttackImpactPaths)
                .Concat(ArmoredSpecialStartPaths).Concat(ArmoredSpecialImpactPaths)
                .Concat(DroneAttackStartPaths).Concat(DroneAttackImpactPaths)
                .Concat(new[] { DroneLoopPath });
        }

        private static void GeneratePool(string[] paths, SoundKind kind, float duration, int seed)
        {
            for (int index = 0; index < paths.Length; index++)
            {
                GenerateWav(paths[index], kind, duration, seed + index * 37);
            }
        }

        private static void GenerateWav(string assetPath, SoundKind kind, float duration, int seed)
        {
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(seed);
            float noiseState = 0f;
            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)SampleRate;
                float normalized = time / duration;
                float noise = (float)(random.NextDouble() * 2d - 1d);
                noiseState = Mathf.Lerp(noiseState, noise, kind == SoundKind.DroneLoop ? 0.02f : 0.22f);
                samples[index] = Synthesize(kind, time, normalized, noise, noiseState, seed);
            }

            Normalize(samples, 0.86f);
            string fullPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? string.Empty);
            File.WriteAllBytes(fullPath, EncodeWave(samples));
        }

        private static float Synthesize(
            SoundKind kind,
            float time,
            float normalized,
            float noise,
            float smoothNoise,
            int seed)
        {
            float variation = (seed % 19 - 9) * 0.012f;
            float attack = SmoothEnvelope(normalized, 0f, 0.035f, 0.18f);
            float body = Mathf.Exp(-normalized * 5.5f);
            float transient = Mathf.Exp(-normalized * 30f);
            switch (kind)
            {
                case SoundKind.PlayerStep:
                    return Mathf.Sin(Tau * (165f + variation * 100f) * time) * body * 0.44f +
                           Mathf.Sin(Tau * 920f * time) * transient * 0.22f + noise * transient * 0.32f;
                case SoundKind.SaboteurStep:
                    return Mathf.Sin(Tau * (235f + variation * 120f) * time) * body * 0.34f +
                           Mathf.Sin(Tau * 1450f * time) * transient * 0.3f + noise * transient * 0.28f;
                case SoundKind.ArmoredStep:
                    return Mathf.Sin(Tau * (68f + variation * 35f) * time) * body * 0.7f +
                           Mathf.Sin(Tau * 245f * time) * Mathf.Exp(-normalized * 9f) * 0.32f + noise * transient * 0.24f;
                case SoundKind.Jump:
                    return Chirp(time, 145f, 520f, normalized) * attack * 0.52f + smoothNoise * body * 0.34f +
                           Mathf.Sin(Tau * 92f * time) * transient * 0.28f;
                case SoundKind.DoubleJump:
                    return Chirp(time, 260f, 980f, normalized) * attack * 0.48f +
                           Chirp(time, 1100f, 420f, normalized) * body * 0.2f + smoothNoise * body * 0.26f;
                case SoundKind.Dash:
                    return smoothNoise * Mathf.Sin(Mathf.PI * normalized) * 0.65f +
                           Chirp(time, 760f, 120f, normalized) * attack * 0.36f;
                case SoundKind.LightLanding:
                    return Mathf.Sin(Tau * 105f * time) * body * 0.5f + noise * transient * 0.3f;
                case SoundKind.HeavyLanding:
                    return Mathf.Sin(Tau * 52f * time) * body * 0.78f +
                           Mathf.Sin(Tau * 185f * time) * Mathf.Exp(-normalized * 7f) * 0.32f + noise * transient * 0.28f;
                case SoundKind.SaboteurCutter:
                    return SynthesizeSaboteurBladeSlice(time, normalized, noise, smoothNoise, variation);
                case SoundKind.SaboteurImpact:
                    return SynthesizeSaboteurBladeImpact(time, normalized, noise, smoothNoise, variation);
                case SoundKind.ArmoredWindup:
                    return Chirp(time, 72f, 250f, normalized) * attack * 0.6f +
                           Mathf.Sin(Tau * 38f * time) * body * 0.32f + smoothNoise * attack * 0.18f;
                case SoundKind.ArmoredStrike:
                    return Mathf.Sin(Tau * 62f * time) * body * 0.72f +
                           Mathf.Sin(Tau * 290f * time) * Mathf.Exp(-normalized * 8f) * 0.35f + noise * transient * 0.25f;
                case SoundKind.ArmoredLeap:
                    return Chirp(time, 48f, 310f, normalized) * attack * 0.62f + smoothNoise * attack * 0.22f;
                case SoundKind.ArmoredSlam:
                    return Mathf.Sin(Tau * 42f * time) * Mathf.Exp(-normalized * 4.2f) * 0.82f +
                           Mathf.Sin(Tau * 132f * time) * body * 0.38f + smoothNoise * Mathf.Exp(-normalized * 9f) * 0.34f;
                case SoundKind.DroneCharge:
                    return Chirp(time, 420f, 1680f, normalized) * attack * 0.55f +
                           Mathf.Sin(Tau * 96f * time) * attack * 0.18f;
                case SoundKind.DroneDischarge:
                    return Chirp(time, 1860f, 160f, normalized) * body * 0.52f + noise * transient * 0.3f;
                case SoundKind.DroneLoop:
                    float loopLfo = 0.82f + Mathf.Sin(Tau * 2f * time) * 0.08f;
                    return (Mathf.Sin(Tau * 96f * time) * 0.42f +
                            Mathf.Sin(Tau * 192f * time) * 0.22f +
                            Mathf.Sin(Tau * 384f * time) * 0.1f) * loopLfo;
                default:
                    return 0f;
            }
        }

        private static float SynthesizeSaboteurBladeSlice(
            float time,
            float normalized,
            float noise,
            float smoothNoise,
            float variation)
        {
            // Knife hands should read as displaced air and sharpened steel, not as a pitched power-up.
            float airEnvelope = SmoothEnvelope(normalized, 0f, 0.1f, 0.86f);
            float edgeEnvelope = SmoothEnvelope(normalized, 0.025f, 0.13f, 0.58f);
            float tipEnvelope = SmoothEnvelope(normalized, 0.015f, 0.065f, 0.24f);
            float highNoise = noise - smoothNoise * 0.78f;
            float serration = 0.72f + Mathf.Sin(Tau * (43f + variation * 30f) * time) * 0.28f;
            float steelEdge =
                Mathf.Sin(Tau * (2380f + variation * 420f) * time) * 0.11f +
                Mathf.Sin(Tau * (3970f - variation * 510f) * time) * 0.055f;
            float bladeTip = Chirp(time, 5100f + variation * 300f, 1750f, normalized);

            return highNoise * airEnvelope * serration * 0.7f +
                   smoothNoise * airEnvelope * 0.16f +
                   steelEdge * edgeEnvelope +
                   bladeTip * tipEnvelope * 0.085f;
        }

        private static float SynthesizeSaboteurBladeImpact(
            float time,
            float normalized,
            float noise,
            float smoothNoise,
            float variation)
        {
            float contact = Mathf.Exp(-normalized * 38f);
            float scrapeEnvelope = SmoothEnvelope(normalized, 0f, 0.055f, 0.72f);
            float highNoise = noise - smoothNoise * 0.82f;
            float edgeRing =
                Mathf.Sin(Tau * (2540f + variation * 360f) * time) * Mathf.Exp(-normalized * 17f) * 0.22f +
                Mathf.Sin(Tau * (4280f - variation * 440f) * time) * Mathf.Exp(-normalized * 27f) * 0.1f;

            return highNoise * contact * 0.72f +
                   highNoise * scrapeEnvelope * 0.2f +
                   smoothNoise * scrapeEnvelope * 0.08f +
                   edgeRing;
        }

        private static float Chirp(float time, float startFrequency, float endFrequency, float normalized)
        {
            float frequency = Mathf.Lerp(startFrequency, endFrequency, normalized);
            return Mathf.Sin(Tau * frequency * time);
        }

        private static float SmoothEnvelope(float value, float start, float peak, float end)
        {
            if (value <= start || value >= end)
            {
                return 0f;
            }

            if (value < peak)
            {
                return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, peak, value));
            }

            return Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(peak, end, value));
        }

        private static void Normalize(float[] samples, float peak)
        {
            float maximum = samples.Select(Mathf.Abs).DefaultIfEmpty(1f).Max();
            float scale = maximum > 0.0001f ? peak / maximum : 1f;
            for (int index = 0; index < samples.Length; index++)
            {
                samples[index] = Mathf.Clamp(samples[index] * scale, -1f, 1f);
            }
        }

        private static byte[] EncodeWave(float[] samples)
        {
            using MemoryStream stream = new MemoryStream(44 + samples.Length * 2);
            using BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + samples.Length * 2);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(samples.Length * 2);
            foreach (float sample in samples)
            {
                writer.Write((short)Mathf.RoundToInt(sample * short.MaxValue));
            }

            return stream.ToArray();
        }

        private static void ConfigureImporter(string path, bool loop)
        {
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
            {
                return;
            }

            importer.forceToMono = true;
            importer.loadInBackground = false;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.quality = 1f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static AudioMixerGroup EnsureGroup(object controller, AudioMixerGroup parent, string name)
        {
            AudioMixer mixer = controller as AudioMixer;
            AudioMixerGroup existing = mixer?.FindMatchingGroups(name).FirstOrDefault();
            if (existing != null)
            {
                return existing;
            }

            AudioMixerGroup group = InvokeInstance(controller, "CreateNewGroup", name, false) as AudioMixerGroup;
            if (group == null)
            {
                throw new InvalidOperationException("Unity could not create AudioMixer group " + name + ".");
            }

            AttachChild(parent, group);
            return group;
        }

        private static void ReparentGroup(AudioMixerGroup oldParent, AudioMixerGroup newParent, AudioMixerGroup child)
        {
            Array oldChildren = GetInstanceProperty(oldParent, "children") as Array;
            if (oldChildren != null && oldChildren.Cast<object>().Contains(child))
            {
                Array reduced = Array.CreateInstance(oldChildren.GetType().GetElementType(), oldChildren.Length - 1);
                int destination = 0;
                foreach (object item in oldChildren)
                {
                    if (!ReferenceEquals(item, child))
                    {
                        reduced.SetValue(item, destination++);
                    }
                }

                SetInstanceProperty(oldParent, "children", reduced);
            }

            AttachChild(newParent, child);
        }

        private static void AttachChild(AudioMixerGroup parent, AudioMixerGroup child)
        {
            Array children = GetInstanceProperty(parent, "children") as Array;
            if (children == null || children.Cast<object>().Contains(child))
            {
                return;
            }

            Type childType = children.GetType().GetElementType();
            Array expanded = Array.CreateInstance(childType, children.Length + 1);
            Array.Copy(children, expanded, children.Length);
            expanded.SetValue(child, children.Length);
            SetInstanceProperty(parent, "children", expanded);
        }

        private static void SetExposedVolumes(
            object controller,
            AudioMixerGroup master,
            AudioMixerGroup music,
            AudioMixerGroup sfx)
        {
            Type exposedType = FindEditorType("UnityEditor.Audio.ExposedAudioParameter");
            Type guidType = FindEditorType("UnityEditor.GUID");
            if (exposedType == null || guidType == null)
            {
                throw new InvalidOperationException("Unity AudioMixer exposed parameter types were unavailable.");
            }

            Array exposed = Array.CreateInstance(exposedType, 3);
            SetExposedEntry(exposed, 0, exposedType, GetVolumeGuid(master), AudioSettingsController.MasterParameter);
            SetExposedEntry(exposed, 1, exposedType, GetVolumeGuid(music), AudioSettingsController.MusicParameter);
            SetExposedEntry(exposed, 2, exposedType, GetVolumeGuid(sfx), AudioSettingsController.SfxParameter);
            SetInstanceProperty(controller, "exposedParameters", exposed);
            InvokeInstance(controller, "OnChangedExposedParameter");
        }

        private static object GetVolumeGuid(AudioMixerGroup group)
        {
            return InvokeInstance(group, "GetGUIDForVolume");
        }

        private static void SetExposedEntry(Array array, int index, Type entryType, object guid, string name)
        {
            object entry = entryType.IsValueType
                ? Activator.CreateInstance(entryType)
                : FormatterServices.GetUninitializedObject(entryType);
            entryType.GetField("guid", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(entry, guid);
            entryType.GetField("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(entry, name);
            array.SetValue(entry, index);
        }

        private static void EnsureDialogueCompressor(AudioMixer mixer, AudioMixerGroup dialogue)
        {
            Array effects = GetInstanceProperty(dialogue, "effects") as Array;
            object compressor = effects?.Cast<object>().FirstOrDefault(effect =>
                effect != null && string.Equals(
                    GetInstanceProperty(effect, "effectName") as string,
                    "Compressor",
                    StringComparison.Ordinal));
            if (compressor == null)
            {
                Type effectType = FindEditorType("UnityEditor.Audio.AudioMixerEffectController");
                compressor = Activator.CreateInstance(
                    effectType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new object[] { "Compressor" },
                    null);
                if (compressor is not UnityEngine.Object compressorAsset)
                {
                    throw new InvalidOperationException("Unity could not create the dialogue compressor.");
                }

                AssetDatabase.AddObjectToAsset(compressorAsset, mixer);
                InvokeInstance(dialogue, "InsertEffect", compressor, effects?.Length ?? 0);
                EditorUtility.SetDirty(compressorAsset);
            }

            object snapshot = GetInstanceProperty(mixer, "TargetSnapshot");
            InvokeInstance(compressor, "SetValueForParameter", mixer, snapshot, "Threshold", -18f);
            InvokeInstance(compressor, "SetValueForParameter", mixer, snapshot, "Attack", 10f);
            InvokeInstance(compressor, "SetValueForParameter", mixer, snapshot, "Release", 90f);
            InvokeInstance(compressor, "SetValueForParameter", mixer, snapshot, "Makeup Gain", 2f);
        }

        private static string[] Paths(string prefix, int count)
        {
            string[] result = new string[count];
            for (int index = 0; index < count; index++)
            {
                result[index] = SfxRoot + "/" + prefix + "_" + (index + 1).ToString("00") + ".wav";
            }

            return result;
        }

        private static void EnsureFolder(string path)
        {
            string normalized = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            int separator = normalized.LastIndexOf('/');
            string parent = normalized.Substring(0, separator);
            string name = normalized.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static Type FindEditorType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(type => type != null);
        }

        private static object GetInstanceProperty(object target, string propertyName)
        {
            return target?.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target);
        }

        private static void SetInstanceProperty(object target, string propertyName, object value)
        {
            target?.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(target, value);
        }

        private static object InvokeInstance(object target, string methodName, params object[] arguments)
        {
            if (target == null)
            {
                return null;
            }

            MethodInfo method = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(candidate => candidate.Name == methodName && candidate.GetParameters().Length == arguments.Length);
            return method?.Invoke(target, arguments);
        }

        private static readonly float Tau = Mathf.PI * 2f;
    }
}
