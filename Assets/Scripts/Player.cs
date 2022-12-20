using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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
        // _handButtons = GameManager.Singleton.localPlayerCardButtons;
        // UpdateButtons();
    }
    
    public override void OnStartLocalPlayer()
    {
        Debug.Log($"Player::OnStartLocalPlayer: I am player {netId} on the local player");
        // GameManager.Singleton.localPlayer = this;
        _handButtons = GameManager.Singleton.localPlayerCardButtons;
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
        playerName = newName;
        name = newName;
    }

    // [Command]
    // public void CmdDrawHand(GameManager.SyncListCardDeck hand)
    // {
    //     handCards = hand;
    //     Debug.Log($"Player::CmdDrawHand: (Server) player {netId} drawing cards " +
    //               $"{string.Join(", ", handCards)}");
    // }
    //
    // [ClientRpc]
    // public void RpcDrawHand(GameManager.SyncListCardDeck hand)
    // {
    //     if (!isLocalPlayer)
    //     {
    //         Debug.Log($"Player::RpcDrawHand: Player {netId} is not localPlayer. Doing nothing.");
    //         return;
    //     }
    //
    //     handCards = hand;
    //     Debug.Log(
    //         $"Player::RpcDrawHand: I am client {netId}. isLocalPlayer={isLocalPlayer}, hasAuthority={hasAuthority}, localPlayerAuthority={localPlayerAuthority}." +
    //         $"Now drawing cards {string.Join(", ", handCards)}");
    //
    //     // we are on the client, so we will try to use the buttons that we accessed in OnStartLocalPlayer
    //     //Debug.Log($"We have {_handButtons.Count} buttons available");
    //
    //     for (int i = 0; i < hand.Count; i++)
    //     {
    //         _handButtons[i].image.sprite = PlayingCard.SpriteDict[handCards[i]];
    //     }
    // }

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
}