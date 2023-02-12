namespace Oxide.Pooling
{
    /// <summary>
    /// Defines a item that can be reset
    /// </summary>
    public interface IResetable
    {
        /// <summary>
        /// Reset this item back to its original state
        /// </summary>
        void Reset();
    }
}
