using Combat;

namespace Items.Strategies
{
    public class MushroomStrategy : IItemStrategy
    {
        public void Use(KartItemSystem user)
        {
            user.ActivateBoostClientRpc();
        }
    }
}
