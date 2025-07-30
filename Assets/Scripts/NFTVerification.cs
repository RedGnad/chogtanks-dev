using UnityEngine;
using TMPro;
using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

[Serializable]
public class ButtonUnlockRule
{
    [Tooltip("Le bouton à activer/désactiver")]
    public UnityEngine.UI.Button button;
    
    [Tooltip("Texte TMP à afficher quand le bouton est verrouillé (optionnel)")]
    public TMPro.TextMeshProUGUI lockedText;
    
    [Tooltip("Message à afficher quand verrouillé")]
    public string lockedMessage = "NFT requis";
    
    [Tooltip("Liste des adresses de contrats NFT")]
    public List<string> requiredNFTContracts = new List<string>();
    
    [Tooltip("Nombre minimum de NFTs différents requis pour débloquer")]
    [Range(1, 10)]
    public int minNFTsRequired = 1;
    
    [Tooltip("Couleur du bouton quand débloqué")]
    public Color unlockedColor = Color.white;
    
    [Tooltip("Couleur du bouton quand verrouillé")]
    public Color lockedColor = Color.gray;
}

[Serializable]
public class NFTCondition
{
    public enum Standard { ERC721, ERC1155 }
    public enum UnlockMode { AnyToken, SpecificToken }

    [Tooltip("ERC-721 ou ERC-1155")]
    public Standard standard = Standard.ERC1155;

    [Tooltip("AnyToken = n'importe quel jeton | SpecificToken = IDs listés")]
    public UnlockMode unlockMode = UnlockMode.AnyToken;

    [Tooltip("Adresse du contrat NFT")]
    public string contractAddress;

    [Tooltip("Liste des tokenIds (pour SpecificToken), ou vide pour AnyToken")]
    public List<string> tokenIds = new List<string>();

    [Tooltip("Texte à afficher si la condition est remplie")]
    public string successMessage = "NFT détecté !";
}

