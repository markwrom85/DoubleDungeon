using UnityEngine;
using UnityEngine.SceneManagement;

// The paired camera viewports crop the two halves of the sphere at the divider.
[RequireComponent(typeof(BatController))]
public sealed class BatPortalVisual : MonoBehaviour
{
    private BatController bat;
    private MeshRenderer source, ghost;
    private MaterialPropertyBlock properties;

    private void Awake()
    {
        bat = GetComponent<BatController>();
        source = GetComponentInChildren<MeshRenderer>(true);
        properties = new MaterialPropertyBlock();
        var copy = new GameObject(name + " portal visual");
        SceneManager.MoveGameObjectToScene(copy, gameObject.scene);
        copy.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
        ghost = copy.AddComponent<MeshRenderer>();
        ghost.sharedMaterial = source.sharedMaterial;
        ghost.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ghost.receiveShadows = false;
        ghost.enabled = false;
    }

    private void LateUpdate()
    {
        if (bat.CurrentSide == null || bat.CurrentSide.LevelBounds == null
            || bat.CurrentSide.OppositeSide == null || !bat.IsRoomPairActive())
        {
            ghost.enabled = false;
            return;
        }
        Bounds room = bat.CurrentSide.LevelBounds.bounds;
        float edge = bat.IsOnLeft ? room.max.x : room.min.x;
        if (!source.enabled || Mathf.Abs(source.transform.position.x - edge) > source.bounds.extents.x + 0.02f)
        {
            ghost.enabled = false;
            return;
        }
        Bounds other = bat.CurrentSide.OppositeSide.LevelBounds.bounds;
        float scale = other.size.y / room.size.y;
        Vector2 mapped = bat.MapAcross(source.transform.position);
        ghost.transform.SetPositionAndRotation(new Vector3(mapped.x, mapped.y, source.transform.position.z), source.transform.rotation);
        ghost.transform.localScale = source.transform.lossyScale * scale;
        ghost.sortingLayerID = source.sortingLayerID;
        ghost.sortingOrder = source.sortingOrder;
        source.GetPropertyBlock(properties);
        ghost.SetPropertyBlock(properties);
        ghost.enabled = true;
    }

    private void OnDisable() { if (ghost != null) ghost.enabled = false; }
    private void OnDestroy() { if (ghost != null) Destroy(ghost.gameObject); }
}
