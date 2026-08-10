using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.8f;

    [Header("Combat Settings")]
    [SerializeField] private Transform attackPoint;      // Kılıç vuruş merkezinin noktası
    [SerializeField] private float attackRange = 0.5f;     // Vuruş alanının yarıçapı
    [SerializeField] private int attackDamage = 20;       // Vuruş Hasarı
    
    [Header("Attack Rate Settings")]
    [SerializeField] private float attackCooldown = 0.4f;
    private float nextAttackTime = 0f;
    
    // Referanslar
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Camera mainCamera;
    private Vector2 aimDirection = Vector2.right;
    [SerializeField] private Transform aimOrigin;
    private float attackPointDistance;
    private NetworkAnimator networkAnimator;
    
    // Durumlar
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right;
    private bool isDashing = false;
    private bool canDash = true;

    
    private NetworkVariable<Vector2> networkAimDirection =
        new NetworkVariable<Vector2>(
            Vector2.right,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
    
    private float nextServerAttackTime;
    private const float AimSyncThreshold = 0.0025f;
    
    private void Awake()
    {
        networkAnimator = GetComponent<NetworkAnimator>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        mainCamera = Camera.main;
        if (aimOrigin == null)
        {
            aimOrigin = transform;
        }

        if (attackPoint != null)
        {
            attackPointDistance = Vector2.Distance(
                aimOrigin.position,
                attackPoint.position
            );
        }
    }

    private void Update()
    {
        if (!IsOwner) return; 
        if (isDashing) return;

        // Girdiler (WASD / Yön Tuşları)
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        if (moveInput != Vector2.zero)
        {
            lastMoveDirection = moveInput;
        }

        UpdateAnimations();

        // Sağa / Sola Dönüş (FlipX)
        HandleAiming();

        //Saldırı (SOL TIK)
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            PerformAttack();
            nextAttackTime = Time.time + attackCooldown;
        }
        
        // Dash (Space)
        if (Input.GetKeyDown(KeyCode.Space) && canDash)
        {
            StartCoroutine(PerformDash());
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || isDashing) return;

        rb.MovePosition(
            rb.position + moveInput * (moveSpeed * Time.fixedDeltaTime)
        );
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;

        float speed = moveInput.magnitude;

        // Karakterin karada olduğunu belirtiyoruz (Şart olan parametre)
        anim.SetBool("Grounded", true);

        // Hareket ediyorsa AnimState = 1 (Run), duruyorsa AnimState = 0 (Idle)
        anim.SetInteger("AnimState", speed > 0.1f ? 1 : 0);
    }
    private void HandleAiming()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;

            if (mainCamera == null)
                return;
        }

        Vector3 mouseScreenPosition = Input.mousePosition;

        mouseScreenPosition.z =
            mainCamera.WorldToScreenPoint(aimOrigin.position).z;

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        Vector2 newAimDirection =
            ((Vector2)mouseWorldPosition -
             (Vector2)aimOrigin.position).normalized;

        if (newAimDirection == Vector2.zero)
            return;

        aimDirection = newAimDirection;

        // Sahip oyuncuda anında göster.
        ApplyAimVisuals(aimDirection);

        // Yön yeterince değiştiyse network'e gönder.
        if (IsSpawned &&
            (networkAimDirection.Value - aimDirection).sqrMagnitude
            > AimSyncThreshold)
        {
            networkAimDirection.Value = aimDirection;
        }
    }

    private void PerformAttack()
    {
        // Animasyon sahibinde hemen oynar ve NetworkAnimator paylaşır.
        if (networkAnimator != null && IsSpawned)
        {
            networkAnimator.SetTrigger("Attack1");
        }
        else if (anim != null)
        {
            anim.SetTrigger("Attack1");
        }

        if (attackPoint == null || aimOrigin == null)
            return;

        if (IsSpawned)
        {
            RequestAttackRpc(aimDirection);
        }
    }

    //editör ekranında hitbox'u görmemizi sağlayacak
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
    
    private IEnumerator PerformDash()
    {
    canDash = false;
    isDashing = true;

    if (networkAnimator != null && IsSpawned)
    {
        networkAnimator.SetTrigger("Roll");
    }
    else if (anim != null)
    {
        anim.SetTrigger("Roll");
    }

    Vector2 dashDirection =
        moveInput != Vector2.zero
            ? moveInput.normalized
            : lastMoveDirection.normalized;

    float elapsedTime = 0f;

    while (elapsedTime < dashDuration)
    {
        rb.linearVelocity = dashDirection * dashSpeed;

        yield return new WaitForFixedUpdate();
        elapsedTime += Time.fixedDeltaTime;
    }

    rb.linearVelocity = Vector2.zero;
    isDashing = false;

    yield return new WaitForSeconds(dashCooldown);

    canDash = true;
    }
    
    public override void OnNetworkSpawn()
    {
        networkAimDirection.OnValueChanged += OnAimDirectionChanged;

        ApplyAimVisuals(networkAimDirection.Value);
    }

    public override void OnNetworkDespawn()
    {
        networkAimDirection.OnValueChanged -= OnAimDirectionChanged;
    }

    private void OnAimDirectionChanged(
        Vector2 previousDirection,
        Vector2 newDirection)
    {
        if (!IsOwner)
        {
            ApplyAimVisuals(newDirection);
        }
    }
    [Rpc(SendTo.Server)]
    private void RequestAttackRpc(Vector2 requestedAimDirection)
    {
        ResolveAttackOnServer(requestedAimDirection);
    }

    private void ResolveAttackOnServer(Vector2 requestedAimDirection)
    {
        if (!IsServer)
            return;

        // Client değiştirilmiş bir kodla saldırı spam'leyemesin.
        if (Time.time < nextServerAttackTime)
            return;

        nextServerAttackTime = Time.time + attackCooldown;

        Vector2 attackDirection = requestedAimDirection.normalized;

        if (attackDirection == Vector2.zero)
            return;

        Vector2 attackCenter =
            (Vector2)aimOrigin.position +
            attackDirection * attackPointDistance;

        Collider2D[] hitColliders =
            Physics2D.OverlapCircleAll(
                attackCenter,
                attackRange
            );

        HashSet<EnemyHealth> damagedEnemies =
            new HashSet<EnemyHealth>();

        foreach (Collider2D hit in hitColliders)
        {
            EnemyHealth enemy =
                hit.GetComponentInParent<EnemyHealth>();

            if (enemy == null)
                continue;

            // Bir enemy'de birden fazla collider varsa bir kez hasar ver.
            if (!damagedEnemies.Add(enemy))
                continue;

            enemy.TakeDamage(
                attackDamage,
                transform.position
            );
        }
    }
    
    private void ApplyAimVisuals(Vector2 direction)
    {
        if (direction == Vector2.zero)
            return;

        Vector2 normalizedDirection = direction.normalized;
        Vector2 originPosition = aimOrigin.position;

        if (attackPoint != null)
        {
            Vector2 attackPosition =
                originPosition +
                normalizedDirection * attackPointDistance;

            attackPoint.position = new Vector3(
                attackPosition.x,
                attackPosition.y,
                attackPoint.position.z
            );
        }

        if (normalizedDirection.x > 0.01f)
        {
            spriteRenderer.flipX = false;
        }
        else if (normalizedDirection.x < -0.01f)
        {
            spriteRenderer.flipX = true;
        }
    }
}