using Cinemachine;
using MyFramework.Core.Singleton;

namespace MyFramework.Camera
{
    public class CameraManager : SingletonMono<CameraManager>
    {
        
        public CinemachineVirtualCamera mapCamera;

        public CinemachineVirtualCamera currentCamera;

        public void SwapToCamera(CinemachineVirtualCamera vcam)
        {
            currentCamera.Priority = 0;
            currentCamera = vcam;
            currentCamera.Priority = 20;
        }

        public void ResetCameraToMap()
        {
            currentCamera.Priority = 0;
            currentCamera = mapCamera;
            currentCamera.Priority = 20;
        }
    }
}

