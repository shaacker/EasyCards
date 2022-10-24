using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace EasyCards.Helpers;

public static class InputHelper
{
    private static Dictionary<Key, List<Action>> s_keyEvents = new();
    private static KeyBehaviour s_keyBehaviourInstance = null;

    private class KeyBehaviour : MonoBehaviour
    {
        private void LateUpdate()
        {
            foreach (var keyEvent in s_keyEvents)
            {
                if (Keyboard.current[keyEvent.Key].wasPressedThisFrame)
                {
                    EasyCards.Log.LogInfo($"{keyEvent.Key} was pressed!");
                    foreach (var action in keyEvent.Value)
                    {
                        EasyCards.Log.LogInfo($"Invoking action!");
                        action.Invoke();
                    }
                }
            }
        }
    }
    
    public static void AddKeyListener(Key key, Action action)
    {
        if (action == null) return;

        if (s_keyBehaviourInstance == null)
        {
            s_keyBehaviourInstance = EasyCards.Instance.AddComponent<KeyBehaviour>();
        }
        
        if (!s_keyEvents.ContainsKey(key))
        {
            s_keyEvents[key] = new();
        }

        s_keyEvents[key].Add(action);
    }
}