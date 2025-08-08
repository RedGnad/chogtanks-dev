using UnityEngine;
using Fusion;

public class TankAppearanceHandler : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer tankSpriteRenderer;
    
    private void Awake()
    {
        if (tankSpriteRenderer == null)
        {
            tankSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        if (!gameObject.CompareTag("Player"))
        {
            gameObject.tag = "Player";
        }
    }
    
    private void Start()
    {
        if (Object)
        {
            int selectedSkin = PlayerPrefs.GetInt("SelectedTankSkin", 0);
            string skinName = GetSkinNameForIndex(selectedSkin);
            
            if (!string.IsNullOrEmpty(skinName))
            {
                ChangeTankSpriteRpc(skinName);
            }
        }
    }
    
        private string GetSkinNameForIndex(int index)
    {
        string[] skins = new string[] 
        { 
            "chog",
            "molazi", 
            "nini",
            "steve",
            "bananachog",
            "beholdak",
            "mouch",
        };
        
        if (index >= 0 && index < skins.Length)
        {
            return skins[index];
        }
        
        return null;
    }
    
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void ChangeTankSpriteRpc(string spriteName)
    {
        Sprite newSprite = Resources.Load<Sprite>("TankSprites/" + spriteName);
        
        if (newSprite == null)
        {
            newSprite = Resources.Load<Sprite>(spriteName);
        }
        
        if (newSprite != null && tankSpriteRenderer != null)
        {
            tankSpriteRenderer.sprite = newSprite;
        }
    }
}