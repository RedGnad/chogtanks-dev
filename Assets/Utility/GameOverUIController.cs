using TMPro;
using UnityEngine;

public class GameOverUIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private TextMeshProUGUI winnerText; // NOUVEAU : Texte pour afficher le gagnant

    private void Awake()
    {
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (winText      != null) winText.gameObject.SetActive(false);
        if (winnerText   != null) winnerText.gameObject.SetActive(false); // NOUVEAU
    }

    public void ShowGameOver()
    {
        if (gameOverText != null) gameOverText.gameObject.SetActive(true);
        if (winText      != null) winText.gameObject.SetActive(false);
        if (winnerText   != null) winnerText.gameObject.SetActive(false); // NOUVEAU
    }

    // MODIFICATION : Ajouter le paramètre winnerName
    public void ShowWin(string winnerName = "")
    {
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (winText      != null) 
        {
            winText.gameObject.SetActive(true);
            // OPTIONNEL : Personnaliser le message de victoire
            if (!string.IsNullOrEmpty(winnerName))
            {
                winText.text = $"You Win, {winnerName}!";
            }
        }
        if (winnerText   != null) winnerText.gameObject.SetActive(false); // NOUVEAU
    }

    // NOUVELLE MÉTHODE : Afficher le gagnant pour les perdants
    public void ShowWinner(string winnerName)
    {
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (winText      != null) winText.gameObject.SetActive(false);
        if (winnerText   != null) 
        {
            winnerText.gameObject.SetActive(true);
            winnerText.text = $"{winnerName} Wins!";
        }
    }
    
    public void HideAll()
    {
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (winText      != null) winText.gameObject.SetActive(false);
        if (winnerText   != null) winnerText.gameObject.SetActive(false); // NOUVEAU
    }
}