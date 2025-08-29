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
            SceneManager.LoadScene(_mainScene);
        }

    }
}