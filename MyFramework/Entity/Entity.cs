using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyFramework.Entity
{
    public class Entity : MonoBehaviour, IMonoOwner
    {
        private void OnDisable()
        {
            UnregisterUpdateListeners();
        }

        protected virtual void OnDestroy()
        {
            UnregisterUpdateListeners();
        }

        #region Loop

        protected virtual void Start()
        {
            RegisterUpdateListeners();
            Init();
        }

        protected virtual void Init()
        {
        }

        private void OnUpdate()
        {
            foreach (var entityComponent in entityComponentsList) entityComponent.OnUpdate(Time.deltaTime);
        }

        private void OnFixedUpdate()
        {
            foreach (var entityComponent in entityComponentsList) entityComponent.OnFixedUpdate(Time.fixedDeltaTime);
        }

        private void OnLateUpdate()
        {
            foreach (var entityComponent in entityComponentsList) entityComponent.OnLateUpdate();

            if (delEntityComponentsList.Count > 0)
            {
                foreach (var entityComponent in delEntityComponentsList) entityComponentsList.Remove(entityComponent);
                delEntityComponentsList.Clear();
            }
        }

        private readonly List<IEntityComponent> entityComponentsList = new();
        private readonly List<IEntityComponent> delEntityComponentsList = new();

        public void AddEntityComponent(IEntityComponent entityComponent)
        {
            // 检查是否已存在相同类型的组件
            Type componentType = entityComponent.GetType();
            if (HasComponentOfType(componentType))
                // Debug.LogWarning($"Component of type {componentType.Name} already exists!");
                return; // 不添加重复的组件

            entityComponentsList.Add(entityComponent);

            // 如果组件在删除列表中，从删除列表中移除
            if (delEntityComponentsList.Contains(entityComponent)) delEntityComponentsList.Remove(entityComponent);
        }

        public void RemoveEntityComponent(IEntityComponent entityComponent)
        {
            // 确保不会从删除列表中重复添加
            if (!delEntityComponentsList.Contains(entityComponent)) delEntityComponentsList.Add(entityComponent);
        }

        public IEntityComponent GetEntityComponent(Type type)
        {
            foreach (var component in entityComponentsList)
                if (type.IsAssignableFrom(component.GetType()))
                    return component;

            return null;
        }

        public bool HasComponentOfType(Type type)
        {
            foreach (var component in entityComponentsList)
                if (type.IsAssignableFrom(component.GetType()))
                    return true;

            return false;
        }

        public virtual void RegisterUpdateListeners()
        {
        }

        public virtual void UnregisterUpdateListeners()
        {
        }

        #endregion
    }
}