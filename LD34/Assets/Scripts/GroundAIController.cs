using UnityEngine;
using System.Collections;

public class GroundAIController : MonoBehaviour 
{
    
    void Start()
    {
        
    }

    void OnDeath()
    {
        Destroy(gameObject);
    }
}

