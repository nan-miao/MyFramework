using System.Collections;
using System.Collections.Generic;
using MyFramework;
using MyFramework.Core;
using MyFramework.Core.Singleton;
using MyFramework.Debuger;
using UnityEngine;
using UnityEngine.Events;
using MyFramework.Pool;

// 添加命名空间引用

/// <summary>
/// 计时器管理器 主要用于开启、停止、重置等等操作来管理计时器
/// </summary>
public class TimeManager : SingletonAutoMono<TimeManager>
{
    /// <summary>
    /// 用于记录当前将要创建的唯一ID的
    /// </summary>
    private int TIMER_KEY = 0;
    private int hitStopTimerId = -1;
    /// <summary>
    /// 用于存储管理所有计时器的字典容器
    /// </summary>
    private Dictionary<int, TimerItem> timerDic = new Dictionary<int, TimerItem>();
    /// <summary>
    /// 用于存储管理所有计时器的字典容器（不受Time.timeScale影响的计时器）
    /// </summary>
    private Dictionary<int, TimerItem> realTimerDic = new Dictionary<int, TimerItem>();
    /// <summary>
    /// 待移除列表
    /// </summary>
    private List<TimerItem> delList = new List<TimerItem>();

    //为了避免内存的浪费 每次while都会生成 
    //我们直接将其声明为成员变量
    private WaitForSecondsRealtime waitForSecondsRealtime = new WaitForSecondsRealtime(intervalTime);
    private WaitForSeconds waitForSeconds = new WaitForSeconds(intervalTime);

    private Coroutine timer;
    private Coroutine realTimer;

    /// <summary>
    /// 计时器管理器中的唯一计时用的协同程序 的间隔时间
    /// </summary>
    private const float intervalTime = 0.02f;
    
    //开启计时器管理器的方法
    protected override void OnStart()
    {
        base.OnStart();
        
        // 确保TimerItem的数据池已创建
        if (!PoolManager.Instance.HasDataPool<TimerItem>())
        {
            PoolManager.Instance.CreateDataPool<TimerItem>();
        }
        
        timer = MonoManager.Instance.StartCoroutine(StartTiming(false, timerDic));
        realTimer = MonoManager.Instance.StartCoroutine(StartTiming(true, realTimerDic));
    }

    //关闭计时器管理器的方法
    public void Stop()
    {
        MonoManager.Instance.StopCoroutine(timer);
        MonoManager.Instance.StopCoroutine(realTimer);
    }

