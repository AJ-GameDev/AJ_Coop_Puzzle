using UnityEngine;

public class PlatformOscillator : MonoBehaviour
{
    public GameObject player;

    [SerializeField] private float period = 2f;
    [SerializeField] private Vector3 movementVector;
    private float MovementFactor;
    private Vector3 startingPosition;

    private void Start()
    {
        startingPosition = transform.position;
    }


    private void Update()
    {
        if (period <= Mathf.Epsilon) return; // To avoid number 0 or close to 0. Epsilon is a tiny number

        var cycles = Time.time / period; // Continuous rolling over time 

        const float tau = Mathf.PI * 2; // Constant value of 6.28
        var rawSineWave = Mathf.Sin(cycles * tau); // Values from -1 to 1

        MovementFactor = (rawSineWave + 1f) / 2f; // Recalculated values from  0 to 1

        var offsetPosition = movementVector * MovementFactor;
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