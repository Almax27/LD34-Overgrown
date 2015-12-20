using UnityEngine;
using System.Collections;

public class ThiefController : MonoBehaviour
{
    public float attackSpeed = 1;
    public int damage = 1;
    public float pauseAfterShooting = 0.2f;
    public bool canMoveAndShoot = true;
    public Transform fireRightTransform;
    public Transform fireLeftTransform;
    public SoldierBullet bulletPrefab;

    Animator animator;
    PlayerController playerController;

    float lastFireTime = 0;

    // Use this for initialization
    void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        bool tryFire = Input.GetButton("Fire1");
        bool isFiring = tryFire && playerController.grounder.isGrounded;

        if (isFiring)
        {
            lastFireTime = Time.time;
        }

        bool isPausedAfterShooting = playerController.grounder.isGrounded && Time.time - lastFireTime < pauseAfterShooting;

        playerController.canLook = !isFiring && !isPausedAfterShooting;
        playerController.canMove = canMoveAndShoot || (!isFiring && !isPausedAfterShooting);

        //update animator
        animator.SetFloat("attackSpeed", attackSpeed);
        animator.SetBool("isFiring", isFiring);
    }

    //caled buy fire animation
    void OnFire()
    {
        var bullet = Instantiate(bulletPrefab.gameObject).GetComponent<SoldierBullet>();

        if (playerController.isLookingRight)
        {
            bullet.transform.position = fireRightTransform.position;
            bullet.direction = new Vector3(1, 0, 0);
        }
        else
        {
            bullet.transform.position = fireLeftTransform.position;
            bullet.direction = new Vector3(-1, 0, 0);
        }
        bullet.damage = damage;
    }

    void OnDeath()
    {
        this.enabled = false;
    }
}

