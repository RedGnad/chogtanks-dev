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
    public UnityEngine.UI.Button button;
    
    public TMPro.TextMeshProUGUI lockedText;
    
    public string lockedMessage = "NFT requis";
    
    public List<string> requiredNFTContracts = new List<string>();
    
    [Range(1, 10)]
    public int minNFTsRequired = 1;
    
    public Color unlockedColor = Color.white;
    
    public Color lockedColor = Color.gray;
}

[Serializable]
public class NFTCondition
{
    public enum Standard { ERC721, ERC1155 }
    public enum UnlockMode { AnyToken, SpecificToken }

    public Standard standard = Standard.ERC1155;

    public UnlockMode unlockMode = UnlockMode.AnyToken;

    public string contractAddress;

    public List<string> tokenIds = new List<string>();

    public string successMessage = "NFT detected !";
}

public class NFTVerification : MonoBehaviour
{
    [Header("Configuration")]
    public string rpcUrl = "https://testnet-rpc.monad.xyz";

    public List<NFTCondition> conditions = new List<NFTCondition>();

    [Header("UI Elements")]
    public TextMeshProUGUI statusText;
    
    public string customSuccessMessage = "";
    
    public string noNFTOwnedMessage = "";

    [Header("Button Management")]
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
            bool signApproved = PlayerPrefs.GetInt("personalSignApproved", 0) == 1;
            if (signApproved)
            {
                StartCoroutine(CheckAllNFTs());
            }
            else
            {
                Debug.Log("[NFT-VERIFICATION] Wallet found but personal sign required");
            }
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
        UpdateStatus("Verifying NFTs...", true);
        
        if (string.IsNullOrEmpty(currentWallet))
        {
            string error = "No Connected Wallet";
            UpdateStatus(error, true);
            
            LockAllButtons();
            yield break;
        }

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
        
    }

    private void CheckWalletUpdate()
    {
        string savedWallet = PlayerPrefs.GetString("walletAddress", "");
        
        bool signApproved = PlayerPrefs.GetInt("personalSignApproved", 0) == 1;
        
        if (!string.IsNullOrEmpty(savedWallet) && signApproved && savedWallet != currentWallet)
        {
            currentWallet = savedWallet;
            StartCoroutine(CheckAllNFTs());
        }
        else if (!string.IsNullOrEmpty(savedWallet) && signApproved && savedWallet == currentWallet)
        {
            RefreshButtonStatesOnly();
        }
        else if (!signApproved && !string.IsNullOrEmpty(currentWallet))
        {
            currentWallet = "";
            LockAllButtons();
        }
    }
    
    private void RefreshButtonStatesOnly()
    {
        foreach (var rule in buttonUnlockRules)
        {
            if (rule.button == null || rule.lockedText == null) continue;
            
            if (!string.IsNullOrEmpty(currentWallet) && rule.lockedText.text == "Connect Wallet")
            {
                rule.lockedText.text = rule.lockedMessage;
            }
        }
    }
    
    private void OnDestroy()
    {
        CancelInvoke(nameof(CheckWalletUpdate));
    }
}
