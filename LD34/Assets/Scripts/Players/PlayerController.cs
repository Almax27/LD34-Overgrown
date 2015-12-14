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

    bool tryJump = true;
    float xInputRaw = 0;
    float xInput = 0;
    float xInputVel = 0;
    Vector2 velocity = Vector2.zero;

	void Start () 
	{
		animator = GetComponent<Animator> ();
		rigidbody2D = GetComponent<Rigidbody2D>();
		jumpsRemaining = maxJumps;
	}

    void Update()
    {
        //get input
        xInputRaw = Input.GetAxisRaw("Horizontal");
        tryJump = tryJump || Input.GetButtonDown("Jump");
    }

    void FixedUpdate()
    {
        var isGrounded = grounder.isGrounded && velocity.y <= 0;

        //apply gravity
        if (isGrounded)
        {
            velocity.y = 0;
        }
        velocity.y -= gravity * Time.fixedDeltaTime;

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
            jumpTick += Time.fixedDeltaTime;
            float deltaY = (jumpCurve.Evaluate(jumpTick) - jumpCurve.Evaluate(lastJumpTick)) * jumpHeight;
            if(deltaY != 0)
            {
                velocity.y = deltaY / Time.fixedDeltaTime;
            }
        }
        tryJump = false;

        //handle movement
        if (isGrounded)
        {
            hasMovedInAir = false;
        }
        if (canMove)
        {
            xInput = Mathf.SmoothDamp(xInput, xInputRaw, ref xInputVel, 0.1f);
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
        animator.SetFloat("moveSpeed", Mathf.Abs(xInput));
        animator.SetBool("isRunning", canMove && xInputRaw != 0);
        animator.SetBool("isGrounded", isGrounded);

        //move rigidbody
        Vector3 newPosition = transform.position + new Vector3(velocity.x, velocity.y, 0) * Time.fixedDeltaTime;
        rigidbody2D.MovePosition(newPosition);
    }
}
