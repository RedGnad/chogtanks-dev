using TMPro;
using UnityEngine;

public class GameOverUIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI winText;

    private void Awake()
    {
        if (gameOverText   != null) gameOverText.gameObject.SetActive(false);
        if (winText        != null) winText.gameObject.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (gameOverText != null) gameOverText.gameObject.SetActive(true);
        if (winText      != null) winText.gameObject.SetActive(false);
    }

    public void ShowWin()
    {
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (winText      != null) winText.gameObject.SetActive(true);
    }
}
