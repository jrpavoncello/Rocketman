using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelChanger : MonoBehaviour
{
    [SerializeField]
    private string nextLevel;

    private Animator animator;
    private string levelOverride;
    private Action callback;
    private const string FADEOUT = "FadeOut";

    // Start is called before the first frame update
    void Start()
    {
        this.animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BeginNextLevel()
    {
        animator.SetTrigger(FADEOUT);

    }

    public void ReloadLevel(Action callback)
    {
        animator.SetTrigger(FADEOUT);

        this.levelOverride = SceneManager.GetActiveScene().name;
        this.callback = callback;
    }

    public void OnFadeCompleted()
    {
        if(this.callback != null)
        {
            this.callback();
            this.callback = null;
        }

        if(string.IsNullOrEmpty(this.levelOverride))
        {
            SceneManager.LoadScene(nextLevel);
        }
        else
        {
            SceneManager.LoadScene(this.levelOverride);
        }
    }
}
