using UnityEngine;
using System.Collections;

public class Grounder : MonoBehaviour {

    public bool isGrounded = false;
    public LayerMask groundMask = new LayerMask();

    public Vector2 size = Vector2.one;

    void FixedUpdate()
    {
        Vector2 pos2D = new Vector2(transform.position.x, transform.position.y);
        var ground = Physics2D.OverlapArea(pos2D - (size*0.5f), pos2D + (size*0.5f), groundMask);
        isGrounded = ground != null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(this.transform.position, new Vector3(size.x, size.y, 0));
    }
}
