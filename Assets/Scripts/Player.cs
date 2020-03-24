using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
#pragma warning disable 618

public class Player : NetworkBehaviour
{
    private List<PlayingCard.PlayingCardInfo> _playingCards;
    private List<Button> _playingCardButtons;
    [field: SyncVar] private string PlayerName { get; set; }
    private bool _nameIsSet;

    [Command]
    private void CmdSetUserName(string newName)
    {
        PlayerName = newName;
        name = newName;
    }

    private void OnGUI()
    {
        if (!isLocalPlayer) return;
        if (_nameIsSet) return;
        
        var xpos = 30;
        var ypos = 100;
            
        GUI.Label(new Rect(xpos, ypos, 100, 20), "Player Name:");
        PlayerName = GUI.TextField(new Rect(xpos + 100, ypos, 100, 20), PlayerName);

        ypos += 20;
            
        if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Set Name"))
        {
            CmdSetUserName(PlayerName);
            _nameIsSet = true;
        }
    }
}
