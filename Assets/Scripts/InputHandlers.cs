using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;

[RequireComponent(typeof(ScreenShooter))]
[RequireComponent(typeof(InputHandlers))]
public class InputHandlers : MonoBehaviour
{
    [SerializeField]
    private InputActionReference reset, resetzero, zero, togglech;

    [SerializeField]
    private Texture[] crosshairTextures;

    private OdysseyHubClient client;
    private ScreenShooter screenShooter;

    private bool showCrosshair = true;

    private class Player {
        public Radiosity.OdysseyHubClient.IDevice device;
        public Vector2 point = Vector2.zero;
    }

    private SlotMachine<Player> players = new();

    private void OnEnable()
    {
        reset.action.performed += PerformReset;
        resetzero.action.performed += PerformResetZero;
        zero.action.performed += PerformZero;
        togglech.action.performed += ToggleCrosshair;
    }

    private void OnDisable()
    {
        reset.action.performed -= PerformReset;
        resetzero.action.performed -= PerformResetZero;
        zero.action.performed -= PerformZero;
        togglech.action.performed -= ToggleCrosshair;
    }

    private void ToggleCrosshair(InputAction.CallbackContext obj) {
        showCrosshair = !showCrosshair;
    }

    public void PerformPoint(Radiosity.OdysseyHubClient.IDevice device, Vector2 point)
    {
        Player player = players.Find(p => p.device.Equals(device));
        if (player == null) {
            player = new Player();
            player.device = device;
            players.Allocate(player);
        }
        player.point = point;
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

    private void PerformReset(InputAction.CallbackContext obj)
    {
        screenShooter.ClearBulletHoles();
    }

    private void PerformResetZero(InputAction.CallbackContext obj)
    {
        if (client.isConnected()) {
            foreach (var (_, player) in players) {
                if (player.device != null) {
                    client.client.ResetZero(client.handle, player.device);
                }
            }
        }
    }

    private void PerformZero(InputAction.CallbackContext obj)
    {
        if (client.isConnected()) {
            foreach (var (_, player) in players) {
                if (player.device != null) {
                    client.client.Zero(client.handle, player.device, new Radiosity.OdysseyHubClient.Vector3(0, -0.0635f, 0), new Radiosity.OdysseyHubClient.Vector2(0.5f, 0.5f));
                }
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        client = GetComponent<OdysseyHubClient>();
        screenShooter = GetComponent<ScreenShooter>();
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
                GUI.DrawTexture(new Rect(screenPoint.x - 16 / 2, Screen.height - screenPoint.y - 16 / 2, 16, 16), texture, ScaleMode.StretchToFill, true, 0);
            }
            GUI.EndGroup();
        }
    }
}
