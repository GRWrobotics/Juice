using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    
    [SerializeField] GameObject camera;
    [SerializeField] float smoothing = 1;
    [SerializeField] Transform target;
    [SerializeField] Vector3 cameraOffset = new Vector3(0,0,-10);
    // Update is called once per frame
    void Update() {
        StartCoroutine(LerpCamera());
    }
    IEnumerator LerpCamera(){
        Vector3 cameraPos = camera.transform.position;
        Vector3 targetPos = target.position;
        
        Vector3 newPos = targetPos+cameraOffset; //new Vector3(playerPos.x+cameraOffset.x, playerPos.y + cameraOffset.y, playerPos.z + cameraOffset.z);
        // camera.transform.position = newPos;
        camera.transform.position = Vector3.Lerp(cameraPos, newPos, smoothing);
        yield return null;
    }
}
