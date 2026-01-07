using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class PointerClickHandler : MonoBehaviour, IPointerClickHandler
{
    public Action OnRightClickEvent;
    public Action OnLeftClickEvent;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClickEvent?.Invoke();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClickEvent?.Invoke();
        }
    }
}