using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class SpecialCooldownValidation
{
    private static double runAt;
    private static int phase;
    private static PlayerInfo player;
    private static TomeDefinition tome;
    private static Image overlay;
    private static TextMeshProUGUI number;
    private static Animator animator;
    private static SpecialAttack oldAbility;

    static SpecialCooldownValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("SpecialCooldownValidate", false)) return;
            phase = 0;
            runAt = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update += Check;
        };
    }

    [MenuItem("Tools/Double Dungeon/Check Special Cooldown in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("SpecialCooldownValidate", true);
        EditorApplication.isPlaying = true;
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static void Check()
    {
        if (EditorApplication.timeSinceStartup < runAt) return;
        try
        {
            if (phase == 0)
            {
                tome = AssetDatabase.LoadAssetAtPath<TomeDefinition>("Assets/ScriptableObjects/Items/Tomes/LightningTome.asset");
                player = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"), new Vector3(500, 500), Quaternion.identity).GetComponent<PlayerInfo>();
                player.SetPlayerId(1);
                Require(player.EquipTome(tome), "Tome did not equip.");
                var panel = GameObject.Find("Player 1 Panel").GetComponent<PlayerPanel>();
                overlay = panel.transform.Find("Special Cooldown Sweep").GetComponent<Image>();
                number = overlay.GetComponentInChildren<TextMeshProUGUI>();
                animator = ((Image)new SerializedObject(panel).FindProperty("specialAbilityIcon").objectReferenceValue).GetComponent<Animator>();
                var settings = new SerializedObject(player.spAttack);
                settings.FindProperty("spLength").floatValue = 0.5f;
                settings.FindProperty("spCooldown").floatValue = 1f;
                settings.ApplyModifiedPropertiesWithoutUndo();
                player.TriggerSpecialAttack();
                oldAbility = player.spAttack;
                panel.SetSpecialAbilityCooldown(player.spAttack);
                Require(!player.spAttack.IsReady && overlay.enabled && overlay.fillAmount > 0.99f, "Initial cooldown did not cover whole slot.");
                Require(number.text == "2" && animator.speed == 0f, "Countdown or animation pause failed.");
                Require(overlay.rectTransform.sizeDelta == new Vector2(28, 28), "Overlay does not cover full slot.");
                Require(!GameObject.Find("Player 2 Panel").transform.Find("Special Cooldown Sweep").GetComponent<Image>().enabled, "Other player's cooldown changed.");
                runAt = EditorApplication.timeSinceStartup + 0.8;
                phase = 1;
            }
            else if (phase == 1)
            {
                Require(overlay.fillAmount > 0 && overlay.fillAmount < 0.8f && number.text == "1", "Sweep/countdown did not progress.");
                Require(!player.spAttack.IsReady && animator.speed == 0, "Ability became ready too early.");
                runAt = EditorApplication.timeSinceStartup + 1;
                phase = 2;
            }
            else
            {
                Require(player.spAttack.IsReady && !overlay.enabled && !number.enabled && animator.speed == 1, "Ready state did not clear countdown and resume animation.");
                player.TriggerSpecialAttack();
                Require(player.EquipTome(tome) && !oldAbility.gameObject.activeSelf, "Replacing a cooling ability failed.");
                Require(!overlay.enabled && player.spAttack.IsReady && animator.speed == 1, "New tome retained previous cooldown.");
                Debug.Log("SPECIAL_COOLDOWN_PLAY_TEST_OK: full slot coverage, numeric countdown, sweep progression, animation pause/resume, player isolation, actual readiness and tome replacement passed.");
                Stop();
            }
        }
        catch (Exception error) { Debug.LogException(error); Stop(); }
    }

    private static void Stop()
    {
        EditorApplication.update -= Check;
        SessionState.SetBool("SpecialCooldownValidate", false);
        EditorApplication.isPlaying = false;
    }
}
