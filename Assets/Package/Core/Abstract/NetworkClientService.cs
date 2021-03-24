using System;
using GameWorkstore.Patterns;

namespace GameWorkstore.NetworkLibrary
{
    public abstract class NetworkClientService<T> : IService where T : NetworkClient , new()
    {
        private readonly T _client = new T();

        public T Instance { get { return _client; } }
        
        public override void Preprocess()
        {
        }

        public override void Postprocess()
        {
        }
    }
}
