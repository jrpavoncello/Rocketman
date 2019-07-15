using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RocketController : MonoBehaviour
{
    [SerializeField]
    private float liftMultiplier = 3400f;

    [SerializeField]
    private float rotationMultiplier = 1;

    [SerializeField]
    private float gravityAcceleration = -150f;

    [SerializeField]
    private float volumeFadeSpeed = .03f;

    [SerializeField]
    private AudioSource thrustAudioSource;

    [SerializeField]
    private AudioSource deathAudioSource;

    [SerializeField]
    private AudioSource finishAudioSource;

    [SerializeField]
    private LevelChanger levelChanger;

    [SerializeField]
    private int finishDelay = 2;

    [SerializeField]
    private int deathDelay = 2;

    private RocketState state = RocketState.Alive;
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

        if (this.state != RocketState.Alive)
        {
            return;
        }

        var verticalInput = Input.GetAxis("Vertical");
        var horizontalInput = Input.GetAxis("Horizontal");

        if (verticalInput > 0f)
        {
            HandleRocketThrust(verticalInput);

            HandleThrustAudio(true);
        }
        else
        {
            HandleThrustAudio(false);
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
                KillTheRocket();
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
        if (this.state == RocketState.Alive)
        {
            this.state = RocketState.WaitingToFinish;

            StopThrustSounds();

            this.finishAudioSource.Play();

            BehaviourHelpers.DelayInvoke(this, () =>
            {
                if(this.state == RocketState.WaitingToFinish)
                {
                    // In case the rocket hits the finish after dying
                    levelChanger.BeginNextLevel();
                }
            },
            this.finishDelay);
        }
    }

    private void KillTheRocket()
    {
        if (this.state == RocketState.Dead)
        {
            // Already dead, waiting for the animation to complete
            return;
        }

        this.state = RocketState.Dead;

        rigidBody.constraints = RigidbodyConstraints.None;

        StopThrustSounds();

        this.deathAudioSource.Play();

        BehaviourHelpers.DelayInvoke(this, () =>
        {
            levelChanger.ReloadLevel(() =>
            {
                ResetRocketToStart();
            });
        },
        this.deathDelay);
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

            t += this.volumeFadeSpeed;

            yield return false;
        }

        if (stopOnFinish)
        {
            source.Stop();
        }

        yield return true;
    }
}
