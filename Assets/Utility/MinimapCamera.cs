using UnityEngine;
using Photon.Pun;

public class MinimapCamera : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float height = 20f;
    [SerializeField] private float mapSize = 15f;
    
    private Camera minimapCam;
    private Transform playerTarget;
    private bool isInGameMode = false;
    private bool wasInGameMode = false; 
    
    private void Awake()
    {
        minimapCam = GetComponent<Camera>();
        if (minimapCam == null)
        {
            minimapCam = gameObject.AddComponent<Camera>();
        }
        
        minimapCam.orthographic = true;
        minimapCam.orthographicSize = mapSize;
        minimapCam.depth = 1;
        minimapCam.clearFlags = CameraClearFlags.Depth;
        minimapCam.rect = new Rect(0.75f, 0.0f, 0.25f, 0.25f);
        minimapCam.cullingMask = -1;
        
        transform.position = new Vector3(0, 0, -height);
        transform.rotation = Quaternion.identity;
        
        minimapCam.enabled = false;
    }
    
    private void Start()
    {
        InvokeRepeating(nameof(CheckForTanks), 0f, 0.5f);
    }
    
    private void CheckForTanks()
    {
        bool shouldBeInGameMode = PhotonNetwork.InRoom;
        
        if (shouldBeInGameMode)
        {
            var tanks = FindObjectsOfType<TankHealth2D>();
            shouldBeInGameMode = tanks.Length > 0;
        }
        
        if (shouldBeInGameMode != wasInGameMode)
        {
            
            if (shouldBeInGameMode && !isInGameMode)
            {
                EnterGameMode();
            }
            else if (!shouldBeInGameMode && isInGameMode)
            {
                ExitGameMode();
            }
            
            wasInGameMode = shouldBeInGameMode;
        }
        
        if (isInGameMode && playerTarget == null)
        {
            FindPlayerTarget();
        }
    }
    
    private void EnterGameMode()
    {
        
        isInGameMode = true;
        minimapCam.enabled = true;
        
        playerTarget = null;
        
        CancelInvoke(nameof(FindPlayerTarget));
        InvokeRepeating(nameof(FindPlayerTarget), 0f, 0.2f);
    }
    
    private void ExitGameMode()
    {
        
        isInGameMode = false;
        minimapCam.enabled = false;
        
        playerTarget = null;
        
        CancelInvoke(nameof(FindPlayerTarget));
    }
    
    private void LateUpdate()
    {
        if (isInGameMode && playerTarget != null)
        {
            Vector3 newPos = playerTarget.position;
            newPos.z = -height;
            transform.position = newPos;
        }
    }
    
    private void FindPlayerTarget()
    {
        if (!isInGameMode) 
        {
            CancelInvoke(nameof(FindPlayerTarget));
            return;
        }
        
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerGO in playerObjects)
        {
            var health = playerGO.GetComponent<TankHealth2D>();
            if (health != null && health.photonView != null && health.photonView.IsMine)
            {
                playerTarget = playerGO.transform;
                CancelInvoke(nameof(FindPlayerTarget)); 
                return;
            }
        }
        
        var tanks = FindObjectsOfType<TankHealth2D>();
        foreach (var tank in tanks)
        {
            if (tank.photonView != null && tank.photonView.IsMine && !tank.IsDead)
            {
                playerTarget = tank.transform;
                CancelInvoke(nameof(FindPlayerTarget));
                return;
            }
        }
        
        foreach (var tank in tanks)
        {
            string owner = tank.photonView.Owner?.NickName ?? "null";
        }
    }
    
    public void ForceReset()
    {
        ExitGameMode();
        wasInGameMode = false;
        
        Invoke(nameof(CheckForTanks), 0.1f);
    }
}