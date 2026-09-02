using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")] [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField, Min(0f)] private float acceleration = 25f;

    [SerializeField, Min(0f)] private float deceleration = 35f;

    [Header("Stamina Settings")] [SerializeField, Min(1f)]
    private float maxStamina = 100f;

    [SerializeField, Min(0f)] private float dashStaminaCost = 50f;

    [SerializeField, Min(0f)] private float staminaRegenerationPerSecond = 50f;

    [SerializeField, Min(0f)] private float staminaRegenerationDelay = 1.25f;

    [SerializeField] private StaminaBarUI staminaBar;

    [Header("Dash Settings")] [SerializeField]
    private float dashSpeed = 12f;

    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.8f;
    [SerializeField, Min(0.01f)] private float dashRecoveryDuration = 0.15f;

    [Header("Attack Area Visual")] [SerializeField]
    private AttackAreaVFX attackAreaVfxPrefab;

    [Header("Combat Settings")] [SerializeField]
    private Transform attackPoint; // Kılıç vuruş merkezinin noktası

    [SerializeField] private float attackRange = 0.5f; // Vuruş alanının yarıçapı

    [SerializeField] private int attackDamage = 20; // Vuruş Hasarı

    [SerializeField, Min(0.1f)] private float maxAttackOriginDifference = 3f;

    [Header("Attack Rate Settings")] [SerializeField]
    private float attackCooldown = 0.4f;

    private float nextAttackTime = 0f;

    [Header("Critical Hit Settings")] [SerializeField, Range(0f, 1f)]
    private float criticalChance = 0.15f;

    [SerializeField, Min(1f)] private float criticalDamageMultiplier = 1.5f;

    [Header("Critical Knockback Settings")] [SerializeField, Min(0f)]
    private float criticalKnockbackDistance = 1.8f;

    [SerializeField, Min(0.01f)] private float criticalKnockbackDuration = 0.18f;

    [Header("Combat Crate Damage")] [SerializeField, Min(1)]
    private int combatCrateAttackDamage = 1;

    [SerializeField, Min(1)] private int combatCrateKnockbackDamage = 2;

    [Header("Preparation Crate Damage")]
    [SerializeField, Min(1)]
    private int preparationCrateAttackDamage = 1;
    
    // Referanslar
    private PlayerLoadout playerLoadout;
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

    [SerializeField, Min(0.1f)] private float breakableValidationDistance = 2.5f;

    private float serverKnockbackValidUntil;
    private Vector2 currentMoveVelocity;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right;
    private bool isDashing = false;
    private bool canDash = true;
    private bool isKnockedBack;
    private float currentStamina;
    private float lastEffectiveMaxStamina;
    private float staminaRegenerationStartTime;
    private float nextStaminaNetworkSyncTime;
    private const float StaminaSyncInterval = 0.1f;


    private NetworkVariable<Vector2> networkAimDirection =
        new NetworkVariable<Vector2>(
            Vector2.right,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

    private readonly NetworkVariable<float>
        networkStamina = new(
            100f,
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
        playerLoadout = GetComponent<PlayerLoadout>();

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

        lastEffectiveMaxStamina = GetEffectiveMaxStamina();

        currentStamina = lastEffectiveMaxStamina;

        UpdateStaminaBar();
    }

    private ItemDefinition GetEquippedWeapon()
    {
        if (playerLoadout == null)
            return null;

        return playerLoadout.EquippedWeapon;
    }

    private float GetEffectiveMoveSpeed()
    {
        float multiplier =
            playerLoadout != null
                ? playerLoadout
                    .TotalMoveSpeedMultiplier
                : 1f;

        return moveSpeed * multiplier;
    }

    private float GetEffectiveMaxStamina()
    {
        float bonus =
            playerLoadout != null
                ? playerLoadout
                    .TotalMaxStaminaBonus
                : 0f;

        return Mathf.Max(
            1f,
            maxStamina + bonus
        );
    }

    private float GetEffectiveDashStaminaCost()
    {
        float multiplier =
            playerLoadout != null
                ? playerLoadout
                    .TotalDashStaminaCostMultiplier
                : 1f;

        return Mathf.Max(
            1f,
            dashStaminaCost * multiplier
        );
    }

    private float GetEffectiveDashCooldown()
    {
        float multiplier =
            playerLoadout != null
                ? playerLoadout
                    .TotalDashCooldownMultiplier
                : 1f;

        return Mathf.Max(
            0.05f,
            dashCooldown * multiplier
        );
    }

    private float GetEffectiveStaminaRegeneration()
    {
        float multiplier =
            playerLoadout != null
                ? playerLoadout
                    .TotalStaminaRegenerationMultiplier
                : 1f;

        return
            staminaRegenerationPerSecond *
            multiplier;
    }

    private int GetEffectiveAttackDamage()
    {
        ItemDefinition weapon =
            GetEquippedWeapon();

        return weapon != null
            ? weapon.AttackDamage
            : attackDamage;
    }

    private float GetEffectiveAttackCooldown()
    {
        ItemDefinition weapon =
            GetEquippedWeapon();

        return weapon != null
            ? Mathf.Max(
                0.05f,
                weapon.AttackCooldown
            )
            : attackCooldown;
    }

    private float GetEffectiveAttackReach()
    {
        ItemDefinition weapon =
            GetEquippedWeapon();

        float reachMultiplier =
            weapon != null
                ? Mathf.Clamp(
                    weapon.AttackReachMultiplier,
                    0.75f,
                    1.5f
                )
                : 1f;

        // Eski dairenin en ileri erişimi:
        // merkez mesafesi + daire yarıçapı.
        float baseForwardReach =
            attackPointDistance +
            attackRange;

        return
            baseForwardReach *
            reachMultiplier;
    }

    private float GetEffectiveCriticalChance()
    {
        ItemDefinition weapon =
            GetEquippedWeapon();

        return weapon != null
            ? Mathf.Clamp01(
                weapon.CriticalChance
            )
            : criticalChance;
    }

    private float
        GetEffectiveCriticalDamageMultiplier()
    {
        ItemDefinition weapon =
            GetEquippedWeapon();

        return weapon != null
            ? Mathf.Max(
                1f,
                weapon.CriticalDamageMultiplier
            )
            : criticalDamageMultiplier;
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        UpdateStamina();

        if (!canControl ||
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
        if (Input.GetMouseButtonDown(0) &&
            Time.time >= nextAttackTime)
        {
            PerformAttack();

            nextAttackTime =
                Time.time +
                GetEffectiveAttackCooldown();
        }

        // Dash (Space)
        if (Input.GetKeyDown(KeyCode.Space) &&
            canDash &&
            currentStamina >=
            GetEffectiveDashStaminaCost())
        {
            SpendDashStamina();

            StartCoroutine(
                PerformDash()
            );
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
            moveInput * GetEffectiveMoveSpeed();

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

            if (IsOwner)
            {
                ResetStamina();
            }
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
            RequestAttackRpc(
                (Vector2)aimOrigin.position,
                aimDirection
            );
        }
    }

    //editör ekranında hitbox'u görmemizi sağlayacak
    private void OnDrawGizmosSelected()
    {
        if (aimOrigin == null ||
            attackPoint == null)
        {
            return;
        }

        Vector2 originPosition =
            aimOrigin.position;

        Vector2 direction =
            (Vector2)attackPoint.position -
            originPosition;

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;
        else
            direction.Normalize();

        float reach;

        if (Application.isPlaying)
        {
            reach =
                GetEffectiveAttackReach();
        }
        else
        {
            float editorPointDistance =
                Vector2.Distance(
                    aimOrigin.position,
                    attackPoint.position
                );

            reach =
                editorPointDistance +
                attackRange;
        }

        Vector2 boxCenter =
            originPosition +
            direction *
            (reach * 0.5f);

        float boxAngle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        Matrix4x4 previousMatrix =
            Gizmos.matrix;

        Gizmos.color = Color.red;

        Gizmos.matrix =
            Matrix4x4.TRS(
                boxCenter,
                Quaternion.Euler(
                    0f,
                    0f,
                    boxAngle
                ),
                Vector3.one
            );

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(
                reach,
                attackRange * 2f,
                0f
            )
        );

        Gizmos.matrix =
            previousMatrix;
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
            moveInput * GetEffectiveMoveSpeed();

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
            GetEffectiveDashCooldown()
        );

        canDash = true;
    }

    private void SpendDashStamina()
    {
        float effectiveDashCost =
            GetEffectiveDashStaminaCost();

        currentStamina =
            Mathf.Max(
                0f,
                currentStamina -
                effectiveDashCost
            );

        staminaRegenerationStartTime =
            Time.time +
            staminaRegenerationDelay;

        UpdateStaminaBar();
        SyncStamina(true);
    }

    private void UpdateStamina()
    {
        float effectiveMaxStamina =
            GetEffectiveMaxStamina();

        if (currentStamina >=
            effectiveMaxStamina)
        {
            return;
        }

        if (Time.time <
            staminaRegenerationStartTime)
        {
            return;
        }

        currentStamina =
            Mathf.MoveTowards(
                currentStamina,
                effectiveMaxStamina,
                GetEffectiveStaminaRegeneration() *
                Time.deltaTime
            );

        UpdateStaminaBar();
        SyncStamina(false);
    }

    private void ResetStamina()
    {
        lastEffectiveMaxStamina =
            GetEffectiveMaxStamina();

        currentStamina =
            lastEffectiveMaxStamina;

        staminaRegenerationStartTime = 0f;

        UpdateStaminaBar();
        SyncStamina(true);
    }

    private void UpdateStaminaBar()
    {
        if (staminaBar == null)
            return;

        staminaBar.UpdateStamina(
            currentStamina,
            GetEffectiveMaxStamina()
        );
    }

    private void SyncStamina(bool forceSync)
    {
        if (!IsOwner ||
            !IsSpawned)
        {
            return;
        }

        if (!forceSync &&
            Time.time <
            nextStaminaNetworkSyncTime)
        {
            return;
        }

        networkStamina.Value =
            currentStamina;

        nextStaminaNetworkSyncTime =
            Time.time +
            StaminaSyncInterval;
    }

    private void OnNetworkStaminaChanged(
        float previousStamina,
        float newStamina)
    {
        if (!IsOwner)
        {
            currentStamina =
                newStamina;
        }

        UpdateStaminaBar();
    }

    private void OnLoadoutChanged()
    {
        float newMaximum =
            GetEffectiveMaxStamina();

        if (IsOwner)
        {
            float gainedMaximum =
                Mathf.Max(
                    0f,
                    newMaximum -
                    lastEffectiveMaxStamina
                );

            currentStamina =
                Mathf.Clamp(
                    currentStamina +
                    gainedMaximum,
                    0f,
                    newMaximum
                );

            SyncStamina(true);
        }
        else
        {
            currentStamina =
                Mathf.Min(
                    currentStamina,
                    newMaximum
                );
        }

        lastEffectiveMaxStamina =
            newMaximum;

        UpdateStaminaBar();

        Vector2 currentAimDirection =
            IsOwner
                ? aimDirection
                : networkAimDirection.Value;

        ApplyAimVisuals(
            currentAimDirection
        );
    }

    public override void OnNetworkSpawn()
    {
        if (playerLoadout != null)
        {
            playerLoadout.LoadoutChanged +=
                OnLoadoutChanged;
        }

        networkAimDirection.OnValueChanged +=
            OnAimDirectionChanged;

        networkStamina.OnValueChanged +=
            OnNetworkStaminaChanged;

        ApplyAimVisuals(
            networkAimDirection.Value
        );

        if (IsOwner)
        {
            ResetStamina();

            StartCoroutine(
                MoveToSpawnPoint()
            );
        }
        else
        {
            currentStamina =
                networkStamina.Value;

            UpdateStaminaBar();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (playerLoadout != null)
        {
            playerLoadout.LoadoutChanged -=
                OnLoadoutChanged;
        }

        networkAimDirection.OnValueChanged -=
            OnAimDirectionChanged;

        networkStamina.OnValueChanged -=
            OnNetworkStaminaChanged;
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
    private void RequestAttackRpc(
        Vector2 requestedAttackOrigin,
        Vector2 requestedAimDirection)
    {
        ResolveAttackOnServer(
            requestedAttackOrigin,
            requestedAimDirection
        );
    }

    private void ResolveAttackOnServer(
        Vector2 requestedAttackOrigin,
        Vector2 requestedAimDirection)
    {
        if (!IsServer)
            return;

        if (MatchManager.Instance == null)
            return;

        MatchPhase currentMatchPhase =
            MatchManager.Instance.CurrentPhase;

        bool isCombatPhase =
            currentMatchPhase ==
            MatchPhase.Combat;

        bool isPreparationPhase =
            currentMatchPhase ==
            MatchPhase.Preparation;

        if (!isCombatPhase &&
            !isPreparationPhase)
        {
            return;
        }

        // Server tarafında saldırı cooldown kontrolü.
        if (Time.time < nextServerAttackTime)
            return;

        nextServerAttackTime =
            Time.time +
            GetEffectiveAttackCooldown();

        Vector2 attackDirection =
            requestedAimDirection.normalized;

        if (attackDirection == Vector2.zero)
            return;

        Vector2 serverAttackOrigin =
            aimOrigin.position;

        Vector2 requestedOriginOffset =
            requestedAttackOrigin -
            serverAttackOrigin;

        Vector2 validatedAttackOrigin =
            serverAttackOrigin +
            Vector2.ClampMagnitude(
                requestedOriginOffset,
                maxAttackOriginDifference
            );

        float effectiveAttackReach =
            GetEffectiveAttackReach();

        Vector2 attackCenter =
            validatedAttackOrigin +
            attackDirection *
            (effectiveAttackReach * 0.5f);

        Vector2 attackSize =
            new Vector2(
                effectiveAttackReach,
                attackRange * 2f
            );

        float attackAngle =
            Mathf.Atan2(
                attackDirection.y,
                attackDirection.x
            ) * Mathf.Rad2Deg;

        PlayAttackAreaVfxRpc(
            validatedAttackOrigin,
            attackDirection,
            effectiveAttackReach,
            attackRange * 2f
        );

        int effectiveAttackDamage =
            GetEffectiveAttackDamage();

        float effectiveCriticalChance =
            GetEffectiveCriticalChance();

        float effectiveCriticalMultiplier =
            GetEffectiveCriticalDamageMultiplier();

        bool isCritical =
            UnityEngine.Random.value <
            effectiveCriticalChance;

        int resolvedDamage =
            isCritical
                ? Mathf.RoundToInt(
                    effectiveAttackDamage *
                    effectiveCriticalMultiplier
                )
                : effectiveAttackDamage;

        Collider2D[] hitColliders =
            Physics2D.OverlapBoxAll(
                attackCenter,
                attackSize,
                attackAngle
            );

        if (isPreparationPhase)
        {
            ResolvePreparationAttackOnServer(
                hitColliders,
                attackCenter
            );

            // Preparation sırasında oyunculara veya
            // EnemyHealth nesnelerine hasar verilmez.
            return;
        }

        HashSet<FighterHealth> damagedFighters =
            new HashSet<FighterHealth>();

        HashSet<EnemyHealth> damagedEnemies =
            new HashSet<EnemyHealth>();

        HashSet<CrateDurability>
            damagedCombatCrates = new();

        foreach (Collider2D hit in hitColliders)
        {
            if (hit == null)
                continue;

            CrateDurability combatCrate =
                hit.GetComponentInParent<
                    CrateDurability>();

            if (combatCrate != null)
            {
                if (!damagedCombatCrates.Add(
                        combatCrate))
                {
                    continue;
                }

                Vector2 crateImpactPoint =
                    hit.ClosestPoint(
                        attackCenter
                    );

                combatCrate.DamageOnServer(
                    combatCrateAttackDamage,
                    crateImpactPoint
                );

                continue;
            }

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

    [Rpc(SendTo.Everyone)]
    private void PlayAttackAreaVfxRpc(
        Vector2 attackOrigin,
        Vector2 attackDirection,
        float attackReach,
        float attackWidth)
    {
        if (attackAreaVfxPrefab == null)
        {
            Debug.LogWarning(
                "PlayerController: " +
                "AttackAreaVFX prefabı atanmamış.",
                this
            );

            return;
        }

        AttackAreaVFX areaVfx =
            Instantiate(
                attackAreaVfxPrefab
            );

        areaVfx.Play(
            attackOrigin,
            attackDirection,
            attackReach,
            attackWidth
        );
    }

    private void ResolvePreparationAttackOnServer(
        Collider2D[] hitColliders,
        Vector2 attackCenter)
    {
        if (!IsServer ||
            hitColliders == null)
        {
            return;
        }

        HashSet<CrateDurability>
            damagedCrates = new();

        foreach (Collider2D hit
                 in hitColliders)
        {
            if (hit == null)
                continue;

            PreparationLootCrate preparationCrate =
                hit.GetComponentInParent<
                    PreparationLootCrate>();

            if (preparationCrate == null)
                continue;

            CrateDurability durability =
                preparationCrate.GetComponent<
                    CrateDurability>();

            if (durability == null)
            {
                Debug.LogWarning(
                    $"{preparationCrate.name}: " +
                    "CombatCrateDurability bulunamadı."
                );

                continue;
            }

            // Aynı kutunun birden fazla collider'ı varsa
            // tek saldırıda yalnızca bir hasar alır.
            if (!damagedCrates.Add(durability))
                continue;

            Vector2 impactPoint =
                hit.ClosestPoint(
                    attackCenter
                );

            durability.DamageOnServer(
                preparationCrateAttackDamage,
                impactPoint
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
            float effectiveAttackReach =
                GetEffectiveAttackReach();

            Vector2 attackPosition =
                originPosition +
                normalizedDirection *
                effectiveAttackReach;

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

        CrateDurability combatCrate =
            breakable.GetComponent<
                CrateDurability>();

        if (combatCrate != null)
        {
            combatCrate.DamageOnServer(
                combatCrateKnockbackDamage,
                impactPoint
            );
        }
        else
        {
            breakable.BreakOnServer(
                impactPoint
            );
        }
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