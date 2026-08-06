using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MyFramework.UI.Element
{
    public class MouseInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action MouseEnter;

        public Action MouseExit;

        public void OnPointerEnter(PointerEventData eventData)
        {
            MouseEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            MouseExit?.Invoke();
        }
    }
}