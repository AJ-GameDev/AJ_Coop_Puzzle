using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIVirtualButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Output")] public BoolEvent buttonStateOutputEvent;

    public Event buttonClickOutputEvent;

    public void OnPointerClick(PointerEventData eventData)
    {
        OutputButtonClickEvent();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OutputButtonStateValue(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OutputButtonStateValue(false);
    }

    void OutputButtonStateValue(bool buttonState)
    {
        buttonStateOutputEvent.Invoke(buttonState);
    }

    void OutputButtonClickEvent()
    {
        buttonClickOutputEvent.Invoke();
    }

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool>
    {
    }

    [System.Serializable]
    public class Event : UnityEvent
    {
    }
}