using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(InputHandlers))]
public class OdysseyHubClient : MonoBehaviour
{
    private InputHandlers inputHandlers;

    private CancellationTokenSource cancellationTokenSource = new();

    private async void Start() {
        inputHandlers = GetComponent<InputHandlers>();

        Radiosity.OdysseyHubClient.Handle handle = new();
        Radiosity.OdysseyHubClient.Client client = new();

        Radiosity.OdysseyHubClient.ClientError clientError;

        while ((clientError = await client.Connect(handle)) != Radiosity.OdysseyHubClient.ClientError.None) {
            Debug.Log($"Error connecting to Odyssey Hub: {clientError}. Trying again in 1 second.");
            await Awaitable.WaitForSecondsAsync(1, cancellationTokenSource.Token);
        }

        Debug.Log("Connected to Odyssey Hub. Starting event stream");

        Channel<(Radiosity.OdysseyHubClient.IEvent, Radiosity.OdysseyHubClient.ClientError, string)> eventChannel = Channel.CreateUnbounded<(Radiosity.OdysseyHubClient.IEvent, Radiosity.OdysseyHubClient.ClientError, string)>();
        client.StartStream(handle, eventChannel.Writer);

        try {
            await foreach ((var @event, var err, var err_msg) in eventChannel.Reader.ReadAllAsync(cancellationTokenSource.Token)) {
                switch (@event) {
                    case Radiosity.OdysseyHubClient.DeviceEvent deviceEvent:
                        switch (deviceEvent.kind) {
                            case Radiosity.OdysseyHubClient.DeviceEvent.Tracking tracking:
                                inputHandlers.PerformPoint(new Vector2(tracking.aimpoint.x, tracking.aimpoint.y));
                                break;
                            case Radiosity.OdysseyHubClient.DeviceEvent.Impact impact:
                                inputHandlers.PerformShoot();
                                break;
                            case Radiosity.OdysseyHubClient.DeviceEvent.Connect _:
                                break;
                            case Radiosity.OdysseyHubClient.DeviceEvent.Disconnect _:
                                break;
                            default:
                                break;
                        }
                        break;
                }
            }
        } catch (System.OperationCanceledException) { }
    }

    private void OnDestroy() {
        cancellationTokenSource.Cancel();
    }
}
