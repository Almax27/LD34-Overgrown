using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public SpriteRenderer body;
	public SpriteRenderer legs;
    public Grounder grounder;

    [Header("Platforming")]
	public float gravity = 1;
	public AnimationCurve jumpCurve = new AnimationCurve();
	public float jumpHeight = 1;
	public float groundSpeed = 0.3f;
	public float airSpeed = 0.1f;
    public bool canMove = true;
    public bool canLook = true;
    public bool canJump = true;
    public bool isMovingRight = true;
    public bool isLookingRight = true;

	Animator animator;
	new Rigidbody2D rigidbody2D;

	int jumpsRemaining = 0;
	int maxJumps = 1;
	bool hasJumped = false;
	float jumpTick = 0;

	bool hasMovedInAir = false;

	Vector2 velocity = new Vector2(0,0);

	void Start () 
	{
		animator = GetComponent<Animator> ();
		rigidbody2D = GetComponent<Rigidbody2D>();
		jumpsRemaining = maxJumps;
	}

    void Update()
    {
        var isGrounded = grounder.isGrounded && velocity.y <= 0;

        //get input
        float xInput = Input.GetAxis("Horizontal");
        bool tryJump = Input.GetButtonDown("Jump");

        //apply gravity
        if (isGrounded)
        {
            velocity.y = 0;
        }
        velocity.y -= gravity * Time.smoothDeltaTime;

        //handle jumps
        if (isGrounded)
        {
            jumpsRemaining = maxJumps;
            hasJumped = false;
        }
        if (canJump && tryJump && jumpsRemaining > 0)
        {
            jumpTick = 0;
            hasJumped = true;
            jumpsRemaining--;
        }
        if (hasJumped)
        {
            float lastJumpTick = jumpTick;
            jumpTick += Time.smoothDeltaTime;
            float deltaY = (jumpCurve.Evaluate(jumpTick) - jumpCurve.Evaluate(lastJumpTick)) * jumpHeight;
            if(deltaY != 0)
            {
                velocity.y = deltaY / Time.smoothDeltaTime;
            }
        }

        //handle movement
        if (isGrounded)
        {
            hasMovedInAir = false;
        }
        if (canMove)
        {
            float desiredSpeed = groundSpeed;
            //use air speed when changing direction in the air
            if (!isGrounded && (hasMovedInAir || Mathf.Sign(velocity.x) != Mathf.Sign(xInput)))
            {
                desiredSpeed = airSpeed;
                hasMovedInAir = true;
            }
            velocity.x = xInput * desiredSpeed;
        }
        else
        {
            xInput = 0;
            velocity.x = 0;
        }

        //update facing
        if (xInput != 0)
        {
            isMovingRight = xInput > 0;
            if (canLook)
            {
                isLookingRight = isMovingRight;
            }
            body.flipX = !isLookingRight;
            legs.flipX = !isMovingRight;
        }

        //handle direction facing
        if (xInput != 0)
        {
            bool isMovingRight = xInput > 0;
            if (canLook)
            {
                isLookingRight = isMovingRight;
            }
        }

        //update animator
        animator.SetFloat("moveSpeed", xInput);
        animator.SetBool("isRunning", xInput != 0);
        animator.SetBool("isGrounded", isGrounded);

        //move rigidbody
        Vector3 newPosition = transform.position + new Vector3(velocity.x, velocity.y, 0) * Time.smoothDeltaTime;
        rigidbody2D.MovePosition(newPosition);
    }
}
