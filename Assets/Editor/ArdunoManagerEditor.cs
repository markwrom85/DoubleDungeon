using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

[CustomEditor(typeof(ardunoManager))]
public class ArdunoManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var manager = (ardunoManager)target;
        EditorGUILayout.HelpBox("Enable 1–4 slots with unique COM ports. Leave Player empty to press fire and join through the scene's PlayerInputManager, or assign an existing scene PlayerInput. Disabled slots leave desktop controls alone.", MessageType.Info);
        if (!Application.isPlaying) return;
        if (GUILayout.Button("Reconnect all Arduinos")) manager.ReconnectAll();
        for (int i = 0; manager.slots != null && i < Mathf.Min(4, manager.slots.Length); i++)
        {
            if (manager.slots[i] == null) continue;
            EditorGUILayout.LabelField("Slot " + (i + 1), manager.slots[i].status);
            if (GUILayout.Button("Calibrate slot " + (i + 1) + " (release stick first)"))
            {
                if (!manager.CalibrateCenter(i)) Debug.LogWarning("No recent data for that slot.", manager);
            }
        }
        Repaint();
    }

    [MenuItem("Double Dungeon/Set Up Arduino Manager")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying) return;
        PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Unity_4_8);
        var manager = Object.FindAnyObjectByType<ardunoManager>();
        if (manager == null)
        {
            var obj = new GameObject("Arduino Manager");
            Undo.RegisterCreatedObjectUndo(obj, "Create Arduino manager");
            manager = Undo.AddComponent<ardunoManager>(obj);
            // Carry over the last saved prototype COM port; verify it in the Inspector.
            manager.slots[0].portName = "COM9";
            var players = Object.FindObjectsByType<PlayerInput>();
            if (players.Length == 1)
            {
                manager.slots[0].player = players[0];
                manager.slots[0].enabled = true;
            }
        }
        Selection.activeGameObject = manager.gameObject;
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }
}
