using System;

/// <summary>
/// Classe statique pour maintenir l'état de la session du joueur, comme l'adresse du portefeuille.
/// Accessible de n'importe où dans le code.
/// </summary>
public static class PlayerSession
{
    /// <summary>
    /// L'adresse du portefeuille du joueur connecté. Null ou vide si non connecté.
    /// </summary>
    public static string WalletAddress { get; private set; }

    /// <summary>
    /// Événement déclenché lorsque le portefeuille est connecté avec succès.
    /// L'adresse du portefeuille est passée en argument.
    /// </summary>
    public static event Action<string> OnWalletConnected;

    /// <summary>
    /// Vérifie si un portefeuille est actuellement connecté.
    /// </summary>
    public static bool IsConnected => !string.IsNullOrEmpty(WalletAddress);

    /// <summary>
    /// Méthode pour définir l'adresse du portefeuille et notifier les autres systèmes.
    /// </summary>
    /// <param name="address">L'adresse du portefeuille à définir.</param>
    public static void SetWalletAddress(string address)
    {
        WalletAddress = address;
        OnWalletConnected?.Invoke(address);
    }

    /// <summary>
    /// Réinitialise l'état de la session (déconnexion).
    /// </summary>
    public static void Clear()
    {
        WalletAddress = null;
    }
}
