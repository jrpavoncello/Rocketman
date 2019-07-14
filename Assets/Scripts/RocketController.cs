using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketController : MonoBehaviour
{
    [SerializeField]
    float liftMultiplier = 3400f;

    [SerializeField]
    float rotationMultiplier = 1;

    [SerializeField]
    float gravityAcceleration = -150f;

    [SerializeField]
    float volumeFadeSpeed = .03f;

    [SerializeField]
    AudioSource primaryAudioSource;

    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private Rigidbody rigidBody;
    private AudioSource[] supportSources = new AudioSource[10];
    private int supportIndex = 0;
    private float rocketVolume;
    private HashSet<GameObject> collidingGameObjects = new HashSet<GameObject>();

    public bool IsInCollision => this.collidingGameObjects.Count > 0;

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

        var currentVolumeSource = this.supportSources[this.supportIndex];

        if (verticalInput > 0f)
        {
            rigidBody.AddRelativeForce(new Vector3(0, liftMultiplier * verticalInput * Time.deltaTime, 0));

            if(!currentVolumeSource.isPlaying)
            {
                StartCoroutine(FadeVolume(currentVolumeSource, this.rocketVolume, false));
            }
        }
        else
        {
            if(currentVolumeSource.isPlaying)
            {
                this.supportIndex = (this.supportIndex + 1) % this.supportSources.Length;

                StartCoroutine(FadeVolume(currentVolumeSource, 0f, true));
            }
        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetRocket();
        }
        else
        {
            rigidBody.AddForce(Vector3.up * rigidBody.mass * gravityAcceleration * Time.deltaTime);

            if (horizontalInput != 0f)
            {
                rigidBody.freezeRotation = true;

                this.transform.Rotate(new Vector3(0, 0, rotationMultiplier * horizontalInput * Time.deltaTime));

                rigidBody.freezeRotation = false;
            }
        }
    }

    private void ResetRocket()
    {
        transform.position = startingPosition;
        transform.rotation = startingRotation;

        // Stop the rigid from moving or rotation once we reset the position to the launch pad
        rigidBody.velocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        switch(collision.gameObject.tag)
        {
            case ProjectTags.FRIENDLY:
                break;
            case ProjectTags.FINISH:
                break;
            case ProjectTags.FUEL:
                break;
            default:
                ResetRocket();
                break;
        }

        this.collidingGameObjects.Add(collision.gameObject);
    }

    private void OnCollisionExit(Collision collision)
    {
        this.collidingGameObjects.Remove(collision.gameObject);
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
