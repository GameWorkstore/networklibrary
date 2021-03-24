using GameWorkstore.Patterns;

namespace GameWorkstore.NetworkLibrary
{
    public abstract class NetworkHostService<T> : IService where T : NetworkHost, new()
    {
        private readonly T _server = new T();

        public T Instance { get { return _server; }}

        public override void Preprocess() { }
        public override void Postprocess() { }
    }
}
