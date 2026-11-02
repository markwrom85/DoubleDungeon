using UnityEngine;

public class MultiMonitorBoot : MonoBehaviour
{
    void Start()
    {
        // Check if the operating system detects more than one monitor
        if (Display.displays.Length > 1)
        {
            // Activate the second monitor (index 1)
            // You can optionally pass specific resolution targets here
            Display.displays[1].Activate();
        }
        else
        {
            Debug.LogWarning("Secondary monitor not detected.");
        }
    }
}