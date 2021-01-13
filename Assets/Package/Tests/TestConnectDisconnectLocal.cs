#if UNITY_EDITOR
using GameWorkstore.Patterns;
using System;
using UnityEngine;

namespace GameWorkstore.NetworkLibrary.Tests
{
    public class TestConnectDisconnectLocal : MonoBehaviour
    {
        void Awake()
        {
            DebugMessege.SetLogLevel(DebugLevel.INFO);
            InitServer();
        }

        private void InitServer()
        {
            ServiceProvider.GetService<SampleServer>().Init(OnServerInitialized);
        }

        private void OnServerInitialized(bool success)
        {
            if (!success) return;
            ServiceProvider.GetService<SampleClient>().Connect("127.0.0.1", OnConnected);
        }

        private void OnConnected(bool success)
        {
            if (!success) return;

            Invoke("DisconnectNow", 2);
        }

        private void DisconnectNow()
        {
            ServiceProvider.GetService<SampleClient>().Disconnect(OnDisconnect);
        }

        private void OnDisconnect()
        {
            ServiceProvider.GetService<SampleServer>().Shutdown();
            Invoke("InitServer", 1);
        }
    }
}
#endif