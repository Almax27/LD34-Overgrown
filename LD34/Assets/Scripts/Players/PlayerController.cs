using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public SpriteRenderer body;
	public SpriteRenderer legs;
    public Collider2D groundedCollider;

    [Header("Platforming")]
	public float gravity = 1;
	public AnimationCurve jumpCurve = new AnimationCurve();
	public float jumpHeight = 1;
	public LayerMask isGroundedMask = new LayerMask();
	public bool isGrounded = true;
    public bool isTouchingGround = true; //may flicker
	public float groundSpeed = 0.3f;
	public float airSpeed = 0.1f;
    public bool canMove = true;
    public bool canLook = true;
    public bool canJump = true;
    public bool isMovingRight = true;
    public bool isLookingRight = true;

	Animator animator;
	Rigidbody2D ridgidBody2D;

	int jumpsRemaining = 0;
	int maxJumps = 1;
	bool hasJumped = false;
	float jumpTick = 0;

    float groundedTick = 0;
    bool pendingGroundedState = false;

	bool hasMovedInAir = false;

	Vector2 velocity = new Vector2(0,0);

	// Use this for initialization
	void Start () 
	{
		animator = GetComponent<Animator> ();
		ridgidBody2D = GetComponent<Rigidbody2D>();
		jumpsRemaining = maxJumps;
	}
	
	// Update is called once per frame
	void Update () 
	{
		bool groundedThisFrame = calculateIsGrounded();
        bool becameGroundedThisFrame = !isGrounded && groundedThisFrame;
        isGrounded = groundedThisFrame;

		//get input
		float xInput = Input.GetAxis("Horizontal");
		bool tryJump = Input.GetButtonDown("Jump");

        if (!isTouchingGround)
		{
            velocity.y -= gravity * Time.smoothDeltaTime;
		}
		else
		{
			velocity.y = 0;
		}

		//handle jumps
		if (becameGroundedThisFrame)
		{
			jumpsRemaining = maxJumps;
			hasJumped = false;
			velocity.y = 0;
			hasMovedInAir = false;
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
			jumpTick += Time.deltaTime;
			float deltaY = (jumpCurve.Evaluate(jumpTick) - jumpCurve.Evaluate(lastJumpTick)) * jumpHeight;
			if(deltaY != 0)
			{
				velocity.y = deltaY / Time.deltaTime;
			}
		}

		//handle movement
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
            bool facingRight = xInput > 0;
            body.flipX = canLook && !facingRight;
            legs.flipX = !facingRight;
        }

		//move rigidbody
        Vector3 newPosition = transform.position + new Vector3(velocity.x, velocity.y, 0) * Time.smoothDeltaTime;
		ridgidBody2D.MovePosition(newPosition);

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
	}

	bool calculateIsGrounded()
	{
		var collider = GetComponent<Collider2D>();
        isTouchingGround = groundedCollider.IsTouchingLayers(isGroundedMask);

        //if we're waiting on verification then states will be the same
        if (pendingGroundedState == isTouchingGround)
        {
            if (groundedTick > 0.05f)
            {
                return isTouchingGround;
            }
            groundedTick += Time.deltaTime;
        }
        //if states are different then restart verification
        else
        {
            groundedTick = 0;
            pendingGroundedState = isTouchingGround;
        }

        //return what we already know
        return isGrounded;
	}
}
