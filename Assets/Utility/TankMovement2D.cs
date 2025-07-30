using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TankMovement2D : Photon.Pun.MonoBehaviourPunCallbacks
{
    [Header("Réglages Mouvement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private float wallJumpForceX = 8f;
    [SerializeField] private float wallJumpForceY = 12f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Détection Mur")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.2f;

    [Header("Visuel (optionnel)")]
    [SerializeField] private Transform visualTransform;

    [Header("Contrôles mobiles (optionnel)")]
    public MobileInputButton leftButton;
    public MobileInputButton rightButton;

    [Header("Optimisation")]
    [SerializeField] private bool useMobileOptimizations = true;
    [SerializeField] private float mobileRaycastFrequency = 0.2f; // Seconds between slope alignment checks

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isWallSliding;
    private Vector2 groundNormal = Vector2.up;
    private int groundContactCount = 0;
    private int explosionLockFrames = 0;
    
    private float lastAlignmentTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (visualTransform == null)
            
        if (leftButton == null)
            leftButton = GameObject.Find("LeftButton")?.GetComponent<MobileInputButton>();
        if (rightButton == null)
            rightButton = GameObject.Find("RightButton")?.GetComponent<MobileInputButton>();
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
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

        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (leftButton != null && leftButton.IsPressed)
            horizontalInput = -1f;
        else if (rightButton != null && rightButton.IsPressed)
            horizontalInput = 1f;

        if (Mathf.Approximately(prevHorizontalInput, horizontalInput))
            return;
            
        prevHorizontalInput = horizontalInput;

        if (Input.GetButtonDown("Jump") && groundContactCount > 0)
        {
            Jump();
        }
    }

    private float prevPhysicsInput = 0f;
    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        if (explosionLockFrames > 0)
        {
            explosionLockFrames--;
            return;
        }

        bool wallCheckNeeded = groundContactCount == 0 && Mathf.Abs(horizontalInput) > 0f;
        if (wallCheckNeeded)
        {
            Vector2 wallDir = horizontalInput >= 0 ? Vector2.right : Vector2.left;
            isWallSliding = Physics2D.Raycast(
                wallCheck.position,
                wallDir,
                wallCheckDistance,
                groundLayer
            );
        }
        else
        {
            isWallSliding = false;
        }

        float yVel = rb.velocity.y;
        if (isWallSliding)
        {
            yVel = Mathf.Clamp(rb.velocity.y, -wallSlideSpeed, float.MaxValue);
        }

        if (groundContactCount > 0 && Mathf.Abs(rb.velocity.y) < 0.01f)
        {
            rb.velocity = new Vector2(horizontalInput * moveSpeed, yVel);
        }
        else
        {
            float currentVelX = rb.velocity.x;
            if (Mathf.Approximately(currentVelX, 0f))
            {
                rb.velocity = new Vector2(horizontalInput * moveSpeed, yVel);
            }
            else
            {
                if (Mathf.Abs(horizontalInput) > 0f && groundContactCount == 0)
                {
                    rb.AddForce(new Vector2(horizontalInput * (moveSpeed * 0.02f), 0f),
                                ForceMode2D.Impulse);
                }
                rb.velocity = new Vector2(currentVelX, yVel);
            }
        }

        if (visualTransform != null && 
            (!useMobileOptimizations || Time.time - lastAlignmentTime >= mobileRaycastFrequency))
        {
            AlignToSlope();
            lastAlignmentTime = Time.time;
        }
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    private void WallJump()
    {
        int dir = (wallCheck.position.x > transform.position.x) ? 1 : -1;
        rb.velocity = new Vector2(-dir * wallJumpForceX, wallJumpForceY);
    }

    private void AlignToSlope()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            groundCheckPosition(),
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

    private Vector3 groundCheckPosition()
    {
        return transform.position;
    }

    public void NotifySelfExplosion()
    {
        explosionLockFrames = 3;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            groundContactCount++;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            groundContactCount = Mathf.Max(groundContactCount - 1, 0);
        }
    }
}