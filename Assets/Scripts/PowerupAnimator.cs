using UnityEngine;

/// <summary>
/// Animation simple pour les power-ups : rotation et mouvement de vague vertical
/// </summary>
public class PowerupAnimator : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 90f; // degrés par seconde
    
    [Header("Mouvement de vague")]
    [SerializeField] private float waveAmplitude = 0.5f; // hauteur du mouvement
    [SerializeField] private float waveSpeed = 2f; // vitesse de la vague
    
    private Vector3 startPosition;
    private float timeOffset;
    
    void Start()
    {
        // Sauvegarder la position initiale
        startPosition = transform.position;
        
        // Offset aléatoire pour éviter que tous les power-ups bougent en sync
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }
    
    void Update()
    {
        // Rotation continue sur l'axe Z
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        
        // Mouvement de vague vertical (haut/bas)
        float waveOffset = Mathf.Sin(Time.time * waveSpeed + timeOffset) * waveAmplitude;
        transform.position = startPosition + new Vector3(0f, waveOffset, 0f);
    }
}
