using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{   

    [SerializeField] bool jumpable = true;
    // [SerializeField] float x = 0;
    // [SerializeField] float y = 0;
    [SerializeField] float vx = 0;
    [SerializeField] float vy = 0;
    
    [SerializeField] float ax = 0.01f;
    [SerializeField] float aGrav = -0.001f;
    [SerializeField] float jumpForce = 0.1f;
    [SerializeField] float floatForce = 0.005f;
    [SerializeField] float maxSpeed = 0.1f;
    [SerializeField] float maxFallSpeed = 0.01f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    private void FixedUpdate() {
        Vector3 velocity = new Vector3(vx, vy, 0);
        transform.position += velocity;
        
        if(Input.GetKey(KeyCode.A) && vx > -maxSpeed){
            vx -= ax;
        }
        if(Input.GetKey(KeyCode.D) && vx < maxSpeed){
            vx += ax;
        }
        if(Input.GetKey(KeyCode.Space)){
            if(jumpable){
                vy += jumpForce;
                jumpable = false;
            }else if(vy>-maxFallSpeed){
                vy += aGrav/2;
            }
        }else if(vy>-maxFallSpeed){
            vy += aGrav;
        }
    }
}
