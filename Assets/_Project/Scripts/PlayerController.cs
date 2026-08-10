using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
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
    
    // Durumlar
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right;
    private bool isDashing = false;
    private bool canDash = true;

    private void Awake()
    {
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
        if (isDashing) return;

        // Fizik tabanlı hareket
        rb.MovePosition(rb.position + moveInput * (moveSpeed * Time.fixedDeltaTime));
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

        // Mouse'u AimOrigin ile aynı dünya düzlemine dönüştürür.
        mouseScreenPosition.z =
            mainCamera.WorldToScreenPoint(aimOrigin.position).z;

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        Vector2 originPosition = aimOrigin.position;

        aimDirection =
            ((Vector2)mouseWorldPosition - originPosition).normalized;

        if (aimDirection == Vector2.zero)
            return;

        if (attackPoint != null)
        {
            Vector2 attackPosition =
                originPosition + aimDirection * attackPointDistance;

            attackPoint.position = new Vector3(
                attackPosition.x,
                attackPosition.y,
                attackPoint.position.z
            );
        }

        if (aimDirection.x > 0.01f)
        {
            spriteRenderer.flipX = false;
        }
        else if (aimDirection.x < -0.01f)
        {
            spriteRenderer.flipX = true;
        }
    }

    private void PerformAttack()
    {
        if (anim != null)
        {
            //temel vuruş için attack1 trigger'ını çalıştırır
            anim.SetTrigger("Attack1");
        }

        if (attackPoint == null) return;
        
        //vuruş alanındaki tüm collider'ları topla
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);
            
        foreach (Collider2D hit in hitColliders)
        {
            //kendimize vurmayı engelliyoruz.
            if (hit.gameObject == gameObject) continue; 
            
            //vurulan objede enemy enemyhealth script'i var mı kontrol et
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage, transform.position);
            }
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

    if (anim != null)
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
}