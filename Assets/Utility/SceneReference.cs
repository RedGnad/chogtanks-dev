using UnityEngine;

namespace Sample
{
    [System.Serializable]
    public class SceneReference : ISerializationCallbackReceiver
    {
        [SerializeField] private string scenePath = "";
        [SerializeField] private string sceneName = "";

        public string SceneName => sceneName;
        public string ScenePath => scenePath;

        public void OnBeforeSerialize()
        {
            // ...
        }

        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(scenePath))
            {
                int lastSlash = scenePath.LastIndexOf('/');
                if (lastSlash >= 0 && lastSlash < scenePath.Length - 1)
                {
                    sceneName = scenePath.Substring(lastSlash + 1);
                    int dotIndex = sceneName.LastIndexOf('.');
                    if (dotIndex > 0)
                    {
                        sceneName = sceneName.Substring(0, dotIndex);
                    }
                }
            }
        }

        public static implicit operator string(SceneReference sceneReference)
        {
            return sceneReference.SceneName;
        }
    }
}
