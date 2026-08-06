using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using UnityEngine;

namespace MyFramework.Debuger
{
    public class LogData
    {
        public string log;
        public string trace;
        public LogType logType;
    }

    public class UnityLogHelper : MonoBehaviour
    {
        private StreamWriter m_StreamWriter;//NOTE::文件写入流
    
        private readonly ConcurrentQueue<LogData> m_ConVurrentQueue = new ConcurrentQueue<LogData>();//NOTE::日志数据队列 用于子线程的安全队列
    
        private readonly ManualResetEvent m_ManualResetEvent =new ManualResetEvent(false);//NOTE::工作信号事件
    
        private bool m_ThredRuning = false;

        private string m_NowTime{get{return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");}}
    
        public void InitLogFileModule(string savePath, string logFileName)
        {
            string logFilePath = Path.Combine(savePath, logFileName);
            Debug.Log("logFilePath:"+logFilePath);
            m_StreamWriter = new StreamWriter(logFilePath);
        
            Application.logMessageReceivedThreaded += OnLogMessageReceivedThreaded;

            m_ThredRuning = true;
            Thread fileThread = new Thread(FileLogThread);
            fileThread.Start();
        }

        public void FileLogThread()
        {
            while (m_ThredRuning)
            {
                m_ManualResetEvent.WaitOne();//NOTE::让线程进入等待,并进行阻塞
                if (m_StreamWriter==null)
                {
                    break;
                }

                LogData data;
                while (m_ConVurrentQueue.Count>0 && m_ConVurrentQueue.TryDequeue(out data))
                {
                    if (data.logType==LogType.Log)
                    {
                        m_StreamWriter.Write("Log >>>");
                        m_StreamWriter.WriteLine(data.log);
                        m_StreamWriter.WriteLine(data.trace);//NOTE::堆栈信息
                    }
                    else if (data.logType==LogType.Warning)
                    {
                        m_StreamWriter.Write("Warning >>>");
                        m_StreamWriter.WriteLine(data.log);
                        m_StreamWriter.WriteLine(data.trace);//NOTE::堆栈信息
                    }
                    else if (data.logType==LogType.Error)
                    {
                        m_StreamWriter.Write("Error >>>");
                        m_StreamWriter.WriteLine(data.log);
                        m_StreamWriter.Write('\n');
                        m_StreamWriter.WriteLine(data.trace);//NOTE::堆栈信息
                    }
                    m_StreamWriter.Write("\r\n");
                }
                m_StreamWriter.Flush();//NOTE::保存当前内容，使其生效
            
                //NOTE::线程休息
                m_ManualResetEvent.Reset();
                Thread.Sleep(1);
            }
        }

        private void OnApplicationQuit()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
            m_ThredRuning = false;
            m_ManualResetEvent.Reset();
            m_StreamWriter.Close();
            m_StreamWriter = null;
        }

        private void OnLogMessageReceivedThreaded(string condition, string stackTrace, LogType logType)
        {
            m_ConVurrentQueue.Enqueue(new LogData() { log =m_NowTime+" "+ condition, trace = stackTrace, logType = logType });
            m_ManualResetEvent.Set();
        }
    }
}