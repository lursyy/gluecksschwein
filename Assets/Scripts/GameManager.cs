using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Singleton;
    public List<Player> players = new List<Player>();
    public List<Button> localPlayerCardButtons;
    public Button dealCardsButton;

    [SyncVar(hook = nameof(OnGameStateTextChanged))] [SerializeField]
    private string gameStateText;

    [SerializeField] private Text gameStateTextField;

    private List<PlayingCard.PlayingCardInfo> _cardDeck;

    public class SyncListPlayingCard : SyncListStruct<PlayingCard.PlayingCardInfo>
    {
    }

    private readonly SyncListPlayingCard _syncListCardDeck = new SyncListPlayingCard();
    [SerializeField] private List<GameObject> playedCardSlots;
    private readonly SyncListPlayingCard _playedCards = new SyncListPlayingCard();
    
    // Start is called before the first frame update
    private void Awake()
    {
        Singleton = this;
        _cardDeck = PlayingCard.InitializeCardDeck();
        _playedCards.Callback = OnCardPlayed;
    }

    public override void OnStartServer()
    {
        foreach (PlayingCard.PlayingCardInfo cardInfo in _cardDeck)
        {
            _syncListCardDeck.Add(cardInfo);
        }

        dealCardsButton.onClick.AddListener(StartRound);
        dealCardsButton.gameObject.SetActive(true);

        gameStateText = "Warte auf Spieler... (1)";
    }

    [Command]
    public void CmdPlayCard(PlayingCard.PlayingCardInfo cardInfo)
    {
        Debug.Log($"GameManager::CmdPlayCard: someone wants me (the server) to play {cardInfo}");

        // add the card to the played cards
        _playedCards.Add(cardInfo);
    }

    private void OnCardPlayed(SyncList<PlayingCard.PlayingCardInfo>.Operation op, int i)
    {
        Debug.Log($"GameManager::RpcPlayCard: the server notified me that {_playedCards[i]} was played");

        // put the correct image in the position of the just played card
        playedCardSlots[i].SetActive(true);
        playedCardSlots[i].GetComponent<Image>().sprite = PlayingCard.SpriteDict[_playedCards[i]];
    }

    [Server]
    public void AddPlayer(Player player)
    {
        Debug.Log($"GameManager::AddPlayer called, adding Player {player.netId}");
        players.Add(player);

        gameStateText = $"Warte auf Spieler... ({players.Count})";
        // TODO if (players.Count == 4) { EnterReadyState(); }
    }


    /// <summary>
    /// Starts a new round TODO what does "round" mean?
    /// </summary>
    private void StartRound()
    {
        gameStateText = "Runde geht los...";
        DealCards();
    }

    /// <summary>
    /// Gives out the cards from the deck: card 00-07 to player 1, card 08-15 to player 2, card 16-23 to player 3, card 24-31 to player 4
    /// </summary>
    [Server]
    public void DealCards()
    {
        int handedCards = 0;
        foreach (Player player in players)
        {
            Debug.Log($"GameManager::CmdDealCards: Player {player.netId} should get " +
                      $"cards {handedCards} to {handedCards + 7}");
            // this updates the player's cards on the server object, which then notifies his respective client object
            for (int i = handedCards; i < handedCards + 8; i++)
            {
                player.handCards.Add(_syncListCardDeck[i]);
            }

            handedCards += 8;
        }

        // localPlayer.UpdateButtons();
    }

    void OnGameStateTextChanged(string newText)
    {
        Debug.Log($"GameManager::{nameof(OnGameStateTextChanged)}: new text = \"{newText}\"");
        gameStateTextField.text = newText;
    }
}