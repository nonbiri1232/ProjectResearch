using UnityEngine;

public class ShutDownGame : MonoBehaviour
{
    public void ShutDown()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else   
            Application.Quit();
        #endif
    }
}
