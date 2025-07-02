using UnityEngine;
using Photon.Pun; // Ajout de la référence Photon

public class CameraFollow : MonoBehaviour
{
    [Header("Délai de recherche du joueur")]
    [Tooltip("Intervalle (en secondes) entre deux tentatives de recherche")]
    [SerializeField] private float searchInterval = 0.2f;

    [Header("Offset de la caméra")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float smoothTime = 0.1f;

    [Header("Zoom caméra")]
    [SerializeField] private float orthoSizeLandscape = 5f;
    [SerializeField] private float orthoSizePortrait = 8f;

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

        if (target == null)
        {
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
        // Recherche de manière robuste le tank du joueur local
        PhotonView[] photonViews = FindObjectsOfType<PhotonView>();
        foreach (PhotonView pv in photonViews)
        { 
            // Si le PhotonView appartient au client local et a le bon tag
            if (pv.IsMine && pv.CompareTag("Player"))
            {
                target = pv.transform;
                Debug.Log("[CameraFollow] Cible trouvée : " + target.name);
                return; // On a trouvé notre tank, on arrête la recherche
            }
        }
    }
}