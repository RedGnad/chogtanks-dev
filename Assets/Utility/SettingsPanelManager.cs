using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelManager : MonoBehaviour
{
    [Header("Settings Panel")]
    public GameObject settingsPanel;
    public Button settingsButton;
    
    [Header("Close Buttons")]
    [Tooltip("Bouton principal pour fermer le panel")]
    public Button closeButton;
    
    [Tooltip("Boutons additionnels pour fermer le panel")]
    public Button[] additionalCloseButtons;
    
    void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
            
        if (settingsButton != null)
            settingsButton.onClick.AddListener(ToggleSettingsPanel);
            
        // Configuration du bouton de fermeture principal
        if (closeButton != null)
            closeButton.onClick.AddListener(HideSettingsPanel);
            
        // Configuration des boutons de fermeture additionnels
        if (additionalCloseButtons != null)
        {
            foreach (Button button in additionalCloseButtons)
            {
                if (button != null)
                    button.onClick.AddListener(HideSettingsPanel);
            }
        }
    }
    
    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }
    
    public void HideSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
}