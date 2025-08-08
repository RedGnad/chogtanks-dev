using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class SimpleSkinSelector : NetworkBehaviour
{
    [SerializeField] private GameObject skinPanel;
    [SerializeField] private Button[] skinButtons;
    [SerializeField] private string[] spriteNames;
    
    private NetworkObject localTankView;
    
    private const string SELECTED_SKIN_KEY = "SelectedTankSkin";
    
    private void Start()
    {
        if (skinPanel) skinPanel.SetActive(false);
        
        for (int i = 0; i < skinButtons.Length && i < spriteNames.Length; i++)
        {
            int index = i;  // Pour capture dans lambda
            if (skinButtons[i] != null)
            {
                skinButtons[i].onClick.AddListener(() => SelectSkin(index));
            }
        }
    }
    
    private void OnEnable()
    {
        PhotonTankSpawner.OnTankSpawned += OnTankSpawned;
    }
    
    private void OnDisable()
    {
        PhotonTankSpawner.OnTankSpawned -= OnTankSpawned;
    }
    
    public void ToggleSkinPanel()
    {
        if (skinPanel)
            skinPanel.SetActive(!skinPanel.activeSelf);
    }
    
    private void SelectSkin(int index)
    {
        if (index < 0 || index >= spriteNames.Length) return;
        
        PlayerPrefs.SetInt(SELECTED_SKIN_KEY, index);
        PlayerPrefs.Save();
        
        if (localTankView == null)
        {
            GameObject[] tanks = GameObject.FindGameObjectsWithTag("Player");
            foreach (var tank in tanks)
            {
                NetworkObject view = tank.GetComponent<NetworkObject>();
                if (view && view)
                {
                    localTankView = view;
                    break;
                }
            }
        }
        
        if (localTankView != null)
        {
            TankAppearanceHandler handler = localTankView.GetComponent<TankAppearanceHandler>();
            if (handler != null)
            {
                // localTankView.RPC("ChangeTankSprite", RpcTarget.AllBuffered, spriteNames[index]); // RPC removed for Fusion
                handler.ChangeTankSpriteRpc(spriteNames[index]); // Direct call instead of RPC
            }
        }
        
        if (skinPanel) skinPanel.SetActive(false);
    }
    
    private void OnTankSpawned(GameObject tank, NetworkObject view)
    {
        if (view)
        {
            localTankView = view;
            
            TankAppearanceHandler handler = tank.GetComponent<TankAppearanceHandler>();
            if (handler != null)
            {
                int savedSkinIndex = PlayerPrefs.GetInt(SELECTED_SKIN_KEY, 0);
                if (savedSkinIndex >= 0 && savedSkinIndex < spriteNames.Length)
                {
                    Debug.Log($"[SKIN] Application du skin sauvegardé: {spriteNames[savedSkinIndex]}");
                    // view.RPC("ChangeTankSprite", RpcTarget.AllBuffered, spriteNames[savedSkinIndex]); // RPC removed for Fusion
                    TankAppearanceHandler appearanceHandler = view.GetComponent<TankAppearanceHandler>();
                    if (appearanceHandler != null) appearanceHandler.ChangeTankSpriteRpc(spriteNames[savedSkinIndex]);
                }
            }
        }
    }
    
    public void HideSkinPanel()
    {
        if (skinPanel) skinPanel.SetActive(false);
    }
}