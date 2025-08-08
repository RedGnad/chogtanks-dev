using System.Linq;
using Newtonsoft.Json;
using Reown.AppKit.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sample
{
    public class AppInit : MonoBehaviour
    {
        [SerializeField] private SceneReference _mainScene;

        [Space]
        [SerializeField] private GameObject _debugConsole;

        private void Start()
        {
            // Debug console and Mixpanel integrations removed
            SceneManager.LoadScene(_mainScene);
        }

        // Debug console initialization removed

        // Mixpanel configuration removed

        // Console methods have been removed as they required the debug console package
    }
}