using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleNFTDisplay : MonoBehaviour
{
    [Header("Test Simple")]
    public Transform container;
    public GameObject prefab;
    public Button testButton;
    
    void Start()
    {
        if (testButton != null)
            testButton.onClick.AddListener(TestCreateElements);
    }
    
    public void TestCreateElements()
    {
        Debug.Log("[SIMPLE-TEST] === DIAGNOSTIC WEBGL ===");
        
        if (container == null)
        {
            Debug.LogError("[SIMPLE-TEST] Container NULL!");
            return;
        }
        
        if (prefab == null)
        {
            Debug.LogError("[SIMPLE-TEST] Prefab NULL!");
            return;
        }
        
        Debug.Log($"[SIMPLE-TEST] Container: {container.name}");
        Debug.Log($"[SIMPLE-TEST] Container actif: {container.gameObject.activeInHierarchy}");
        Debug.Log($"[SIMPLE-TEST] Container parent: {container.parent?.name}");
        
        var canvas = container.GetComponentInParent<Canvas>();
        Debug.Log($"[SIMPLE-TEST] Canvas trouvé: {(canvas != null ? canvas.name : "NULL")}");
        
        var canvasGroup = container.GetComponentInParent<CanvasGroup>();
        if (canvasGroup != null)
        {
            Debug.Log($"[SIMPLE-TEST] CanvasGroup alpha: {canvasGroup.alpha}");
            Debug.Log($"[SIMPLE-TEST] CanvasGroup interactable: {canvasGroup.interactable}");
        }
        
        ClearContainer();
        
        for (int i = 0; i < 3; i++)
        {
            Debug.Log($"[SIMPLE-TEST] Création élément {i + 1}");
            
            GameObject item = Instantiate(prefab, container);
            item.name = $"TestItem_{i}";
            item.SetActive(true);
            
            var rect = item.GetComponent<RectTransform>();
            if (rect != null)
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(50, 400 - (i * 100));
                rect.sizeDelta = new Vector2(300, 80);
                Debug.Log($"[SIMPLE-TEST] WEBGL: Item {i} position absolue: {rect.anchoredPosition}");
                #else
                rect.sizeDelta = new Vector2(150, 100);
                rect.anchoredPosition = new Vector2(0, -110 * i);
                Debug.Log($"[SIMPLE-TEST] EDITOR: Item {i} position: {rect.anchoredPosition}");
                #endif
            }
            
            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"TEST NFT #{i + 1}";
                text.color = Color.white;
                text.fontSize = 16;
                text.gameObject.SetActive(true);
                Debug.Log($"[SIMPLE-TEST] Text défini: {text.text}");
            }
            
            var image = item.GetComponentInChildren<Image>();
            if (image != null)
            {
                image.color = Color.blue;
                image.gameObject.SetActive(true);
                Debug.Log($"[SIMPLE-TEST] Image configurée en bleu");
            }
            
            Debug.Log($"[SIMPLE-TEST] Item {i} créé, actif: {item.activeInHierarchy}");
        }
        
        Debug.Log($"[SIMPLE-TEST] Container enfants: {container.childCount}");
        
        StartCoroutine(ForceRefresh());
    }
    
    void ClearContainer()
    {
        Debug.Log($"[SIMPLE-TEST] Nettoyage container: {container.childCount} enfants");
        
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
            Debug.Log($"[SIMPLE-TEST] Suppression: {child.name}");
            DestroyImmediate(child.gameObject);
        }
        
        Debug.Log($"[SIMPLE-TEST] Container nettoyé: {container.childCount} enfants restants");
    }
    
    IEnumerator ForceRefresh()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
        Debug.Log("[SIMPLE-TEST] Refresh forcé");
        
        Debug.Log($"[SIMPLE-TEST] === ÉTAT FINAL ===");
        for (int i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i);
            var rect = child.GetComponent<RectTransform>();
            Debug.Log($"[SIMPLE-TEST] Enfant {i}: {child.name}, Actif: {child.gameObject.activeInHierarchy}, Position: {rect.anchoredPosition}");
        }
    }
    
    [ContextMenu("Test Simple")]
    public void TestSimple()
    {
        TestCreateElements();
    }
}