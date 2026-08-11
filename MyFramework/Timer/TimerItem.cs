using UnityEngine.Events;
using MyFramework.Pool; // 添加命名空间引用

/// <summary>
/// 计时器对象 里面存储了计时器的相关数据
/// </summary>
public class TimerItem : IPoolObject // 实现IPoolObject接口
{
    /// <summary>
    /// 唯一ID
    /// </summary>
    public int keyID;
    /// <summary>
    /// 计时结束后的委托回调
    /// </summary>
    public UnityAction overCallBack;
    /// <summary>
    /// 间隔一定时间去执行的委托回调
    /// </summary>
    public UnityAction callBack;

    /// <summary>
    /// 表示计时器总的计时时间 毫秒：1s = 1000ms
    /// </summary>
    public int allTime;
    /// <summary>
    /// 记录一开始计时时的总时间 用于时间重置
    /// </summary>
    public int maxAllTime;

    /// <summary>
    /// 间隔执行回调的时间 毫秒 毫秒：1s = 1000ms
    /// </summary>
    public int intervalTime;
    /// <summary>
    /// 记录一开始的间隔时间
    /// </summary>
    public int maxIntervalTime;

    /// <summary>
    /// 是否在进行计时
    /// </summary>
    public bool isRuning;

    /// <summary>
    /// 永久计时
    /// </summary>
    public bool forever;

    /// <summary>
    /// 初始化计时器数据
    /// </summary>
    /// <param name="keyID">唯一ID</param>
    /// <param name="allTime">总的时间</param>
    /// <param name="overCallBack">总时间计时结束后的回调</param>
    /// <param name="intervalTime">间隔执行的时间</param>
    /// <param name="callBack">间隔执行时间结束后的回调</param>
    public void InitInfo(int keyID, int allTime, UnityAction overCallBack, int intervalTime = 0, UnityAction callBack = null,bool forever = false)
    {
        this.keyID = keyID;
        this.allTime = allTime;
        this.intervalTime = intervalTime;
        this.maxIntervalTime = intervalTime;
        this.overCallBack = overCallBack;
        this.callBack = callBack;
        this.forever = forever;
        this.isRuning = true;  // 明确设置为 true
    }

    /// <summary>
    /// 重置计时器
    /// </summary>
    public void ResetTimer()
    {
        this.allTime = this.maxAllTime;
        this.intervalTime = this.maxIntervalTime;
        this.isRuning = true;
    }

    /// <summary>
    /// 缓存池回收时  清除相关引用数据
    /// </summary>
    public void ResetInfo()
    {
        keyID = -1;
        allTime = 0;
        intervalTime = 0;
        maxIntervalTime = 0;
        callBack = null;
        overCallBack = null;
        forever = false;
        isRuning = false;
    }
}