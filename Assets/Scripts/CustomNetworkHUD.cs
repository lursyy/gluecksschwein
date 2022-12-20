using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Networking.Match;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NetworkManager))]
// ReSharper disable once InconsistentNaming
public class CustomNetworkHUD : MonoBehaviour
{
    [SerializeField] private GameObject lobbyPanel;

    [SerializeField] private Button hostGameButton;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private TMP_InputField hostGameInputField; // for the match name

    [SerializeField] private GameObject joinGamePanel;
    [SerializeField] private Transform joinGameButtonList;
    [SerializeField] private Button joinGameButtonPrefab;

    [SerializeField] private float refreshIntervalSeconds = 1f;
    private NetworkManager _manager;
    private float _nextRefreshTime;
    private bool _uiActive = true;

    private async void Awake()
    {
        lobbyPanel.SetActive(true);

        _manager = GetComponent<NetworkManager>();

        try
        {
            var playerId = await RelayManager.SetupRelay();
            Debug.Log($"Player auth done: {playerId}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        // _manager.StartMatchMaker();
        
        // empty match name is not allowed
        hostGameInputField.onValueChanged.AddListener(val => joinGameButton.interactable = val != "");
    }

    private void OnGUI()
    {
        // TODO remove?
        // if (Time.time >= _nextRefreshTime) RefreshMatches();
    }

    /// <summary>
    ///     Host a new game with a given name
    /// </summary>
    public async void CreateMatch()
    {
        // var matchName = hostGameInputField.text;
        var (ipv4Address, port, allocationIdBytes, connectionData, key, joinCode) =
            await RelayManager.AllocateRelayServerAndGetJoinCode(4);
        GameManager.Singleton.SetJoinCode(joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>()
            .SetRelayServerData(ipv4Address, port, allocationIdBytes, key, connectionData);
        NetworkManager.Singleton.StartHost();
        Debug.Log($"host started match '{joinCode}'");
        DisableUi();
        // _manager.matchMaker.CreateMatch(matchName, _manager.matchSize, true, "", "", "", 0, 0, OnMatchCreate);
    }

    private void OnMatchCreate(bool success, string extendedInfo, MatchInfo responseData)
    {
        // DisableUi();

        // forward the callback to the network manager
        // TODO
        // _manager.OnMatchCreate(success, extendedInfo, responseData);
    }

    private void DisableUi()
    {
        _uiActive = false;
        lobbyPanel.SetActive(false);
    }

    public async void JoinMatch()
    {
        var joinCode = hostGameInputField.text;
        
        var (ipv4Address, port, allocationIdBytes, connectionData, hostConnectionData, key) =
            await RelayManager.JoinRelayServerFromJoinCode(joinCode);

        // When connecting as a client to a Relay server, connectionData and hostConnectionData are different.
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(ipv4Address, port, allocationIdBytes,
            key, connectionData, hostConnectionData);

        NetworkManager.Singleton.StartClient();
        Debug.Log($"client joined match '{joinCode}'");
        DisableUi();
    }

    private void OnMatchJoined(bool success, string extendedInfo, MatchInfo responseData)
    {
        // DisableUi();
        // forward the callback to the network manager
        // TODO
        // _manager.OnMatchJoined(success, extendedInfo, responseData);
    }

    private void RefreshMatches()
    {
        _nextRefreshTime += refreshIntervalSeconds;

        // TODO
        // if (_manager.matchMaker == null) _manager.StartMatchMaker();

        if (!_uiActive) return;

        Debug.Log("Refreshing Match List...");
        // TODO
        // _manager.matchMaker.ListMatches(0, 10, "", true, 0, 0, OnMatchList);
    }

    private void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
    {
        // TODO
        // _manager.OnMatchList(success, extendedInfo, matchList);
        UpdateJoinMatchButtons();
    }

    private void UpdateJoinMatchButtons()
    {
        // var hasMatches = _manager.matches != null && _manager.matches.Count > 0;
        // joinGamePanel.SetActive(hasMatches);

        // clear the old buttons
        foreach (Transform button in joinGameButtonList) Destroy(button.gameObject);

        // if (!hasMatches) return;
        // foreach (var matchInfo in _manager.matches)
        // {
        //     var joinMatchButton = Instantiate(joinGameButtonPrefab, joinGameButtonList);
        //     joinMatchButton.GetComponentInChildren<TextMeshProUGUI>().text =
        //         $"{matchInfo.name} ({matchInfo.currentSize}P)";
        //     joinMatchButton.onClick.AddListener(() => JoinMatch(matchInfo));
        // }
    }
}