using UnityEngine;
using System.Collections;

public class FlyingEnemy : MonoBehaviour {

    public float speed = 5.0f;
    public float turnSmoothing = 1.0f;
    public float maxTurnSpeed = 1.0f;
    public int damage = 1;
    public float attackRate = 1.0f;
    public float attackRange = 1.0f;

    float attackTick = 0;
    Health target;

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
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            target = player ? player.GetComponent<Health>() : null;
        }
        if (target && target.currentHealth > 0)
        {
            var targetDirection = target.transform.position - this.transform.position;

            //try attack
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

            //offset so we line up in y before we reach the player
            if (targetDirection.sqrMagnitude > 10 * 10)
            {
                float offset = -Mathf.Sign(targetDirection.x) * 10;
                targetDirection = (target.transform.position + new Vector3(offset,0,0)) - this.transform.position;
            }

            //update movement
            var targetAngle = Vector2.Angle(Vector2.left, targetDirection) * (targetDirection.y > 0 ? -1 : 1);
            angle = Mathf.SmoothDampAngle(angle, targetAngle, ref angleVel, turnSmoothing, maxTurnSpeed);
        }

        var direction = Quaternion.Euler(new Vector3(0, 0, angle)) * Vector3.left;
        transform.position += direction * speed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, Vector2.Angle(Vector2.left, direction) * (direction.y > 0 ? -1 : 1)));
	}

    void OnDeath()
    {
        Destroy(gameObject);
    }
}
