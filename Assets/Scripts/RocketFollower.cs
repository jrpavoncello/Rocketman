using UnityEngine;

public class RocketFollower : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Offset that will applied to the position of the camera in addition to any velocity changed.")]
    private Vector3 cameraOffset = new Vector3(0, .5f, 5.156f);

    [SerializeField]
    private RocketController rocket;

    [SerializeField]
    [Tooltip("Multiplied to the rocket velocity in the (X, Y) direction before adding it to the offset of the camera. " +
        "The higher the multiplier, the further ahead the camera will lead ahead of the rocket.")]
    private Vector2 velocityMultiplier = new Vector2(.1f, .2f);

    [SerializeField]
    [Tooltip("Lerp rate that will be used to smoothly move the camera as the rocket is flying (not in collision).")]
    private float flightLerpRate = .7f;

    [SerializeField]
    [Tooltip("Lerp rate that will be used to smoothly move the camera back to the rocket when it collides. " +
        "This is important so that the camera doesn't hard whip back to the rocket when collision occurs.")]
    private float collisionLerpRate = .3f;

    [SerializeField]
    [Tooltip("Lerp rate that will be used to smoothly follow the rocket as it's being exploded. Should be really low.")]
    private float explosionLerpRate = 0.03f;

    private Rigidbody rocketRigidBody;
    private float previousLerpRate = 0f;

    // Start is called before the first frame update
    void Start()
    {
        this.previousLerpRate = flightLerpRate;

        rocketRigidBody = rocket.GetComponent<Rigidbody>();

        // Set the camera directly at the start
        SetCameraPosition();
    }

    void FixedUpdate()
    {
        // We do this in FixedUpdate because the camera uses RigidBody collisions to prevent it 
        // from going through the ground. It will causes clipping and jittering otherwise.
        SetCameraPosition();
    }

    private void SetCameraPosition()
    {
        Vector3 targetPosition = this.rocket.transform.position + cameraOffset;

        float lerp = rocket.IsInCollision ? collisionLerpRate : this.flightLerpRate;

        if (rocket.State == RocketState.Dead)
        {
            // Stop moving the camera and move it away on death
            targetPosition += cameraOffset.Multiply(new Vector3(0, 0, .5f));

            lerp = this.explosionLerpRate;
        }
        else
        {

            var velocityOffset = rocketRigidBody.velocity.Multiply(new Vector3(velocityMultiplier.x, velocityMultiplier.y, 0f));

            targetPosition += velocityOffset;
        }

        // Use either the new lower Lerp rate, or lerp between the old lower lerp rate and new higher lerp rate
        // This is so collisions don't jerk the camera so hard.
        lerp = Mathf.Min(lerp, Mathf.Lerp(this.previousLerpRate, lerp, 0.001f));

        this.transform.position = Vector3.Lerp(this.transform.position, targetPosition, lerp);

        this.previousLerpRate = lerp;
    }
}
