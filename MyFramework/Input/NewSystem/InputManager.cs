using MyFramework.AssetLoad.AA;
using MyFramework.Core.Singleton;

namespace MyFramework.NewSystem
{
    using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : SingletonAutoMono<InputManager>
{
    [SerializeField] private InputActionAsset inputActionAsset;
    private Dictionary<InputActionType, InputAction> inputActions = new Dictionary<InputActionType, InputAction>();
    private List<InputActionMapType> inputActionMaps = new List<InputActionMapType>();
    
    // 添加这些字典来跟踪回调
    private Dictionary<InputActionType, List<Action<InputAction.CallbackContext>>> _startCallbacks = 
        new Dictionary<InputActionType, List<Action<InputAction.CallbackContext>>>();
    private Dictionary<InputActionType, List<Action<InputAction.CallbackContext>>> _performedCallbacks = 
        new Dictionary<InputActionType, List<Action<InputAction.CallbackContext>>>();
    private Dictionary<InputActionType, List<Action<InputAction.CallbackContext>>> _canceledCallbacks = 
        new Dictionary<InputActionType, List<Action<InputAction.CallbackContext>>>();

    protected override void OnStart()
    {
        LoadAsset();
    }

    public AddressablesInfo LoadAsset()
    {
        if (inputActionAsset != null)
            return null;
        
        AddressablesInfo info= AddressablesManager.Instance.LoadAssetAsync<InputActionAsset>("GameInput", (obj) =>
        {
            inputActionAsset = obj.Result;
            // 只启用 GamePlay，不启用全部
            inputActionAsset.FindActionMap("GamePlay").Enable();
        });

        return info;
    }


    /// <summary>
    /// 注册输入事件,若已存在该事件则启用该事件监听。
    /// </summary>
    /// <param name="inputActionType">输入事件类型</param>
    /// <param name="inputActionMapName"></param>
    public InputAction RegisterInputAction(InputActionType inputActionType, InputActionMapType inputActionMapName)
    {
        if (inputActions.ContainsKey(inputActionType))
        {
            inputActions[inputActionType].Enable();
            return inputActions[inputActionType];
        }
        
        if (inputActionAsset == null)
        {
            var op = LoadAsset();
            op.handle.WaitForCompletion();
        }
        InputAction newInputAction = inputActionAsset.FindActionMap(inputActionMapName.ToString())?.FindAction(inputActionType.ToString());
        if (!inputActionMaps.Contains(inputActionMapName))
        {
            inputActionMaps.Add(inputActionMapName);
        }
        if (newInputAction != null)
        {
            inputActions[inputActionType] = newInputAction;
        }

        return newInputAction;
    }

    /// <summary>
    /// 注销输入事件
    /// onlyDisable默认为true，若只是想短暂关闭监听应设置为false方便后续重新注册时查找直接开启。
    /// </summary>
    /// <param name="actionType">事件类型</param>
    /// <param name="onlyDisable">是否仅将事件disable</param>
    public void UnregisterInputAction(InputActionType actionType, bool onlyDisable = true)
    {
        if (inputActions.TryGetValue(actionType, out var action))
        {
            if (onlyDisable)
            {
                action.Disable();
                return;
            }
            
            // 完全注销时，清理所有回调
            ClearAllCallbacksForAction(actionType);
            action.Dispose(); // 释放资源
            inputActions.Remove(actionType);
        }
    }

    #region 添加/移除 输入动作开始激活事件，输入动作有效执行（满足条件）事件，输入动作取消/结束事件 回调方法

    /// <summary>
    /// 添加一个输入动作开始激活事件回调方法
    /// </summary>
    /// <param name="actionType">输入事件类型</param>
    /// <param name="callback"></param>
    public void AddStartInputAction(InputActionType actionType, Action<InputAction.CallbackContext> callback)
    {
        if (callback != null && inputActions.TryGetValue(actionType, out var value))
        {
            value.started += callback;
        
            // 记录回调
            if (!_startCallbacks.ContainsKey(actionType))
                _startCallbacks[actionType] = new List<Action<InputAction.CallbackContext>>();
            _startCallbacks[actionType].Add(callback);
        }
    }

    /// <summary>
    /// 移除输入动作开始激活事件的一个回调方法
    /// </summary>
    /// <param name="actionType">输入事件类型</param>
    /// <param name="callback"></param>
    public void RemoveStartInputAction(InputActionType actionType, Action<InputAction.CallbackContext> callback)
    {
        if (callback != null && inputActions.TryGetValue(actionType, out var value))
        {
            value.started -= callback;
            
            // 从跟踪字典中移除
            if (_startCallbacks.TryGetValue(actionType, out var callbacks))
            {
                callbacks.Remove(callback);
                if (callbacks.Count == 0)
                    _startCallbacks.Remove(actionType);
            }
        }
    }

    /// <summary>
    /// 添加一个输入动作有效执行事件回调方法
    /// </summary>
    /// <param name="actionType">输入事件类型</param>
    /// <param name="callback"></param>
    public void AddPreformedInputAction(InputActionType actionType, Action<InputAction.CallbackContext> callback)
    {
        if (callback != null && inputActions.TryGetValue(actionType, out var value))
        {
            value.performed += callback;
            
            // 记录回调
            if (!_performedCallbacks.ContainsKey(actionType))
                _performedCallbacks[actionType] = new List<Action<InputAction.CallbackContext>>();
            _performedCallbacks[actionType].Add(callback);
        }
    }

    /// <summary>
    /// 移除输入动作有效执行事件的一个回调方法
    /// </summary>
    /// <param name="actionType">输入事件类型</param>
    /// <param name="callback"></param>
    public void RemovePreformedInputAction(InputActionType actionType, Action<InputAction.CallbackContext> callback)
    {
        if (callback != null && inputActions.TryGetValue(actionType, out var value))
        {
            value.performed -= callback;
            
            // 从跟踪字典中移除
            if (_performedCallbacks.TryGetValue(actionType, out var callbacks))
            {
                callbacks.Remove(callback);
                if (callbacks.Count == 0)
                    _performedCallbacks.Remove(actionType);
            }
        }
    }

    /// <summary>
    /// 添加一个输入动作取消/结束事件回调方法
    /// </summary>
    /// <param name="actionType">输入事件类型</param>
    /// <param name="callback"></param>
    public void AddCancelInputAction(InputActionType actionType, Action<InputAction.CallbackContext> callback)
    {
        if (callback != null && inputActions.TryGetValue(actionType, out var value))
        {
            value.canceled += callback;
            
            // 记录回调
            if (!_canceledCallbacks.ContainsKey(actionType))
                _canceledCallbacks[actionType] = new List<Action<InputAction.CallbackContext>>();
            _canceledCallbacks[actionType].Add(callback);
        }
    }

    /// <summary>
    /// 移除输入动作取消/结束事件的一个回调方法
    /// </summary>
    /// <param name="actionType">输入事件类型</param>
    /// <param name="callback"></param>
    public void RemoveCancelInputAction(InputActionType actionType, Action<InputAction.CallbackContext> callback)
    {
        if (callback != null && inputActions.TryGetValue(actionType, out var value))
        {
            value.canceled -= callback;
            
            // 从跟踪字典中移除
            if (_canceledCallbacks.TryGetValue(actionType, out var callbacks))
            {
                callbacks.Remove(callback);
                if (callbacks.Count == 0)
                    _canceledCallbacks.Remove(actionType);
            }
        }
    }

    #endregion

    /// <summary>
    /// 清空所有输入回调
    /// </summary>
    public void ClearAllInputCallbacks()
    {
        Debug.Log("[InputManager] 清空所有输入回调");
        
        // 清空 started 回调
        foreach (var kvp in _startCallbacks)
        {
            var actionType = kvp.Key;
            if (inputActions.TryGetValue(actionType, out var action))
            {
                foreach (var callback in kvp.Value)
                    action.started -= callback;
            }
        }
        _startCallbacks.Clear();
        
        // 清空 performed 回调
        foreach (var kvp in _performedCallbacks)
        {
            var actionType = kvp.Key;
            if (inputActions.TryGetValue(actionType, out var action))
            {
                foreach (var callback in kvp.Value)
                    action.performed -= callback;
            }
        }
        _performedCallbacks.Clear();
        
        // 清空 canceled 回调
        foreach (var kvp in _canceledCallbacks)
        {
            var actionType = kvp.Key;
            if (inputActions.TryGetValue(actionType, out var action))
            {
                foreach (var callback in kvp.Value)
                    action.canceled -= callback;
            }
        }
        _canceledCallbacks.Clear();
    }

    /// <summary>
    /// 清空特定动作的所有回调
    /// </summary>
    /// <param name="actionType">动作类型</param>
    private void ClearAllCallbacksForAction(InputActionType actionType)
    {
        if (inputActions.TryGetValue(actionType, out var action))
        {
            // 清空 started 回调
            if (_startCallbacks.TryGetValue(actionType, out var startCallbacks))
            {
                foreach (var callback in startCallbacks)
                    action.started -= callback;
                _startCallbacks.Remove(actionType);
            }
            
            // 清空 performed 回调
            if (_performedCallbacks.TryGetValue(actionType, out var performedCallbacks))
            {
                foreach (var callback in performedCallbacks)
                    action.performed -= callback;
                _performedCallbacks.Remove(actionType);
            }
            
            // 清空 canceled 回调
            if (_canceledCallbacks.TryGetValue(actionType, out var canceledCallbacks))
            {
                foreach (var callback in canceledCallbacks)
                    action.canceled -= callback;
                _canceledCallbacks.Remove(actionType);
            }
        }
    }

    public void SwitchInputActionMap(InputActionMapType inputActionMapName)
    {
        foreach (var inputActionMap in inputActionMaps)
        {
            if (inputActionMap != inputActionMapName)
            {
                DisableInputActionMap(inputActionMap);
            }
            else
            {
                EnableInputActonMap(inputActionMap);
            }
        }
    }
    
    private void EnableInputActonMap(InputActionMapType inputActionMapType)
    {
        inputActionAsset.FindActionMap(inputActionMapType.ToString())?.Enable();
    }

    private void DisableInputActionMap(InputActionMapType inputActionMapType)
    {
        inputActionAsset.FindActionMap(inputActionMapType.ToString())?.Disable();
    }

    public InputAction GetInputAction(InputActionType inputActionType)
    {
        inputActions.TryGetValue(inputActionType, out var value);
        return value;
    }
    
    /// <summary>
    /// 重新初始化输入系统（用于场景切换后）
    /// </summary>
    public void ReinitializeInputSystem()
    {
        if (inputActionAsset != null)
        {
            // 禁用所有 Action Map
            foreach (var map in inputActionAsset.actionMaps)
            {
                map.Disable();
            }
            
            // 只启用 GamePlay
            var gameplayMap = inputActionAsset.FindActionMap("GamePlay");
            gameplayMap?.Enable();
            
            Debug.Log("[InputManager] 输入系统重新初始化完成");
        }
    }
    
    /// <summary>
    /// 清理所有资源（在销毁时调用）
    /// </summary>
    private  void OnDestroy()
    {
        AddressablesManager.Instance.Release<InputActionAsset>("GameInput",false);
        ClearAllInputCallbacks();
        
        // 释放所有 InputAction
        foreach (var action in inputActions.Values)
        {
            action?.Dispose();
        }
        inputActions.Clear();
        inputActionMaps.Clear();
    }
}
}