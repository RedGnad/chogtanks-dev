// Assets/Utility/TankShoot2D.cs

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

        bool clickLeft = Input.GetMouseButtonDown(0);
        bool keySpace = Input.GetKeyDown(KeyCode.Space);
        if ((clickLeft || keySpace) && (Time.time - lastFireTime > fireCooldown))
        {
            lastFireTime = Time.time;
            loggedThisShot = false;

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
                Vector2 impulseAir = -shootDir * (inAirForce * inAirMultiplier);
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
                    Vector2 impulseSurface = -shootDir * rocketJumpForce;
                    if (!loggedThisShot)
                    {
                        Debug.Log($"[TankShoot2D] Self-explosion (rocket-jump) : impulse = {impulseSurface}");
                        loggedThisShot = true;
                    }
                    var movement = GetComponent<TankMovement2D>();
                    movement?.NotifySelfExplosion();
                    // Propulsion à 360° : applique la force dans la direction opposée au tir
                    Vector2 propulsion = -shootDir * rocketJumpForce;
                    Debug.Log($"[ROCKET JUMP] AddForce: direction={-shootDir}, force={rocketJumpForce}, vector={propulsion}");
                    rb.AddForce(propulsion, ForceMode2D.Impulse);
                }
            }

            Vector3 spawnPos = firePoint.position + (Vector3)(shootDir * 0.2f);
            spawnPos.z = 0f;
            // Instancie le shell réseau (un seul objet synchronisé)
            GameObject shell = PhotonNetwork.Instantiate(shellPrefab.name, spawnPos, Quaternion.Euler(0f, 0f, angle), 0);
            Rigidbody2D shellRb = shell.GetComponent<Rigidbody2D>();
            shellRb.linearVelocity = shootDir * shellSpeed;
            // Optionnel : log pour debug
            Debug.Log($"[DEBUG SHOOT] PhotonNetwork.Instantiate shell sur {PhotonNetwork.LocalPlayer.NickName} (Actor {PhotonNetwork.LocalPlayer.ActorNumber})");
        }
    }


}
