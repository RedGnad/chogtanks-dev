using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MonadGamesIDIntegration : MonoBehaviour
{
    [Header("Server Configuration")]
    [SerializeField] private string serverUrl = "https://chogtanks-server.onrender.com";
    [SerializeField] private string monadGamesIdEndpoint = "/api/monad-games-id/update-player";
    
    [Header("Debug UI")]
    [SerializeField] private Text debugText;
    
    private static MonadGamesIDIntegration _instance;
    public static MonadGamesIDIntegration Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MonadGamesIDIntegration>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MonadGamesIDIntegration");
                    _instance = go.AddComponent<MonadGamesIDIntegration>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void SubmitScoreOnMint(string playerAddress, int scoreAmount, string transactionAmount)
    {
        StartCoroutine(SubmitToServerCoroutine(playerAddress, scoreAmount, transactionAmount, "mint"));
    }
    
    public void SubmitScoreOnEvolve(string playerAddress, int scoreAmount, string transactionAmount)
    {
        StartCoroutine(SubmitToServerCoroutine(playerAddress, scoreAmount, transactionAmount, "evolve"));
    }
    
    public void TestMintSubmission()
    {
        string testAddress = "0x123456789abcdef0123456789abcdef012345678";
        int testScore = 100;
        string testAmount = "1000000000000000000"; // 1 ETH in wei
        
        SubmitScoreOnMint(testAddress, testScore, testAmount);
    }
    
    public void TestEvolveSubmission()
    {
        string testAddress = "0x123456789abcdef0123456789abcdef012345678";
        int testScore = 200;
        string testAmount = "2000000000000000000"; // 2 ETH in wei
        
        SubmitScoreOnEvolve(testAddress, testScore, testAmount);
    }
    
    private IEnumerator SubmitToServerCoroutine(string playerAddress, int scoreAmount, string transactionAmount, string actionType)
    {
        ScoreSubmissionPayload payload = new ScoreSubmissionPayload
        {
            playerAddress = playerAddress,
            scoreAmount = scoreAmount,
            transactionAmount = transactionAmount,
            actionType = actionType
        };
        
        string jsonPayload = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        
        string url = $"{serverUrl}{monadGamesIdEndpoint}";
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            UpdateDebugText($"Sending to Monad Games ID: {actionType}...");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                UpdateDebugText($"Monad Games ID {actionType} success!");
            }
            else
            {
                UpdateDebugText($"Monad Games ID {actionType} failed: {request.error}");
            }
        }
    }
    
    private void UpdateDebugText(string message)
    {
        if (debugText != null)
        {
            debugText.text = message;
        }
    }
    
    [Serializable]
    private class ScoreSubmissionPayload
    {
        public string playerAddress;
        public int scoreAmount;
        public string transactionAmount;
        public string actionType;
    }
}