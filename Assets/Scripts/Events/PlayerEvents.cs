using System;

namespace Assets.Scripts.Events
{
    public static class PlayerEvents
    {
        public static event Action<ulong> OnPlayerJoined;
        public static event Action<ulong> OnPlayerEliminated;
        
        public static void InvokePlayerJoined(ulong clientId)
        {
            OnPlayerJoined?.Invoke(clientId);
        }

        public static void InvokePlayerEliminated(ulong clientId)
        {
            OnPlayerEliminated?.Invoke(clientId);
        }
    }
}
