using System;

public static class PlayerSession
{
    public static string WalletAddress { get; private set; }

    public static event Action<string> OnWalletConnected;

    public static bool IsConnected => !string.IsNullOrEmpty(WalletAddress);

    public static void SetWalletAddress(string address)
    {
        WalletAddress = address;
        OnWalletConnected?.Invoke(address);
    }

    public static void Clear()
    {
        WalletAddress = null;
    }
}
