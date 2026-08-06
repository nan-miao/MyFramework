using UnityEngine;

namespace MyFramework.Entity
{
    public class EnemyComponentBase : MonoBehaviour, IEntityComponent
    {
        [SerializeField] protected Entity _enemy;

        protected virtual void Start()
        {
            _enemy = GetComponentInParent<Entity>();
            _enemy?.AddEntityComponent(this);
        }

        private void OnEnable()
        {
            _enemy?.AddEntityComponent(this);
        }

        private void OnDisable()
        {
            _enemy?.RemoveEntityComponent(this);
        }

        public void OnUpdate(float deltaTime)
        {
            ChildOnUpdate(deltaTime);
        }

        public void OnFixedUpdate(float deltaTime)
        {
            ChildOnFixeUpdate(deltaTime);
        }

        public void OnLateUpdate()
        {
            ChildOnLateUpdate();
        }

        protected virtual void ChildOnUpdate(float deltaTime)
        {
        }

        protected virtual void ChildOnFixeUpdate(float deltaTime)
        {
        }

        protected virtual void ChildOnLateUpdate()
        {
        }
    }
}