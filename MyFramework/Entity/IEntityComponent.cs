namespace MyFramework.Entity
{
    public interface IEntityComponent
    {
        public  void OnUpdate(float deltaTime);
        public void OnFixedUpdate(float deltaTime);
        public void OnLateUpdate();
    }
}