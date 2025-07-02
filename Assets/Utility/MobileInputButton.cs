using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInputButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    public bool IsPressed { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
        Debug.Log($"{name} OnPointerDown");
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
        Debug.Log($"{name} OnPointerUp");
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        IsPressed = true;
        Debug.Log($"{name} OnPointerEnter");
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        IsPressed = false;
        Debug.Log($"{name} OnPointerExit");
    }
}