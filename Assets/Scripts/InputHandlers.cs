using Apt.Unity.Projection;
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
    private InputActionReference reset, togglech, togglezerotarget, toggledarkmode;

    [SerializeField]
    private Canvas crosshairCanvas;

    [SerializeField]
    private Canvas zeroTargetCanvas;

    [SerializeField]
    private Texture2D[] crosshairTextures;

    private OdysseyHubClient client;
    private ScreenShooter screenShooter;
    private LightingModeManager lightingModeManager;
    private AppControls appControls;

    private bool showCrosshair = true;
    private bool showZeroTarget = false;

    private Vector3 zero_translation = Vector3.zero;

    private float distance_offset = 1.0f; // todo this is the camera's initial local position, maybe retrieve it instead of relying on it to not be changed

    public ATSAppConfig appConfig = new ATSAppConfig();

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
        // Enable all the actions
        reset.action.Enable();
        togglech.action.Enable();
        togglezerotarget.action.Enable();
        
        reset.action.performed += PerformReset;
        togglech.action.performed += ToggleCrosshairs;
        togglezerotarget.action.performed += ToggleZeroTarget;
    }

    private void OnDisable()
    {
        reset.action.performed -= PerformReset;
        togglech.action.performed -= ToggleCrosshairs;
        togglezerotarget.action.performed -= ToggleZeroTarget;
        
        // Disable all the actions
        reset.action.Disable();
        togglech.action.Disable();
        togglezerotarget.action.Disable();
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
    
    private void ToggleDarkMode(InputAction.CallbackContext obj)
    {
        Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] InputHandlers: ToggleDarkMode() triggered via input - Phase: {obj.phase}");
        
        if (lightingModeManager != null)
        {
            Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] InputHandlers: Calling LightingModeManager.ToggleLightingMode()");
            lightingModeManager.ToggleLightingMode();
        }
        else
        {
            Debug.LogWarning($"[{System.DateTime.Now:HH:mm:ss.fff}] InputHandlers: LightingModeManager is null, cannot toggle dark mode");
        }
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

        // Set the offset of the Odyssey (0,0) � camera origin � in Unity space
        zero_translation = f(new ohc.uniffi.Vector2f32(0f, 0f));
        zero_translation.z = distance_offset;
    }

    public async Task DeviceConnected(ohc.uniffi.DeviceRecord device) {
        var player = new Player();
        player.device = device;
        player.point = new Vector2(-1, -1);
        player.shotDelayMS = await client.client.GetShotDelay(device);
        player.trackingHistory = new ohc.uniffi.TrackingHistory(100);
        var index = players.Allocate(player);
        var i = index > crosshairTextures.Length ? crosshairTextures.Length - 1 : index;
        player.crosshair = new GameObject("CrosshairPlayer" + index).AddComponent<Image>();
        player.crosshair.transform.SetParent(crosshairCanvas.transform, false);
        player.crosshair.GetComponent<Image>().sprite = Sprite.Create(crosshairTextures[i], new Rect(0, 0, crosshairTextures[i].width, crosshairTextures[i].height), new Vector2(0.5f, 0.5f), 1.0f);
    }

    public void DeviceDisconnected(ohc.uniffi.DeviceRecord deviceR) {
        players.RemoveWhere(p => p.device == deviceR);
    }

    // Start is called before the first frame update
    void Start()
    {
        client = GetComponent<OdysseyHubClient>();
        screenShooter = GetComponent<ScreenShooter>();
        appConfig.Load();
        
        if (toggledarkmode == null || toggledarkmode.action == null)
        {
            appControls = new AppControls();
            appControls.Player.Enable();
            
            appControls.Player.ToggleDarkMode.performed += ToggleDarkMode;
        }
        
        lightingModeManager = FindObjectOfType<LightingModeManager>();
        if (lightingModeManager == null)
        {
            Debug.LogError($"[{System.DateTime.Now:HH:mm:ss.fff}] InputHandlers: LightingModeManager not found in scene! Please add it to the scene.");
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnDestroy()
    {
        if (appControls != null)
        {
            appControls.Player.Disable();
            appControls.Dispose();
        }
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
