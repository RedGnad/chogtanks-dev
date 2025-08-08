using UnityEngine;
using Fusion;

public class MinimapCamera : NetworkBehaviour
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
        InvokeRepeating(nameof(CheckForTanks), 0f, 2.0f); 
    }
    
    private void CheckForTanks()
    {
        bool shouldBeInGameMode = Runner != null && Runner.IsConnectedToServer;
        
        if (shouldBeInGameMode)
        {
            var tanks = FindFirstObjectByType<NetworkObject>();
            shouldBeInGameMode = tanks != null;
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
        InvokeRepeating(nameof(FindPlayerTarget), 0f, 1.0f); 
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
        
        // Find player objects using GameObject tags instead of PlayerRef.Object
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerGO in playerObjects)
        {
            var health = playerGO.GetComponent<TankHealth2D>();
            NetworkObject networkObj = playerGO.GetComponent<NetworkObject>();
            if (health != null && networkObj != null && networkObj.HasInputAuthority)
            {
                playerTarget = playerGO.transform;
                CancelInvoke(nameof(FindPlayerTarget)); 
                return;
            }
        }
        
        // Find all tank objects with NetworkObject component
        var tankObjects = FindObjectsByType<TankHealth2D>(FindObjectsSortMode.None);
        foreach (var health in tankObjects)
        {
            NetworkObject networkObj = health.GetComponent<NetworkObject>();
            if (health != null && networkObj != null && networkObj.HasInputAuthority)
            {
                playerTarget = health.transform;
                CancelInvoke(nameof(FindPlayerTarget));
                return;
            }
        }
        
        var tanks = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        foreach (var tank in tanks)
        {
            if (tank != null && tank.InputAuthority != null)
            {
                playerTarget = tank.transform;
                CancelInvoke(nameof(FindPlayerTarget));
                return;
            }
        }
        
        foreach (var tank in tanks)
        {
            var networkObject = tank.GetComponent<NetworkObject>();
            string owner = networkObject != null && networkObject.InputAuthority != null ? 
                          networkObject.InputAuthority.ToString() : "null";
        }
    }
    
    public void ForceReset()
    {
        ExitGameMode();
        wasInGameMode = false;
        
        Invoke(nameof(CheckForTanks), 0.1f);
    }
}