using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class LightningTomeValidation
{
    private static double runAt;
    [MenuItem("Tools/Double Dungeon/Check Lightning Tome in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("LightningTomeValidate", true);
        EditorApplication.isPlaying = true;
    }
    static LightningTomeValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("LightningTomeValidate", false)) return;
            runAt = EditorApplication.timeSinceStartup + 3;
            EditorApplication.update += Check;
        };
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Check()
    {
        if (EditorApplication.timeSinceStartup < runAt) return;
        EditorApplication.update -= Check;
        SessionState.SetBool("LightningTomeValidate", false);
        try
        {
            var tome = AssetDatabase.LoadAssetAtPath<TomeDefinition>("Assets/ScriptableObjects/Items/Tomes/LightningTome.asset");
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            var player = UnityEngine.Object.Instantiate(playerPrefab, new Vector3(500, 500), Quaternion.identity).GetComponent<PlayerInfo>();
            player.SetPlayerId(1);
            var other = UnityEngine.Object.Instantiate(playerPrefab, new Vector3(510, 500), Quaternion.identity).GetComponent<PlayerInfo>();
            other.SetPlayerId(2);
            var otherPanel = GameObject.Find("Player 2 Panel").GetComponent<PlayerPanel>();
            var before = otherPanel.GetComponentsInChildren<Image>(true).Select(i => i.sprite).ToArray();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/LightningTomePickup.prefab");
            var pickup = UnityEngine.Object.Instantiate(prefab, new Vector3(520, 500), Quaternion.identity);
            var pedestal = pickup.transform.Find("Pedestal");
            var animator = pickup.GetComponentInChildren<Animator>();
            animator.Update(0);
            var first = animator.GetComponent<SpriteRenderer>().sprite;
            animator.Update(0.25f);
            Require(first != animator.GetComponent<SpriteRenderer>().sprite, "Tome animation does not change frames.");
            var item = pickup.GetComponent<ItemInfo>();
            Require(item.TryCollect(player), "Pickup rejected Lightning tome.");
            Require(!item.TryCollect(player), "Pickup allowed duplicate collection.");
            Require(pedestal != null && pedestal.parent != pickup.transform, "Pedestal was not preserved.");
            Require(player.spAttack is HyperBeam, "Lightning did not equip HyperBeam.");
            var panel = GameObject.Find("Player 1 Panel").GetComponent<PlayerPanel>();
            var serialized = new SerializedObject(panel);
            Require(((Image)serialized.FindProperty("tomeOutline").objectReferenceValue).sprite == tome.panelOutline, "Outline did not change.");
            Require(((Image)serialized.FindProperty("tomeBackground").objectReferenceValue).sprite == tome.panelBackground, "Background did not change.");
            Require(((Animator)serialized.FindProperty("specialAbilityAnimator").objectReferenceValue).runtimeAnimatorController == tome.abilityAnimation, "Ability animation not connected.");
            Require(other.spAttack == null && before.SequenceEqual(otherPanel.GetComponentsInChildren<Image>(true).Select(i => i.sprite)), "Other player was changed.");
            var previous = player.spAttack;
            Require(player.EquipTome(tome) && !previous.gameObject.activeSelf, "Old ability remained active.");
            player.TriggerSpecialAttack();
            Debug.Log("LIGHTNING_TOME_PLAY_TEST_OK: animation, pickup, duplicate protection, pedestal, HyperBeam, HUD, player isolation, ability replacement and activation passed.");
        }
        catch (Exception error) { Debug.LogException(error); }
        finally { EditorApplication.isPlaying = false; }
    }
}
