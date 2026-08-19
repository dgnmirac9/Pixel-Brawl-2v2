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
    
    [SerializeField, Min(0f)]
    private float acceleration = 25f;

    [SerializeField, Min(0f)]
    private float deceleration = 35f;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.8f;
    [SerializeField, Min(0.01f)]
    private float dashRecoveryDuration = 0.15f;
    
    [Header("Combat Settings")]
    [SerializeField] private Transform attackPoint;      // Kılıç vuruş merkezinin noktası
    [SerializeField] private float attackRange = 0.5f;     // Vuruş alanının yarıçapı
    [SerializeField] private int attackDamage = 20;       // Vuruş Hasarı
    
    [Header("Attack Rate Settings")]
    [SerializeField] private float attackCooldown = 0.4f;
    private float nextAttackTime = 0f;
    
    [Header("Critical Hit Settings")]
    [SerializeField, Range(0f, 1f)]
    private float criticalChance = 0.15f;

    [SerializeField, Min(1f)]
    private float criticalDamageMultiplier = 1.5f;
    
    [Header("Critical Knockback Settings")]
    [SerializeField, Min(0f)]
    private float criticalKnockbackDistance = 1.8f;

    [SerializeField, Min(0.01f)]
    private float criticalKnockbackDuration = 0.18f;
    
    // Referanslar
    private bool canControl = true;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Camera mainCamera;
    private Vector2 aimDirection = Vector2.right;
    [SerializeField] private Transform aimOrigin;
    private float attackPointDistance;
    private NetworkAnimator networkAnimator;
    
    // Durumlar
    
    [SerializeField, Min(0.1f)]
    private float breakableValidationDistance = 2.5f;
    
    private float serverKnockbackValidUntil;
    private Vector2 currentMoveVelocity;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right;
    private bool isDashing = false;
    private bool canDash = true;
    private bool isKnockedBack;

    
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
        if (!IsOwner ||
            !canControl ||
            isKnockedBack)
        {
            return;
        }

        if (isDashing)
            return;

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
        if (!IsOwner ||
            !canControl ||
            isDashing ||
            isKnockedBack)
        {
            return;
        }

        Vector2 targetVelocity =
            moveInput * moveSpeed;

        bool hasMovementInput =
            moveInput.sqrMagnitude > 0.001f;

        float velocityChangeRate =
            hasMovementInput
                ? acceleration
                : deceleration;

        currentMoveVelocity =
            Vector2.MoveTowards(
                currentMoveVelocity,
                targetVelocity,
                velocityChangeRate *
                Time.fixedDeltaTime
            );

        rb.MovePosition(
            rb.position +
            currentMoveVelocity *
            Time.fixedDeltaTime
        );
    }
    private void UpdateAnimations()
    {
        if (anim == null) return;

        float speed =
            currentMoveVelocity.magnitude;

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

    public void SetControlEnabled(bool controlEnabled)
    {
        canControl = controlEnabled;

        if (!controlEnabled)
        {
            StopAllCoroutines();

            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            currentMoveVelocity = Vector2.zero;

            isDashing = false;
            isKnockedBack = false;
            canDash = true;
        }
    }
    public void ServerMoveToSpawn(Vector3 spawnPosition)
    {
        if (!IsServer)
            return;

        ApplySpawnPositionRpc(spawnPosition);
    }

    [Rpc(SendTo.Owner)]
    private void ApplySpawnPositionRpc(Vector3 spawnPosition)
    {
        MoveImmediately(spawnPosition);
    }

    private void MoveImmediately(Vector3 targetPosition)
    {
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        currentMoveVelocity = Vector2.zero;
        
        rb.position = new Vector2(
            targetPosition.x,
            targetPosition.y
        );

        transform.position = new Vector3(
            targetPosition.x,
            targetPosition.y,
            transform.position.z
        );
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

        currentMoveVelocity = Vector2.zero;

        Vector2 dashVelocity =
            dashDirection * dashSpeed;

        float elapsedTime = 0f;

        // Tam hızlı dash bölümü.
        while (elapsedTime < dashDuration)
        {
            rb.linearVelocity =
                dashVelocity;

            yield return new WaitForFixedUpdate();

            elapsedTime += Time.fixedDeltaTime;
        }

        // Dash sonrasında ulaşılacak normal hareket hızı.
        Vector2 recoveryTargetVelocity =
            moveInput * moveSpeed;

        float recoveryElapsed = 0f;

        // Dash hızından normal hıza yumuşak geçiş.
        while (recoveryElapsed <
               dashRecoveryDuration)
        {
            float transition =
                recoveryElapsed /
                dashRecoveryDuration;

            transition = Mathf.SmoothStep(
                0f,
                1f,
                transition
            );

            rb.linearVelocity =
                Vector2.Lerp(
                    dashVelocity,
                    recoveryTargetVelocity,
                    transition
                );

            yield return new WaitForFixedUpdate();

            recoveryElapsed +=
                Time.fixedDeltaTime;
        }

        // Normal hareket sistemine aynı hızla devret.
        currentMoveVelocity =
            recoveryTargetVelocity;

        rb.linearVelocity = Vector2.zero;
        isDashing = false;

        yield return new WaitForSeconds(
            dashCooldown
        );

        canDash = true;
    }
    
    public override void OnNetworkSpawn()
    {
        networkAimDirection.OnValueChanged += OnAimDirectionChanged;

        ApplyAimVisuals(networkAimDirection.Value);
        
        if (IsOwner)
        {
            StartCoroutine(MoveToSpawnPoint());
        }
    }

    public override void OnNetworkDespawn()
    {
        networkAimDirection.OnValueChanged -= OnAimDirectionChanged;
    }
    private IEnumerator MoveToSpawnPoint()
    {
        // NetworkTransform'un spawn işlemini tamamlamasını bekle.
        yield return null;

        if (PlayerSpawnManager.Instance == null)
        {
            Debug.LogError(
                "PlayerSpawnManager sahnede bulunamadı!"
            );

            yield break;
        }

        Vector3 spawnPosition =
            PlayerSpawnManager.Instance.GetSpawnPosition(
                OwnerClientId
            );

        MoveImmediately(spawnPosition);

        Debug.Log(
            $"Player {OwnerClientId}, " +
            $"{spawnPosition} pozisyonunda oluşturuldu."
        );
    }
    public void ServerSetControlEnabled(bool controlEnabled)
    {
        if (!IsServer)
            return;

        // Bu karakter Host'a aitse server ve owner aynı uygulamadır.
        if (IsOwner)
        {
            SetControlEnabled(controlEnabled);
            return;
        }

        // Karakter uzak Client'a aitse komutu sahibine gönder.
        ApplyControlStateRpc(controlEnabled);
    }

    [Rpc(SendTo.Owner)]
    private void ApplyControlStateRpc(bool controlEnabled)
    {
        SetControlEnabled(controlEnabled);
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

    private void ResolveAttackOnServer(
        Vector2 requestedAimDirection)
    {
        if (!IsServer)
            return;

        // Server tarafında saldırı cooldown kontrolü.
        if (Time.time < nextServerAttackTime)
            return;

        nextServerAttackTime =
            Time.time + attackCooldown;

        Vector2 attackDirection =
            requestedAimDirection.normalized;

        if (attackDirection == Vector2.zero)
            return;

        Vector2 attackCenter =
            (Vector2)aimOrigin.position +
            attackDirection * attackPointDistance;
        
        bool isCritical =
            UnityEngine.Random.value < criticalChance;

        int resolvedDamage =
            isCritical
                ? Mathf.RoundToInt(
                    attackDamage *
                    criticalDamageMultiplier
                )
                : attackDamage;
        
        Collider2D[] hitColliders =
            Physics2D.OverlapCircleAll(
                attackCenter,
                attackRange
            );

        HashSet<FighterHealth> damagedFighters =
            new HashSet<FighterHealth>();

        HashSet<EnemyHealth> damagedEnemies =
            new HashSet<EnemyHealth>();

        foreach (Collider2D hit in hitColliders)
        {
            FighterHealth fighter =
                hit.GetComponentInParent<FighterHealth>();

            if (fighter != null)
            {
                // Oyuncunun kendisine vurmasını engeller.
                if (fighter.NetworkObject == NetworkObject)
                    continue;

                if (!fighter.IsAlive)
                    continue;

                // Birden fazla collider aynı oyuncuya
                // birden fazla hasar vermesin.
                if (!damagedFighters.Add(fighter))
                    continue;

                Vector2 hitPosition =
                    hit.ClosestPoint(attackCenter);
                
                fighter.TakeDamage(
                    resolvedDamage,
                    hitPosition,
                    isCritical
                );

                if (isCritical && fighter.IsAlive)
                {
                    PlayerController targetController =
                        fighter.GetComponent<PlayerController>();

                    Vector2 knockbackDirection =
                        (Vector2)fighter.transform.position -
                        (Vector2)transform.position;

                    if (knockbackDirection == Vector2.zero)
                    {
                        knockbackDirection =
                            attackDirection;
                    }

                    targetController?.ServerApplyKnockback(
                        knockbackDirection.normalized,
                        criticalKnockbackDistance,
                        criticalKnockbackDuration
                    );
                }
                
                continue;
            }

            EnemyHealth enemy =
                hit.GetComponentInParent<EnemyHealth>();

            if (enemy == null)
                continue;

            if (!damagedEnemies.Add(enemy))
                continue;

            enemy.TakeDamage(
                resolvedDamage,
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
    public void ServerApplyKnockback(
        Vector2 direction,
        float distance,
        float duration)
    {
        if (!IsServer || !IsSpawned)
            return;

        if (direction == Vector2.zero)
            return;

        serverKnockbackValidUntil =
            Time.time + duration + 0.25f;

        ApplyKnockbackRpc(
            direction.normalized,
            Mathf.Max(0f, distance),
            Mathf.Max(0.01f, duration)
        );
    }

    [Rpc(SendTo.Owner)]
    private void ApplyKnockbackRpc(
        Vector2 direction,
        float distance,
        float duration)
    {
        StopAllCoroutines();

        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;   
        currentMoveVelocity = Vector2.zero;
        
        isDashing = false;
        canDash = true;

        StartCoroutine(
            PerformKnockback(
                direction,
                distance,
                duration
            )
        );
    }
    
    private void OnCollisionEnter2D(
        Collision2D collision)
    {
        Debug.Log(
            $"[Collision] {name} → " +
            $"{collision.collider.name}"
        );
        
        if (!IsOwner ||
            !IsSpawned ||
            !isKnockedBack)
        {
            return;
        }

        BreakableObject breakable =
            collision.collider
                .GetComponentInParent<
                    BreakableObject>();

        if (breakable == null ||
            breakable.IsBroken)
        {
            return;
        }

        NetworkObject breakableNetworkObject =
            breakable.NetworkObject;

        if (breakableNetworkObject == null)
            return;

        RequestBreakableCollisionRpc(
            new NetworkObjectReference(
                breakableNetworkObject
            )
        );
    }
    
    [Rpc(SendTo.Server)]
    private void RequestBreakableCollisionRpc(
        NetworkObjectReference breakableReference)
    {
        if (Time.time >
            serverKnockbackValidUntil)
        {
            return;
        }

        if (!breakableReference.TryGet(
                out NetworkObject breakableObject))
        {
            return;
        }

        BreakableObject breakable =
            breakableObject.GetComponent<
                BreakableObject>();

        if (breakable == null ||
            breakable.IsBroken)
        {
            return;
        }

        float distanceToBreakable =
            Vector2.Distance(
                transform.position,
                breakable.transform.position
            );

        if (distanceToBreakable >
            breakableValidationDistance)
        {
            return;
        }

        Collider2D breakableCollider =
            breakable.GetComponent<Collider2D>();

        Vector2 impactPoint =
            breakableCollider != null
                ? breakableCollider.ClosestPoint(
                    transform.position
                )
                : breakable.transform.position;

        breakable.BreakOnServer(
            impactPoint
        );

        // Aynı knockback yalnızca bir nesne kırsın.
        serverKnockbackValidUntil = 0f;
    }

    private IEnumerator PerformKnockback(
        Vector2 direction,
        float distance,
        float duration)
    {
        isKnockedBack = true;

        float knockbackSpeed =
            distance / duration;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            rb.linearVelocity =
                direction.normalized *
                knockbackSpeed;

            yield return new WaitForFixedUpdate();

            elapsedTime += Time.fixedDeltaTime;
        }

        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
    }
}