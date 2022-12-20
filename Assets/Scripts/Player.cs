using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    public GameManager.SyncListPlayingCard handCards = new GameManager.SyncListPlayingCard();
    private List<Button> _handButtons;
    public string playerName;

    #region General
    
    public override void OnStartServer()
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"I am player {netId} on the server");
        GameManager.Singleton.AddPlayer(this);
    }

    public override void OnStartClient()
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"I am player {netId} on the client");
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"I am player {netId} on the local player");
        // GameManager.Singleton.localPlayer = this;
        _handButtons = GameManager.Singleton.localPlayerCardButtons;

        _handButtons.ForEach(button =>
            button.onClick.AddListener(() => OnClickCardButton(_handButtons.IndexOf(button)))
        ); 

        // set default name
        // (this is not reflected for the gameObject names on the clients)
        CmdSetUserName($"Spieler {netId}");
    }

    private void Awake()
    {
        handCards.Callback = OnSyncListHandCardsChanged;
    }
    
    [Command]
    private void CmdSetUserName(string newName)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"Player {netId} was asked to change name");
        playerName = newName;
        name = newName;
        RpcSetUserName(newName);
    }
    
    [ClientRpc]
    public void RpcSetUserName(string newName)
    {
        // if (!isLocalPlayer) {return;}
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"Player {netId} was asked to change name");
        playerName = newName;
        name = newName;
    }

    [Client]
    private void OnGUI()
    {
        if (!isLocalPlayer || GameManager.Singleton.CurrentGameState > GameManager.GameState.GameRunning) return;

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

    #endregion

    #region PreRound
    
    [ClientRpc]
    public void RpcDisplayPreRoundButtons(GameManager.RoundMode currentRoundMode)
    {
        // are we on the client that is controlling this player? otherwise, stop here
        if (!isLocalPlayer) { return; }
        
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"client {netId} was asked to display the preRound buttons, {nameof(currentRoundMode)}={currentRoundMode}");
        
        // make the button panel visible
        GameManager.Singleton.preRoundButtonPanel.gameObject.SetActive(true);
        
        // disable specific buttons depending on the current game mode
        switch (currentRoundMode)
        {
            case GameManager.RoundMode.Ramsch:
            {
                // for ramsch we don't disable any button
                break;
            }
            case GameManager.RoundMode.SauspielBlatt:
            case GameManager.RoundMode.SauspielEichel:
            case GameManager.RoundMode.SauspielSchelln:
            {
                GameManager.Singleton.preRoundSauspielDropdown.interactable = false;
                break;
            }
            case GameManager.RoundMode.Solo:
            {
                GameManager.Singleton.preRoundSauspielDropdown.interactable = false;
                GameManager.Singleton.preRoundSoloButton.interactable = false;
                break;
            }
            case GameManager.RoundMode.Wenz:
            {
                throw new InvalidOperationException(
                    $"player {netId} has no choices (round mode = Wenz) but was asked to make one");
            }
            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }
        
        GameManager.Singleton.preRoundSauspielDropdown.onValueChanged.AddListener(OnPreRoundChooseSauSpiel);
        GameManager.Singleton.preRoundSoloButton.onClick.AddListener(OnPreRoundChooseSolo);
        GameManager.Singleton.preRoundWenzButton.onClick.AddListener(OnPreRoundChooseWenz);
        GameManager.Singleton.preRoundWeiterButton.onClick.AddListener(OnPreRoundChooseWeiter);
    }

    private void OnPreRoundChooseSauSpiel(int sauChoiceInt)
    {
        if (!isLocalPlayer) { return; }
        
        // do nothing if the player clicked the first dropdown item (which simply says "Sauspiel...")
        if (sauChoiceInt == 0) { return; }
        
        // otherwise, convert the clicked item index into a choice
        GameManager.PreRoundChoice sauChoice = (GameManager.PreRoundChoice) sauChoiceInt;
        
        HidePreRoundButtons();
        CmdSelectPreRoundChoice(sauChoice);
    }

    [Client]
    private void OnPreRoundChooseSolo()
    {
        if (!isLocalPlayer) { return; }
        HidePreRoundButtons();
        CmdSelectPreRoundChoice(GameManager.PreRoundChoice.Solo);
    }

    [Client]
    private void OnPreRoundChooseWenz()
    {
        if (!isLocalPlayer) { return; }
        HidePreRoundButtons();
        CmdSelectPreRoundChoice(GameManager.PreRoundChoice.Wenz);
    }

    [Client]
    private void OnPreRoundChooseWeiter()
    {
        if (!isLocalPlayer) { return; }
        HidePreRoundButtons();
        CmdSelectPreRoundChoice(GameManager.PreRoundChoice.Weiter);
    }

    [Client]
    private void HidePreRoundButtons()
    {
        if (!isLocalPlayer) { return; }
        
        // disable the button panel
        GameManager.Singleton.preRoundButtonPanel.gameObject.SetActive(false);

        // reset all the buttons to their active state (we disable the ones we don't want in RcpDisplayPreRoundButtons)
        GameManager.Singleton.preRoundSauspielDropdown.interactable = true;
        GameManager.Singleton.preRoundSoloButton.interactable = true;
        
        // remove all onclick listeners, so we don't get notified when another player clicks these buttons
        GameManager.Singleton.preRoundSauspielDropdown.onValueChanged.RemoveListener(OnPreRoundChooseSauSpiel);
        GameManager.Singleton.preRoundSoloButton.onClick.RemoveListener(OnPreRoundChooseSolo);
        GameManager.Singleton.preRoundWenzButton.onClick.RemoveListener(OnPreRoundChooseWenz);
        GameManager.Singleton.preRoundWeiterButton.onClick.RemoveListener(OnPreRoundChooseWeiter);
    }
    
    #endregion
    
    #region Round
    
    private void OnSyncListHandCardsChanged(SyncList<PlayingCard.PlayingCardInfo>.Operation op, int index)
    {
        if (isLocalPlayer)
        {
            // TODO check if list was cleared (via op)
            Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                      $"Player {netId} was notified of a change in his hand: card {index}: {handCards[index]}");
            _handButtons[index].image.sprite = PlayingCard.SpriteDict[handCards[index]];
        }
    }

    [Client]
    void OnClickCardButton(int index)
    {
        // disable the button GameObject so we cannot play the card again
        _handButtons[index].gameObject.SetActive(false);

        // make ALL the buttons non-interactable because the player has had their turn
        _handButtons.ForEach(button => button.interactable = false);
        
        // tell the server to play the card
        CmdPlayCard(handCards[index]);
    }


    [Command]
    private void CmdPlayCard(PlayingCard.PlayingCardInfo handCard)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"sending card {handCard} to the GameManager");
        GameManager.Singleton.CmdHandlePlayCard(handCard);
    }

    [Command]
    private void CmdSelectPreRoundChoice(GameManager.PreRoundChoice choice)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"player {netId} sending choice {choice} to the GameManager");
        GameManager.Singleton.CmdHandlePreRoundChoice(netId, choice);
    }
    
    /// <summary>
    /// enables the hand buttons for the local player. Disables the buttons for every other player
    /// </summary>
    [ClientRpc]
    public void RpcStartTurn()
    {
        if (!isLocalPlayer) { return; }

        // enable the card buttons
        _handButtons.ForEach(button => button.interactable = true);
    }
    
    #endregion
    
}