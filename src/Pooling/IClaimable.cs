namespace Oxide.Pooling
{
    public interface IClaimable<TInstance>
    {
        TInstance Claim();

        void Unclaim(TInstance instance);
    }

    public interface IClaimable<TInstance, TParameter> : IClaimable<TInstance>
    {
        TInstance Claim(TParameter parameter);
    }
}
