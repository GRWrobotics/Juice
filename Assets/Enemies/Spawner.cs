using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    [SerializeField] GameObject spawnObject;
    [SerializeField] float spawnDelay = 1;
    [SerializeField] float spawnNumber = 1;
    [SerializeField] bool ejectRandomly = true;
    [SerializeField] float maxEjectStrength = 1000;
    float spawnTimer = 0;
    
    // Update is called once per frame
    void Update()
    {
        if(spawnTimer > spawnDelay){
            spawnTimer = 0;
            for(int i = 0; i < spawnNumber; i++){
                GameObject spawned = Instantiate(spawnObject, gameObject.transform);
                if(ejectRandomly && spawned.GetComponent<Rigidbody2D>()){
                    Vector2 force = new Vector2(Random.Range(-maxEjectStrength, maxEjectStrength), Random.Range(-maxEjectStrength, maxEjectStrength));
                    spawned.GetComponent<Rigidbody2D>().AddForce(force);
                }    
            }
        }   
        spawnTimer += Time.deltaTime;
    }
}
