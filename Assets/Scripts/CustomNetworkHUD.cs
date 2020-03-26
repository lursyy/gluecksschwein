using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 618

[RequireComponent(typeof(NetworkManager))]
public class CustomNetworkHUD : MonoBehaviour
{
    /// <summary>
    /// The NetworkManager associated with this HUD.
    /// </summary>
    private NetworkManager _manager;

    private NetworkTransform bla;

    /// <summary>
    /// The horizontal offset in pixels to draw the HUD runtime GUI at.
    /// </summary>
    [SerializeField] public int offsetX;

    /// <summary>
    /// The vertical offset in pixels to draw the HUD runtime GUI at.
    /// </summary>
    [SerializeField] public int offsetY;

    void Awake()
    {
        _manager = GetComponent<NetworkManager>();
        _manager.StartMatchMaker();
    }

    private void OnGUI()
    {
        int xpos = 30 + offsetX;
        int ypos = 30 + offsetY;
        const int spacing = 24;

        bool noConnection = (_manager.client == null || _manager.client.connection == null ||
                             _manager.client.connection.connectionId == -1);

        if (!_manager.IsClientConnected() && !NetworkServer.active && _manager.matchMaker == null)
        {
            if (noConnection)
            {
                if (GUI.Button(new Rect(xpos, ypos, 200, 20), "LAN Host(H)"))
                {
                    _manager.StartHost();
                }
                
                ypos += spacing;
            

                if (GUI.Button(new Rect(xpos, ypos, 105, 20), "LAN Client(C)"))
                {
                    _manager.StartClient();
                }

                _manager.networkAddress = GUI.TextField(new Rect(xpos + 100, ypos, 95, 20), _manager.networkAddress);
                ypos += spacing;
            }
            else
            {
                GUI.Label(new Rect(xpos, ypos, 200, 20),
                    "Connecting to " + _manager.networkAddress + ":" + _manager.networkPort + "..");
                ypos += spacing;


                if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Cancel Connection Attempt"))
                {
                    _manager.StopClient();
                }
            }
        }
        else
        {
            if (NetworkServer.active)
            {
                string serverMsg = $"Server running on {NetworkManager.singleton.matchInfo.address}";

                GUI.Label(new Rect(xpos, ypos, 300, 20), serverMsg);
                ypos += spacing;
            }

            if (_manager.IsClientConnected())
            {
                GUI.Label(new Rect(xpos, ypos, 300, 20),
                    $"Client connected to {NetworkManager.singleton.matchInfo.address}");
                ypos += spacing;
            }
        }

        if (_manager.IsClientConnected() && !ClientScene.ready)
        {
            if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Client Ready"))
            {
                ClientScene.Ready(_manager.client.connection);

                if (ClientScene.localPlayers.Count == 0)
                {
                    ClientScene.AddPlayer(0);
                }
            }

            ypos += spacing;
        }

        if (NetworkServer.active || _manager.IsClientConnected())
        {
            if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Stop (X)"))
            {
                _manager.StopHost();
                _manager.StartMatchMaker();
            }

            ypos += spacing;
        }

        if (!NetworkServer.active && !_manager.IsClientConnected() && noConnection)
        {
            ypos += 10;
            
            if (_manager.matchMaker == null)
            {
                if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Enable Match Maker (M)"))
                {
                    _manager.StartMatchMaker();
                }
            }
            else
            {
                if (_manager.matchInfo == null)
                {
                    if (_manager.matches == null)
                    {
                        // GUI.Label(new Rect(xpos, ypos, 100, 20), "Player Name:");
                        // _manager.matchName = GUI.TextField(new Rect(xpos + 100, ypos, 100, 20), "");
                        // ypos += spacing;

                        if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Create Internet Match"))
                        {
                            _manager.matchMaker.CreateMatch(_manager.matchName, _manager.matchSize, true, "", "", "", 0, 0,
                                _manager.OnMatchCreate);
                        }
                        ypos += spacing;

                        GUI.Label(new Rect(xpos, ypos, 100, 20), "Room Name:");
                        _manager.matchName = GUI.TextField(new Rect(xpos + 100, ypos, 100, 20), _manager.matchName);
                        ypos += spacing;

                        ypos += 10;

                        if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Find Internet Match"))
                        {
                            _manager.matchMaker.ListMatches(0, 20, "", false, 0, 0, _manager.OnMatchList);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < _manager.matches.Count; i++)
                        {
                            var match = _manager.matches[i];
                            if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Join Match:" + match.name))
                            {
                                _manager.matchName = match.name;
                                _manager.matchMaker.JoinMatch(match.networkId, "", "", "", 0, 0, _manager.OnMatchJoined);
                            }

                            ypos += spacing;
                        }

                        if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Back to Match Menu"))
                        {
                            _manager.matches = null;
                        }
                    }
                }
            }
        }
    }
}