using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
public static class ArduinoTestBootstrap {
 public static void Run() {
  PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ApiCompatibilityLevel.NET_Unity_4_8);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var runner=new GameObject("Arduino test runner").AddComponent<ArduinoPlayChecks>();
  runner.asset=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
  EditorSceneManager.SaveScene(scene,"Assets/ArduinoChecks.unity");
  EditorApplication.EnterPlaymode();
 }
}
