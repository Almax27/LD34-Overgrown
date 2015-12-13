using UnityEngine;
using System.Collections;

public class FollowCamera : MonoBehaviour {

    public Transform target = null;
    public float lookDamp = 0.3f;
    public float followDamp = 0.5f;
    public Vector3 targetOffset = new Vector3(0,0,0);
    public Vector3 offset = new Vector3(0,2,-10);

    Vector3 followVelocity = Vector3.zero;
    Vector3 desiredPosition = Vector3.zero;

    Vector3 lookAtPosition = Vector3.zero;
    Vector3 lookAtPositionVelocity = Vector3.zero;

	// Use this for initialization
	void Start () 
    {
        if (target)
        {
            desiredPosition = target.position + targetOffset;
        }
	}
	
	// Update is called once per frame
	void LateUpdate () 
    {
        if (target != null)
        {
            desiredPosition = target.position + targetOffset;
            if (followDamp > 0)
            {
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition + offset, ref followVelocity, followDamp, float.MaxValue, Time.smoothDeltaTime);
            }
            else if (followDamp == 0)
            {
                transform.position = desiredPosition + offset;
            }
            if (lookDamp > 0)
            {
                if (lookAtPosition == Vector3.zero)
                {
                    lookAtPosition = desiredPosition;
                }
                else
                {
                    lookAtPosition = Vector3.SmoothDamp(lookAtPosition, desiredPosition, ref lookAtPositionVelocity, lookDamp, float.MaxValue, Time.smoothDeltaTime);
                }
                transform.LookAt(lookAtPosition);
            }
            else if (lookDamp == 0)
            {
                transform.LookAt(lookAtPosition);
            }
        }
	}
}