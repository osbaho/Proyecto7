using Assets.Scripts.Combat;

namespace Assets.Scripts.Items.Strategies
{
    public class MushroomStrategy : IItemStrategy
    {
        public void Use(KartItemSystem user)
        {
            user.ActivateBoostClientRpc();
        }
    }
}
