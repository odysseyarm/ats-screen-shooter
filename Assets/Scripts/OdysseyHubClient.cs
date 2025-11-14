using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using UnityEngine;
using ohc = Radiosity.OdysseyHubClient;

[RequireComponent(typeof(InputHandlers))]
public class OdysseyHubClient : MonoBehaviour
{
    private InputHandlers inputHandlers;

    [SerializeField]
    private ScreenGUI screenGUI;

    private CancellationTokenSource cancellationTokenSource = new();

    public Radiosity.OdysseyHubClient.Client client = new();

    private readonly System.Collections.Generic.Dictionary<string, Channel<(ushort?, ohc.uniffi.ClientException?)>> shotDelayChannels = new();

    private bool _isConnected = false;

    private async void Start() {
        inputHandlers = GetComponent<InputHandlers>();

        while (true) {
            try {
                await client.Connect();
                break;
            } catch (ohc.uniffi.AnyhowException e) {
                Debug.Log($"Error connecting to Odyssey Hub:\n\n{e.AnyhowMessage()} \n\nTrying again in 1 second.");
                await Awaitable.WaitForSecondsAsync(1, cancellationTokenSource.Token);
            }
        }

        _isConnected = true;

        Debug.Log("Connected to Odyssey Hub");

        {
            var screen_info = await client.GetScreenInfoById(0);
            inputHandlers.HandleScreenZeroInfo(screen_info);
        }

        {
            var devices = await client.GetDeviceList();
            foreach (var device in devices) {
                await inputHandlers.DeviceConnected(device);
                StartShotDelaySubscription(device);
            }
            screenGUI.Refresh();
        }

#nullable enable
        Channel<(ohc.uniffi.Event?, ohc.uniffi.ClientException?)> eventChannel = Channel.CreateUnbounded<(ohc.uniffi.Event?, ohc.uniffi.ClientException?)>();
#nullable disable
        await Task.Factory.StartNew(async () => await client.SubscribeEvents(eventChannel.Writer), TaskCreationOptions.LongRunning);

        try {
            await foreach ((var @event, var err) in eventChannel.Reader.ReadAllAsync(cancellationTokenSource.Token)) {
                if (err != null) {
                    Debug.Log(err.Message);
                    break;
                }
                if (@event != null) {
                    switch (@event) {
                        case ohc.uniffi.Event.DeviceEvent deviceEvent:
                            switch (deviceEvent.v1.kind) {
                                case ohc.uniffi.DeviceEventKind.TrackingEvent tracking:
                                    inputHandlers.TrackingEventHandler(deviceEvent.v1.device, tracking.v1);
                                    break;
                                case ohc.uniffi.DeviceEventKind.ImpactEvent impact:
                                    inputHandlers.PerformShoot(deviceEvent.v1.device, impact.v1.timestamp);
                                    break;
                                case ohc.uniffi.DeviceEventKind.ConnectEvent _:
                                    _ = Task.Run(async () => { await inputHandlers.DeviceConnected(deviceEvent.v1.device); });
                                    StartShotDelaySubscription(deviceEvent.v1.device);
                                    break;
                                case ohc.uniffi.DeviceEventKind.DisconnectEvent _:
                                    inputHandlers.DeviceDisconnected(deviceEvent.v1.device);
                                    StopShotDelaySubscription(deviceEvent.v1.device);
                                    break;
                                case ohc.uniffi.DeviceEventKind.ZeroResult zeroResult:
                                    if (zeroResult.v1) {
                                        Debug.Log("Zero successful");
                                    } else {
                                        Debug.Log("Zero failed. Try again");
                                    }
                                    break;
                                default:
                                    break;
                            }
                            break;
                    }
                }
            }
        } catch (System.OperationCanceledException) { }
    }

    public bool isConnected() {
        return _isConnected;
    }

    private void OnDestroy() {
        cancellationTokenSource.Cancel();
        foreach (var channel in shotDelayChannels.Values) {
            channel.Writer.TryComplete();
        }
    }

    private void StartShotDelaySubscription(ohc.uniffi.Device device) {
        var key = DeviceKey(device);
        if (shotDelayChannels.ContainsKey(key)) {
            return;
        }

        var channel = Channel.CreateUnbounded<(ushort?, ohc.uniffi.ClientException?)>();
        shotDelayChannels[key] = channel;

        _ = client.SubscribeShotDelay(device, channel.Writer);

        _ = Task.Run(async () => {
            try {
                await foreach (var (delay, err) in channel.Reader.ReadAllAsync(cancellationTokenSource.Token)) {
                    if (err != null) {
                        Debug.LogWarning($"Shot delay stream error: {err.Message}");
                        break;
                    }
                    if (delay.HasValue) {
                        inputHandlers.ShotDelayChangedHandler(device, delay.Value);
                    }
                }
            } catch (OperationCanceledException) {
                // ignore cancellation during shutdown
            }
        }, cancellationTokenSource.Token);
    }

    private void StopShotDelaySubscription(ohc.uniffi.Device device) {
        var key = DeviceKey(device);
        if (shotDelayChannels.TryGetValue(key, out var channel)) {
            shotDelayChannels.Remove(key);
            channel.Writer.TryComplete();
        }
    }

    private static string DeviceKey(ohc.uniffi.Device device) {
        return BitConverter.ToString(device.uuid);
    }
}
