using UnityEngine;

public class AppManager : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    public void QuitApplication()
    {
        Debug.Log("Quitting application...");
        
    #if UNITY_EDITOR
                      UnityEditor.EditorApplication.isPlaying = false;
              #else
                          Application.Quit();
              #endif
    }
    
}