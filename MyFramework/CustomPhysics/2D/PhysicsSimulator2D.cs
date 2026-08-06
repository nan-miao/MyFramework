using System.Collections.Generic;
using MyFramework.Core;
using MyFramework.Core.Singleton;
using UnityEngine;

namespace MyFramework.CustomPhysics._2D
{
    public interface IPhysicsObject
    {
        public void TickUpdate(float delta, float time);
        public void TickFixedUpdate(float delta);
        public void TickLateUpdate();
    }

    public interface IPhysicsMover
    {
        //TODO::补充相关的信息
        public Vector2 FramePositionDelta { get; }
        public Vector2 FramePosition { get; }
        public Vector2 Velocity { get; }
        public Vector2 TakeOffVelocity { get; }
    }

    public class PhysicsSimulator2D : SingletonAutoMono<PhysicsSimulator2D>,IMonoOwner
    {
        private readonly HashSet<IPhysicsObject> _platforms = new();
        private readonly HashSet<IPhysicsObject> _players = new();

        private readonly List<IPhysicsObject> _toAddPlatform = new();
        private readonly List<IPhysicsObject> _toAddPlayer = new();


        private readonly List<IPhysicsObject> needRemovePlatform = new();
        private readonly List<IPhysicsObject> needRemovePlayer = new();

        private float _time;

        private void OnDestroy()
        {
            UnregisterUpdateListeners();
        }

        public void AddPlatform(IPhysicsObject platform)
        {
            _toAddPlatform.Add(platform);
        }

        public void AddPlayer(IPhysicsObject player)
        {
            _toAddPlayer.Add(player);
        }

        public void RemovePlatform(IPhysicsObject platform)
        {
            needRemovePlatform.Add(platform);
        }

        public void RemovePlayer(IPhysicsObject player)
        {
            needRemovePlayer.Add(player);
        }

        protected override void OnStart()
        {
            base.OnStart();
            RegisterUpdateListeners();
        }

        private void OnUpdate()
        {
            if (!gameObject.activeSelf) return;

            var delta = Time.deltaTime;
            _time += delta;
            foreach (var platform in _platforms) platform.TickUpdate(delta, _time);

            foreach (var player in _players) player.TickUpdate(delta, _time);
        }

        private void OnFixedUpdate()
        {
            if (!gameObject.activeSelf) return;

            var delta = Time.deltaTime;
            foreach (var platform in _platforms) platform.TickFixedUpdate(delta);

            foreach (var player in _players) player.TickFixedUpdate(delta);
        }

        private void OnLateUpdate()
        {
            foreach (var platform in _platforms) platform.TickLateUpdate();

            foreach (var player in _players) player.TickLateUpdate();

            if (needRemovePlatform.Count > 0)
            {
                foreach (var platform in needRemovePlatform) _platforms.Remove(platform);
                needRemovePlatform.Clear();
            }

            if (needRemovePlayer.Count > 0)
            {
                foreach (var player in needRemovePlayer) _players.Remove(player);
                needRemovePlayer.Clear();
            }

            foreach (var p in _toAddPlatform) _platforms.Add(p);
            _toAddPlatform.Clear();

            foreach (var player in _toAddPlayer) _players.Add(player);
            _toAddPlayer.Clear();
        }

        public void RegisterUpdateListeners()
        {
            MonoManager.Instance?.AddUpdateListener(OnUpdate);
            MonoManager.Instance?.AddFixedUpdateListener(OnFixedUpdate);
            MonoManager.Instance?.AddLateUpdateListener(OnLateUpdate);
        }

        public void UnregisterUpdateListeners()
        {
            MonoManager.Instance?.RemoveUpdateListener(OnUpdate);
            MonoManager.Instance?.RemoveFixedUpdateListener(OnFixedUpdate);
            MonoManager.Instance?.RemoveLateUpdateListener(OnLateUpdate);
        }
    }
}