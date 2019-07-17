using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorFloatSetter : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Name of the property in the animator.")]
    private string propertyName;

    [SerializeField]
    [Tooltip("Float value to set.")]
    private float value = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        var animator = GetComponent<Animator>();
        animator.SetFloat(this.propertyName, this.value);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
