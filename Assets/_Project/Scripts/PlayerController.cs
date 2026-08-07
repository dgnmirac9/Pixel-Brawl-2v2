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
        HandleFlipping();

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
    private void HandleFlipping()
    {
        if (moveInput.x > 0)
        {
            spriteRenderer.flipX = false;
            if (attackPoint != null)
            {
                Vector3 pos = attackPoint.localPosition;
                attackPoint.localPosition = new Vector3(Mathf.Abs(pos.x), pos.y, pos.z);
            }
        }
        else if (moveInput.x < 0)
        {
            spriteRenderer.flipX = true;

            if (attackPoint != null)
            {
                Vector3 pos = attackPoint.localPosition;
                attackPoint.localPosition = new Vector3(-Mathf.Abs(pos.x), pos.y, pos.z);
            }
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
            //yuvarlanma animasyonu
            anim.SetTrigger("Roll");
        }
        
        Vector2 dashDirection = moveInput != Vector2.zero ? moveInput : lastMoveDirection;

        float timer = 0f;
        while (timer < dashDuration)
        {
            rb.MovePosition(rb.position + dashDirection * (dashSpeed * Time.fixedDeltaTime));
            timer += Time.deltaTime;
            yield return null;
        }

        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}