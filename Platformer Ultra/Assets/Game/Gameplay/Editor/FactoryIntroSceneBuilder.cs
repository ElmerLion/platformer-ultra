using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PlatformerUltra.Audio;
using PlatformerUltra.Audio.Editor;
using PlatformerUltra.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace PlatformerUltra.Gameplay.Editor
{
    public static class FactoryIntroSceneBuilder
    {
        public const string IntroScenePath = "Assets/Game/Scenes/FactoryIntro.unity";
        public const string FactoryScenePath = "Assets/Game/Scenes/FactoryVerticalMap.unity";
        public const string TimelinePath = "Assets/Game/Cinematics/Timelines/TL_FactoryIntro.playable";
        public const string CameraClipPath = "Assets/Game/Cinematics/Timelines/AN_FactoryIntroCamera.anim";
        public const string MixerPath = GameAudioAssetFactory.MixerPath;

        private const string PanelSettingsPath = "Assets/Game/UI/PS_PrototypeHUD.asset";
        private const string IntroLayoutPath = "Assets/Game/UI/FactoryIntro.uxml";
        private const string IntroStylePath = "Assets/Game/UI/FactoryIntro.uss";
        private const string PlayerVisualPath = "Assets/Game/CharacterArt/Prefabs/PF_Player_MaintenanceUnit_Visual.prefab";
        private const string SaboteurVisualPath = "Assets/Game/CharacterArt/Prefabs/PF_Enemy_Saboteur_Cutter_Visual.prefab";
        private const string InteractActionPath = "Assets/Game/Input/IAR_Interact.asset";
        private const string FloorMaterialPath = "Assets/Game/Factory/Materials/M_Factory_MapFloor.mat";
        private const string DeckMaterialPath = "Assets/Game/Factory/Materials/M_Factory_MapDeck.mat";
        private const string WallMaterialPath = "Assets/Game/Factory/Materials/M_Factory_MapWall.mat";
        private const string CyanMaterialPath = "Assets/Game/Factory/Materials/M_Factory_EmissiveCyan.mat";
        private const string PurpleMaterialPath = "Assets/Game/Factory/Materials/M_Factory_MachinePurple.mat";
        private const string OrangeMaterialPath = "Assets/Game/Factory/Materials/M_Factory_EmissiveOrange.mat";
        private const string AlarmClipPath = "Assets/Audio/freesound_community-space-alarm-glitchy-45819.mp3";
        private const string TransitionClipPath = "Assets/Audio/freesound_community-laser-45816.mp3";

        private static readonly string[] VoiceClipPaths =
        {
            "Assets/Audio/Voiceover/dialogue-v3_Emergency_wake_protocol._Maintenance_unit_online.-0.mp3",
            "Assets/Audio/Voiceover/dialogue-v3_Factory_control_network_compromised.-0.mp3",
            "Assets/Audio/Voiceover/dialogue-v3_Worker_and_security_units_corrupted._Hostile_units_are_dismantling_the_productio-0.mp3",
            "Assets/Audio/Voiceover/dialogue-v3_Stabilize_the_portal_and_evacuate_before_factory_control_is_lost.-0.mp3"
        };

        private static readonly string[] VoiceCaptions =
        {
            "Emergency wake protocol. Maintenance unit online.",
            "Factory control network compromised.",
            "Worker and security units corrupted. Hostile units are dismantling the production chain.",
            "Stabilize the portal and evacuate before factory control is lost."
        };

        [MenuItem("Tools/Platformer Ultra/Build Factory Intro Scene")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/Game/Cinematics");
            EnsureFolder("Assets/Game/Cinematics/Timelines");
            EnsureFolder("Assets/Game/Audio");

            AudioMixerGroup dialogueGroup = BuildAudioMixer();
            Scene introScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildIntroScene(introScene, dialogueGroup);
            EditorSceneManager.SaveScene(introScene, IntroScenePath);
            EnsureFactoryEntryController();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(IntroScenePath);
            Debug.Log($"Factory intro cinematic built at {IntroScenePath}.");
        }

        private static void BuildIntroScene(Scene scene, AudioMixerGroup dialogueGroup)
        {
            Material floor = RequireAsset<Material>(FloorMaterialPath);
            Material deck = RequireAsset<Material>(DeckMaterialPath);
            Material wall = RequireAsset<Material>(WallMaterialPath);
            Material cyan = RequireAsset<Material>(CyanMaterialPath);
            Material purple = RequireAsset<Material>(PurpleMaterialPath);
            Material orange = RequireAsset<Material>(OrangeMaterialPath);

            GameObject environment = new GameObject("01 Intro Diorama");
            CreateBox("Factory Floor", environment.transform, new Vector3(0f, -0.18f, 1f), new Vector3(19f, 0.35f, 12f), floor);
            CreateBox("Rear Wall", environment.transform, new Vector3(0f, 3f, 4.7f), new Vector3(19f, 6.4f, 0.35f), wall);
            CreateBox("Left Wall", environment.transform, new Vector3(-9.3f, 2.5f, 1f), new Vector3(0.35f, 5.4f, 8f), wall);
            CreateBox("Right Wall", environment.transform, new Vector3(9.3f, 2.5f, 1f), new Vector3(0.35f, 5.4f, 8f), wall);
            CreateBox("Ceiling Beam", environment.transform, new Vector3(0f, 5.8f, 1f), new Vector3(19f, 0.45f, 0.55f), deck);
            for (int index = -4; index <= 4; index++)
            {
                CreateBox("Hazard Floor Light " + (index + 5), environment.transform,
                    new Vector3(index * 2f, 0.015f, -1.85f), new Vector3(0.9f, 0.025f, 0.08f), orange);
            }

            GameObject playerVisual = InstantiatePrefab(PlayerVisualPath, environment.transform, "Maintenance Unit Visual");
            playerVisual.transform.SetPositionAndRotation(new Vector3(-4f, 0f, 1.1f), Quaternion.Euler(0f, 180f, 0f));
            BuildRepairCradle(environment.transform, deck, cyan);

            GameObject networkConsole = new GameObject("Factory Network Console");
            networkConsole.transform.SetParent(environment.transform, false);
            CreateBox("Console Plinth", networkConsole.transform, new Vector3(0f, 0.75f, 2.2f), new Vector3(2.6f, 1.5f, 1.3f), deck);
            Renderer networkDisplay = CreateBox("Network Status Display", networkConsole.transform,
                new Vector3(0f, 1.75f, 1.52f), new Vector3(2.25f, 1.05f, 0.08f), cyan).GetComponent<Renderer>();
            CreateBox("Corruption Conduit", networkConsole.transform, new Vector3(0f, 0.12f, 2.8f), new Vector3(0.18f, 0.18f, 3.8f), purple);

            GameObject sabotageSet = new GameObject("Sabotage Vignette");
            sabotageSet.transform.SetParent(environment.transform, false);
            CreateBox("Damaged Production Machine", sabotageSet.transform, new Vector3(4f, 1f, 2.25f), new Vector3(2.7f, 2f, 1.6f), deck);
            CreateBox("Machine Corruption Core", sabotageSet.transform, new Vector3(4f, 1.2f, 1.39f), new Vector3(0.7f, 0.7f, 0.08f), purple);
            GameObject saboteurVisual = InstantiatePrefab(SaboteurVisualPath, sabotageSet.transform, "Corrupted Saboteur Visual");
            saboteurVisual.transform.SetPositionAndRotation(new Vector3(4f, 0f, 0.05f), Quaternion.identity);
            MonoBehaviour cinematicAttacker = saboteurVisual
                .GetComponentsInChildren<MonoBehaviour>(true)
                .FirstOrDefault(component => component is ICinematicAttackPerformer);

            ParticleSystem sparks = BuildSparks(sabotageSet.transform, new Vector3(4f, 1.2f, 1.35f), orange);
            Light warningLight = CreateLight("Corruption Warning Light", sabotageSet.transform,
                new Vector3(4f, 3.9f, 0.8f), new Color(0.9f, 0.08f, 1f), 0f, 8f);

            Renderer[] hologramNodes = BuildProductionHologram(environment.transform, cyan, purple, deck);

            GameObject lighting = new GameObject("02 Lighting");
            Light key = lighting.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.56f, 0.72f, 0.78f);
            key.intensity = 1.15f;
            key.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            CreateLight("Player Wake Light", lighting.transform, new Vector3(-4f, 3.2f, -1.3f), new Color(0.2f, 0.85f, 1f), 9f, 9f);
            CreateLight("Machine Rim Light", lighting.transform, new Vector3(4f, 3.2f, -1.3f), new Color(1f, 0.25f, 0.08f), 7f, 9f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.055f, 0.075f, 0.09f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.018f, 0.025f, 0.032f);
            RenderSettings.fogDensity = 0.018f;

            GameObject cameraRig = new GameObject("Cinematic Camera Rig");
            Animator cameraAnimator = cameraRig.AddComponent<Animator>();
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(cameraRig.transform, false);
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.005f, 0.009f, 0.012f);
            cameraObject.AddComponent<AudioListener>();

            GameObject cinematicRoot = new GameObject("03 Cinematic");
            PlayableDirector playableDirector = cinematicRoot.AddComponent<PlayableDirector>();
            BuildTimeline(playableDirector, cameraAnimator);
            FactoryIntroVisualDirector visualDirector = cinematicRoot.AddComponent<FactoryIntroVisualDirector>();
            visualDirector.Configure(playableDirector, cinematicAttacker, sparks, warningLight, networkDisplay, hologramNodes);

            GameObject audioRoot = new GameObject("04 Cinematic Audio");
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            AudioMixerGroup sfxGroup = mixer.FindMatchingGroups(GameAudioAssetFactory.SfxGroupName).First();
            AudioSettingsController audioSettings = audioRoot.AddComponent<AudioSettingsController>();
            audioSettings.Configure(mixer);
            AudioSource alarmSource = audioRoot.AddComponent<AudioSource>();
            alarmSource.clip = RequireAsset<AudioClip>(AlarmClipPath);
            alarmSource.playOnAwake = true;
            alarmSource.loop = true;
            alarmSource.spatialBlend = 0f;
            alarmSource.volume = 0.28f;
            alarmSource.outputAudioMixerGroup = sfxGroup;

            AudioSource primaryVoice = CreatePrimaryVoiceSource(audioRoot.transform, dialogueGroup);
            AudioSource metallicVoice = CreateMetallicVoiceSource(audioRoot.transform, dialogueGroup);

            GameObject transitionRoot = new GameObject("05 Persistent Transition UI");
            UIDocument document = transitionRoot.AddComponent<UIDocument>();
            document.panelSettings = RequireAsset<PanelSettings>(PanelSettingsPath);
            document.visualTreeAsset = RequireAsset<VisualTreeAsset>(IntroLayoutPath);
            document.sortingOrder = 100;
            FactoryIntroPresenter presenter = transitionRoot.AddComponent<FactoryIntroPresenter>();
            presenter.Configure(document, RequireAsset<StyleSheet>(IntroStylePath));
            AudioSource transitionSource = transitionRoot.AddComponent<AudioSource>();
            transitionSource.playOnAwake = false;
            transitionSource.spatialBlend = 0f;
            transitionSource.volume = 0.7f;
            transitionSource.outputAudioMixerGroup = sfxGroup;
            FactorySceneTransition transition = transitionRoot.AddComponent<FactorySceneTransition>();
            transition.Configure(presenter, transitionSource, RequireAsset<AudioClip>(TransitionClipPath));

            FactoryAIVoiceEmitter voiceEmitter = audioRoot.AddComponent<FactoryAIVoiceEmitter>();
            FactoryAIVoiceEmitter.VoiceLine[] lines = new FactoryAIVoiceEmitter.VoiceLine[VoiceClipPaths.Length];
            for (int index = 0; index < lines.Length; index++)
            {
                lines[index] = new FactoryAIVoiceEmitter.VoiceLine
                {
                    Caption = VoiceCaptions[index],
                    Clip = AssetDatabase.LoadAssetAtPath<AudioClip>(VoiceClipPaths[index])
                };
            }

            voiceEmitter.Configure(primaryVoice, metallicVoice, presenter, lines, new[] { alarmSource }, 0.93f, 0.012f);

            FactoryIntroController introController = cinematicRoot.AddComponent<FactoryIntroController>();
            introController.Configure(
                playableDirector,
                voiceEmitter,
                presenter,
                transition,
                RequireAsset<InputActionReference>(InteractActionPath),
                new[]
                {
                    new FactoryIntroController.VoiceCue { StartTime = 0.65f, LineIndex = 0 },
                    new FactoryIntroController.VoiceCue { StartTime = 5.05f, LineIndex = 1 },
                    new FactoryIntroController.VoiceCue { StartTime = 8.02f, LineIndex = 2 },
                    new FactoryIntroController.VoiceCue { StartTime = 14.02f, LineIndex = 3 }
                },
                19.15f);

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void BuildRepairCradle(Transform parent, Material deck, Material cyan)
        {
            GameObject cradle = new GameObject("Maintenance Wake Cradle");
            cradle.transform.SetParent(parent, false);
            CreateBox("Cradle Base", cradle.transform, new Vector3(-4f, 0.12f, 1.1f), new Vector3(2.6f, 0.24f, 2.4f), deck);
            CreateBox("Cradle Left Arm", cradle.transform, new Vector3(-5.15f, 1.25f, 1.15f), new Vector3(0.18f, 2.5f, 0.25f), deck);
            CreateBox("Cradle Right Arm", cradle.transform, new Vector3(-2.85f, 1.25f, 1.15f), new Vector3(0.18f, 2.5f, 0.25f), deck);
            CreateBox("Wake Light Left", cradle.transform, new Vector3(-5.02f, 1.65f, 0.98f), new Vector3(0.08f, 0.65f, 0.08f), cyan);
            CreateBox("Wake Light Right", cradle.transform, new Vector3(-2.98f, 1.65f, 0.98f), new Vector3(0.08f, 0.65f, 0.08f), cyan);
        }

        private static Renderer[] BuildProductionHologram(Transform parent, Material cyan, Material purple, Material deck)
        {
            GameObject root = new GameObject("Evacuation Production Hologram");
            root.transform.SetParent(parent, false);
            CreateBox("Hologram Projector", root.transform, new Vector3(0f, 0.42f, 3.2f), new Vector3(5.8f, 0.84f, 1.4f), deck);
            Renderer[] nodes = new Renderer[6];
            for (int index = 0; index < nodes.Length; index++)
            {
                float x = -3.5f + index * 1.4f;
                nodes[index] = CreateBox(
                    index == nodes.Length - 1 ? "Portal Hologram Node" : "Production Hologram Node " + (index + 1),
                    root.transform,
                    new Vector3(x, 3.15f, 3.1f),
                    index == nodes.Length - 1 ? new Vector3(0.8f, 1.35f, 0.12f) : new Vector3(0.58f, 0.58f, 0.12f),
                    index == nodes.Length - 1 ? purple : cyan).GetComponent<Renderer>();
                if (index < nodes.Length - 1)
                {
                    CreateBox("Hologram Link " + (index + 1), root.transform,
                        new Vector3(x + 0.7f, 3.15f, 3.12f), new Vector3(0.78f, 0.08f, 0.06f), cyan);
                }
            }

            return nodes;
        }

        private static ParticleSystem BuildSparks(Transform parent, Vector3 position, Material material)
        {
            GameObject effect = new GameObject("Sabotage Sparks");
            effect.transform.SetParent(parent, false);
            effect.transform.position = position;
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.35f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.28f, 0.03f), new Color(0.4f, 0.9f, 1f));
            main.maxParticles = 64;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 38f;
            shape.radius = 0.12f;
            ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
            renderer.material = material;
            return particles;
        }

        private static AudioSource CreatePrimaryVoiceSource(Transform parent, AudioMixerGroup group)
        {
            GameObject voice = new GameObject("Factory AI Primary Voice");
            voice.transform.SetParent(parent, false);
            AudioSource source = voice.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = group;
            AudioHighPassFilter high = voice.AddComponent<AudioHighPassFilter>();
            high.cutoffFrequency = 170f;
            AudioLowPassFilter low = voice.AddComponent<AudioLowPassFilter>();
            low.cutoffFrequency = 4800f;
            AudioDistortionFilter distortion = voice.AddComponent<AudioDistortionFilter>();
            distortion.distortionLevel = 0.16f;
            AudioChorusFilter chorus = voice.AddComponent<AudioChorusFilter>();
            chorus.dryMix = 0.82f;
            chorus.wetMix1 = 0.18f;
            chorus.wetMix2 = 0.06f;
            chorus.wetMix3 = 0f;
            chorus.delay = 14f;
            chorus.rate = 0.8f;
            chorus.depth = 0.08f;
            AudioEchoFilter echo = voice.AddComponent<AudioEchoFilter>();
            echo.delay = 38f;
            echo.decayRatio = 0.12f;
            echo.dryMix = 0.9f;
            echo.wetMix = 0.1f;
            return source;
        }

        private static AudioSource CreateMetallicVoiceSource(Transform parent, AudioMixerGroup group)
        {
            GameObject voice = new GameObject("Factory AI Metallic Voice");
            voice.transform.SetParent(parent, false);
            AudioSource source = voice.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = group;
            AudioHighPassFilter high = voice.AddComponent<AudioHighPassFilter>();
            high.cutoffFrequency = 900f;
            AudioLowPassFilter low = voice.AddComponent<AudioLowPassFilter>();
            low.cutoffFrequency = 3200f;
            AudioDistortionFilter distortion = voice.AddComponent<AudioDistortionFilter>();
            distortion.distortionLevel = 0.32f;
            return source;
        }

        private static AudioMixerGroup BuildAudioMixer()
        {
            AudioMixer mixer = GameAudioAssetFactory.BuildOrUpdateMixer();
            return mixer.FindMatchingGroups(GameAudioAssetFactory.DialogueGroupName).First();
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

            MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(candidate => candidate.Name == methodName && candidate.GetParameters().Length == arguments.Length);
            return method?.Invoke(target, arguments);
        }

        private static void BuildTimeline(PlayableDirector director, Animator cameraAnimator)
        {
            AssetDatabase.DeleteAsset(TimelinePath);
            AssetDatabase.DeleteAsset(CameraClipPath);

            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "TL_FactoryIntro";
            AssetDatabase.CreateAsset(timeline, TimelinePath);
            AnimationClip cameraClip = new AnimationClip { name = "AN_FactoryIntroCamera" };
            AssetDatabase.CreateAsset(cameraClip, CameraClipPath);

            AnimationTrack cameraTrack = timeline.CreateTrack<AnimationTrack>(null, "Cinematic Camera");
            TimelineClip timelineClip = cameraTrack.CreateDefaultClip();
            AnimationPlayableAsset playable = timelineClip.asset as AnimationPlayableAsset;
            if (playable == null)
            {
                throw new InvalidOperationException("Could not create the factory intro camera Timeline clip.");
            }

            playable.clip = cameraClip;
            timelineClip.displayName = "Factory Incident Camera";
            timelineClip.duration = 20d;
            timelineClip.clipIn = 0d;
            director.playableAsset = timeline;
            director.extrapolationMode = DirectorWrapMode.None;
            director.SetGenericBinding(cameraTrack, cameraAnimator);

            CameraPose[] poses =
            {
                Pose(0f, new Vector3(-4f, 2.15f, -5.2f), new Vector3(-4f, 1.25f, 1.1f)),
                Pose(4.8f, new Vector3(-3.15f, 2.4f, -4.65f), new Vector3(-4f, 1.35f, 1.1f)),
                Pose(5.15f, new Vector3(0f, 2.65f, -5.05f), new Vector3(0f, 1.45f, 1.65f)),
                Pose(7.8f, new Vector3(0.45f, 2.55f, -4.55f), new Vector3(0f, 1.55f, 1.65f)),
                Pose(8.1f, new Vector3(4.25f, 2.2f, -5.25f), new Vector3(4f, 1.25f, 1.55f)),
                Pose(13.8f, new Vector3(3.35f, 2.85f, -4.45f), new Vector3(4f, 1.3f, 1.65f)),
                Pose(14.1f, new Vector3(0f, 4.7f, -7.1f), new Vector3(0f, 2.65f, 3.1f)),
                Pose(19.15f, new Vector3(0f, 3.8f, -6.05f), new Vector3(0f, 2.65f, 3.1f)),
                Pose(20f, new Vector3(0f, 3.8f, -6.05f), new Vector3(0f, 2.65f, 3.1f))
            };
            SetTransformCurves(cameraClip, poses, "Main Camera");
            EditorUtility.SetDirty(cameraClip);
            EditorUtility.SetDirty(timeline);
        }

        private static void SetTransformCurves(AnimationClip clip, CameraPose[] poses, string relativePath)
        {
            AnimationCurve px = Curve(poses.Select(value => new Keyframe(value.Time, value.Position.x)).ToArray());
            AnimationCurve py = Curve(poses.Select(value => new Keyframe(value.Time, value.Position.y)).ToArray());
            AnimationCurve pz = Curve(poses.Select(value => new Keyframe(value.Time, value.Position.z)).ToArray());
            AnimationCurve rx = Curve(poses.Select(value => new Keyframe(value.Time, value.Rotation.x)).ToArray());
            AnimationCurve ry = Curve(poses.Select(value => new Keyframe(value.Time, value.Rotation.y)).ToArray());
            AnimationCurve rz = Curve(poses.Select(value => new Keyframe(value.Time, value.Rotation.z)).ToArray());
            AnimationCurve rw = Curve(poses.Select(value => new Keyframe(value.Time, value.Rotation.w)).ToArray());
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalPosition.x", px);
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalPosition.y", py);
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalPosition.z", pz);
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.x", rx);
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.y", ry);
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.z", rz);
            clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.w", rw);
            clip.EnsureQuaternionContinuity();
        }

        private static AnimationCurve Curve(Keyframe[] keys)
        {
            AnimationCurve curve = new AnimationCurve(keys);
            for (int index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
            }

            return curve;
        }

        private static CameraPose Pose(float time, Vector3 position, Vector3 target)
        {
            return new CameraPose(time, position, Quaternion.LookRotation(target - position, Vector3.up));
        }

        private static void EnsureFactoryEntryController()
        {
            Scene factoryScene = EditorSceneManager.OpenScene(FactoryScenePath, OpenSceneMode.Single);
            FactoryHudPresenter hud = UnityEngine.Object.FindFirstObjectByType<FactoryHudPresenter>(FindObjectsInactive.Include);
            PlayerStatusPresenter status = UnityEngine.Object.FindFirstObjectByType<PlayerStatusPresenter>(FindObjectsInactive.Include);
            ThirdPersonPlayerController player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>(FindObjectsInactive.Include);
            PlayerInteractor interactor = UnityEngine.Object.FindFirstObjectByType<PlayerInteractor>(FindObjectsInactive.Include);
            ThirdPersonOrbitCamera orbit = UnityEngine.Object.FindFirstObjectByType<ThirdPersonOrbitCamera>(FindObjectsInactive.Include);
            FactoryPauseController pause = UnityEngine.Object.FindFirstObjectByType<FactoryPauseController>(FindObjectsInactive.Include);
            MonoBehaviour spawnManager = UnityEngine.Object
                .FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(component => component is IEnemySpawningController);
            if (hud == null || status == null || player == null || interactor == null || orbit == null || pause == null)
            {
                throw new InvalidOperationException("Factory scene is missing one or more components required for cinematic entry handoff.");
            }

            FactorySceneEntryController entry = hud.GetComponent<FactorySceneEntryController>();
            if (entry == null)
            {
                entry = hud.gameObject.AddComponent<FactorySceneEntryController>();
            }

            entry.Configure(
                player,
                interactor,
                orbit,
                player.GetComponent<Targetable>(),
                status,
                hud,
                pause,
                spawnManager);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(entry);
            EditorSceneManager.MarkSceneDirty(factoryScene);
            EditorSceneManager.SaveScene(factoryScene);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(IntroScenePath, true),
                new EditorBuildSettingsScene(FactoryScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", false),
                new EditorBuildSettingsScene("Assets/Game/Scenes/ConveyorTestScene.unity", false)
            };
        }

        private static GameObject InstantiatePrefab(string path, Transform parent, string name)
        {
            GameObject prefab = RequireAsset<GameObject>(path);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Could not instantiate prefab at " + path);
            }

            instance.name = name;
            return instance;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private static Light CreateLight(
            string name,
            Transform parent,
            Vector3 position,
            Color color,
            float intensity,
            float range)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
            return light;
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Required asset is missing at {path}.");
            }

            return asset;
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }

        private readonly struct CameraPose
        {
            public CameraPose(float time, Vector3 position, Quaternion rotation)
            {
                Time = time;
                Position = position;
                Rotation = rotation;
            }

            public float Time { get; }
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
        }
    }
}
