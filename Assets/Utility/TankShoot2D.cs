using Photon.Pun;
using UnityEngine;

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

    // SFX
    [Header("SFX")]
    [SerializeField] private AudioSource fireNormalSFX;
    [SerializeField] private AudioSource firePrecisionSFX;
    [SerializeField] private AudioSource chargeReadySFX;

    // Ajout pour tir chargé
    [Header("Tir chargé")]
    [SerializeField] private float chargeTimeThreshold = 1.0f;
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
            Debug.LogError("[TankShoot2D] cannonPivot non assigné !");
    }

    private void Start()
    {
        if (!photonView.IsMine)
        {
            Debug.Log("[TankShoot2D] Script désactivé (pas mon tank) sur " + PhotonNetwork.LocalPlayer.NickName);
            enabled = false;
            return;
        }
        else
        {
            Debug.Log("[TankShoot2D] Script actif (tank local) sur " + PhotonNetwork.LocalPlayer.NickName);
        }
    }

    private void Update()
    {
        if (!photonView.IsMine || Camera.main == null) return;

        Vector3 mouseScreen = Input.mousePosition;
        float camZ = -Camera.main.transform.position.z;
        Vector3 mouseWorld3D = Camera.main.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, camZ)
        );
        Vector2 mouseWorld = new Vector2(mouseWorld3D.x, mouseWorld3D.y);

        Vector2 pivotPos = (Vector2)cannonPivot.position;
        Vector2 shootDir = (mouseWorld - pivotPos).normalized;
        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
        cannonPivot.rotation = Quaternion.Euler(0f, 0f, angle);

        // Gestion du tir chargé
        bool firePressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        bool fireHeld = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
        bool fireReleased = Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space);

        // Début du chargement
        if (firePressed && !isCharging)
        {
            isCharging = true;
            chargeStartTime = Time.time;
            chargeSFXPlayed = false;
        }

        // SFX de charge prête
        if (isCharging && !chargeSFXPlayed)
        {
            float heldTime = Time.time - chargeStartTime;
            if (heldTime >= chargeTimeThreshold)
            {
                if (chargeReadySFX != null)
                    chargeReadySFX.Play();
                chargeSFXPlayed = true;
            }
        }

        // Fin du chargement (relâchement)
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

        // SFX
        if (isPrecision)
        {
            if (firePrecisionSFX != null) firePrecisionSFX.Play();
        }
        else
        {
            if (fireNormalSFX != null) fireNormalSFX.Play();
        }

        // --- Recul adapté pour le tir chargé ---
        float recoilMultiplier = isPrecision ? precisionRecoilMultiplier : 1f;

        // Gestion du recul (identique à avant)
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
        if (isGrounded != wasGrounded)
            Debug.Log($"[TankShoot2D] isGrounded = {isGrounded}");

        if (!isGrounded)
        {
            Vector2 impulseAir = -shootDir * (inAirForce * inAirMultiplier * recoilMultiplier);
            if (!loggedThisShot)
            {
                Debug.Log($"[TankShoot2D] Jump en vol : impulse = {impulseAir}");
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
                    Debug.Log($"[TankShoot2D] Self-explosion (rocket-jump) : impulse = {impulseSurface}");
                    loggedThisShot = true;
                }
                var movement = GetComponent<TankMovement2D>();
                movement?.NotifySelfExplosion();
                Vector2 propulsion = -shootDir * rocketJumpForce * recoilMultiplier;
                Debug.Log($"[ROCKET JUMP] AddForce: direction={-shootDir}, force={rocketJumpForce * recoilMultiplier}, vector={propulsion}");
                rb.AddForce(propulsion, ForceMode2D.Impulse);
            }
        }

        // Calcul de la vitesse du shell selon le type de tir
        float shellSpeedFinal = isPrecision ? shellSpeed * precisionShellSpeedMultiplier : shellSpeed;

        Vector3 spawnPos = firePoint.position + (Vector3)(shootDir * 0.2f);
        spawnPos.z = 0f;
        GameObject shell = PhotonNetwork.Instantiate(shellPrefab.name, spawnPos, Quaternion.Euler(0f, 0f, angle), 0);
        Rigidbody2D shellRb = shell.GetComponent<Rigidbody2D>();
        shellRb.linearVelocity = shootDir * shellSpeedFinal;

        // --- Synchronisation du sprite du shell ---
        var shellHandler = shell.GetComponent<ShellCollisionHandler>();
        if (shellHandler != null)
        {
            shellHandler.photonView.RPC("SetPrecision", RpcTarget.AllBuffered, isPrecision);
        }

        Debug.Log($"[DEBUG SHOOT] PhotonNetwork.Instantiate shell {(isPrecision ? "CHARGÉ" : "normal")} sur {PhotonNetwork.LocalPlayer.NickName} (Actor {PhotonNetwork.LocalPlayer.ActorNumber})");
    }
}