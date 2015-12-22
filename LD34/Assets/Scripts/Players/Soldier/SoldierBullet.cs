using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class SoldierBullet : MonoBehaviour
{
    public GameObject owner = null;
    public int damage = 1;
    public Vector2 direction = new Vector2(0, 0);
    public float speed = 5;
    public float maxDistance = 20;
    public int maxPenetrations = 0;

    public LayerMask collisionMask = new LayerMask();
    public LayerMask damageableMask = new LayerMask();

    public GameObject hitEffectPrefab;
    public AudioSource spawnSoundPrefab;
    public AudioSource destroySoundPrefab;

    new Rigidbody2D rigidbody2D = null;
    SpriteRenderer sprite = null;

    Vector3 lastRayOrigin = new Vector3();
    float distanceTraveled = 0;

    List<Collider2D> collidersPenetrated = new List<Collider2D>();
    int penetrationsOccured = 0;

    // Use this for initialization
    void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        lastRayOrigin = transform.position;
        distanceTraveled = 0;
        SpawnSound(spawnSoundPrefab);
    }

    void FixedUpdate()
    {
        var hitInfo = Physics2D.Raycast(transform.position, direction.normalized, speed * Time.fixedDeltaTime, collisionMask);
        if (hitInfo)
        {
            if ((damageableMask.value & 1 << hitInfo.collider.gameObject.layer) != 0)
            {
                if (!collidersPenetrated.Contains(hitInfo.collider))
                {
                    collidersPenetrated.Add(hitInfo.collider);
                    hitInfo.collider.SendMessageUpwards("OnDamage", new Damage(damage, owner), SendMessageOptions.DontRequireReceiver);
                }


                penetrationsOccured++;

                if (penetrationsOccured > maxPenetrations)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                SpawnSound(destroySoundPrefab);
                Destroy(gameObject);
            }

            if (hitEffectPrefab)
            {
                var hitGobj = Instantiate(hitEffectPrefab);
                hitGobj.transform.position = hitInfo.point;
                hitGobj.transform.localScale = new Vector3(direction.x < 0 ? -1 : 1, 1, 1);
            }
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

    void SpawnSound(AudioSource prefab)
    {
        if (prefab)
        {
            GameObject gobj = Instantiate(prefab.gameObject);
            gobj.transform.position = transform.position;
        }
    }
}

