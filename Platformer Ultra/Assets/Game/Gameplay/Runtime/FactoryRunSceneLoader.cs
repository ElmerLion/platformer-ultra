using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlatformerUltra.Gameplay
{
    public static class FactoryRunSceneLoader
    {
        public const string IntroSceneName = "FactoryIntro";
        public const string FactorySceneName = "FactoryVerticalMap";

        public static AsyncOperation LoadNewRun()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (Application.CanStreamedLevelBeLoaded(IntroSceneName))
            {
                return SceneManager.LoadSceneAsync(IntroSceneName, LoadSceneMode.Single);
            }

            Debug.LogWarning($"{IntroSceneName} is unavailable; reloading the active scene instead.");
            Scene activeScene = SceneManager.GetActiveScene();
            return !string.IsNullOrWhiteSpace(activeScene.path)
                ? SceneManager.LoadSceneAsync(activeScene.path, LoadSceneMode.Single)
                : SceneManager.LoadSceneAsync(activeScene.buildIndex, LoadSceneMode.Single);
        }
    }
}
