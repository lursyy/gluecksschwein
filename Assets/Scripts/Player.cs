using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    public GameManager.SyncListPlayingCard handCards = new GameManager.SyncListPlayingCard();
    private List<Button> _handButtons;
    public string playerName;

    #region Callbacks
    
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
        CmdSetUserName($"Spieler {netId}");
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

    #endregion

    #region Server Functions

    [Command]
    private void CmdPlayCard(PlayingCard.PlayingCardInfo handCard)
    {
        Debug.Log($"Player::CmdPlayCard: sending card {handCard} to the GameManager");
        GameManager.Singleton.CmdPlayCard(handCard);
    }

    [Command]
    private void CmdSetUserName(string newName)
    {
        Debug.Log($"Cmd: Player {netId} was asked to change name");
        playerName = newName;
        name = newName;
        RpcSetUserName(newName);
    }
    
    #endregion

    #region Client Functions

        
    [ClientRpc]
    public void RpcSetUserName(string newName)
    {
        // if (!isLocalPlayer) {return;}
        Debug.Log($"Rpc: Player {netId} was asked to change name");
        playerName = newName;
        name = newName;
    }

    [Client]
    private void OnGUI()
    {
        if (!isLocalPlayer || GameManager.Singleton.currentGameState > GameManager.GameState.GameRunning) return;

        var xpos = 30;
        var ypos = 100;

        GUI.Label(new Rect(xpos, ypos, 100, 20), "Player Name:");
        playerName = GUI.TextField(new Rect(xpos + 100, ypos, 100, 20), playerName);

        ypos += 20;

        if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Set Name"))
        {
            CmdSetUserName(playerName);
        }
    }

    [ClientRpc]
    public void RpcDisplayPreRoundButtons()
    {
        // are we on the client that is controlling this player? otherwise, stop here
        if (!isLocalPlayer) { return; }
        
        Debug.Log($"{nameof(Player)}::{nameof(RpcDisplayPreRoundButtons)}:" +
                  $"client {netId} was asked to display the preRound buttons");
        
        // make the button panel visible
        GameManager.Singleton.preRoundButtonPanel.gameObject.SetActive(true);

        // (the onClick Listeners are already hooked up via the inspector)
    }

    [Client]
    public void PreRoundOnChooseSauSpiel()
    {
        if (!isLocalPlayer) { return; }
        RemovePreRoundButtons();
        
        throw new NotImplementedException();
    }

    [Client]
    public void PreRoundOnChooseSolo()
    {
        if (!isLocalPlayer) { return; }
        RemovePreRoundButtons();

        throw new NotImplementedException();
    }

    [Client]
    public void PreRoundOnChooseWenz()
    {
        if (!isLocalPlayer) { return; }
        RemovePreRoundButtons();

        throw new NotImplementedException();
    }

    [Client]
    public void PreRoundOnChooseWeiter()
    {
        if (!isLocalPlayer) { return; }
        RemovePreRoundButtons();

        throw new NotImplementedException();
    }

    private void RemovePreRoundButtons()
    {
        // disable button panel again
        GameManager.Singleton.preRoundButtonPanel.SetActive(false);
        
        // remove all onclick listeners, so we don't get notified when another player clicks these buttons
        // TODO I think we don't need to do that because the buttons are only displayed for one player at a time
        
    }

    

    #endregion
}