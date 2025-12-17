namespace Assets.Scripts.Interfaces
{
    public interface IDamageable
    {
        void TakeDamage(int amount);
        ulong OwnerClientId { get; } // Useful for checking friendly fire if needed
    }
}
