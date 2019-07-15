using UnityEngine;

public class RocketFollower : MonoBehaviour
{
    [SerializeField]
    private Vector3 cameraOffset = new Vector3(0, .5f, 5.156f);

    [SerializeField]
    private RocketController rocket;

    [SerializeField]
    private Vector2 velocityMultiplier = new Vector2(.1f, .2f);

    [SerializeField]
    private float flightLerpRate = .7f;

    [SerializeField]
    // We use a slower lerp rate when the rocket is in collision because the velocity change is really jarring
    private float rocketCollisionLerpRate = .05f;

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
        SetCameraPosition();
    }

    private void SetCameraPosition()
    {
        var targetPosition = this.rocket.transform.position;

        targetPosition += cameraOffset;

        var velocityOffset = new Vector3(
            rocketRigidBody.velocity.x * velocityMultiplier.x, 
            rocketRigidBody.velocity.y * velocityMultiplier.y, 
            0f);

        targetPosition += velocityOffset;

        var lerp = rocket.IsInCollision ? rocketCollisionLerpRate : this.flightLerpRate;

        lerp = Mathf.Min(lerp, Mathf.Lerp(this.previousLerpRate, lerp, 0.001f));

        this.transform.position = Vector3.Lerp(this.transform.position, targetPosition, lerp);

        this.previousLerpRate = lerp;
    }
}
