using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;

[RequireComponent(typeof(ScreenShooter))]
public class InputHandlers : MonoBehaviour
{
    [SerializeField]
    private InputActionReference reset, zero, togglech;

    [SerializeField]
    private InputActionReference sw1, sw2, sw3;

    [SerializeField]
    private Texture crosshairTexture;

    private ScreenShooter screenShooter;

    private Vector2 point;

    private Vector2[] offset = new Vector2[3];

    private int weaponIndex = 0;

    private bool showCrosshair = true;

    private void OnEnable()
    {
        reset.action.performed += PerformReset;
        zero.action.performed += PerformZero;
        sw1.action.performed += SwitchWeapon1;
        sw2.action.performed += SwitchWeapon2;
        sw3.action.performed += SwitchWeapon3;
        togglech.action.performed += ToggleCrosshair;
    }

    private void OnDisable()
    {
        reset.action.performed -= PerformReset;
        zero.action.performed -= PerformZero;
        sw1.action.performed -= SwitchWeapon1;
        sw2.action.performed -= SwitchWeapon2;
        sw3.action.performed -= SwitchWeapon3;
        togglech.action.performed -= ToggleCrosshair;
    }

    private void SwitchWeapon1(InputAction.CallbackContext obj) {
        weaponIndex = 0;
    }

    private void SwitchWeapon2(InputAction.CallbackContext obj) {
        weaponIndex = 1;
    }

    private void SwitchWeapon3(InputAction.CallbackContext obj) {
        weaponIndex = 2;
    }

    private void ToggleCrosshair(InputAction.CallbackContext obj) {
        showCrosshair = !showCrosshair;
    }

    public void PerformPoint(Vector2 point)
    {
        this.point = point;
    }

    public void PerformShoot()
    {
        var _offset = offset[weaponIndex];

        Vector2 screenPointNormal = point + _offset;
        Vector2 screenPoint = new Vector2(screenPointNormal.x * Screen.width, Screen.height - screenPointNormal.y * Screen.height);
        screenShooter.CreateShot(screenPoint);
    }

    private void PerformReset(InputAction.CallbackContext obj)
    {
        screenShooter.ClearBulletHoles();
    }

    private void PerformZero(InputAction.CallbackContext obj)
    {
        // offset[weaponIndex] = new Vector2(0.5f, 0.5f) - point;
        // todo hub has the zero function now
    }

    // Start is called before the first frame update
    void Start()
    {
        screenShooter = GetComponent<ScreenShooter>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnGUI() {
        if (showCrosshair) {
            var _offset = offset[weaponIndex];

            GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
            Vector2 screenPointNormal = point + _offset;
            Vector2 screenPoint = new Vector2(screenPointNormal.x * Screen.width, Screen.height - screenPointNormal.y * Screen.height);
            GUI.DrawTexture(new Rect(screenPoint.x - 96 / 2, Screen.height - screenPoint.y - 80 / 2, 96, 80), crosshairTexture, ScaleMode.StretchToFill, true, 0);
            GUI.EndGroup();
        }
    }
}
