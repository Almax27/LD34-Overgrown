using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class SoldierBullet : MonoBehaviour
{
    public int damage = 1;
    public Vector2 direction = new Vector2(0, 0);
    public float speed = 5;
    public float maxDistance = 20;

    public LayerMask collisionMask;

    public GameObject hitEffectPrefab;

    new Rigidbody2D rigidbody2D = null;
    SpriteRenderer sprite = null;

    Vector3 lastRayOrigin = new Vector3();
    float distanceTraveled = 0;

    // Use this for initialization
    void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        lastRayOrigin = transform.position;
        distanceTraveled = 0;
    }

    void FixedUpdate()
    {
        var hitInfo = Physics2D.Raycast(transform.position, direction.normalized, speed * Time.fixedDeltaTime, collisionMask);
        if (hitInfo)
        {
            if (hitInfo.rigidbody)
            {
                hitInfo.rigidbody.SendMessage("OnDamage", damage);
            }
            if (hitEffectPrefab)
            {
                var hitGobj = Instantiate(hitEffectPrefab);
                hitGobj.transform.position = hitInfo.point;
                hitGobj.transform.localScale = new Vector3(direction.x < 0 ? -1 : 1, 1, 1);
            }
            Destroy(gameObject);
        }
        lastRayOrigin = transform.position;

        Vector2 pos2D = transform.position;
        var deltaPos = direction.normalized * speed * Time.fixedDeltaTime;
        rigidbody2D.MovePosition(pos2D + deltaPos);
        if (sprite)
        {
            sprite.flipX = direction.x < 0;
        }

        distanceTraveled += deltaPos.magnitude;
        if (distanceTraveled > maxDistance)
        {
            Destroy(gameObject);
        }
    }
}

