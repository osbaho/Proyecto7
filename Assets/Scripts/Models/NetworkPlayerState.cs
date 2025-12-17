using Unity.Netcode;
using Unity.Collections;
using System;

public struct NetworkPlayerState : INetworkSerializable, IEquatable<NetworkPlayerState>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public bool IsReady;

    public NetworkPlayerState(ulong clientId, FixedString32Bytes playerName, bool isReady)
    {
        ClientId = clientId;
        PlayerName = playerName;
        IsReady = isReady;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref IsReady);
    }

    public bool Equals(NetworkPlayerState other)
    {
        return ClientId == other.ClientId && 
               PlayerName.Equals(other.PlayerName) && 
               IsReady == other.IsReady;
    }
}
