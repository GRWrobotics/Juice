    using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    public IEnumerator Shake(float duration, float magnitude){
        Vector3 originalPos = transform.localPosition;
        float shakeTimer = 0;
        
        while(shakeTimer < duration){
            float x = transform.localPosition.x + Random.Range(-1f, 1f) * magnitude;
            float y = transform.localPosition.y + Random.Range(-1f, 1f) * magnitude;
            
            transform.localPosition = new Vector3(x, y, originalPos.z); 
            
            shakeTimer+=Time.deltaTime;
            
            yield return null;
        }
        transform.localPosition = originalPos;   
    }
}
