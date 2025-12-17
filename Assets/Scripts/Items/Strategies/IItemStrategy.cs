using Assets.Scripts.Combat;

namespace Assets.Scripts.Items.Strategies
{
    public interface IItemStrategy
    {
        void Use(KartItemSystem user);
    }
}
