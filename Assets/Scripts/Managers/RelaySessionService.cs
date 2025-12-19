using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Minimal helper to bootstrap Unity Services and create/join Relay allocations.
    /// </summary>
    public static class RelaySessionService
    {
        private static bool _initialized;
        private static Task _initializingTask;

        public static async Task EnsureInitializedAsync()
        {
            if (_initialized)
            {
                return;
            }

            if (_initializingTask == null)
            {
                _initializingTask = InitializeInternalAsync();
            }

            await _initializingTask;
        }

        private static async Task InitializeInternalAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized &&
                UnityServices.State != ServicesInitializationState.Initializing)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            _initialized = true;
        }

        public static async Task<(Allocation allocation, string joinCode)> CreateHostAllocationAsync(int maxPlayers)
        {
            if (maxPlayers < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPlayers), "Max players must be at least 2 (host + 1 client)");
            }

            await EnsureInitializedAsync();

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return (allocation, joinCode);
        }

        public static async Task<JoinAllocation> JoinAllocationAsync(string joinCode)
        {
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                throw new ArgumentException("Join code is empty", nameof(joinCode));
            }

            await EnsureInitializedAsync();

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return allocation;
        }
    }
}
