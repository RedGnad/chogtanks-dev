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
        
        
        var canvas = container.GetComponentInParent<Canvas>();
        
        var canvasGroup = container.GetComponentInParent<CanvasGroup>();
        if (canvasGroup != null)
        {
            Debug.Log($"[SIMPLE-TEST] CanvasGroup alpha: {canvasGroup.alpha}");
            Debug.Log($"[SIMPLE-TEST] CanvasGroup interactable: {canvasGroup.interactable}");
        }
        
        ClearContainer();
        
        for (int i = 0; i < 3; i++)
        {
            
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
                #else
                rect.sizeDelta = new Vector2(150, 100);
                rect.anchoredPosition = new Vector2(0, -110 * i);
                #endif
            }
            
            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"TEST NFT #{i + 1}";
                text.color = Color.white;
                text.fontSize = 16;
                text.gameObject.SetActive(true);
            }
            
            var image = item.GetComponentInChildren<Image>();
            if (image != null)
            {
                image.color = Color.blue;
                image.gameObject.SetActive(true);
            }
            
        }
        
        StartCoroutine(ForceRefresh());
    }
    
    void ClearContainer()
    {
        
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
            DestroyImmediate(child.gameObject);
        }
    }
    
    IEnumerator ForceRefresh()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
        
        for (int i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i);
            var rect = child.GetComponent<RectTransform>();
        }
    }
    
    [ContextMenu("Test Simple")]
    public void TestSimple()
    {
        TestCreateElements();
    }
}