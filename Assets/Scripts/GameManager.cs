﻿using System.Collections.Generic;
 using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Singleton;
    public List<Player> players = new List<Player>();
    public Player localPlayer;
    public List<Button> localPlayerCardButtons;
    
    // Server only stuff
    public Button dealCardsButton;

    private List<PlayingCard.PlayingCardInfo> _cardDeck;
    public class SyncListCardDeck : SyncListStruct<PlayingCard.PlayingCardInfo>
    {
    }
    public SyncListCardDeck syncListCardDeck = new SyncListCardDeck();
    
    public List<GameObject> playedCardSlots;

    // public class PlayingCardSyncList : SyncListStruct<PlayingCard.PlayingCardInfo> { }
    public List<PlayingCard.PlayingCardInfo> playedCards = new List<PlayingCard.PlayingCardInfo>();

    // private List<Player> _players;

    // Start is called before the first frame update
    private void Awake()
    {
        Singleton = this;
        //cardDeck.Callback = (op, index) => Debug.Log($"cardDeck changed at index {index} on netId {netId}");
        _cardDeck = PlayingCard.InitializeCardDeck();
    }

    public override void OnStartServer()
    {
        foreach (PlayingCard.PlayingCardInfo cardInfo in _cardDeck)
        {
            syncListCardDeck.Add(cardInfo);
        }
        
        dealCardsButton.onClick.AddListener(DealCards);
    }

    // private void Shuffle()
    // {
    //     cardDeck.Shuffle();
    // }

    // [Command]
    // void CmdPlayCard(PlayingCard.PlayingCardInfo cardInfo)
    // {
    //     Debug.Log($"GameManager::CmdPlayCard: someone wants me (the server) to play {cardInfo}");
    //
    //     // add the card to the played cards
    //     playedCards.Add(cardInfo);
    //
    //     // put the correct image in the position of the just played card
    //     playedCardSlots[playedCards.IndexOf(cardInfo)].GetComponent<Image>().sprite = PlayingCard.SpriteDict[cardInfo];
    //
    //     RpcPlayCard(cardInfo);
    // }
    //
    // [ClientRpc]
    // void RpcPlayCard(PlayingCard.PlayingCardInfo cardInfo)
    // {
    //     Debug.Log($"GameManager::RpcPlayCard: the server notified me of a new played card: {cardInfo}");
    //     playedCards.Add(cardInfo);
    //     playedCardSlots[playedCards.IndexOf(cardInfo)].GetComponent<Image>().sprite = PlayingCard.SpriteDict[cardInfo];
    // }

    [Server]
    public void AddPlayer(Player player)
    {
        Debug.Log($"GameManager::AddPlayer called, adding Player {player.netId}");
        players.Add(player);

        // TODO if (players.Count == 4) { EnterReadyState(); }
    }

    /// <summary>
    /// Gives out the cards from the deck:
    /// card 00-07 to player 1
    /// card 08-15 to player 2
    /// card 16-23 to player 3
    /// card 24-31 to player 4
    /// </summary>
    [Server]
    public void DealCards()
    {
        int handedCards = 0;
        foreach (Player player in players)
        {
            Debug.Log($"GameManager::CmdDealCards: Player {player.netId} should get " +
                      $"cards {handedCards} to {handedCards+7}");
            // this updates the player's cards on the server object, which then notifies his respective client object
            for (int i = handedCards; i < handedCards+8; i++)
            {
                player.handCards.Add(syncListCardDeck[i]);
            }
            handedCards += 8;
        }
        // localPlayer.UpdateButtons();
    }
}