using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageOnCollide : MonoBehaviour
{
    
    [SerializeField] float damage = 10;
    [SerializeField] string collisionTag = "Enemy";
    private void OnCollisionEnter2D(Collision2D other) {
        if( other.gameObject.CompareTag(collisionTag)){
            if(other.gameObject.GetComponent<Health>()){
                Health enemyHealth = other.gameObject.GetComponent<Health>();
                enemyHealth.dealDamage(damage);
            }else{
                Debug.Log("No Health Script Detected");
            }
        }
    }
}
