using Scribe.Tools;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Scribe
{
    public class SceneManager
    {
        public static async void TransitionTo(SceneField scene)
        {
            TransitionTo(scene.BuildIndex);
        }

        public static async void TransitionTo(int sceneBuildIdx)
        {
            UnitySceneManager.LoadSceneAsync(sceneBuildIdx);
        }

        public static async void TransitionTo(string name)
        {
            UnitySceneManager.LoadSceneAsync(name);
        }
    }
}
