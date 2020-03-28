using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    public GameManager.SyncListPlayingCard handCards = new GameManager.SyncListPlayingCard();
    private List<Button> _handButtons;

    public string playerName;
    //private bool _nameIsSet;

    public override void OnStartServer()
    {
        Debug.Log($"Player::OnStartServer: I am player {netId} on the server");
        GameManager.Singleton.AddPlayer(this);
    }

    public override void OnStartClient()
    {
        Debug.Log($"Player::OnStartClient: I am player {netId} on the client");
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log($"Player::OnStartLocalPlayer: I am player {netId} on the local player");
        // GameManager.Singleton.localPlayer = this;
        _handButtons = GameManager.Singleton.localPlayerCardButtons;
        
        // set default name
        // (this is not reflected for the gameObject names on the clients)
        // TODO ask all players to enter name ONCE before starting the first round
        CmdSetUserName($"Spieler {netId.Value-1}");
    }

    private void Awake()
    {
        handCards.Callback = OnSyncListHandCardsChanged;
    }

    private void OnSyncListHandCardsChanged(SyncList<PlayingCard.PlayingCardInfo>.Operation op, int index)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"(Local) Player {netId} was notified of a change in his hand: card {index}: {handCards[index]}");
            _handButtons[index].image.sprite = PlayingCard.SpriteDict[handCards[index]];
            _handButtons[index].onClick.RemoveAllListeners();
            _handButtons[index].onClick.AddListener(() => OnClickCardButton(index));
        }
    }

    void OnClickCardButton(int index)
    {
        // disable the button GameObject so we cannot play the card again
        _handButtons[index].gameObject.SetActive(false);

        // tell the server to play the card
        CmdPlayCard(handCards[index]);
    }

    [Command]
    private void CmdPlayCard(PlayingCard.PlayingCardInfo handCard)
    {
        Debug.Log($"Player::CmdPlayCard: sending card {handCard} to the GameManager");
        GameManager.Singleton.CmdPlayCard(handCard);
    }

    // public void UpdateButtons()
    // {
    //     Debug.Log($"Player::UpdateButtons: player {netId}, isLocalPlayer={isLocalPlayer}");
    //     for (int i = 0; i < handCards.Count; i++)
    //     {
    //         _handButtons[i].image.sprite = PlayingCard.SpriteDict[handCards[i]];
    //     }
    // }


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
        // if (!isLocalPlayer) {return;}
        Debug.Log($"Player {netId} was asked to change name");
        playerName = newName;
        name = newName;
    }

    [Client]
    private void OnGUI()
    {
        if (!isLocalPlayer) return;
        // if (_nameIsSet) return;

        var xpos = 30;
        var ypos = 100;

        GUI.Label(new Rect(xpos, ypos, 100, 20), "Player Name:");
        playerName = GUI.TextField(new Rect(xpos + 100, ypos, 100, 20), playerName);

        ypos += 20;

        if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Set Name"))
        {
            CmdSetUserName(playerName);
            // _nameIsSet = true;
        }
    }

    [ClientRpc]
    public void RpcDisplayPreRoundButtons()
    {
        // only do this on the client that is controlling this player
        if (!isLocalPlayer) {return;}
        
        Debug.Log($"{nameof(Player)}::{nameof(RpcDisplayPreRoundButtons)}:" +
                  $"client {netId} was asked to display the preRound buttons");
        
        foreach (Button preRoundButton in GameManager.Singleton.preRoundButtons)
        {
            preRoundButton.gameObject.SetActive(true);
            // TODO provide onClick listener
        }
    }
}