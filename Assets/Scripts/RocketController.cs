using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RocketController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Will be multiplied to the value from the vertical input axes to apply a lift to the rocket in the direction it's facing.")]
    private float liftMultiplier = 3400f;

    [SerializeField]
    [Tooltip("Will be multiplied to the value from the horizontal input axes to apply a rotation to the rocket.")]
    private float rotationMultiplier = 75f;

    [SerializeField]
    [Tooltip("Acceleration in m/s^2 that will be used in a gravity calculation to apply a gravity force to the rocket.")]
    private float gravityAcceleration = -150f;

    [SerializeField]
    [Tooltip("Lerp rate in milliseconds that the volume of the thrust will be faded in and out (higher rate, faster fade).")]
    [Range(0f, 1f)]
    private float thrustVolumeLerp = .03f;

    [SerializeField]
    [Tooltip("Audio played when the player is applying thrust. This will be copied into an array of AudioSource " +
        "copies to gracefully handle fading in/out.")]
    private AudioSource thrustAudioSource;

    [SerializeField]
    [Tooltip("Audio played when the player collides with an obstacle or lands inappropriately on the landing pad.")]
    private AudioSource deathAudioSource;

    [SerializeField]
    [Tooltip("Audio played when the player begins a landing on the landing pad.")]
    private AudioSource finishAudioSource;

    [SerializeField]
    [Tooltip("Particles emitted when the player is applying thrust.")]
    private ParticleSystem thrustParticles;

    [SerializeField]
    [Tooltip("Particles emitted when the player collides with an obstacle or lands inappropriately on the landing pad.")]
    private ParticleSystem deathParticles;

    [SerializeField]
    [Tooltip("Particles emitted when the player begins a landing on the landing pad.")]
    private ParticleSystem finishParticles;

    [SerializeField]
    [Tooltip("Responsible for fading in/out and changing levels.")]
    private LevelChanger levelChanger;

    [SerializeField]
    [Tooltip("Delay in seconds before the landing is considered successful and the camera will begin to fade to black. " +
        "If the rocket falls over or touches an obstacle before this, it will be considered a death.")]
    [Range(0, 30)]
    private int finishDelay = 2;

    [SerializeField]
    [Tooltip("Delay in seconds before the camera will begin to fade to black.")]
    [Range(0, 30)]
    private int deathDelay = 2;
    
    [SerializeField]
    [Tooltip("The force of the explosion that will be applied where the rocket collided with an obstacle. " +
        "This will be multiplied by the magnitude of the rocket's velocity (faster moving, larger explosion).")]
    private float deathExplosionBaseForce = 3000f;

    [SerializeField]
    [Tooltip("Will be multiplied to the force added from velocity for the death explosion.")]
    private float explosionVelocityMultiplier = .5f;

    [SerializeField]
    [Tooltip("When the death explosion velocity is factored in, it will be maxed with this value so that slow brushes with " +
        "walls still cause a bit of a boom.")]
    private float minExplosionForceMultiplier = 0.5f;

    public RocketState State { get; private set; } = RocketState.Alive;

    private RigidbodyConstraints startingConstraints;

    public bool IsInCollision => numCollisions > 0;

    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private Rigidbody rigidBody;
    private AudioSource[] supportSources = new AudioSource[10];
    private int supportIndex = 0;
    private float rocketVolume;
    private int numCollisions = 0;

    // Start is called before the first frame update
    void Start()
    {
        this.rigidBody = GetComponent<Rigidbody>();
        this.startingPosition = transform.position;
        this.startingRotation = transform.rotation;
        this.rocketVolume = this.thrustAudioSource.volume;
        this.startingConstraints = this.rigidBody.constraints;

        InitializeAudioSources();
    }

    private void InitializeAudioSources()
    {
        for (int i = 0; i < this.supportSources.Length; i++)
        {
            var supportSource = Instantiate(this.thrustAudioSource.gameObject, this.thrustAudioSource.gameObject.transform);

            supportSources[i] = supportSource.GetComponent<AudioSource>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        ApplyGravity();

        if (this.State != RocketState.Alive)
        {
            return;
        }

        var verticalInput = Input.GetAxis("Vertical");
        var horizontalInput = Input.GetAxis("Horizontal");

        if (verticalInput > 0f)
        {
            HandleRocketThrust(verticalInput);

            HandleThrustAudio(true);

            this.thrustParticles.Play();
        }
        else
        {
            HandleThrustAudio(false);

            this.thrustParticles.Stop();
        }

        // Allow rocket rotation to happen
        HandleRocketRotation(horizontalInput);
    }

    private void ApplyGravity()
    {
        rigidBody.AddForce(Vector3.up * rigidBody.mass * gravityAcceleration * Time.deltaTime);
    }

    private void HandleRocketThrust(float verticalInput)
    {
        rigidBody.AddRelativeForce(new Vector3(0, liftMultiplier * verticalInput * Time.deltaTime, 0));
    }

    private void HandleRocketRotation(float horizontalInput)
    {
        if (horizontalInput != 0f)
        {
            rigidBody.freezeRotation = true;

            this.transform.Rotate(new Vector3(0, 0, rotationMultiplier * horizontalInput * Time.deltaTime));

            rigidBody.freezeRotation = false;
        }
    }

    private void HandleThrustAudio(bool isThrusting)
    {
        var currentVolumeSource = this.supportSources[this.supportIndex];

        if (isThrusting)
        {
            if (!currentVolumeSource.isPlaying)
            {
                StartCoroutine(FadeVolume(currentVolumeSource, this.rocketVolume, false));
            }
        }
        else
        {
            if (currentVolumeSource.isPlaying)
            {
                this.supportIndex = (this.supportIndex + 1) % this.supportSources.Length;
                StartCoroutine(FadeVolume(currentVolumeSource, 0f, true));
            }
        }
    }

    private void ResetRocketToStart()
    {
        transform.position = startingPosition;
        transform.rotation = startingRotation;

        // Stop the rigid from moving or rotating once we reset the position to the launch pad
        rigidBody.velocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;

        // The rocket crashes, we remove constraints to allow it to fly off in other directions
        // so reset them to the original constraints.
        rigidBody.constraints = this.startingConstraints;
    }

    private void OnCollisionEnter(Collision collision)
    {
        switch(collision.gameObject.tag)
        {
            case ProjectTags.FRIENDLY:
                // Do nothing :)
                break;

            case ProjectTags.FINISH:

                if(IsBadLanding(collision))
                {
                    KillTheRocket();
                }
                else
                {
                    HandleLevelComplete();
                }

                break;

            default:
                KillTheRocket(collision.GetContact(0).point);
                break;
        }

        numCollisions++;
    }

    private bool IsBadLanding(Collision collision)
    {
        foreach (var contact in collision.contacts)
        {
            if (contact.thisCollider.tag != ProjectTags.LANDINGGEAR)
            {
                return true;
            }
        }

        return false;
    }

    private void HandleLevelComplete()
    {
        if (this.State == RocketState.Alive)
        {
            this.State = RocketState.WaitingToFinish;

            StopThrustSounds();

            this.finishAudioSource.Play();

            this.finishParticles.Play();

            Invoke(nameof(CheckRocketPostDelay), this.finishDelay);
        }
    }

    private void CheckRocketPostDelay()
    {
        if (this.State == RocketState.WaitingToFinish)
        {
            // In case the rocket hits the finish after dying
            levelChanger.BeginNextLevel();
        }
    }

    private void KillTheRocket()
    {
        KillTheRocket(Vector3.negativeInfinity);
    }

    private void KillTheRocket(Vector3 explosionPosition)
    {
        if (this.State == RocketState.Dead)
        {
            // Already dead, waiting for the animation to complete
            return;
        }

        this.State = RocketState.Dead;

        rigidBody.constraints = RigidbodyConstraints.None;

        if(!explosionPosition.IsInfinityOrNaN())
        {
            // Protect against really low rocket velocity when collision occurs
            var velocityMultiplier = Mathf.Max(rigidBody.velocity.magnitude, minExplosionForceMultiplier);
            var explosionForce = this.deathExplosionBaseForce + (velocityMultiplier * explosionVelocityMultiplier);

            rigidBody.AddExplosionForce(explosionForce, explosionPosition, 0f, 0f, ForceMode.Force);
        }

        StopThrustSounds();

        this.deathAudioSource.Play();
        this.deathParticles.Play();
        // Gives a cool smoking effect
        this.thrustParticles.Play();

        Invoke(nameof(ReloadLevelPostDelay), this.deathDelay);
    }

    private void ReloadLevelPostDelay()
    {
        levelChanger.ReloadLevel(() =>
        {
            ResetRocketToStart();
        });
    }

    private void StopThrustSounds()
    {
        for (int i = 0; i < this.supportSources.Length; i++)
        {
            var currentSource = this.supportSources[i];

            if (currentSource.isPlaying)
            {
                StartCoroutine(FadeVolume(currentSource, 0f, true));
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        numCollisions--;
    }

    IEnumerator FadeVolume(AudioSource source, float targetVol, bool stopOnFinish)
    {
        if(!stopOnFinish)
        {
            source.Play();
        }

        float t = 0;
        float startVol = source.volume;

        while(t < 1f &&
            (source.volume > targetVol + .001f || 
            source.volume < targetVol - .001f))
        {
            source.volume = Mathf.Lerp(startVol, targetVol, t);

            t += this.thrustVolumeLerp;

            yield return false;
        }

        if (stopOnFinish)
        {
            source.Stop();
        }

        yield return true;
    }
}
