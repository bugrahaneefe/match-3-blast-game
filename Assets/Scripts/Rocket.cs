using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rocket : MonoBehaviour
{
    public float startSpeed = 5f;  // Initial speed
    public float maxSpeed = 25f;   // Maximum speed
    public float accelerationTime = 0.2f; // Time (in seconds) to reach max speed

    private float currentSpeed;
    public Vector2 direction;

    private void Start()
    {
        currentSpeed = startSpeed;
        StartCoroutine(AccelerateRocket()); // Smoothly increases speed
        StartCoroutine(MoveRocket());
    }

    private IEnumerator AccelerateRocket()
    {
        float elapsedTime = 0f;

        while (elapsedTime < accelerationTime)
        {
            elapsedTime += Time.deltaTime;
            currentSpeed = Mathf.Lerp(startSpeed, maxSpeed, elapsedTime / accelerationTime);
            yield return null;
        }

        currentSpeed = maxSpeed; // Ensure it reaches the final speed exactly
    }

    private IEnumerator MoveRocket()
    {
        while (true)
        {
            transform.position += (Vector3)direction * currentSpeed * Time.deltaTime;
            yield return null;
        }
    }
}
