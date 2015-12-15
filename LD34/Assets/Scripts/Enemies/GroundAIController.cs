using UnityEngine;
using System.Collections;

public class GroundAIController : MonoBehaviour
{
    public SpriteRenderer body;
    public SpriteRenderer deadPrefab;

    public LayerMask targetMask = new LayerMask();
    public LayerMask sightMask = new LayerMask();
    public LayerMask attackMask = new LayerMask();
    public LayerMask navigationMask = new LayerMask();
    public float aggroRange = 10;
    public float attackSpeed = 20;
    public float attackDistance = 1;
    public int attackDamage = 0;
    public float moveSpeed = 10;
    public float minMoveActionTime = 1;
    public float maxMoveActionTime = 3;
    public Collider2D attackCollider;
    public Collider2D floorTest = null;
    public Collider2D wallTest = null;

    float targetingTick = 0;
    float moveTick = 0;
    float moveDuration = 0;
    Transform currentTarget;

    Vector2 velocity = new Vector2(0,0);

    new Rigidbody2D rigidbody2D = null;
    Animator animator = null;

    bool isAttacking = false;

    // Use this for initialization
    void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }
	
    // Update is called once per frame
    void FixedUpdate()
    {
        targetingTick += Time.fixedDeltaTime;
        if (targetingTick > 0.5f)
        {
            var newTarget = FindTarget();
            if (currentTarget != null && newTarget == null)
            {
                velocity.x = 0;
            }
            currentTarget = newTarget;
            targetingTick = 0;
        }

        if (currentTarget && !isAttacking)
        {
            var direction = currentTarget.transform.position - transform.position;
            if (direction.magnitude < attackDistance)
            {
                isAttacking = true;
                bool isTargetRight = direction.x > 0;
                transform.localScale = new Vector3(isTargetRight ? -1 : 1, 1, 1);
            }
            else
            {
                Mathf.SmoothDamp(this.transform.position.x, currentTarget.transform.position.x, ref velocity.x, 0.2f, attackSpeed);
            }
        }
        else if (isAttacking)
        {
            velocity.x = 0;
        }
        else
        {
            moveTick += Time.fixedDeltaTime;
            if (Physics2D.OverlapArea(wallTest.bounds.min, wallTest.bounds.max, navigationMask) != null ||
                Physics2D.OverlapArea(floorTest.bounds.min, floorTest.bounds.max, navigationMask) == null) //wallTest.IsTouchingLayers(navigationMask) || !floorTest.IsTouchingLayers(navigationMask))
            {
                velocity.x = -velocity.x;
            }
            if (moveTick > moveDuration)
            {
                velocity.x = Random.value > 0.5f ? 0 : moveSpeed;
                velocity.x *= Random.value > 0.5f ? -1 : 1;
                moveTick = 0;
                moveDuration = Random.Range(minMoveActionTime, maxMoveActionTime);
            }
        }

        if (velocity.x != 0)
        {
            bool isMovingRight = velocity.x > 0;
            transform.localScale = new Vector3(isMovingRight ? -1 : 1, 1, 1);
        }

        var pos2D = new Vector2(transform.position.x, transform.position.y);
        rigidbody2D.MovePosition(pos2D + (velocity * Time.fixedDeltaTime));

        animator.SetBool("isMoving", velocity.x != 0);
        animator.SetFloat("moveSpeed", velocity.x / (isAttacking ? attackSpeed : moveSpeed));
        animator.SetBool("isAttacking", isAttacking);
    }

    Transform FindTarget()
    {
        var potentialTargets = GameObject.FindGameObjectsWithTag("Player");
        //var potentialTargets = Physics2D.OverlapCircleAll(this.transform.position, aggroRange, targetMask);

        float minDistanceSq = aggroRange*aggroRange;
        Transform closestTargetInSight = null;
        foreach (var target in potentialTargets)
        {
            var direction = this.transform.position - target.transform.position;
            float distSq = direction.sqrMagnitude;
            if (distSq < minDistanceSq)
            {
                closestTargetInSight = target.transform;
                minDistanceSq = distSq;
            }
        }
        return closestTargetInSight;
    }

    void OnAttack()
    {
        Debug.Log("Attack!");
        var hits = Physics2D.OverlapAreaAll(attackCollider.bounds.min, attackCollider.bounds.max, attackMask);
        foreach (var hit in hits)
        {
            hit.SendMessage("OnDamage", new Damage(attackDamage, gameObject), SendMessageOptions.DontRequireReceiver);
        }
    }

    void OnAttackFinished()
    {
        isAttacking = false;
    }

    void OnDeath()
    {
        this.enabled = false;
        animator.SetBool("isDead", true);
        gameObject.layer = 0;
        rigidbody2D.isKinematic = true;
    }

    void OnDamage(Damage damage)
    {
        currentTarget = damage.owner.transform;
    }

    void onDeathAnimFinished()
    {
        Destroy(gameObject);
        if (deadPrefab)
        {
            var deadSprite = Instantiate(deadPrefab.gameObject).GetComponent<SpriteRenderer>();
            deadSprite.transform.localScale = transform.localScale;
            deadSprite.transform.position = this.transform.position;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(this.transform.position, aggroRange);
    }

}

