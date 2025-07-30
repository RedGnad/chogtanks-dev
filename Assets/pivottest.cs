using UnityEngine;

public class TestPivot : MonoBehaviour
{
    [SerializeField] private Transform firePoint;

    void Update()
    {
        // Pour tester : faire tourner le canon avec les flèches Gauche/Droite
        float h = Input.GetAxis("Horizontal"); // A et D
        if (h != 0f)
        {
            float angle = h * 90f * Time.deltaTime; // rotation lente pour voir
            transform.rotation *= Quaternion.Euler(0, 0, angle);
        }

        // Afficher la position du FirePoint dans la console
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"FirePoint position (monde) = {firePoint.position}");
        }
    }
}
