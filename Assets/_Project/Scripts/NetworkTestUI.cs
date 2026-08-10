using Unity.Netcode;
using UnityEngine;

public class NetworkTestUI : MonoBehaviour
{
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 220, 220));

        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            GUILayout.Label("NetworkManager bulunamadı!");
            GUILayout.EndArea();
            return;
        }

        if (!networkManager.IsClient && !networkManager.IsServer)
        {
            if (GUILayout.Button("Start Host"))
            {
                networkManager.StartHost();
            }

            if (GUILayout.Button("Start Client"))
            {
                networkManager.StartClient();
            }
        }
        else
        {
            string mode = networkManager.IsHost
                ? "Host"
                : "Client";

            GUILayout.Label("Mode: " + mode);
            GUILayout.Label(
                "Client ID: " + networkManager.LocalClientId
            );

            if (networkManager.IsServer)
            {
                GUILayout.Label(
                    "Connected Clients: " +
                    networkManager.ConnectedClientsIds.Count
                );
            }
        }

        GUILayout.EndArea();
    }
}