using Apt.Unity.Projection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;

[RequireComponent(typeof(ScreenShooter))]
[RequireComponent(typeof(InputHandlers))]
public class InputHandlers : TrackerBase
{
    [SerializeField]
    private ProjectionPlane projectionPlane;

    [SerializeField]
    private InputActionReference reset, togglech;

    [SerializeField]
    private Texture[] crosshairTextures;

    private OdysseyHubClient client;
    private ScreenShooter screenShooter;

    private bool showCrosshair = true;

    private Vector3 zero_translation = Vector3.zero;

    private float distance_offset = 1.0f; // todo this is the camera's initial local position, maybe retrieve it instead of relying on it to not be changed

    public AppConfig appConfig = new AppConfig();

    public class Player {
        public Radiosity.OdysseyHubClient.IDevice device;
        public Vector2 point = Vector2.zero;
    }

    public SlotMachine<Player> Players {
        get => players;
        private set => players = value;
    }

    private SlotMachine<Player> players = new();

    private void OnEnable()
    {
        reset.action.performed += PerformReset;
        togglech.action.performed += ToggleCrosshair;
    }

    private void OnDisable()
    {
        reset.action.performed -= PerformReset;
        togglech.action.performed -= ToggleCrosshair;
    }

    private void ToggleCrosshair(InputAction.CallbackContext obj) {
        showCrosshair = !showCrosshair;
    }

    public void PerformTransformAndPoint(Radiosity.OdysseyHubClient.IDevice device, PoseUtils.UnityPose pose, Vector2 point)
    {
        Player player = players.Find(p => p.device.Equals(device));
        if (player == null) {
            player = new Player();
            player.device = device;
            players.Allocate(player);
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
        players.Allocate(player);
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
            GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
            foreach (var (index, player) in players) {
                var i = index > crosshairTextures.Length ? crosshairTextures.Length - 1 : index;
                var texture = crosshairTextures[i];
                Vector2 screenPointNormal = player.point;
                Vector2 screenPoint = new Vector2(screenPointNormal.x * Screen.width, Screen.height - screenPointNormal.y * Screen.height);
                GUI.DrawTexture(new Rect(screenPoint.x - 52 / 2, Screen.height - screenPoint.y - 52 / 2, 52, 52), texture, ScaleMode.StretchToFill, true, 0);
            }
            GUI.EndGroup();
        }
    }
}
