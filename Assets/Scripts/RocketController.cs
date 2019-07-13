using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketController : MonoBehaviour
{
    [SerializeField]
    float LiftMultiplier = 3400f;

    [SerializeField]
    float RotationMultiplier = 1;

    [SerializeField]
    float GravityAcceleration = -150f;

    [SerializeField]
    float VolumeFadeSpeed = .05f;

    [SerializeField]
    AudioSource primaryAudioSource;

    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private Rigidbody rigidBody;
    private AudioSource[] supportSources = new AudioSource[10];
    private int supportIndex = 0;
    private float rocketVolume;

    // Start is called before the first frame update
    void Start()
    {
        this.rigidBody = GetComponent<Rigidbody>();
        this.startingPosition = transform.position;
        this.startingRotation = transform.rotation;
        this.rocketVolume = this.primaryAudioSource.volume;

        for (int i = 0; i < this.supportSources.Length; i++)
        {
            var supportSource = Instantiate(this.primaryAudioSource.gameObject, this.primaryAudioSource.gameObject.transform);

            supportSources[i] = supportSource.GetComponent<AudioSource>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        var verticalInput = Input.GetAxis("Vertical");
        var horizontalInput = Input.GetAxis("Horizontal");

        var currentVolumeSource = this.supportSources[supportIndex];

        if (verticalInput > 0f)
        {
            rigidBody.AddRelativeForce(new Vector3(0, LiftMultiplier * verticalInput * Time.deltaTime, 0));

            if(!currentVolumeSource.isPlaying)
            {
                StartCoroutine(FadeVolume(currentVolumeSource, this.rocketVolume, false));
            }
        }
        else
        {
            if(currentVolumeSource.isPlaying)
            {
                StartCoroutine(FadeVolume(currentVolumeSource, 0f, true));
                this.supportIndex = (this.supportIndex + 1) % this.supportSources.Length;
            }
        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            transform.position = startingPosition;
            transform.rotation = startingRotation;

            // Stop the rigid from moving or rotation once we reset the position to the launch pad
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }
        else
        {
            rigidBody.AddForce(Vector3.up * rigidBody.mass * GravityAcceleration * Time.deltaTime);

            if (horizontalInput != 0f)
            {
                rigidBody.freezeRotation = true;

                this.transform.Rotate(new Vector3(0, 0, RotationMultiplier * horizontalInput * Time.deltaTime));

                rigidBody.freezeRotation = false;
            }
        }
    }

    IEnumerator FadeVolume(AudioSource source, float targetVol, bool stopOnFinish)
    {
        if(!stopOnFinish)
        {
            source.Play();
        }

        float t = 0;
        float startVol = source.volume;

        while(source.volume > targetVol + .001f || 
            source.volume < targetVol - .001f)
        {
            source.volume = Mathf.Lerp(startVol, targetVol, t);

            t += this.VolumeFadeSpeed;

            yield return false;
        }

        if(stopOnFinish)
        {
            source.Stop();
        }

        yield return true;
    }
}
