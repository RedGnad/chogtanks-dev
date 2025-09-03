using System.Collections.Generic;

public static class MonadBadgeState
{
    private static readonly Dictionary<int, bool> _verified = new Dictionary<int, bool>();

    public static void Set(int actorNumber, bool isVerified)
    {
        _verified[actorNumber] = isVerified;
    }

    public static bool TryGet(int actorNumber, out bool isVerified)
    {
        return _verified.TryGetValue(actorNumber, out isVerified);
    }

    public static void Remove(int actorNumber)
    {
        _verified.Remove(actorNumber);
    }
}
