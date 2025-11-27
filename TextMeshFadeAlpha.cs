using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class TextMeshFadeAlpha : MonoBehaviour
{
    public TextMeshPro textMesh;
    public float delay = 0;
    public float duration = 1;
    float perSecond;
    float startTime;

    void Start()
    {
        
        perSecond = textMesh.color.a / duration;

        
        startTime = Time.time + delay;
    }

    void Update()
    {
        if (Time.time >= startTime)
        {
            
            Color color = textMesh.color;
            color.a -= perSecond * Time.deltaTime;
            textMesh.color = color;
        }
    }
}
