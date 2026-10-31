using UnityEngine;

public class SpanDisplays : MonoBehaviour
{
    void Start()
    {
        // Example for two 1920x1080 monitors side-by-side
        int combinedWidth = 3840; 
        int height = 1080;

        // Force Unity to resize the window across the combined workspace
        // Setting fullScreen to false ensures it behaves as a window that can stretch
        Screen.SetResolution(combinedWidth, height, false);
    }
}