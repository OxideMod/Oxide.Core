namespace Oxide.Pooling
{
    public interface IArrayPoolSource<T> : IPoolSource<T[]>
    {
        T[] Get(int length);
    }
}
