using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    public string PlayerName { get; private set; }
    private TMP_InputField _playerNameInput;

    private List<Button> _handButtons;

    public NetworkList<PlayingCard.PlayingCardInfo> HandCards { get; private set; }

    #region General
    
    private void Awake()
    {
        HandCards = new NetworkList<PlayingCard.PlayingCardInfo>();
        HandCards.OnListChanged += OnSyncListHandCardsChanged;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log($"I am player {NetworkObjectId} on the server");
            GameManager.Singleton.AddPlayer(this);
        }

        if (IsLocalPlayer)
        {
            Debug.Log($"I am player {NetworkObjectId} on the local player");
            // GameManager.Singleton.localPlayer = this;
            _handButtons = GameManager.Singleton.localPlayerCardButtons;

            _handButtons.ForEach(button =>
                button.onClick.AddListener(() => OnClickCardButton(_handButtons.IndexOf(button)))
            );

            // set default name
            // (this is not reflected for the gameObject names on the clients)
            _playerNameInput = GameManager.Singleton.playerNameInput;
            var playerName = _playerNameInput.text.Length == 0
                ? $"Spieler {NetworkObjectId - 1}"
                : _playerNameInput.text;

            SetUserNameServerRpc(playerName);
            _playerNameInput.interactable = false;
        }
    }

    [ServerRpc]
    private void SetUserNameServerRpc(string newName)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                  $"Player {NetworkObjectId} was asked to change name");
        PlayerName = newName;
        name = newName;
        SetUserNameClientRpc(newName);
    }

    [ClientRpc]
    private void SetUserNameClientRpc(string newName)
    {
        if (!IsLocalPlayer)
        {
            return;
        }

        Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                  $"Player {NetworkObjectId} was asked to change name");
        PlayerName = newName;
        name = newName;
        _playerNameInput.text = PlayerName;
    }

    #endregion

    #region PreRound

    [ClientRpc]
    public void DisplayPreRoundButtonsClientRpc(GameManager.RoundMode currentRoundMode)
    {
        // are we on the client that is controlling this player? otherwise, stop here
        if (!IsLocalPlayer) return;

        Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                  $"client {NetworkObjectId} was asked to display the preRound buttons, {nameof(currentRoundMode)}={currentRoundMode}");

        // disable specific buttons depending on the current game mode
        switch (currentRoundMode)
        {
            case GameManager.RoundMode.Ramsch:
                // for ramsch we don't disable any button
                break;
            case GameManager.RoundMode.Sauspiel:
                GameManager.Singleton.preRoundSauspielDropdown.interactable = false;
                break;
            case GameManager.RoundMode.FarbSolo:
                GameManager.Singleton.preRoundSauspielDropdown.interactable = false;
                GameManager.Singleton.preRoundSoloDropdown.interactable = false;
                break;
            case GameManager.RoundMode.Wenz:
            case GameManager.RoundMode.FarbWenz:
                throw new InvalidOperationException(
                    $"player {NetworkObjectId} has no choices (round mode = Wenz) but was asked to make one");
            default:
                throw new ArgumentOutOfRangeException();
        }

        GameManager.Singleton.preRoundSauspielDropdown.onValueChanged.AddListener(OnPreRoundChooseSauSpiel);
        GameManager.Singleton.preRoundSoloDropdown.onValueChanged.AddListener(OnPreRoundChooseSolo);
        GameManager.Singleton.preRoundWenzDropdown.onValueChanged.AddListener(OnPreRoundChooseWenz);
        GameManager.Singleton.preRoundWeiterButton.onClick.AddListener(OnPreRoundChooseWeiter);

        // we do this last, so the user cannot accidentally click an invalid option before it is disabled
        GameManager.Singleton.preRoundButtonPanel.gameObject.SetActive(true);
    }

    private void OnPreRoundChooseSauSpiel(int sauChoiceInt)
    {
        if (!IsLocalPlayer) return;
        HidePreRoundButtons();

        Assert.IsTrue(0 <= sauChoiceInt && sauChoiceInt < 4);

        // do nothing if the player clicked the first dropdown item (which simply says "Sauspiel...")
        if (sauChoiceInt == 0) return;

        // otherwise, convert the clicked item index into a choice
        var sauSuit = (PlayingCard.Suit)sauChoiceInt - 1;

        SelectPreRoundChoiceServerRpc(GameManager.RoundMode.Sauspiel, sauSuit);
    }

    private void OnPreRoundChooseSolo(int soloChoiceInt)
    {
        if (!IsLocalPlayer) return;
        HidePreRoundButtons();

        // 0: nothing, 1-4: Suit Selection
        Assert.IsTrue(0 <= soloChoiceInt && soloChoiceInt <= 4);

        // do nothing if the player clicked the first dropdown item
        if (soloChoiceInt == 0) return;

        // otherwise, directly convert the clicked item index
        var additionalTrumps = (PlayingCard.Suit)soloChoiceInt - 1;

        SelectPreRoundChoiceServerRpc(GameManager.RoundMode.FarbSolo, additionalTrumps);
    }

    private void OnPreRoundChooseWenz(int wenzChoiceInt)
    {
        if (!IsLocalPlayer) return;
        HidePreRoundButtons();


        // 0: nothing, 1-4: FarbWenz Suit Selection, 5: Normal Wenz
        Assert.IsTrue(0 <= wenzChoiceInt && wenzChoiceInt <= 5);

        switch (wenzChoiceInt)
        {
            // do nothing if the player clicked the first dropdown item
            case 0:
                return;
            // Did the player choose a normal Wenz (i.e. without Suit)?
            case 5:
                // the Suit does not matter here, but we have to pass one
                SelectPreRoundChoiceServerRpc(GameManager.RoundMode.Wenz, PlayingCard.Suit.Blatt);
                break;
            default:
            {
                // otherwise, directly convert the clicked item index
                var additionalTrumps = (PlayingCard.Suit)wenzChoiceInt - 1;
                SelectPreRoundChoiceServerRpc(GameManager.RoundMode.FarbWenz, additionalTrumps);
                break;
            }
        }
    }

    private void OnPreRoundChooseWeiter()
    {
        if (!IsLocalPlayer) return;

        HidePreRoundButtons();

        /*
         * We use Ramsch as a stand-in for "no choice".
         * This is a bit "dirty", but it lets us use the RoundMode enum directly.
         * (The suit does not matter here, but we can't pass null)
         */
        SelectPreRoundChoiceServerRpc(GameManager.RoundMode.Ramsch, PlayingCard.Suit.Herz);
    }

    private void HidePreRoundButtons()
    {
        if (!IsLocalPlayer) return;

        // disable the button panel
        GameManager.Singleton.preRoundButtonPanel.gameObject.SetActive(false);

        // reset all the buttons to their active state (we disable the ones we don't want in RcpDisplayPreRoundButtons)
        GameManager.Singleton.preRoundSauspielDropdown.interactable = true;
        GameManager.Singleton.preRoundSoloDropdown.interactable = true;

        // remove all onclick listeners, so we don't get notified when another player clicks these buttons
        GameManager.Singleton.preRoundSauspielDropdown.onValueChanged.RemoveListener(OnPreRoundChooseSauSpiel);
        GameManager.Singleton.preRoundSoloDropdown.onValueChanged.RemoveListener(OnPreRoundChooseSolo);
        GameManager.Singleton.preRoundWenzDropdown.onValueChanged.RemoveListener(OnPreRoundChooseWenz);
        GameManager.Singleton.preRoundWeiterButton.onClick.RemoveListener(OnPreRoundChooseWeiter);
    }

    #endregion

    #region Round

    private void OnSyncListHandCardsChanged(NetworkListEvent<PlayingCard.PlayingCardInfo> changeEvent)
    {
        if (!IsLocalPlayer) return;

        switch (changeEvent.Type)
        {
            case NetworkListEvent<PlayingCard.PlayingCardInfo>.EventType.Add:
                var i = changeEvent.Index;
                Debug.Log($"OnSyncListHandCardsChanged: Player {NetworkObjectId} was notified of new card " +
                          $"at position {i}: {HandCards[i]}");
                _handButtons[i].image.sprite = PlayingCard.SpriteDict[HandCards[i]];
                _handButtons[i].gameObject.SetActive(true);
                break;
            case NetworkListEvent<PlayingCard.PlayingCardInfo>.EventType.Clear:
                Debug.Log($"OnSyncListHandCardsChanged: Player {NetworkObjectId} was notified " +
                          "of a cleared hand, changing all card to default image");
                foreach (var button in _handButtons)
                {
                    button.image.sprite = PlayingCard.DefaultCardSprite;
                    button.gameObject.SetActive(false); // this might not be necessary, but it doesn't hurt to do it
                }

                break;
            default:
                throw new InvalidOperationException($"unhandled event type: {changeEvent.Type}");
        }
    }
    
    private void OnClickCardButton(int index)
    {
        if (!IsLocalPlayer) throw new InvalidOperationException(); // sanity check
        
        // disable the button GameObject so we cannot play the card again
        _handButtons[index].gameObject.SetActive(false);

        // make ALL the buttons non-interactable because the player has had their turn
        _handButtons.ForEach(button => button.interactable = false);

        // tell the server to play the card
        PlayCardServerRpc(HandCards[index]);
    }

    [ServerRpc]
    // ReSharper disable once MemberCanBeMadeStatic.Local
    private void PlayCardServerRpc(PlayingCard.PlayingCardInfo handCard)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                  $"sending card {handCard} to the GameManager");
        GameManager.Singleton.HandlePlayCardServerRpc(handCard);
    }

    [ServerRpc]
    private void SelectPreRoundChoiceServerRpc(GameManager.RoundMode roundMode, PlayingCard.Suit roundSuit)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                  $"player {NetworkObjectId} sending roundMode {roundMode}, roundSuit {roundSuit} to the GameManager");
        GameManager.Singleton.HandlePreRoundChoiceServerRpc(NetworkObjectId, roundMode, roundSuit);
    }

    /// <summary>
    ///     enables the hand buttons for the local player. Disables the buttons for every other player
    /// </summary>
    [ClientRpc]
    public void StartTurnClientRpc()
    {
        if (!IsLocalPlayer) return;

        // enable the card buttons
        _handButtons.ForEach(button => button.interactable = true);
    }

    #endregion
}

public class SerializableString : INetworkSerializable
{
    public string value;

    public SerializableString()
    {
        value = "";
    }

    public SerializableString(string value)
    {
        this.value = value;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            serializer.GetFastBufferWriter().WriteValueSafe(value);
        }
        else
        {
            serializer.GetFastBufferReader().ReadValueSafe(out value);
        }
    }
}