using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class SpiralCoin : MonoBehaviour
{
    [Header("Spiral Settings")]
    public float duration = 0.75f; // How long the orbit lasts
    public float spinSpeed = 15f;  // How fast it orbits around the player
    public float initialRadius = 2f; // Starting distance from player
    public Vector3 targetOffset = new Vector3(0, 1f, 0); // Aim for the player's chest, not their feet

    [Header("Events (Hook up MM Feel here!)")]
    public UnityEvent OnPickupStart;
    public UnityEvent OnPickupComplete;

    private bool isCollected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            isCollected = true;
            // Disable collider so it doesn't trigger twice
            GetComponent<Collider>().enabled = false; 
            
            StartCoroutine(OrbitAndAbsorb(other.transform));
        }
    }

    private IEnumerator OrbitAndAbsorb(Transform playerTransform)
    {
        // 1. Fire the initial juice (sounds, particles)
        OnPickupStart.Invoke();

        float timeElapsed = 0f;
        float currentAngle = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float percent = timeElapsed / duration; // Goes from 0 to 1

            // Shrink the radius as time goes on so it gets sucked into the player
            float currentRadius = Mathf.Lerp(initialRadius, 0f, percent);
            
            // Increase the angle to make it spin
            currentAngle += spinSpeed * Time.deltaTime;

            // Calculate the new position around the player using trigonometry
            float xOffset = Mathf.Cos(currentAngle) * currentRadius;
            float zOffset = Mathf.Sin(currentAngle) * currentRadius;

            // Apply the position, keeping it relative to the moving player
            Vector3 orbitPosition = playerTransform.position + targetOffset;
            orbitPosition += new Vector3(xOffset, 0f, zOffset);

            transform.position = orbitPosition;

            // Optional: Shrink the coin as it gets closer
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, percent);

            yield return null;
        }

        // 2. Fire the final juice (pop sound, score text)
        OnPickupComplete.Invoke();

        // 3. Destroy or return to object pool
        Destroy(gameObject);
    }
}