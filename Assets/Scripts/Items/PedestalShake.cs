using UnityEngine;

public class PedestalShake : MonoBehaviour
{
    [SerializeField] private float returnStrength = 25f;
    [SerializeField] private float damping = 5f; // how much resistance to movement the pedestal has
    [SerializeField] private float maxDistance = .5f;

    private Rigidbody2D rb;
    private Vector2 startingPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startingPosition = rb.position;
    }

    private void FixedUpdate()
    {
        Vector2 offset = startingPosition - rb.position; // where the pedestal is vs where it should be
        Vector2 returnForce = offset * returnStrength; // how much force to apply to return it to its starting position

        returnForce -= rb.linearVelocity * damping; // apply damping to the force based on the current velocity of the pedestal (resistance to movement)
        rb.AddForce(returnForce, ForceMode2D.Force); // actually move the pedestal to its starting position

        if (offset.magnitude > maxDistance)
        {
            rb.position = startingPosition + (-offset.normalized * maxDistance); // stop pedestal from moving too far away from its starting position
        }
    }
}