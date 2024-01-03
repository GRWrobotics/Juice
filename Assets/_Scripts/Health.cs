using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Health : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    [SerializeField] float health = 15;
    [SerializeField] ParticleSystem deathParticles;
    [SerializeField] private float impactTime = 0.1f;
    [SerializeField] private Freeze freeze;
    public void dealDamage(float damage){
        health -= damage;
        // StartCoroutine(freeze.FreezeFrame(impactTime));
        checkDeath();
    }
    [SerializeField] Explosion explosion;
    void checkDeath(){
        Vector3 explosionPos = transform.position;
        if(health <= 0){
            if(gameObject.CompareTag("Player")){
                if(deathParticles){
                    deathParticles.Play();
                    gameObject.GetComponent<SpriteRenderer>().enabled = false;
                }
                StartCoroutine(DelayReloadScene(deathParticles.duration));
            }else{
                StartCoroutine(explosion.Explode(explosionPos, 0.5f));
                if(deathParticles){
                    deathParticles.Play();
                    gameObject.GetComponent<SpriteRenderer>().enabled = false;
                    gameObject.GetComponent<Collider2D>().enabled = false;
                    Destroy(gameObject, deathParticles.duration);
                }else{
                    Destroy(gameObject);
                }
            }
            
        }
    }
    IEnumerator DelayReloadScene(float delay){
        yield return new WaitForSeconds(delay);
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}
