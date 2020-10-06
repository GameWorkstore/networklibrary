using GameWorkstore.Patterns;
using UnityEngine;

namespace GameWorkstore.NetworkLibrary.Tests
{
    public class Test : MonoBehaviour
    {
        void Awake()
        {
            ServiceProvider.GetService<SampleServer>().Init((success) =>
                ServiceProvider.GetService<SampleClient>().Connect("127.0.0.1")
            );
        }
    }

    public class SampleClient : NetworkClientService
    {
    }

    public class SampleServer : NetworkHostService
    {
        protected override void Preinitialize()
        {
        }
    }
}
