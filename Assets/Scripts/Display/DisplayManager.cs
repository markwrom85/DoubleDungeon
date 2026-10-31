using UnityEngine;
using UnityEngine.UI;

public class DisplayManager : MonoBehaviour
{
    [SerializeField] private Camera leftCamera, rightCamera;
    [SerializeField] private Canvas mainCanvas, secondaryCanvas;
    [SerializeField] private RectTransform canvasUI;
    public bool oneScreenMode = false;

    void Awake()
    {
        if (oneScreenMode)
        {
            leftCamera.targetDisplay = 0;
            rightCamera.targetDisplay = 0;

            secondaryCanvas.gameObject.SetActive(false);

            mainCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(3840, 1080);
            canvasUI.localPosition = Vector2.zero;

            leftCamera.rect = new Rect(0f, 0f, 0.5f, 1f);
            rightCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
        }
        else
        {
            leftCamera.targetDisplay = 0;
            rightCamera.targetDisplay = 1;

            secondaryCanvas.gameObject.SetActive(true);

            mainCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);

            leftCamera.rect = new Rect(0f, 0f, 1f, 1f);
            rightCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }
}
