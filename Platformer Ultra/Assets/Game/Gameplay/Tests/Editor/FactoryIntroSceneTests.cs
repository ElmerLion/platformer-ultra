using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PlatformerUltra.Gameplay.Tests
{
    public sealed class FactoryIntroSceneTests
    {
        private const string IntroScenePath = "Assets/Game/Scenes/FactoryIntro.unity";
        private const string FactoryScenePath = "Assets/Game/Scenes/FactoryVerticalMap.unity";

        [Test]
        public void ReleaseBuild_StartsWithIntroThenFactory()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            Assert.That(scenes, Has.Length.GreaterThanOrEqualTo(2));
            Assert.That(scenes[0].enabled, Is.True);
            Assert.That(scenes[0].path, Is.EqualTo(IntroScenePath));
            Assert.That(scenes[1].enabled, Is.True);
            Assert.That(scenes[1].path, Is.EqualTo(FactoryScenePath));
            Assert.That(scenes.Skip(2).Where(scene => scene.enabled), Is.Empty,
                "Development scenes must remain available but disabled in release Build Settings.");
        }

        [Test]
        public void IntroScene_HasCinematicVoiceUiAndTransitionContracts()
        {
            Scene scene = EditorSceneManager.OpenScene(IntroScenePath, OpenSceneMode.Single);

            FactoryIntroController controller = Object.FindFirstObjectByType<FactoryIntroController>();
            FactoryAIVoiceEmitter voice = Object.FindFirstObjectByType<FactoryAIVoiceEmitter>();
            FactorySceneTransition transition = Object.FindFirstObjectByType<FactorySceneTransition>();
            PlayableDirector director = Object.FindFirstObjectByType<PlayableDirector>();
            UIDocument document = Object.FindFirstObjectByType<UIDocument>();

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(controller, Is.Not.Null);
            Assert.That(voice, Is.Not.Null);
            Assert.That(transition, Is.Not.Null);
            Assert.That(director, Is.Not.Null);
            Assert.That(director.playableAsset, Is.Not.Null);
            Assert.That(director.duration, Is.EqualTo(20d).Within(0.05d));
            Assert.That(document, Is.Not.Null);
            Assert.That(document.visualTreeAsset, Is.Not.Null);

            SerializedProperty lines = new SerializedObject(voice).FindProperty("_lines");
            Assert.That(lines, Is.Not.Null);
            Assert.That(lines.arraySize, Is.EqualTo(4));
            for (int index = 0; index < lines.arraySize; index++)
            {
                SerializedProperty line = lines.GetArrayElementAtIndex(index);
                Assert.That(line.FindPropertyRelative("Caption").stringValue, Is.Not.Empty);
                Assert.That(line.FindPropertyRelative("Clip").objectReferenceValue, Is.Not.Null);
            }

            SerializedObject serializedVoice = new SerializedObject(voice);
            AudioSource primary = serializedVoice.FindProperty("_primarySource").objectReferenceValue as AudioSource;
            AudioSource metallic = serializedVoice.FindProperty("_metallicSource").objectReferenceValue as AudioSource;
            Assert.That(primary, Is.Not.Null);
            Assert.That(metallic, Is.Not.Null);
            Assert.That(new[] { primary, metallic }.All(source => source.spatialBlend == 0f), Is.True);
            Assert.That(new[] { primary, metallic }.All(source => source.outputAudioMixerGroup != null), Is.True);
            Assert.That(voice.GetComponentsInChildren<AudioDistortionFilter>(true), Has.Length.EqualTo(2));
            Assert.That(voice.GetComponentsInChildren<AudioHighPassFilter>(true), Has.Length.EqualTo(2));
            Assert.That(voice.GetComponentsInChildren<AudioLowPassFilter>(true), Has.Length.EqualTo(2));
            Assert.That(voice.GetComponentsInChildren<AudioChorusFilter>(true), Has.Length.EqualTo(1));
            Assert.That(voice.GetComponentsInChildren<AudioEchoFilter>(true), Has.Length.EqualTo(1));

            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Game/Audio/AM_FactoryIntro.mixer");
            Assert.That(mixer, Is.Not.Null);
            Assert.That(mixer.FindMatchingGroups("Dialogue"), Has.Length.EqualTo(1));
        }

        [Test]
        public void IntroTimeline_KeepsEveryStoryBeatFramedAboveTheFloor()
        {
            EditorSceneManager.OpenScene(IntroScenePath, OpenSceneMode.Single);
            PlayableDirector director = Object.FindFirstObjectByType<PlayableDirector>();
            Camera camera = Camera.main;

            Assert.That(director, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.transform.parent, Is.Not.Null);
            Assert.That(camera.transform.parent.name, Is.EqualTo("Cinematic Camera Rig"),
                "Animating the Animator root turns the camera curves into root motion and leaves the camera on the floor.");

            AssertFraming(director, camera, 2d, new Vector3(-4f, 1.3f, 1.1f));
            AssertFraming(director, camera, 6.5d, new Vector3(0f, 1.5f, 1.65f));
            AssertFraming(director, camera, 10.5d, new Vector3(4f, 1.3f, 1.55f));
            AssertFraming(director, camera, 16d, new Vector3(0f, 2.65f, 3.1f));
            director.Stop();
        }

        [Test]
        public void FactoryScene_HasEntryGateForSeamlessAndDirectPlay()
        {
            Scene scene = EditorSceneManager.OpenScene(FactoryScenePath, OpenSceneMode.Single);
            FactorySceneEntryController entry = Object.FindFirstObjectByType<FactorySceneEntryController>();

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(entry, Is.Not.Null);

            SerializedObject serializedEntry = new SerializedObject(entry);
            Assert.That(serializedEntry.FindProperty("_playerController").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedEntry.FindProperty("_playerInteractor").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedEntry.FindProperty("_orbitCamera").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedEntry.FindProperty("_factoryHud").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedEntry.FindProperty("_pauseController").objectReferenceValue, Is.Not.Null);
        }

        private static void AssertFraming(PlayableDirector director, Camera camera, double time, Vector3 target)
        {
            director.time = time;
            director.Evaluate();

            Vector3 toTarget = (target - camera.transform.position).normalized;
            Assert.That(camera.transform.position.y, Is.GreaterThan(1.8f), $"Camera dropped to the floor at {time:0.0}s.");
            Assert.That(Vector3.Dot(camera.transform.forward, toTarget), Is.GreaterThan(0.94f),
                $"Camera is not aimed at its story target at {time:0.0}s.");
        }
    }
}
