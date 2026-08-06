# Camera 相机管理

基于 Cinemachine 的虚拟相机管理系统，通过调整 Priority 值实现多相机间平滑切换。

**管理器**: `CameraManager`（继承 `SingletonMono`，需手动挂载）

**命名空间**: `MyFramework.Camera`

---

## 架构

```
CameraManager (SingletonMono, global=false)
├── mapCamera: CinemachineVirtualCamera    ← 默认地图相机
├── currentCamera: CinemachineVirtualCamera ← 当前激活的相机
├── SwapToCamera(vCam)
│   ├── currentCamera.Priority = 0
│   └── vCam.Priority = 20
└── ResetCameraToMap()
    └── SwapToCamera(mapCamera)
```

---

## API

```csharp
using MyFramework.Camera;
using Cinemachine;

// 切换到指定虚拟相机
CameraManager.Instance.SwapToCamera(battleCamera);
// battleCamera.Priority 设为 20，之前相机的 Priority 设为 0

// 切换回地图相机
CameraManager.Instance.ResetCameraToMap();
```

---

## 使用示例

```csharp
public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera battleCam;
    [SerializeField] private CinemachineVirtualCamera dialogCam;

    public void EnterBattle() => CameraManager.Instance.SwapToCamera(battleCam);
    public void EnterDialog() => CameraManager.Instance.SwapToCamera(dialogCam);
    public void ReturnToMap() => CameraManager.Instance.ResetCameraToMap();
}
```

---

## 注意事项

- **依赖 Cinemachine**: 需要在 Package Manager 中安装 Cinemachine。
- **Priority 规则**: 切换相机时旧相机 Priority=0，新相机 Priority=20，由 Cinemachine Brain 自动选择最高 Priority 的相机。
- **手动挂载**: `CameraManager` 是 `SingletonMono`（非 `SingletonAutoMono`），需在场景中手动挂载并设置 `mapCamera`。
- **global=false**: 场景级设计，切换场景时自动销毁。

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[01-Core-核心与单例]]