public class NFTVerification : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("URL du RPC (par défaut: Monad Testnet)")]
    public string rpcUrl = "https://testnet-rpc.monad.xyz";

    [Tooltip("Liste des conditions NFT à vérifier")]
    public List<NFTCondition> conditions = new List<NFTCondition>();

    [Header("UI Elements")]
    [Tooltip("Texte UI (TMP) qui s'affichera uniquement si les conditions NFT sont remplies")]
    public TextMeshProUGUI statusText;
    
    [Tooltip("Message à afficher quand le NFT est détecté (laisser vide pour utiliser le message de la condition)")]
    public string customSuccessMessage = "";
    
    [Tooltip("Message à afficher quand aucun NFT n'est détecté")]
    public string noNFTOwnedMessage = "";

    [Header("Button Management")]
    [Tooltip("Configuration des boutons à débloquer selon les NFTs")]
    public List<ButtonUnlockRule> buttonUnlockRules = new List<ButtonUnlockRule>();

    const string SEL_ERC1155_BALANCE = "0x00fdd58e";  
    const string SEL_ERC721_BALANCE  = "0x70a08231";  
    const string SEL_ERC721_OWNER    = "0x6352211e";
    const string SIG_ERC1155_LOG     = "0xc3d58168c5ab16844f149d4b3945f6c6af9a1c1e0db3a9e6b207d0e2de5e2c8b";

    private string currentWallet;

    private void Start()
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }
        LockAllButtons();
        currentWallet = PlayerPrefs.GetString("walletAddress", "");
        if (!string.IsNullOrEmpty(currentWallet))
        {
            StartCoroutine(CheckAllNFTs());
        }
        
        InvokeRepeating(nameof(CheckWalletUpdate), 1f, 2f);
    }

    public void StartVerification()
    {
        if (string.IsNullOrEmpty(currentWallet))
        {
            UpdateStatus("Connect a Wallet First");
            return;
        }
        StartCoroutine(CheckAllNFTs());
    }

    IEnumerator CheckAllNFTs()
    {
        Debug.Log("[NFT-DEBUG] Début de CheckAllNFTs");
        UpdateStatus("Verifying NFTs...", true);
        
        if (string.IsNullOrEmpty(currentWallet))
        {
            string error = "No Connected Wallet";
            Debug.Log("[NFT-DEBUG] Erreur: pas de wallet connecté");
            UpdateStatus(error, true);
            
            LockAllButtons();
            yield break;
        }

        Debug.Log($"[NFT-DEBUG] Vérification avec wallet: {currentWallet}");
        bool anyNFTFound = false;
        
        foreach (var condition in conditions)
        {
            bool ownsNFT = false;
            
            if (condition.standard == NFTCondition.Standard.ERC1155)
            {
                if (condition.unlockMode == NFTCondition.UnlockMode.AnyToken)
                {
                    yield return StartCoroutine(CheckAnyTokenERC1155(
                        condition.contractAddress, 
                        currentWallet,
                        result => ownsNFT = result
                    ));
                }
                else
                {
                    foreach (var tokenId in condition.tokenIds)
                    {
                        yield return StartCoroutine(CheckBalance1155(
                            condition.contractAddress, 
                            currentWallet, 
                            tokenId,
                            result => ownsNFT |= result
                        ));
                        if (ownsNFT) break;
                    }
                }
            }
            else
            {
                if (condition.unlockMode == NFTCondition.UnlockMode.AnyToken)
                {
                    yield return StartCoroutine(CheckBalance721(
                        condition.contractAddress, 
                        currentWallet,
                        result => ownsNFT = result
                    ));
                }
                else
                {
                    foreach (var tokenId in condition.tokenIds)
                    {
                        yield return StartCoroutine(CheckOwnerOf721(
                            condition.contractAddress, 
                            currentWallet, 
                            tokenId,
                            result => ownsNFT |= result
                        ));
                        if (ownsNFT) break;
                    }
                }
            }

            if (ownsNFT)
            {
                anyNFTFound = true;
                yield return null;
                
                if (statusText != null)
                {
                    string finalMessage = string.IsNullOrEmpty(customSuccessMessage) ? condition.successMessage : customSuccessMessage;
                    statusText.text = finalMessage;
                    statusText.gameObject.SetActive(true);
                }
                
                break;
            }
        }

        if (!anyNFTFound)
        {
            if (!string.IsNullOrEmpty(noNFTOwnedMessage))
            {
                UpdateStatus(noNFTOwnedMessage, true);
            }
            else
            {
                if (statusText != null)
                {
                    statusText.gameObject.SetActive(false);
                }
            }
        }
        
        yield return StartCoroutine(CheckButtonUnlocks());
    }

    IEnumerator CheckBalance1155(string contract, string wallet, string tokenId, Action<bool> cb)
    {
        string ownerHex = wallet.StartsWith("0x") ? wallet.Substring(2).PadLeft(64, '0') : wallet.PadLeft(64, '0');
        string idHex = BigInteger.Parse(tokenId).ToString("X").PadLeft(64, '0');
        string data = SEL_ERC1155_BALANCE + ownerHex + idHex;
        
        yield return CallRpc(contract, data, cb, res =>
        {
            var bal = BigInteger.Parse(res.Substring(2), System.Globalization.NumberStyles.HexNumber);
            return bal > 0;
        });
    }

    IEnumerator CheckBalance721(string contract, string wallet, Action<bool> cb)
    {
        string ownerHex = wallet;
        if (wallet.StartsWith("0x")) ownerHex = wallet.Substring(2);
        ownerHex = ownerHex.ToLower().PadLeft(64, '0');
        
        string data = SEL_ERC721_BALANCE + ownerHex;
        
        yield return CallRpc(contract, data, cb, res =>
        {
            if (string.IsNullOrEmpty(res) || res == "0x")
            {
                return false;
            }
            
            try
            {
                var bal = BigInteger.Parse(res.Substring(2), System.Globalization.NumberStyles.HexNumber);
                return bal > 0;
            }
            catch (Exception)
            {
                return false;
            }
        });
    }

    IEnumerator CheckOwnerOf721(string contract, string wallet, string tokenId, Action<bool> cb)
    {
        string idHex = BigInteger.Parse(tokenId).ToString("X").PadLeft(64, '0');
        string data = SEL_ERC721_OWNER + idHex;
        
        yield return CallRpc(contract, data, cb, res =>
        {
            string owner = "0x" + res.Substring(res.Length - 40);
            return string.Equals(owner, wallet, StringComparison.OrdinalIgnoreCase);
        });
    }

    IEnumerator CheckAnyTokenERC1155(string contract, string wallet, Action<bool> cb)
    {
        BigInteger latest = 0;
        yield return StartCoroutine(CallRpcRaw(new JObject{
            ["jsonrpc"]="2.0", ["method"]="eth_blockNumber", ["params"]=new JArray(), ["id"]=1
        }, json => {
            latest = BigInteger.Parse(
                JObject.Parse(json)["result"].Value<string>().Substring(2),
                System.Globalization.NumberStyles.HexNumber
            );
        }));

        string topicTo = "0x" + wallet.Substring(2).PadLeft(64, '0');
        BigInteger chunk = 100, start = 0; 
        bool found = false;
        
        while (start <= latest && !found)
        {
            BigInteger end = BigInteger.Min(start + chunk - 1, latest);
            var filter = new JObject{
                ["address"] = contract,
                ["fromBlock"] = "0x" + start.ToString("X"),
                ["toBlock"] = "0x" + end.ToString("X"),
                ["topics"] = new JArray(SIG_ERC1155_LOG, null, null, topicTo)
            };
            
            yield return StartCoroutine(CallRpcRaw(new JObject{
                ["jsonrpc"]="2.0", ["method"]="eth_getLogs",
                ["params"]=new JArray(filter), ["id"]=1
            }, json => {
                var logs = JObject.Parse(json)["result"] as JArray;
                if (logs != null && logs.Count > 0) found = true;
            }));
            
            start += chunk;
        }
        
        cb(found);
    }

    IEnumerator CallRpc(string contract, string data, Action<bool> cb, Func<string, bool> parse)
    {
        var payload = new JObject(
            new JProperty("jsonrpc", "2.0"),
            new JProperty("method", "eth_call"),
            new JProperty("params", new JArray(
                new JObject(
                    new JProperty("to", contract), 
                    new JProperty("data", data)
                ),
                "latest"
            )),
            new JProperty("id", 1)
        );
        
        yield return CallRpcRaw(payload, json => {
            if (string.IsNullOrEmpty(json))
            {
                cb(false);
                return;
            }
            
            try
            {
                var response = JObject.Parse(json);
                
                if (response["error"] != null)
                {
                    cb(false);
                    return;
                }
                
                string res = response["result"].Value<string>();
                bool parseResult = parse(res);
                cb(parseResult);
            }
            catch (Exception)
            {
                string errorMsg = "No NFT found";
                UpdateStatus(errorMsg, true); 
                cb(false);
            }
        });
    }

    IEnumerator CallRpcRaw(JObject payload, Action<string> onResult)
    {
        using var uwr = new UnityEngine.Networking.UnityWebRequest(rpcUrl, "POST")
        {
            uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(
                System.Text.Encoding.UTF8.GetBytes(payload.ToString())
            ),
            downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer()
        };
        
        uwr.SetRequestHeader("Content-Type", "application/json");
        yield return uwr.SendWebRequest();
        
        if (uwr.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            onResult(null);
        }
        else
        {
            onResult(uwr.downloadHandler.text);
        }
    }

    private void UpdateStatus(string message, bool hideAfterDelay = false)
    {
        if (statusText != null)
        {
            CancelInvoke(nameof(HideStatus));
            
            statusText.text = message;
            statusText.gameObject.SetActive(true);
            
            if (hideAfterDelay)
            {
                Invoke(nameof(HideStatus), 3f);
            }
        }
    }
    
    private IEnumerator HideStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }
    }
    
    private void HideStatus()
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }
    }

    public void DisconnectWallet()
    {
        currentWallet = "";
        PlayerPrefs.DeleteKey("walletAddress");
        PlayerPrefs.Save();
        UpdateStatus("Déconnecté");
        
        LockAllButtons();
    }
    
    public void ForceNFTCheck()
    {
        
        if (!string.IsNullOrEmpty(currentWallet))
        {
            StartCoroutine(CheckAllNFTs());
            return;
        }
        
        string savedAddress = PlayerPrefs.GetString("walletAddress", "");
        
        if (!string.IsNullOrEmpty(savedAddress))
        {
            currentWallet = savedAddress;
            StartCoroutine(CheckAllNFTs());
        }
        else
        {
            if (statusText != null)
            {
                statusText.text = "Wallet connection required";
                statusText.gameObject.SetActive(true);
            }
        }
    }
    
    IEnumerator CheckButtonUnlocks()
    {
        
        for (int ruleIndex = 0; ruleIndex < buttonUnlockRules.Count; ruleIndex++)
        {
            var rule = buttonUnlockRules[ruleIndex];
            if (rule.button == null || rule.requiredNFTContracts.Count == 0)
            {
                continue;
            }
                
            yield return StartCoroutine(CheckButtonRule(rule));
        }
        
    }
    
    IEnumerator CheckButtonRule(ButtonUnlockRule rule)
    {
        if (string.IsNullOrEmpty(currentWallet))
        {
            UpdateButtonFromRule(rule, false);
            yield break;
        }
        
        int nftsOwned = 0;
        
        foreach (string contractAddress in rule.requiredNFTContracts)
        {
            if (string.IsNullOrEmpty(contractAddress)) 
            {
                continue;
            }
            
            Debug.Log($"[NFT-DEBUG] Vérification contrat: {contractAddress}");
            bool ownsThisNFT = false;
            
            yield return StartCoroutine(CheckBalance721(contractAddress, currentWallet, result => {
                ownsThisNFT = result;
                Debug.Log($"[NFT-DEBUG] Résultat ERC721 pour {contractAddress}: {result}");
            }));
            
            if (!ownsThisNFT)
            {
                yield return StartCoroutine(CheckAnyTokenERC1155(contractAddress, currentWallet, result => {
                    ownsThisNFT = result;
                }));
            }
            
            if (ownsThisNFT)
            {
                nftsOwned++;
            }
            else
            {
                Debug.Log($"[NFT-DEBUG] Aucun NFT trouvé pour contrat {contractAddress}");
            }
        }
        
        bool shouldUnlock = nftsOwned >= rule.minNFTsRequired;
        UpdateButtonFromRule(rule, shouldUnlock);
    }
    
    private void UpdateButtonFromRule(ButtonUnlockRule rule, bool isUnlocked)
    {
        if (rule.button == null) return;
            
        rule.button.interactable = isUnlocked;
        
        UnityEngine.UI.Image buttonImage = rule.button.GetComponent<UnityEngine.UI.Image>();
        if (buttonImage != null)
        {
            buttonImage.color = isUnlocked ? rule.unlockedColor : rule.lockedColor;
        }
        
        if (rule.lockedText != null)
        {
            if (isUnlocked)
            {
                rule.lockedText.gameObject.SetActive(false);
            }
            else
            {
                if (string.IsNullOrEmpty(currentWallet))
                {
                    rule.lockedText.text = "Connect Wallet";
                }
                else
                {
                    rule.lockedText.text = rule.lockedMessage;
                }
                rule.lockedText.gameObject.SetActive(true);
            }
        }
    }
    
    private void LockAllButtons()
    {
        foreach (var rule in buttonUnlockRules)
        {
            if (rule.button != null)
            {
                UpdateButtonFromRule(rule, false);
            }
        }
        
        // Masquer les textes NFT/XP en même temps que les boutons
        ChogTanksNFTManager nftManager = FindObjectOfType<ChogTanksNFTManager>();
        if (nftManager != null)
        {
            nftManager.HideLevelUI();
        }
    }

    private void CheckWalletUpdate()
    {
        string savedWallet = PlayerPrefs.GetString("walletAddress", "");
        
        if (!string.IsNullOrEmpty(savedWallet) && savedWallet != currentWallet)
        {
            Debug.Log($"[NFT-VERIFICATION] Wallet update detected: {currentWallet} → {savedWallet}");
            currentWallet = savedWallet;
            StartCoroutine(CheckAllNFTs());
        }
        else if (!string.IsNullOrEmpty(savedWallet) && savedWallet == currentWallet)
        {
            RefreshButtonStatesOnly();
        }
    }
    
    private void RefreshButtonStatesOnly()
    {
        foreach (var rule in buttonUnlockRules)
        {
            if (rule.button == null || rule.lockedText == null) continue;
            
            if (!string.IsNullOrEmpty(currentWallet) && rule.lockedText.text == "Connect Wallet")
            {
                Debug.Log($"[NFT-VERIFICATION] Fixing UI text for button, showing: {rule.lockedMessage}");
                rule.lockedText.text = rule.lockedMessage;
            }
        }
    }
    
    private void OnDestroy()
    {
        CancelInvoke(nameof(CheckWalletUpdate));
    }
}
