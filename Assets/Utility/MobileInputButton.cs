using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInputButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    public bool IsPressed { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        IsPressed = true;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        IsPressed = false;
    }
}