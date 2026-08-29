using System.Linq;
using NUnit.Framework;
using PlatformerUltra.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace PlatformerUltra.Gameplay.Tests
{
    public sealed class AudioSettingsTests
    {
        private const string HudPath = "Assets/Game/UI/FactoryMapHUD.uxml";
        private const string MixerPath = "Assets/Game/Audio/AM_Game.mixer";

        [SetUp]
        [TearDown]
        public void ClearAudioPreferences()
        {
            PlayerPrefs.DeleteKey(AudioSettingsController.MasterPreferenceKey);
            PlayerPrefs.DeleteKey(AudioSettingsController.MusicPreferenceKey);
            PlayerPrefs.DeleteKey(AudioSettingsController.SfxPreferenceKey);
        }

        [TestCase(0f, -80f)]
        [TestCase(0.1f, -20f)]
        [TestCase(1f, 0f)]
        [TestCase(-1f, -80f)]
        [TestCase(2f, 0f)]
        public void LinearToDecibels_ClampsAndUsesLogarithmicGain(float linear, float expected)
        {
            Assert.That(AudioSettingsController.LinearToDecibels(linear), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void SavedValues_AreClampedRestoredAndAppliedToSharedMixer()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            GameObject owner = new GameObject("Audio Settings Test");
            try
            {
                AudioSettingsController settings = owner.AddComponent<AudioSettingsController>();
                settings.Configure(mixer);
                settings.SetMasterVolume(1.5f);
                settings.SetMusicVolume(0.35f);
                settings.SetSfxVolume(-0.5f);
                settings.LoadAndApply();

                Assert.That(settings.MasterVolume, Is.EqualTo(1f));
                Assert.That(settings.MusicVolume, Is.EqualTo(0.35f).Within(0.001f));
                Assert.That(settings.SfxVolume, Is.EqualTo(0f));
                Assert.That(mixer.GetFloat(AudioSettingsController.MasterParameter, out float masterDb), Is.True);
                Assert.That(mixer.GetFloat(AudioSettingsController.MusicParameter, out float musicDb), Is.True);
                Assert.That(mixer.GetFloat(AudioSettingsController.SfxParameter, out float sfxDb), Is.True);
                Assert.That(masterDb, Is.InRange(-80f, 0f));
                Assert.That(musicDb, Is.InRange(-80f, 0f));
                Assert.That(sfxDb, Is.InRange(-80f, 0f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void PauseHud_ContainsOptionsVolumesAndReadOnlyActiveBindings()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudPath);
            TemplateContainer root = asset.CloneTree();

            Assert.That(root.Q<Button>("options-button"), Is.Not.Null);
            Assert.That(root.Q<Button>("settings-back-button"), Is.Not.Null);
            Assert.That(root.Q<Slider>("master-volume-slider"), Is.Not.Null);
            Assert.That(root.Q<Slider>("music-volume-slider"), Is.Not.Null);
            Assert.That(root.Q<Slider>("sfx-volume-slider"), Is.Not.Null);

            string text = string.Join(" ", root.Query<Label>().ToList().Select(label => label.text));
            Assert.That(text, Does.Contain("WASD"));
            Assert.That(text, Does.Contain("Left Stick"));
            Assert.That(text, Does.Contain("Mouse"));
            Assert.That(text, Does.Contain("Right Stick"));
            Assert.That(text, Does.Contain("Space"));
            Assert.That(text, Does.Contain("South Button"));
            Assert.That(text, Does.Contain("Left Shift"));
            Assert.That(text, Does.Contain("Left Stick Press / East Button"));
            Assert.That(text, Does.Contain("E"));
            Assert.That(text, Does.Contain("West Button"));
            Assert.That(text, Does.Contain("Escape"));
            Assert.That(text, Does.Contain("Start"));
            Assert.That(text, Does.Not.Contain("Sprint"));
            Assert.That(root.Query<Button>().ToList().Select(button => button.name), Does.Not.Contain("rebind-button"));
        }

        [Test]
        public void SharedMixerAndGeneratedAssets_HaveCompleteRoutingContracts()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            Assert.That(mixer, Is.Not.Null);
            AudioMixerGroup sfx = mixer.FindMatchingGroups("SFX").Single(group => group.name == "SFX");
            AudioMixerGroup dialogue = mixer.FindMatchingGroups("Dialogue").Single(group => group.name == "Dialogue");
            SerializedProperty children = new SerializedObject(sfx).FindProperty("m_Children");
            Assert.That(Enumerable.Range(0, children.arraySize)
                .Select(index => children.GetArrayElementAtIndex(index).objectReferenceValue)
                .Contains(dialogue), Is.True, "Dialogue must remain routed beneath SFX.");

            string[] clipGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Game/Audio/SFX" });
            Assert.That(clipGuids, Has.Length.EqualTo(39));
            foreach (string guid in clipGuids)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(clip.frequency, Is.EqualTo(48000), clip.name);
                Assert.That(clip.channels, Is.EqualTo(1), clip.name);
            }

            string[] routedPrefabPaths =
            {
                "Assets/Game/Enemies/Prefabs/PF_Enemy_Drone.prefab",
                "Assets/Game/Enemies/Prefabs/PF_Enemy_Saboteur.prefab",
                "Assets/Game/Enemies/Prefabs/PF_Enemy_Armored.prefab",
                "Assets/Game/Factory/Prefabs/PF_Factory_Mine.prefab",
                "Assets/Game/Factory/Prefabs/PF_Factory_Smelter.prefab",
                "Assets/Game/Factory/Prefabs/PF_Factory_Crusher.prefab",
                "Assets/Game/FactoryDefense/Prefabs/PF_Factory_Turret.prefab"
            };
            foreach (string path in routedPrefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                AudioSource[] sources = prefab.GetComponentsInChildren<AudioSource>(true);
                Assert.That(sources, Is.Not.Empty, path);
                Assert.That(sources.All(source => source.outputAudioMixerGroup != null), Is.True, path);
                Assert.That(sources.All(source => source.outputAudioMixerGroup.name == "SFX"), Is.True, path);
            }
        }

        [Test]
        public void PauseToggle_ClosesOptionsBeforeResumingGameplay()
        {
            float originalTimeScale = Time.timeScale;
            bool originalAudioPause = AudioListener.pause;
            GameObject owner = new GameObject("Pause Options Test");
            try
            {
                UIDocument document = owner.AddComponent<UIDocument>();
                document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudPath);
                AudioSettingsController audio = owner.AddComponent<AudioSettingsController>();
                audio.Configure(AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath));
                FactorySettingsPresenter settings = owner.AddComponent<FactorySettingsPresenter>();
                settings.Configure(document, audio);
                FactoryPauseController pause = owner.AddComponent<FactoryPauseController>();
                pause.Configure(null, null, null, null, null, null, null, settings);

                pause.PauseGame();
                settings.ShowOptions();
                Assert.That(settings.IsOpen, Is.True);
                Assert.That(pause.TogglePause(), Is.True);
                Assert.That(pause.IsPaused, Is.True);
                Assert.That(settings.IsOpen, Is.False);
                Assert.That(pause.TogglePause(), Is.True);
                Assert.That(pause.IsPaused, Is.False);
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                AudioListener.pause = originalAudioPause;
                Object.DestroyImmediate(owner);
            }
        }
    }
}
