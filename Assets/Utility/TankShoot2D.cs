using Fusion;
using UnityEngine;
using UnityEngine.EventSystems; 

[RequireComponent(typeof(Rigidbody2D))]
public class TankShoot2D : NetworkBehaviour
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

    [Header("SFX")]
    // [SerializeField] private AudioSource fireNormalSFX;
    // [SerializeField] private AudioSource firePrecisionSFX;
    // [SerializeField] private AudioSource chargeReadySFX;

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
        if (!Object)
        {
            enabled = false;
            return;
        }
        else
        {
            Debug.Log("[TankShoot2D] Script actif (tank local) sur " + Runner.LocalPlayer);
        }
    }

    private void Update()
    {
        if (!Object || Camera.main == null) return;

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
                SFXManager.Instance.PlaySFX("chargeReady");
                chargeSFXPlayed = true;
            }
        }

        if (isCharging && fireReleased)
        {
            float heldTime = Time.time - chargeStartTime;
            bool isPrecision = heldTime >= chargeTimeThreshold;
            FireShell(shootDir, angle, isPrecision);
            isCharging = false;
            chargeSFXPlayed = false;
        }
    }

    private void FireShell(Vector2 shootDir, float angle, bool isPrecision)
    {
        if (Time.time - lastFireTime < fireCooldown) return;
        lastFireTime = Time.time;

        if (isPrecision)
        {
            SFXManager.Instance.PlaySFX("firePrecision");
        }
        else
        {
            SFXManager.Instance.PlaySFX("fireNormal");
        }

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
        // Runner.Spawn requires NetworkObject, using Instantiate temporarily
        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D shellRb = shell.GetComponent<Rigidbody2D>();
        shellRb.linearVelocity = shootDir * shellSpeedFinal; 
        
        var shellHandler = shell.GetComponent<ShellCollisionHandler>();
        if (shellHandler != null)
        {
            shellHandler.SetPrecisionRpc(isPrecision);
            shellHandler.SetShooterRpc(Object.InputAuthority.PlayerId);
        }

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
                
                var results = new System.Collections.Generic.List<RaycastResult>();
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
            
            var results = new System.Collections.Generic.List<RaycastResult>();
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