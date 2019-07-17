using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorBoolSetter : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Name of the property in the animator.")]
    private string propertyName;

    [SerializeField]
    [Tooltip("Bool value to set.")]
    private bool value = false;

    // Start is called before the first frame update
    void Start()
    {
        var animator = GetComponent<Animator>();
        animator.SetBool(this.propertyName, this.value);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
