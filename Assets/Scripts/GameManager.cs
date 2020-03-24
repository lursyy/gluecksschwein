using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
#pragma warning disable 618

public class GameManager : NetworkBehaviour
{
    [SerializeField] private List<Button> playingCardButtons;
    private List<PlayingCard.PlayingCardInfo> _cardDeck;

    class PlayingCardSyncList : SyncListStruct<PlayingCard.PlayingCardInfo> { }
    private PlayingCardSyncList _playedCards;
    
    // private List<Player> _players;
    
    // Start is called before the first frame update
    void Start()
    {
        _playedCards = new PlayingCardSyncList();
        _cardDeck = PlayingCard.InitializeCardDeck();
        playingCardButtons.ForEach(button => button.onClick.AddListener(Shuffle));
        Shuffle();
    }

    private void Shuffle()
    {
        _cardDeck.Shuffle();
        for (int i = 0; i < playingCardButtons.Count; i++)
        {
            playingCardButtons[i].image.sprite = PlayingCard.SpriteDict[_cardDeck[i]];
        }
    }

    [Command]
    void CmdPlayCard(PlayingCard.PlayingCardInfo cardInfo)
    {
        _playedCards.Add(cardInfo);
    }

    // [Command]
    // void CmdRegisterPlayer(Player player)
    // {
    //     _players.Add(player);
    // }
}
