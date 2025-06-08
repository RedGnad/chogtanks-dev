using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Délai de recherche du joueur")]
    [Tooltip("Intervalle (en secondes) entre deux tentatives de recherche")]
    [SerializeField] private float searchInterval = 0.2f;

    [Header("Offset de la caméra")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float smoothTime = 0.1f;

    private Transform target;       // Référence au TankPlayer
    private Vector3 velocity = Vector3.zero;
    private float nextSearchTime = 0f;

    private void LateUpdate()
    {
        // Si on n'a pas encore référencé le Tank à suivre, on tente de le trouver périodiquement
        if (target == null)
        {
            if (Time.time >= nextSearchTime)
            {
                nextSearchTime = Time.time + searchInterval;
                FindPlayerInstance();
            }
            return;
        }

        // Une fois qu'on a target, on suit son mouvement de façon lissée
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }

    /// <summary>
    /// Cherche dans la scène un objet taggé "Player" (ou "TankPlayer") et l'affecte à target.
    /// </summary>
    private void FindPlayerInstance()
    {
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            target = playerGO.transform;
            Debug.Log("[CameraFollow] Player trouvé, suivi activé.");
        }
    }
}
