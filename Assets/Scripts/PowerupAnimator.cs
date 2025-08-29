using UnityEngine;

public class PowerupAnimator : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 90f; 
    
    [Header("Mouvement de vague")]
    [SerializeField] private float waveAmplitude = 0.5f; 
    [SerializeField] private float waveSpeed = 2f; 
    
    private Vector3 startPosition;
    private float timeOffset;
    
    void Start()
    {
        startPosition = transform.position;
        
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }
    
    void Update()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        
        float waveOffset = Mathf.Sin(Time.time * waveSpeed + timeOffset) * waveAmplitude;
        transform.position = startPosition + new Vector3(0f, waveOffset, 0f);
    }
}
