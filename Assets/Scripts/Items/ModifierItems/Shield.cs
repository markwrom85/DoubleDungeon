using UnityEngine;

public class Shield : MonoBehaviour
{
    [SerializeField] private float speed = 90f;

    // Update is called once per frame
    void Update()
    {
        if (transform.parent == null)
            return;

        Vector3 offset = transform.localPosition;
        if (offset.sqrMagnitude < 0.0001f)
            offset = Vector3.right;

        offset = Quaternion.Euler(0f, 0f, speed * Time.deltaTime) * offset;
        transform.localPosition = offset;
    }
}
