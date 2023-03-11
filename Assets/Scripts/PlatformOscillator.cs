using UnityEngine;

public class PlatformOscillator : MonoBehaviour
{
    public GameObject player;

    [SerializeField] float period = 2f;
    [SerializeField] Vector3 movementVector;
    float MovementFactor;
    Vector3 startingPosition;

    private void Start()
    {
        startingPosition = transform.position;
    }


    private void Update()
    {
        if (period <= Mathf.Epsilon)
        {
            return;
        } // To avoid number 0 or close to 0. Epsilon is a tiny number

        float cycles = Time.time / period; // Continuous rolling over time 

        const float tau = Mathf.PI * 2; // Constant value of 6.28
        float rawSineWave = Mathf.Sin(cycles * tau); // Values from -1 to 1

        MovementFactor = (rawSineWave + 1f) / 2f; // Recalculated values from  0 to 1

        Vector3 offsetPosition = movementVector * MovementFactor;
        transform.position = startingPosition + offsetPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        player.transform.parent = transform;
    }

    private void OnTriggerExit(Collider other)
    {
        player.transform.parent = null;
    }
}