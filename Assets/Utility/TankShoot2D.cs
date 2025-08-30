using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems; 
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class TankShoot2D : Photon.Pun.MonoBehaviourPunCallbacks
{
    [Header("Références Visée / Tir")]
    [SerializeField] private Transform cannonPivot;
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shellSpeed = 15f;
    [SerializeField] private float fireCooldown = 0.5f;
    [SerializeField] private float rocketJumpForce = 75f;
    [SerializeField] private float inAirForce = 75f;
    [SerializeField] private float inAirMultiplier = 0.7f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 1.2f;

    [Header("Détection Sol partagée")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.3f;

    [Header("Power-ups")]
    public bool hasRicochetPowerup = false; // Public for easy activation
    public bool hasExplosivePowerup = false; // Public for easy activation
    public bool hasCloakPowerup = false; // Public for easy activation
    [SerializeField] private float cloakDuration = 8f; // Duration of cloak effect

    [Header("SFX")]
    
    [Header("Tir chargé")]
    [SerializeField] private float chargeTimeThreshold = 0.66f;
    [SerializeField] private float precisionShellSpeedMultiplier = 2f;
    [SerializeField] private float precisionRecoilMultiplier = 0.15f;
    private bool isCharging = false;
    private float chargeStartTime = 0f;
    private bool chargeSFXPlayed = false;

    private float lastFireTime = 0f;
    private Rigidbody2D rb;
    private bool isGrounded = false;
    private bool loggedThisShot = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (cannonPivot == null)
        {
            cannonPivot = transform.Find("CannonPivot");
        }
    }

    private void Start()
    {
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }
        else
        {
            //Debug.Log("[TankShoot2D] Script actif (tank local) sur " + PhotonNetwork.LocalPlayer.NickName);
        }
    }

    private void Update()
    {
        if (!photonView.IsMine || Camera.main == null) return;

        Vector3 mouseScreen = Input.mousePosition;
        
        Vector3 mouseWorld3D;
        if (Camera.main.orthographic)
        {
            mouseWorld3D = Camera.main.ScreenToWorldPoint(new Vector3(
                mouseScreen.x, 
                mouseScreen.y, 
                Camera.main.nearClipPlane
            ));
        }
        else
        {
            float camZ = -Camera.main.transform.position.z;
            mouseWorld3D = Camera.main.ScreenToWorldPoint(
                new Vector3(mouseScreen.x, mouseScreen.y, camZ)
            );
        }
        
        Vector2 mouseWorld = new Vector2(mouseWorld3D.x, mouseWorld3D.y);

        Vector2 pivotPos = (Vector2)cannonPivot.position;
        Vector2 shootDir = (mouseWorld - pivotPos).normalized;
        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
        cannonPivot.rotation = Quaternion.Euler(0f, 0f, angle);

        bool isClickingOnButton = IsPointerOverButton();
        
        bool firePressed = !isClickingOnButton && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space));
        bool fireHeld = !isClickingOnButton && (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space));
        bool fireReleased = Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space);

        if (isClickingOnButton && isCharging)
        {
            isCharging = false;
            chargeSFXPlayed = false;
        }

        if (firePressed && !isCharging)
        {
            isCharging = true;
            chargeStartTime = Time.time;
            chargeSFXPlayed = false;
        }

        if (isCharging && !chargeSFXPlayed)
        {
            float heldTime = Time.time - chargeStartTime;
            if (heldTime >= chargeTimeThreshold)
            {
                SFXManager.Instance.PlaySFX("chargeReady", 1f, 1f);
                chargeSFXPlayed = true;
            }
        }

        if (isCharging && fireReleased)
        {
            float heldTime = Time.time - chargeStartTime;
            bool isPrecision = heldTime >= chargeTimeThreshold;
            FireShell(isPrecision, shootDir, angle);
            isCharging = false;
            chargeSFXPlayed = false;
        }
    }

    private void FireShell(bool isPrecision, Vector2 shootDir, float angle)
    {
        if (Time.time - lastFireTime < fireCooldown) return;
        lastFireTime = Time.time;

        if (CameraShake2D.Instance != null)
        {
            float shakeMagnitude = isPrecision ? 1.5f : 0f;
            float shakeDuration = isPrecision ? 0.08f : 0f;
            CameraShake2D.Instance.Shake(shakeMagnitude, shakeDuration);
        }
        else
        {
            Debug.LogWarning("[CameraShake] CameraShake2D.Instance is null! Make sure the script is attached to the Main Camera.");
        }

        string sfxToPlay;
        if (isPrecision)
        {
            if (hasRicochetPowerup)
                sfxToPlay = "fireRicochet";
            else if (hasExplosivePowerup)
                sfxToPlay = "fireExplosive";
            else
                sfxToPlay = "firePrecision";
        }
        else
        {
            sfxToPlay = "fireNormal";
        }
        
        float pitch = Random.Range(0.95f, 1.05f);
        SFXManager.Instance.PlaySFX(sfxToPlay, 1f, pitch);

        float recoilMultiplier = isPrecision ? precisionRecoilMultiplier : 1f;

        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        if (!isGrounded)
        {
            Vector2 impulseAir = -shootDir * (inAirForce * inAirMultiplier * recoilMultiplier);
            if (!loggedThisShot)
            {
                loggedThisShot = true;
            }
            rb.AddForce(impulseAir, ForceMode2D.Impulse);
        }
        else
        {
            RaycastHit2D hit = Physics2D.Raycast(
                firePoint.position,
                shootDir,
                groundCheckDistance,
                groundLayer
            );

            if (hit.collider != null)
            {
                Vector2 impulseSurface = -shootDir * rocketJumpForce * recoilMultiplier;
                if (!loggedThisShot)
                {
                    loggedThisShot = true;
                }
                var movement = GetComponent<TankMovement2D>();
                movement?.NotifySelfExplosion();
                Vector2 propulsion = -shootDir * rocketJumpForce * recoilMultiplier;
                rb.AddForce(propulsion, ForceMode2D.Impulse);
            }
        }

        float shellSpeedFinal = isPrecision ? shellSpeed * precisionShellSpeedMultiplier : shellSpeed;

        Vector3 spawnPos = firePoint.position + (Vector3)(shootDir * 0.65f);
        spawnPos.z = 0f;
        GameObject shell = PhotonNetwork.Instantiate(shellPrefab.name, spawnPos, Quaternion.Euler(0f, 0f, angle), 0);
        Rigidbody2D shellRb = shell.GetComponent<Rigidbody2D>();
        shellRb.linearVelocity = shootDir * shellSpeedFinal; 
        
        var shellHandler = shell.GetComponent<ShellCollisionHandler>();
        if (shellHandler != null)
        {
            shellHandler.photonView.RPC("SetPrecision", RpcTarget.AllBuffered, isPrecision);
            shellHandler.photonView.RPC("SetShooter", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);

            if (isPrecision)
            {
                if (hasRicochetPowerup)
                {
                    shellHandler.photonView.RPC("ActivateRicochetRPC", RpcTarget.AllBuffered);
                    hasRicochetPowerup = false; 
                    
                    FireRicochetFanShells(shootDir, shellSpeedFinal, isPrecision);
                }
                else if (hasExplosivePowerup)
                {
                    shellHandler.photonView.RPC("ActivateExplosiveShotRPC", RpcTarget.AllBuffered);
                    hasExplosivePowerup = false;
                }
            }
        }

    }

    private void FireRicochetFanShells(Vector2 baseDirection, float speed, bool isPrecision)
    {
        float[] fanAngles = { -30f, -15f, 15f, 30f };
        
        foreach (float angleOffset in fanAngles)
        {
            float angleRad = angleOffset * Mathf.Deg2Rad;
            Vector2 rotatedDir = new Vector2(
                baseDirection.x * Mathf.Cos(angleRad) - baseDirection.y * Mathf.Sin(angleRad),
                baseDirection.x * Mathf.Sin(angleRad) + baseDirection.y * Mathf.Cos(angleRad)
            );
            
            GameObject fanShell = PhotonNetwork.Instantiate(shellPrefab.name, firePoint.position, firePoint.rotation);
            Rigidbody2D fanShellRb = fanShell.GetComponent<Rigidbody2D>();
            fanShellRb.linearVelocity = rotatedDir * speed;
            
            var fanShellHandler = fanShell.GetComponent<ShellCollisionHandler>();
            if (fanShellHandler != null)
            {
                fanShellHandler.photonView.RPC("SetPrecision", RpcTarget.AllBuffered, isPrecision);
                fanShellHandler.photonView.RPC("SetShooter", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
                fanShellHandler.photonView.RPC("ActivateRicochetRPC", RpcTarget.AllBuffered);
            }
        }
        
    }

    [PunRPC]
    public void RPC_ActivateRicochetPowerup()
    {
        if (photonView.IsMine)
        {
            hasRicochetPowerup = true;
        }
    }

    [PunRPC]
    public void RPC_ActivateExplosivePowerup()
    {
        if (photonView.IsMine)
        {
            hasExplosivePowerup = true;
        }
    }

    [PunRPC]
    public void RPC_ActivateCloakPowerup()
    {
        if (photonView.IsMine)
        {
            hasCloakPowerup = true;
            
            MinimapIcon minimapIcon = GetComponent<MinimapIcon>();
            if (minimapIcon != null)
            {
                minimapIcon.ActivateCloak(cloakDuration);
            }
            else
            {
                Debug.LogWarning("[TankShoot2D] MinimapIcon component not found for cloak activation!");
            }
            
            // Start cloak timer
            StartCoroutine(CloakTimer());
        }
    }
    
    private IEnumerator CloakTimer()
    {
        yield return new WaitForSeconds(cloakDuration);
        hasCloakPowerup = false;
    }
    
    private bool IsPointerOverButton()
    {
        if (EventSystem.current == null)
            return false;
            
        GameObject hoveredObject = null;
        
        if (Application.isMobilePlatform)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                var eventData = new PointerEventData(EventSystem.current);
                eventData.position = touch.position;
                
                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);
                
                if (results.Count > 0)
                {
                    hoveredObject = results[0].gameObject;
                }
            }
        }
        else
        {
            var eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            if (results.Count > 0)
            {
                hoveredObject = results[0].gameObject;
            }
        }
        
        if (hoveredObject != null)
        {
            Transform current = hoveredObject.transform;
            while (current != null)
            {
                if (current.GetComponent<UnityEngine.UI.Button>() != null)
                {
                    return true; 
                }
                current = current.parent;
            }
        }
        
        return false;
    }
}