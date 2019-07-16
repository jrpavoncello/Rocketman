using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelChanger : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Name of the next scene that will be loaded when a fade out completes.")]
    private string nextLevelName;

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

        var activeScene = SceneManager.GetActiveScene();

        this.levelOverride = activeScene.name;

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
            var activeScene = SceneManager.GetActiveScene();
            var currentBuildIndex = activeScene.buildIndex;
            var nextBuildIndex = (currentBuildIndex + 1) % SceneManager.sceneCountInBuildSettings;
            SceneManager.LoadScene(nextBuildIndex);
        }
        else
        {
            SceneManager.LoadScene(this.levelOverride);
        }
    }
}
