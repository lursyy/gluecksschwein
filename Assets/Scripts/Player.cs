using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    public List<PlayingCard.PlayingCardInfo> handCards;
    private List<Button> _handButtons;

    public string playerName;
    private bool _nameIsSet;

    public override void OnStartServer()
    {
        GameManager.Singleton.AddPlayer(this);
    }

    public override void OnStartLocalPlayer()
    {
        GameManager.Singleton.localPlayer = this;
    }

    public override void OnStartClient()
    {
        _handButtons = GameManager.Singleton.localPlayerCardButtons;
    }

    [Command]
    private void CmdSetUserName(string newName)
    {
        playerName = newName;
        name = newName;
        RpcSetUserName(newName);
    }

    [ClientRpc]
    private void RpcSetUserName(string newName)
    {
        playerName = newName;
        name = newName;
    }

    [Command]
    public void CmdDrawHand(int index, int range)
    {
        handCards = GameManager.Singleton.syncListCardDeck.ToList().GetRange(index, range);
        Debug.Log($"Player::CmdDrawHand: (Server) player {netId} drawing cards {index} to {index + range - 1}: " +
                  $"{string.Join(", ", handCards)}");
        RpcDrawHand(index, range);
    }

    [ClientRpc]
    public void RpcDrawHand(int index, int range)
    {
        // TODO when is this the local player?
        if (!isLocalPlayer)
        {
            Debug.Log($"Player::RpcDrawHand: Player {netId} is not localPlayer. Doing nothing.");
            return;
        }

        handCards = GameManager.Singleton.syncListCardDeck.ToList().GetRange(index, range);
        Debug.Log(
            $"Player::RpcDrawHand: I am client {netId}. isLocalPlayer={isLocalPlayer}, hasAuthority={hasAuthority}, localPlayerAuthority={localPlayerAuthority}." +
            $"Now drawing cards {index} to {index + range - 1}: {string.Join(", ", handCards)}");

        // we are on the client, so we will try to use the buttons that we accessed in OnStartLocalPlayer
        //Debug.Log($"We have {_handButtons.Count} buttons available");

        if (range != _handButtons.Count)
        {
            throw new InvalidOperationException("number of cards and buttons does not match");
        }

        for (int i = 0; i < range; i++)
        {
            _handButtons[i].image.sprite = PlayingCard.SpriteDict[handCards[i]];
        }
    }

    private void OnGUI()
    {
        if (!isLocalPlayer) return;
        if (_nameIsSet) return;

        var xpos = 30;
        var ypos = 100;

        GUI.Label(new Rect(xpos, ypos, 100, 20), "Player Name:");
        playerName = GUI.TextField(new Rect(xpos + 100, ypos, 100, 20), playerName);

        ypos += 20;

        if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Set Name"))
        {
            CmdSetUserName(playerName);
            _nameIsSet = true;
        }
    }
}