    IEnumerator StartTiming(bool isRealTime, Dictionary<int, TimerItem> timerDic)
    {
        while (true)
        {
            //100毫秒进行一次计时
            if (isRealTime)
                yield return waitForSecondsRealtime;
            else
                yield return waitForSeconds;
            
            //先处理新增
            var list = isRealTime ? realAddList : addList;
           
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var item = list[i];
                timerDic.Add(item.keyID, item);
                list.Remove(list[i]);
            }
            
            //遍历所有的计时器 进行数据更新
            foreach (TimerItem item in timerDic.Values)
            {
                if (!item.isRuning)
                    continue;
                //判断计时器是否有间隔时间执行的需求
                if(item.callBack != null)
                {
                    //减去100毫秒
                    item.intervalTime -= (int)(intervalTime*1000);
                    //满足一次间隔时间执行
                    if(item.intervalTime <= 0)
                    {
                        //间隔一定时间 执行一次回调
                        item.callBack.Invoke();
                        //重置间隔时间
                        item.intervalTime = item.maxIntervalTime;
                    }
                }
                //总的时间更新
                if (!item.forever)
                {
                    item.allTime -= (int)(intervalTime * 1000);
                }
                
                //计时时间到 需要执行完成回调函数
                if(item.allTime <= 0)
                {
                    item.overCallBack?.Invoke();
                    delList.Add(item);
                }
            }

            //移除待移除列表中的数据
            for (int i = 0; i < delList.Count; i++)
            {
                //从字典中移除
                timerDic.Remove(delList[i].keyID);
                //放入缓存池中
                PoolManager.Instance.ReturnData(delList[i]);
            }
            //移除结束后 清空列表
            delList.Clear();
        }
    }

    //先用列表存储 运行时新增后再遍历 
    //用来应对CreateTimer完成回调再次CreateTimer的情况 （避免直接改动字典值而导致报错）
    private List<TimerItem> addList = new List<TimerItem>();
    private List<TimerItem> realAddList = new List<TimerItem>();
    /// <summary>
    /// 创建单个计时器
    /// </summary>
    /// <param name="isRealTime">如果是true不受Time.timeScale影响</param>
    /// <param name="allTime">总的时间 毫秒 1s=1000ms</param>
    /// <param name="overCallBack">总时间结束回调</param>
    /// <param name="interval">间隔计时时间 毫秒 1s=1000ms</param>
    /// <param name="callBack">间隔计时时间结束 回调</param>
    /// <param name="forever">永久计时</param>
    /// <returns>返回唯一ID 用于外部控制对应计时器</returns>
    public int CreateTimer(bool isRealTime, int allTime, UnityAction overCallBack, int interval = 0, UnityAction callBack = null,bool forever = false)
    {
        //构建唯一ID
        int keyID = ++TIMER_KEY;
        //从缓存池取出对应的计时器
        TimerItem timerItem = PoolManager.Instance.GetData<TimerItem>();
        //初始化数据
        timerItem.InitInfo(keyID, allTime, overCallBack, interval, callBack,forever);
        //记录到字典中 进行数据更新
        if (isRealTime)
            realAddList.Add(timerItem);
        else
            addList.Add(timerItem);
        return keyID;
    }
    
    /// <summary>
    /// 创建单个计时器
    /// </summary>
    /// <param name="isRealTime">如果是true不受Time.timeScale影响</param>
    /// <param name="allTime">总的时间 秒 </param>
    /// <param name="overCallBack">总时间结束回调</param>
    /// <param name="interval">间隔计时时间 秒 </param>
    /// <param name="callBack">间隔计时时间结束 回调</param>
    /// <param name="forever">永久计时</param>
    /// <returns>返回唯一ID 用于外部控制对应计时器</returns>
    /// <returns></returns>
    public int CreateTimer(bool isRealTime, float allTime, UnityAction overCallBack, float interval = 0f, UnityAction callBack = null,bool forever = false)
    {
        //构建唯一ID
        int keyID = ++TIMER_KEY;
        //从缓存池取出对应的计时器
        TimerItem timerItem = PoolManager.Instance.GetData<TimerItem>();
        //初始化数据
        timerItem.InitInfo(keyID, (int)(allTime * 1000), overCallBack, (int)(interval * 1000), callBack,forever);
        //记录到字典中 进行数据更新
        if (isRealTime)
            realAddList.Add(timerItem);
        else
            addList.Add(timerItem);
        return keyID;
    }
    
    //移除单个计时器
    public void RemoveTimer(int keyID)
    {
        // 从普通字典移除
        if (timerDic.TryGetValue(keyID, out var timer))
        {
            delList.Add(timer);
            return;
        }

        // 从真实时间字典移除
        if (realTimerDic.TryGetValue(keyID, out var realTimer))
        {
            delList.Add(realTimer);
            return;
        }

        // 从待添加列表中移除
        RemoveTimerFromList(addList, keyID);
        RemoveTimerFromList(realAddList, keyID);
    }

    private void RemoveTimerFromList(List<TimerItem> list, int keyID)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].keyID == keyID)
            {
                // 放入对象池
                PoolManager.Instance.ReturnData(list[i]);
                list.RemoveAt(i);
                break;
            }
        }
    }

    /// <summary>
    /// 重置单个计时器
    /// </summary>
    /// <param name="keyID">计时器唯一ID</param>
    public void ResetTimer(int keyID)
    {
        if (timerDic.ContainsKey(keyID))
        {
            timerDic[keyID].ResetTimer();
        }
        else if (realTimerDic.ContainsKey(keyID))
        {
            realTimerDic[keyID].ResetTimer();
        }
    }

    /// <summary>
    /// 开启当个计时器 主要用于暂停后重新开始
    /// </summary>
    /// <param name="keyID">计时器唯一ID</param>
    public void StartTimer(int keyID)
    {
        if (timerDic.ContainsKey(keyID))
        {
            timerDic[keyID].isRuning = true;
        }
        else if (realTimerDic.ContainsKey(keyID))
        {
            realTimerDic[keyID].isRuning = true;
        }
    }
    
    public void TriggerHitStop(int duration = 100, float timeScale = 0f)
    {
        // 避免重复创建
        if (hitStopTimerId != -1)
        {
            TimeManager.Instance.RemoveTimer(hitStopTimerId);
        }
        
        // 设置时间缩放
        Time.timeScale = timeScale;
        
        // 创建不受时间缩放影响的计时器（因为timeScale可能为0）
        hitStopTimerId = TimeManager.Instance.CreateTimer(
            isRealTime: true,           // 不受Time.timeScale影响
            allTime: duration,          // 卡肉时长(毫秒)
            overCallBack: () =>         // 卡肉结束
            {
                Time.timeScale = 1f;    // 恢复正常速度
                hitStopTimerId = -1;
            },
            interval: 0
        );
    }

    /// <summary>
    /// 停止单个计时器 主要用于暂停
    /// </summary>
    /// <param name="keyID">计时器唯一ID</param>
    public void StopTimer(int keyID)
    {
        // 检查普通计时器字典
        if (timerDic.TryGetValue(keyID, out var timer))
        {
            timer.isRuning = false;
            return;
        }

        // 检查真实时间计时器字典
        if (realTimerDic.TryGetValue(keyID, out var realTimer))
        {
            realTimer.isRuning = false;
            return;
        }

        // 检查待添加的列表
        StopTimerInList(addList, keyID);
        StopTimerInList(realAddList, keyID);
    }

    private void StopTimerInList(List<TimerItem> list, int keyID)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].keyID == keyID)
            {
                list[i].isRuning = false;
                break;
            }
        }
    }
    
    private TimerItem FindTimer(int keyID)
    {
        if (timerDic.TryGetValue(keyID, out var t)) return t;
        if (realTimerDic.TryGetValue(keyID, out t)) return t;
        return FindInList(addList, keyID) ?? FindInList(realAddList, keyID);
    }

    private TimerItem FindInList(List<TimerItem> list, int keyID)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].keyID == keyID)
            {
                return list[i];
            }
        }
        return null;
    }
    
    /// <summary>
    /// 清空所有计时器（安全版本）
    /// </summary>
    public void ClearAllTimersSafe()
    {
        // 收集所有需要清理的计时器
        List<TimerItem> allTimers = new List<TimerItem>();
    
        // 收集普通计时器
        allTimers.AddRange(timerDic.Values);
        timerDic.Clear();
    
        // 收集真实时间计时器
        allTimers.AddRange(realTimerDic.Values);
        realTimerDic.Clear();
    
        // 收集待添加列表
        allTimers.AddRange(addList);
        addList.Clear();
        allTimers.AddRange(realAddList);
        realAddList.Clear();
    
        // 清理所有计时器
        foreach (var timerItem in allTimers)
        {
            // 停止计时器
            timerItem.isRuning = false;
        
            // 放入对象池
            PoolManager.Instance.ReturnData(timerItem);
        }
    
        // 清空待移除列表
        delList.Clear();
    
        // 重置击中停顿计时器ID
        hitStopTimerId = -1;
    
        // 恢复时间缩放
        Time.timeScale = 1f;
    
        Debug.Log($"已清空 {allTimers.Count} 个计时器");
    }
}