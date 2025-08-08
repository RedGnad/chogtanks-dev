using UnityEngine;
using Fusion;

public class CameraFollow : MonoBehaviour
{
    [Header("Délai de recherche du joueur")]
    [SerializeField] private float searchInterval = 0.2f;

    [Header("Offset de la caméra")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -17f);
    [SerializeField] private float smoothTime = 0.1f;

    [Header("Zoom caméra")]
    [SerializeField] private float orthoSizeLandscape = 7.5f;
    [SerializeField] private float orthoSizePortrait = 12f;

    private Transform target;
    private Vector3 velocity = Vector3.zero;
    private float nextSearchTime = 0f;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    private void LateUpdate()
    {
        float aspect = (float)Screen.width / Screen.height;
        if (aspect < 1f)
            cam.orthographicSize = orthoSizePortrait;
        else
            cam.orthographicSize = orthoSizeLandscape;

        // Plus besoin de chercher continuellement - la cible est définie directement par le tank local
        if (target == null)
        {
            // Fallback : chercher seulement si aucune cible n'a été définie
            if (Time.time >= nextSearchTime)
            {
                nextSearchTime = Time.time + searchInterval;
                FindPlayerInstance();
            }
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }

    private void FindPlayerInstance()
    {
        NetworkObject[] Objects = FindObjectsOfType<NetworkObject>();
        foreach (NetworkObject pv in Objects)
        { 
            if (pv != null && pv && pv.CompareTag("Player"))
            {
                target = pv.transform;
                return;
            }
        }
    }
    
    /// <summary>
    /// Méthode publique pour définir directement la cible à suivre (appelée par le tank local)
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        Debug.Log($"[CAMERA] Nouvelle cible définie: {newTarget.name}");
    }
    
    /// <summary>
    /// Réinitialise la cible (utile pour déconnexion/respawn)
    /// </summary>
    public void ClearTarget()
    {
        target = null;
        Debug.Log("[CAMERA] Cible effacée");
    }
}