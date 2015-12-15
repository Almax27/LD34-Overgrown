using UnityEngine;
using System.Collections;

public class FlyingEnemy : MonoBehaviour {

    public float speed = 5.0f;
    public float turnSmoothing = 1.0f;
    public float maxTurnSpeed = 1.0f;
    public int damage = 1;
    public float attackRate = 1.0f;
    public float attackRange = 1.0f;

    float targetingTick = 0;
    float attackTick = 0;
    Transform target;

    float angle = 0;
    float angleVel = 0;

    Animator animator;

	// Use this for initialization
	void Start () {
        animator = GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () 
    {
        targetingTick += Time.deltaTime;
        if (targetingTick > 1.0f)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            target = player ? player.transform : null;
            targetingTick = 0;
        }

        if (target)
        {
            var targetDirection = target.position - this.transform.position;

            var targetAngle = Vector2.Angle(Vector2.left, targetDirection) * (targetDirection.y > 0 ? -1 : 1);
            angle = Mathf.SmoothDampAngle(angle, targetAngle, ref angleVel, turnSmoothing, maxTurnSpeed);

            var direction = Quaternion.Euler(new Vector3(0, 0, angle)) * Vector3.left;

            transform.position += direction * speed * Time.deltaTime;

            attackTick += Time.deltaTime;

            if (targetDirection.sqrMagnitude < attackRange * attackRange)
            {
                if (attackTick > attackRate)
                {
                    target.SendMessage("OnDamage", new Damage(damage, gameObject));
                    animator.SetTrigger("onAttack");
                    attackTick = 0;
                }
            }

            transform.rotation = Quaternion.Euler(new Vector3(0, 0, Vector2.Angle(Vector2.left, direction) * (direction.y > 0 ? -1 : 1)));
        }
	}
}
