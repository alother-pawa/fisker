using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitBtn : BaseBtn
{
    private void Awake()
    {
        // A browser page cannot be quit: the web build has no quit button.
        if (Application.platform == RuntimePlatform.WebGLPlayer) _button.gameObject.SetActive(false);
    }

    public override void OnClick()
    {
        //if running in Unity Editor, quit play mode else quit application
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                Debug.Log("Quit");
        #else
                Application.Quit();
        #endif

    }
}