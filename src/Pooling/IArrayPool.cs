namespace Oxide.Pooling
{
    public interface IArrayPool<TElementType> : IClaimable<TElementType[], int>
    {
        TElementType[] Empty { get; }
    }
}
