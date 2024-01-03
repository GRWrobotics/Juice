using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyProjectile : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    [SerializeField] int collisionsToDestuction = 1;
    [SerializeField] string collisionTag = "Enemy";
    private int numCollisions = 0;
    // Update is called once per frame
    void Update()
    {

    }
    private void OnTriggerEnter2D(Collider2D other) {
        if(other.tag == collisionTag){
            numCollisions++;   
        }
        if(numCollisions >= collisionsToDestuction){
            Destroy(gameObject); 
        }
    }
}
