using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    private List<ArduinoJoystickPlayer> players = new List<ArduinoJoystickPlayer>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        players.AddRange(FindObjectsByType<ArduinoJoystickPlayer>());
        foreach (ArduinoJoystickPlayer player in players)
        {
            player.isPlayable = true;
        }
        Debug.Log(FindObjectsByType<ArduinoJoystickPlayer>().Length + " ArduinoJoystickPlayers found and set to playable.");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
