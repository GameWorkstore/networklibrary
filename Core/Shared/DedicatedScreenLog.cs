using System.Collections.Generic;
using UnityEngine.UI;

namespace UnityEngine.NetLibrary
{
    public class DedicatedScreenLog : MonoBehaviour
    {
        public Queue<string> _queueLog = new Queue<string>();
        public Text outputWindow;

        public void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        public void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            _queueLog.Enqueue(logString);
            while (_queueLog.Count > 50)
            {
                _queueLog.Dequeue();
            }
            string com = "";
            foreach (var st in _queueLog)
            {
                com += st + "\n";
            }
            outputWindow.text = com;
        }
    }
}