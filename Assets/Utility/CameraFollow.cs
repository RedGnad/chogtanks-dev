using UnityEngine;
using Photon.Pun;

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
        PhotonView[] photonViews = FindObjectsOfType<PhotonView>();
        foreach (PhotonView pv in photonViews)
        { 
            if (pv.IsMine && pv.CompareTag("Player"))
            {
                target = pv.transform;
                return;
            }
        }
    }
}