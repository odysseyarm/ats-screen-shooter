using Apt.Unity.Projection;
using PimDeWitte.UnityMainThreadDispatcher;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;
using UnityEngine.UI;
using ohc = Radiosity.OdysseyHubClient;

[RequireComponent(typeof(ScreenShooter))]
[RequireComponent(typeof(InputHandlers))]
public class InputHandlers : TrackerBase
{
    [SerializeField]
    private ProjectionPlane projectionPlane;

    [SerializeField]
    private InputActionReference reset, togglech, togglezerotarget;

    [SerializeField]
    private Canvas crosshairCanvas;

    [SerializeField]
    private Canvas zeroTargetCanvas;

    [SerializeField]
    private Texture2D[] crosshairTextures;

    private OdysseyHubClient client;
    private ScreenShooter screenShooter;

    [SerializeField]
    private ScreenGUI screenGUI;

    private bool showCrosshair = true;
    private bool showZeroTarget = false;

    private Vector3 zero_translation = Vector3.zero;

    private float distance_offset = 1.0f; // todo this is the camera's initial local position, maybe retrieve it instead of relying on it to not be changed

    public AppConfig appConfig = new AppConfig();

    public class Player {
        public ohc.uniffi.DeviceRecord device;
        public ohc.uniffi.TrackingHistory trackingHistory;
        public ushort shotDelayMS;
        public Vector2 point = Vector2.zero;
        public Image crosshair;
    }

    public SlotMachine<Player> Players {
        get => players;
        private set => players = value;
    }

    private SlotMachine<Player> players = new();

    private void OnEnable()
    {
        reset.action.performed += PerformReset;
        togglech.action.performed += ToggleCrosshairs;
        togglezerotarget.action.performed += ToggleZeroTarget;
    }

    private void OnDisable()
    {
        reset.action.performed -= PerformReset;
        togglech.action.performed -= ToggleCrosshairs;
        togglezerotarget.action.performed -= ToggleZeroTarget;
    }

    public void ToggleCrosshairs()
    {
        showCrosshair = !showCrosshair;
    }

    private void ToggleCrosshairs(InputAction.CallbackContext obj) {
        ToggleCrosshairs();
    }

    public void ToggleZeroTarget()
    {
        showZeroTarget = !showZeroTarget;
    }

    private void ToggleZeroTarget(InputAction.CallbackContext obj) {
        ToggleZeroTarget();
    }

    public void TrackingEventHandler(ohc.uniffi.DeviceRecord deviceR, ohc.uniffi.TrackingEvent tracking)
    {
        var unityPose = PoseUtils.ConvertOdyPoseToUnity(tracking.pose);
        var point = new Vector2(tracking.aimpoint.x, tracking.aimpoint.y);
        Player player = players.Find(p => p.device == deviceR);
        if (player == null) {
            // DeviceConnected should handle this
            return;
        }
        player.point = point;
        player.trackingHistory.Push(tracking);

        var device = new ohc.uniffi.Device(deviceR);

        if (appConfig.Data.helmet_uuids.Any(uuid => uuid == device.Uuid()))
        {
            IsTracking = true;
            translation = zero_translation + unityPose.position;
        }
    }

    public void PerformShoot(ohc.uniffi.DeviceRecord device, uint timestamp)
    {
        var player = players.Find(p => p.device == device);
        if (player == null) {
            // device was already connected before we connected our client and never performed point
            return;
        }
        var tracking_point = player.trackingHistory.GetClosest(timestamp - (uint)player.shotDelayMS*1000).aimpoint;
        Vector2 screenPointNormal = new Vector2(tracking_point.x, tracking_point.y);
        Vector2 screenPoint = new Vector2(screenPointNormal.x * Screen.width, Screen.height - screenPointNormal.y * Screen.height);
        screenShooter.CreateShot(screenPoint);
    }

    public void ShotDelayChangedHandler(ohc.uniffi.DeviceRecord device, ushort delay_ms)
    {
        var player = players.Find(p => p.device == device);
        if (player == null) {
            // device was already connected before we connected our client and never performed point
            return;
        }
        player.shotDelayMS = delay_ms;
    }

    public void PerformReset()
    {
        screenShooter.ClearBulletHoles();
    }

