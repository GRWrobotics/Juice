using System.Collections;
using System.Collections.Generic;
using System.IO;
// using System.Numerics;
using Unity.Mathematics;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    [SerializeField] SpriteRenderer explosionSprite;
    public IEnumerator Explode(Vector3 pos, float explosionTime){
        Instantiate(gameObject, pos, Quaternion.identity);
        explosionSprite.color = Color.black;
        yield return new WaitForSeconds(explosionTime/2);
        explosionSprite.color = Color.white;
        yield return new WaitForSeconds(explosionTime/2);
        DestroyImmediate(gameObject);
    }
}
