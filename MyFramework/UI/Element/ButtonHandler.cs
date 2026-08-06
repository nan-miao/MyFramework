using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyFramework.UI.Element
{
    public class ButtonHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Button button;

        public Action MouseEnter;

        public Action MouseExit;

        protected virtual void Start()
        {
            button = GetComponent<Button>();
        }

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