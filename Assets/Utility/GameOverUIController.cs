using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Photon.Pun;

public class GameOverUIController : MonoBehaviourPunCallbacks
{
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI countdownText; 
    
    public void SetCountdownText(TextMeshProUGUI text)
    {
        countdownText = text;
    }

    private void Awake()
    {
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (winText      != null) winText.gameObject.SetActive(false);
        if (winnerText   != null) winnerText.gameObject.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (gameOverText != null) gameOverText.gameObject.SetActive(true);
        if (winText      != null) winText.gameObject.SetActive(false);
        if (winnerText   != null) winnerText.gameObject.SetActive(false); 
    }

    public void ShowWin(string winnerName = "")
    {
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (winText      != null) 
        {
            winText.gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(winnerName))
            {
                winText.text = $"You Win, {winnerName}!";
            }
        }
        if (winnerText   != null) winnerText.gameObject.SetActive(false); // NOUVEAU
    }

    public void ShowWinner(string winnerName)
    {
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (winText      != null) winText.gameObject.SetActive(false);
        if (winnerText   != null) 
        {
            winnerText.gameObject.SetActive(true);
            winnerText.text = $"{winnerName} Wins!";
        }
        
        StartCoroutine(CountdownAndReturnToLobby(6));
    }
    
    public IEnumerator CountdownAndReturnToLobby(int seconds)
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }
        
        for (int i = seconds; i > 0; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = $"Match ended: returning to lobby in {i}...";
            }
            
            yield return new WaitForSeconds(1.0f);
        }
        
        if (countdownText != null)
        {
            countdownText.text = "Returning to lobby...";
        }
        
        
        LobbyUI lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.OnBackToLobby();
        }
        else
        {
            Debug.LogError("[GAMEOVER] LobbyUI non trouvé ! Impossible de simuler le bouton Back");
        }
    }
    
    private class LobbySceneLoader : MonoBehaviourPunCallbacks
    {
        private string _lobbySceneName;
        
        public LobbySceneLoader(string lobbySceneName)
        {
            _lobbySceneName = lobbySceneName;
        }
        
        public override void OnLeftRoom()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(_lobbySceneName);
            
            PhotonNetwork.RemoveCallbackTarget(this);
        }
    }
    
    public void HideAll()
    {
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (winText      != null) winText.gameObject.SetActive(false);
        if (winnerText   != null) winnerText.gameObject.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }
}
