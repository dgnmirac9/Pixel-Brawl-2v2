using UnityEngine;

public class RelayTestUI : MonoBehaviour
{
    private string joinCodeInput = "";

    private void OnGUI()
    {
        GUILayout.BeginArea(
            new Rect(20, 20, 350, 260),
            GUI.skin.box
        );

        GUILayout.Label("RELAY TEST");

        if (RelayManager.Instance == null)
        {
            GUILayout.Label("RelayManager bulunamadı.");
            GUILayout.EndArea();
            return;
        }

        RelayManager relay = RelayManager.Instance;

        GUILayout.Label(
            $"Durum: {relay.StatusMessage}"
        );

        GUILayout.Space(10);

        GUI.enabled = !relay.IsBusy;

        if (GUILayout.Button("Relay Host Başlat"))
        {
            StartRelayHost();
        }

        GUILayout.Space(10);
        GUILayout.Label("Join Code:");

        joinCodeInput = GUILayout.TextField(
            joinCodeInput,
            12
        );

        if (GUILayout.Button("Join Code ile Bağlan"))
        {
            StartRelayClient();
        }

        GUI.enabled = true;

        if (!string.IsNullOrEmpty(relay.CurrentJoinCode))
        {
            GUILayout.Space(15);
            GUILayout.Label("Host Join Code:");

            GUILayout.TextField(
                relay.CurrentJoinCode
            );
        }

        GUILayout.EndArea();
    }

    private async void StartRelayHost()
    {
        await RelayManager.Instance
            .StartHostWithRelayAsync();
    }

    private async void StartRelayClient()
    {
        await RelayManager.Instance
            .StartClientWithRelayAsync(joinCodeInput);
    }
}