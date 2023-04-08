namespace Oxide.Pooling
{
    public interface IPoolSource
    {
        void Free(object item);
    }

    public interface IPoolSource<T> : IPoolSource where T : class
    {
        T Get();
    }
}
