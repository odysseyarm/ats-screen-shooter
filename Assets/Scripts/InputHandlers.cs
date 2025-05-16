using Apt.Unity.Projection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;
using UnityEngine.UI;

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

    private bool showCrosshair = true;
    private bool showZeroTarget = false;

    private Vector3 zero_translation = Vector3.zero;

    private float distance_offset = 1.0f; // todo this is the camera's initial local position, maybe retrieve it instead of relying on it to not be changed

    public AppConfig appConfig = new AppConfig();

    public class Player {
        public Radiosity.OdysseyHubClient.IDevice device;
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

    public void PerformTransformAndPoint(Radiosity.OdysseyHubClient.IDevice device, PoseUtils.UnityPose pose, Vector2 point)
    {
        Player player = players.Find(p => p.device.Equals(device));
        if (player == null) {
            // DeviceConnected should handle this
            return;
        }
        player.point = point;

        if (appConfig.Data.helmet_uuids.Any(uuid => player.device.UUID.SequenceEqual(uuid)))
        {
            IsTracking = true;
            translation = zero_translation + pose.position;
        }
    }

    public void PerformShoot(Radiosity.OdysseyHubClient.IDevice device)
    {
        var player = players.Find(p => p.device.Equals(device));
        if (player == null) {
            // device was already connected before we connected our client and never performed point
            return;
        }
        Vector2 screenPointNormal = player.point;
        Vector2 screenPoint = new Vector2(screenPointNormal.x * Screen.width, Screen.height - screenPointNormal.y * Screen.height);
        screenShooter.CreateShot(screenPoint);
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

    public void HandleScreenZeroInfo(Radiosity.OdysseyHubClient.ScreenInfo screenInfo) {
        // Convert Odyssey coordinate to Unity: center-origin and y-down
        Vector2 f(Radiosity.OdysseyHubClient.Vector2 vec) {
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
        zero_translation = f(new Radiosity.OdysseyHubClient.Vector2(0f, 0f));
        zero_translation.z = distance_offset;
    }

    public void DeviceConnected(Radiosity.OdysseyHubClient.IDevice device) {
        var player = new Player();
        player.device = device;
        player.point = new Vector2(-1, -1);
        var index = players.Allocate(player);
        var i = index > crosshairTextures.Length ? crosshairTextures.Length - 1 : index;
        player.crosshair = new GameObject("CrosshairPlayer" + index).AddComponent<Image>();
        player.crosshair.transform.SetParent(crosshairCanvas.transform, false);
        player.crosshair.GetComponent<Image>().sprite = Sprite.Create(crosshairTextures[i], new Rect(0, 0, crosshairTextures[i].width, crosshairTextures[i].height), new Vector2(0.5f, 0.5f), 1.0f);
    }

    public void DeviceDisconnected(Radiosity.OdysseyHubClient.IDevice device) {
        players.RemoveWhere(p => p.device.Equals(device));
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