    private void PerformReset(InputAction.CallbackContext obj)
    {
        PerformReset();
    }

    // private void PerformResetZero(InputAction.CallbackContext obj)
    // {
    //     if (client.isConnected()) {
    //         foreach (var (_, player) in players) {
    //             if (player.device != null) {
    //                 client.client.ResetZero(client.handle, player.device);
    //             }
    //         }
    //     }
    // }

    // private void PerformZero(InputAction.CallbackContext obj)
    // {
    //     if (client.isConnected()) {
    //         foreach (var (_, player) in players) {
    //             if (player.device != null) {
    //                 client.client.Zero(client.handle, player.device, new Radiosity.OdysseyHubClient.Vector3(0, -0.0635f, 0), new Radiosity.OdysseyHubClient.Vector2(0.5f, 0.5f));
    //             }
    //         }
    //     }
    // }

    public void HandleScreenZeroInfo(ohc.uniffi.ScreenInfo screenInfo) {
        // Convert Odyssey coordinate to Unity: center-origin and y-down
        Vector2 f(ohc.uniffi.Vector2f32 vec) {
            float centerX = (screenInfo.tr.x + screenInfo.tl.x) * 0.5f;
            float centerY = (screenInfo.tl.y + screenInfo.bl.y) * 0.5f;
            float unityX = vec.x - centerX;
            float unityY = -(vec.y - centerY);
            return new Vector2(unityX, unityY);
        }

        Vector2 tl = f(screenInfo.tl);
        Vector2 tr = f(screenInfo.tr);
        Vector2 bl = f(screenInfo.bl);
        Vector2 br = f(screenInfo.br);

        projectionPlane.SetLocalBounds(tl, tr, bl, br);

        // Set the offset of the Odyssey (0,0) — camera origin — in Unity space
        zero_translation = f(new ohc.uniffi.Vector2f32(0f, 0f));
        zero_translation.z = distance_offset;
    }

    public async Task DeviceConnected(ohc.uniffi.DeviceRecord device) {
        try {
            var shotDelayMS = await client.client.GetShotDelay(device);
            await UnityMainThreadDispatcher.Instance().EnqueueAsync(() => {
                var player = new Player();
                player.device = device;
                player.point = new Vector2(-1, -1);
                player.shotDelayMS = shotDelayMS;
                player.trackingHistory = new ohc.uniffi.TrackingHistory(100);
                var index = players.Allocate(player);
                var i = (index >= crosshairTextures.Length) ? crosshairTextures.Length - 1 : index;
                player.crosshair = new GameObject("CrosshairPlayer" + index).AddComponent<Image>();
                player.crosshair.transform.SetParent(crosshairCanvas.transform, false);
                player.crosshair.GetComponent<Image>().sprite = Sprite.Create(crosshairTextures[i], new Rect(0, 0, crosshairTextures[i].width, crosshairTextures[i].height), new Vector2(0.5f, 0.5f), 1.0f);
                screenGUI.Refresh();
            });
        } catch (Exception e) {
            Debug.LogError("Error: " + e.Message);
        }
    }

    public async void DeviceDisconnected(ohc.uniffi.DeviceRecord deviceR) {
        await UnityMainThreadDispatcher.Instance().EnqueueAsync(() => {
            Destroy(players.Find(p => p.device == deviceR).crosshair.gameObject);
            screenGUI.Refresh();
        });
        players.RemoveWhere(p => p.device == deviceR);
    }

    // Start is called before the first frame update
    void Start()
    {
        client = GetComponent<OdysseyHubClient>();
        screenShooter = GetComponent<ScreenShooter>();
        appConfig.Load();
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnGUI() {
        if (showCrosshair) {
            crosshairCanvas.enabled = true;
            foreach (var (index, player) in players) {
                Vector2 screenPointNormal = player.point;
                Vector2 screenPoint = new Vector2(screenPointNormal.x * Screen.width, Screen.height - screenPointNormal.y * Screen.height);
                player.crosshair.rectTransform.position = screenPoint;
            }
        } else {
            crosshairCanvas.enabled = false;
        }
        zeroTargetCanvas.enabled = showZeroTarget;
    }
}
