using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TankMovement2D : Photon.Pun.MonoBehaviourPunCallbacks
{
    [Header("Réglages Mouvement")]
    [SerializeField] private float moveSpeed = 5f;         // Vitesse horizontale au sol
    [SerializeField] private float jumpForce = 12f;        // Force du saut
    [SerializeField] private float wallSlideSpeed = 2f;    // Vitesse de glisse sur mur
    [SerializeField] private float wallJumpForceX = 8f;    // Force horizontale du saut mural
    [SerializeField] private float wallJumpForceY = 12f;   // Force verticale du saut mural
    [SerializeField] private LayerMask groundLayer;       // Couches considérées comme "sol"

    [Header("Détection Mur")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.2f;

    [Header("Visuel (optionnel)")]
    [SerializeField] private Transform visualTransform;   // Pour aligner sur pente si besoin

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isWallSliding;
    private Vector2 groundNormal = Vector2.up;

    // Compte le nombre de collisions actives avec le sol (overlap ne suffit pas)
    private int groundContactCount = 0;

    // Lock de plusieurs frames pour préserver la composante X après self‐explosion
    private int explosionLockFrames = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // IMPORTANT : vérifiez que "Freeze Position X" est décoché dans Rigidbody2D
        // afin que l’on puisse modifier rb.velocity.x sans blocage.
        if (visualTransform == null)
            Debug.LogWarning("[TankMovement2D] visualTransform non assigné (optionnel).");
    }

    private void Start()
    {
        Debug.Log($"[DEBUG] photonView.IsMine={photonView.IsMine} | rb.bodyType={rb.bodyType}");
        if (photonView.IsMine)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            // Assure-toi que le Rigidbody2D n'a pas de contraintes de freeze sur X/Y
            rb.constraints = RigidbodyConstraints2D.None;
        }
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }
    }

    private float prevHorizontalInput = 0f;
    private void Update()
    {
        if (!photonView.IsMine) return;
        // Lecture input horizontal
        horizontalInput = Input.GetAxisRaw("Horizontal");
        if (horizontalInput != prevHorizontalInput && horizontalInput != 0f)
        {
            Debug.Log($"[INPUT] horizontalInput = {horizontalInput}");
        }
        prevHorizontalInput = horizontalInput;

        // Saut
        if (Input.GetButtonDown("Jump") && groundContactCount > 0)
        {
            Debug.Log("[INPUT] Jump press detected");
            Jump();
        }
    }

    private float prevPhysicsInput = 0f;
    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;
        if (horizontalInput != prevPhysicsInput && horizontalInput != 0f)
        {
            Debug.Log($"[PHYSICS] FixedUpdate | horizontalInput utilisé = {horizontalInput}");
        }
        prevPhysicsInput = horizontalInput;

        // --- GESTION ROCKET JUMP MULTI-FRAME ---
        if (explosionLockFrames > 0)
        {
            explosionLockFrames--;
            return;
        }
        // --- FIN GESTION ROCKET JUMP ---

        // 1) Détection glisse murale (on ne touche plus au sol si groundContactCount == 0)
        Vector2 wallDir = horizontalInput >= 0 ? Vector2.right : Vector2.left;
        isWallSliding = Physics2D.Raycast(
            wallCheck.position,
            wallDir,
            wallCheckDistance,
            groundLayer
        ) && groundContactCount == 0 && Mathf.Abs(horizontalInput) > 0f;

        // 2) Calcul composante Y
        float yVel = rb.linearVelocity.y;
        if (isWallSliding)
        {
            yVel = Mathf.Clamp(rb.linearVelocity.y, -wallSlideSpeed, float.MaxValue);
        }

        // Correction : on ne touche pas à la vélocité par défaut, seulement si input ou au sol
        float xVel = horizontalInput * moveSpeed;
        // (plus de reset systématique de la vélocité ici)


        // 4) Sinon, si on est fermement au sol (groundContactCount > 0 ET presque pas de vitesse Y), écraser X
        if (groundContactCount > 0 && Mathf.Abs(rb.linearVelocity.y) < 0.01f)
        {
            Vector2 target = new Vector2(horizontalInput * moveSpeed, yVel);
            rb.linearVelocity = target;
        }
        else
        {
            // 5) En l’air (ou au mur) : conserver composante X existante ou appliquer input si X=0
            float currentVelX = rb.linearVelocity.x;
            if (Mathf.Approximately(currentVelX, 0f))
            {
                rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, yVel);
            }
            else
            {
                if (Mathf.Abs(horizontalInput) > 0f && groundContactCount == 0)
                {
                    // Si en l’air, on permet un très léger ajustement latéral
                    rb.AddForce(new Vector2(horizontalInput * (moveSpeed * 0.02f), 0f),
                                ForceMode2D.Impulse);
                }
                rb.linearVelocity = new Vector2(currentVelX, yVel);
            }
        }

        // 6) Alignement visuel sur pente (optionnel)
        if (visualTransform != null)
            AlignToSlope();
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void WallJump()
    {
        int dir = (wallCheck.position.x > transform.position.x) ? 1 : -1;
        rb.linearVelocity = new Vector2(-dir * wallJumpForceX, wallJumpForceY);
    }

    private void AlignToSlope()
    {
        // Si vous souhaitez aligner visuellement un enfant sur la pente, vérifiez groundNormal
        RaycastHit2D hit = Physics2D.Raycast(
            groundCheckPosition(),    // Méthode imaginaire ; enlevez si pas utilisé
            Vector2.down,
            0.5f,
            groundLayer
        );
        if (hit.collider != null)
        {
            float angle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg - 90f;
            visualTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    // Nécessaire pour AlignToSlope si vous utilisez groundCheck, sinon supprimez ce bout
    private Vector3 groundCheckPosition()
    {
        // Si vous aviez un groundCheck Transform assigné, retournez son position :
        // return groundCheck.position;
        // Sinon, juste le centre du tank :
        return transform.position;
    }

    /// <summary>
    /// Doit être appelé **immédiatement** par TankShoot2D avant d’appliquer l’impulsion X+Y.
    /// </summary>
    public void NotifySelfExplosion()
    {
        explosionLockFrames = 3; // nombre de frames où on laisse la propulsion agir
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Incrémente si c’est un “sol”
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            groundContactCount++;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // Décrémente quand on ne touche plus ce “sol”
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            groundContactCount = Mathf.Max(groundContactCount - 1, 0);
        }
    }
}
