using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WebcamAvatar {
    public sealed class AgentMessageInput : InputField {
        public Action SendMessage;
        public override void OnUpdateSelected(BaseEventData eventData) {
            if(isFocused && Input.compositionString.Length==0 &&
               (Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)) &&
               !Input.GetKey(KeyCode.LeftShift)&&!Input.GetKey(KeyCode.RightShift)) {
                SendMessage?.Invoke();eventData.Use();return;
            }
            base.OnUpdateSelected(eventData);
        }
    }
}
