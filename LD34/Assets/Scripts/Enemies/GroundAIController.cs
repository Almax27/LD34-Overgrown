using UnityEngine;
using System.Collections;

public class GroundAIController : MonoBehaviour
{
    public SpriteRenderer body;
    public SpriteRenderer deadPrefab;

    public LayerMask targetMask = new LayerMask();
    public LayerMask sightMask = new LayerMask();
    public float aggroRange = 10;
    public float speed = 10;
    public float attackDistance = 1;

    float targetingTick = 0;
    Transform currentTarget;

    Vector2 velocity = new Vector2(0,0);

    new Rigidbody2D rigidbody2d = null;
    Animator animator = null;

    bool isAttacking = false;

    // Use this for initialization
    void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }
	
    // Update is called once per frame
    void FixedUpdate()
    {
        targetingTick += Time.deltaTime;
        if (targetingTick > 0.5f)
        {
            currentTarget = FindTarget();
            targetingTick = 0;
        }

        if (currentTarget && !isAttacking)
        {
            float distance = Mathf.Abs(this.transform.position.x - currentTarget.transform.position.x);
            if (distance < attackDistance)
            {
                isAttacking = true;
            }
            else
            {
                Mathf.SmoothDamp(this.transform.position.x, currentTarget.transform.position.x, ref velocity.x, 0.2f, speed);
            }
        }
        else
        {
            velocity.x = 0;
        }
        var pos2D = new Vector2(transform.position.x, transform.position.y);
        rigidbody2d.MovePosition(pos2D + (velocity * Time.fixedDeltaTime));

        if (velocity.x != 0)
        {
            bool isMovingRight = velocity.x > 0;
            body.flipX = isMovingRight;
        }

        animator.SetBool("isMoving", velocity.x != 0);
        animator.SetFloat("moveSpeed", velocity.x / speed);
        animator.SetBool("isAttacking", isAttacking);
    }

    Transform FindTarget()
    {
        var potentialTargets = Physics2D.OverlapCircleAll(this.transform.position, aggroRange, targetMask);

        float minDistanceSq = float.MaxValue;
        Transform closestTargetInSight = null;
        foreach (var target in potentialTargets)
        {
            var direction = this.transform.position - target.transform.position;
            float distSq = direction.sqrMagnitude;
            if (distSq < minDistanceSq)
            {
                //test line of sight
                var hitInfo = Physics2D.Raycast(this.transform.position, direction.normalized, aggroRange, sightMask);
                if (hitInfo)
                {
                    closestTargetInSight = target.transform;
                    minDistanceSq = distSq;
                }
            }
        }
        return closestTargetInSight;
    }

    void OnAttack()
    {
        Debug.Log("Attack!");
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
        rigidbody2d.isKinematic = true;
    }

    void onDeathAnimFinished()
    {
        Destroy(gameObject);
        if (deadPrefab)
        {
            var deadSprite = Instantiate(deadPrefab.gameObject).GetComponent<SpriteRenderer>();
            deadSprite.flipX = body.flipX;
            deadSprite.transform.position = this.transform.position;
        }
    }
}

