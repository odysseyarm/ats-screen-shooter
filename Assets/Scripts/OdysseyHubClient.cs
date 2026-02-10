using System;
using System.Collections.Generic;
using System.Linq;
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

    private readonly Dictionary<string, Channel<(ushort?, ohc.uniffi.ClientException?)>> shotDelayChannels = new();

    // Track currently connected devices by UUID
    private HashSet<string> connectedDeviceKeys = new();

    // Store the last known devices so we can pass them to disconnect handler
    private Dictionary<string, ohc.uniffi.Device> knownDevices = new();

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

        // Get initial device list
        {
            var devices = await client.GetDeviceList();
            foreach (var device in devices) {
                var key = DeviceKey(device);
                connectedDeviceKeys.Add(key);
                knownDevices[key] = device;
                await inputHandlers.DeviceConnected(device);
                StartShotDelaySubscription(device);
            }
            screenGUI.Refresh();
        }

        // Start device list subscription (handles connect/disconnect)
        StartDeviceListSubscription();

        // Start event subscription (handles tracking, impact, zero results)
#nullable enable
        Channel<(ohc.uniffi.Event?, ohc.uniffi.ClientException?)> eventChannel = Channel.CreateUnbounded<(ohc.uniffi.Event?, ohc.uniffi.ClientException?)>();
#nullable disable
        await Task.Factory.StartNew(async () => await client.SubscribeEvents(eventChannel.Writer), TaskCreationOptions.LongRunning);

        Debug.Log("[OdysseyHubClient] Starting event loop");
        try {
            await foreach ((var @event, var err) in eventChannel.Reader.ReadAllAsync(cancellationTokenSource.Token)) {
                if (err != null) {
                    Debug.Log($"[OdysseyHubClient] Event error: {err.Message}");
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
            Debug.Log("[OdysseyHubClient] Event loop ended normally");
        } catch (System.OperationCanceledException) {
            Debug.Log("[OdysseyHubClient] Event loop cancelled");
        } catch (Exception e) {
            Debug.LogError($"[OdysseyHubClient] Event loop exception: {e}");
        }
    }

    private void StartDeviceListSubscription() {
#nullable enable
        Channel<(ohc.uniffi.Device[]?, ohc.uniffi.ClientException?)> deviceListChannel =
            Channel.CreateUnbounded<(ohc.uniffi.Device[]?, ohc.uniffi.ClientException?)>();
#nullable disable

        _ = Task.Factory.StartNew(async () => await client.SubscribeDeviceList(deviceListChannel.Writer), TaskCreationOptions.LongRunning);

        _ = Task.Run(async () => {
            Debug.Log("[OdysseyHubClient] Starting device list subscription");
            try {
                await foreach (var (deviceList, err) in deviceListChannel.Reader.ReadAllAsync(cancellationTokenSource.Token)) {
                    if (err != null) {
                        Debug.LogWarning($"[OdysseyHubClient] Device list stream error: {err.Message}");
                        break;
                    }
                    if (deviceList != null) {
                        Debug.Log($"[OdysseyHubClient] Device list update: {deviceList.Length} devices");
                        await HandleDeviceListUpdate(deviceList);
                    }
                }
                Debug.Log("[OdysseyHubClient] Device list subscription ended");
            } catch (OperationCanceledException) {
                Debug.Log("[OdysseyHubClient] Device list subscription cancelled");
            } catch (Exception e) {
                Debug.LogError($"[OdysseyHubClient] Device list subscription exception: {e}");
            }
        }, cancellationTokenSource.Token);
    }

    private async Task HandleDeviceListUpdate(ohc.uniffi.Device[] newDeviceList) {
        var newDeviceKeys = new HashSet<string>(newDeviceList.Select(DeviceKey));

        // Find disconnected devices (in old list but not in new)
        var disconnectedKeys = connectedDeviceKeys.Except(newDeviceKeys).ToList();

        // Find newly connected devices (in new list but not in old)
        var connectedDevices = newDeviceList.Where(d => !connectedDeviceKeys.Contains(DeviceKey(d))).ToList();

        // Handle disconnections
        foreach (var key in disconnectedKeys) {
            Debug.Log($"[OdysseyHubClient] Device disconnected: {key}");
            if (knownDevices.TryGetValue(key, out var disconnectedDevice)) {
                inputHandlers.DeviceDisconnected(disconnectedDevice);
                StopShotDelaySubscription(disconnectedDevice);
                knownDevices.Remove(key);
            }
        }

        // Handle new connections
        foreach (var device in connectedDevices) {
            var key = DeviceKey(device);
            Debug.Log($"[OdysseyHubClient] Device connected: {key}");
            knownDevices[key] = device;
            await inputHandlers.DeviceConnected(device);
            StartShotDelaySubscription(device);
        }

        // Update tracked devices
        connectedDeviceKeys = newDeviceKeys;

        // Refresh UI if there were any changes (must be on main thread)
        if (disconnectedKeys.Count > 0 || connectedDevices.Count > 0) {
            await PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().EnqueueAsync(() => {
                screenGUI.Refresh();
            });
        }
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
