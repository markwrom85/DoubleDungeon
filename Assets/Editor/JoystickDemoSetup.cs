using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class JoystickDemoSetup
{
    [MenuItem("Double Dungeon/Set Up Joystick Demo")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying) return;
        PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Unity_4_8);
        var player = Object.FindAnyObjectByType<ArduinoJoystickPlayer>();
        if (player == null)
        {
            CombatPrefabSetup.CreateMissing();
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"));
            Undo.RegisterCreatedObjectUndo(obj, "Create joystick player");
            player = obj.GetComponent<ArduinoJoystickPlayer>();
        }
        else if (player.GetComponentInChildren<CardinalGun>() == null)
        {
            CombatPrefabSetup.CreateMissing();
            var gun = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gun.prefab"), player.transform);
            Undo.RegisterCreatedObjectUndo(gun, "Add player gun");
            Undo.RecordObject(player, "Connect player gun");
            player.gun = gun.GetComponent<CardinalGun>();
        }
        if (Camera.main == null)
        {
            var obj = new GameObject("Main Camera", typeof(Camera));
            Undo.RegisterCreatedObjectUndo(obj, "Create demo camera");
            obj.tag = "MainCamera";
            obj.transform.position = new Vector3(0, 0, -10);
            obj.GetComponent<Camera>().orthographic = true;
            obj.GetComponent<Camera>().orthographicSize = 5;
        }
        Selection.activeGameObject = player.gameObject;
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        Debug.Log("Joystick demo ready. Set Player > Port Name to your Arduino COM port, save the scene, then press Play. The square appears during Play.");
    }
}
