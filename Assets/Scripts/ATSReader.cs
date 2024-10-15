using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;

[RequireComponent(typeof(ATSCfg))]
[RequireComponent(typeof(ScreenShooter))]
public class ATSReader : MonoBehaviour
{
    [SerializeField]
    private InputActionReference point, pointNormal, shoot, reset, zero, rotate, togglech;

    [SerializeField]
    private InputActionReference sw1, sw2, sw3;

    [SerializeField]
    private Texture crosshairTexture;

    private ATSCfg atsCfg;
    private ScreenShooter screenShooter;

    private Vector2 normalPoint;

    private Vector2[] offset = new Vector2[3];
    private Vector2[] rotOffset = new Vector2[3];

    private int weaponIndex = 0;

    private bool showCrosshair = true;

    private void OnEnable()
    {
        point.action.performed += PerformPoint;
        pointNormal.action.performed += PerformPointNormal;
        shoot.action.performed += PerformShoot;
        reset.action.performed += PerformReset;
        zero.action.performed += PerformZero;
        sw1.action.performed += SwitchWeapon1;
        sw2.action.performed += SwitchWeapon2;
        sw3.action.performed += SwitchWeapon3;
        togglech.action.performed += ToggleCrosshair;
    }

    private void OnDisable()
    {
        point.action.performed -= PerformPoint;
        pointNormal.action.performed -= PerformPointNormal;
        shoot.action.performed -= PerformShoot;
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

    private void PerformPoint(InputAction.CallbackContext obj)
    {
        normalPoint = obj.action.ReadValue<Vector2>();
        normalPoint.x /= Screen.width;
        normalPoint.y = 1 - normalPoint.y/Screen.height;
    }

    private void PerformPointNormal(InputAction.CallbackContext obj)
    {
        Vector2 normal = obj.action.ReadValue<Vector2>();

        // Use FindHomography to find the transformation matrix and transform normalPoint by it
        Vector3[] src = new Vector3[4] {
            new Vector3(0, 1, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, -1, 0),
            new Vector3(1, 0, 0),
        };

        var view1 = atsCfg.settings.views[0];

        Vector3[] dst = new Vector3[4] {
            new Vector3(view1.Equals(null) ? 0 : view1.marker_bottom.position.x / 2047f, view1.Equals(null) ? 1 : view1.marker_bottom.position.y / 2047f, 0),
            new Vector3(view1.Equals(null) ? -1 : view1.marker_left.position.x / 2047f, view1.Equals(null) ? 0 : view1.marker_left.position.y / 2047f, 0),
            new Vector3(view1.Equals(null) ? 0 : view1.marker_top.position.x / 2047f, view1.Equals(null) ? -1 : view1.marker_top.position.y / 2047f, 0),
            new Vector3(view1.Equals(null) ? 1 : view1.marker_right.position.x / 2047f, view1.Equals(null) ? 0 : view1.marker_right.position.y / 2047f, 0),
        };

        var homography = TransformationHelpers.FindHomography(ref src, ref dst);
        Debug.Log("Normal before: " + normal);
        normal = homography.MultiplyPoint3x4(new Vector3(normal.x, normal.y, 0));
        Debug.Log("Normal after: " + normal);

        normalPoint = new Vector2((normal.x+1)/2, (normal.y+1)/2);

        // Debug.LogFormat("latest: {0}", normal);
    }

    private void PerformShoot(InputAction.CallbackContext obj)
    {
        var rotate_rad = rotate.action.ReadValue<float>() * Mathf.PI;
        var _offset = offset[weaponIndex];

        var screenOffset = Rotate(_offset, rotate_rad);

        Vector2 screenPointNormal = normalPoint + screenOffset;
        Vector2 screenPoint = new Vector2(screenPointNormal.x * Screen.width, Screen.height - screenPointNormal.y * Screen.height);
        screenShooter.CreateShot(screenPoint);
    }

    private void PerformReset(InputAction.CallbackContext obj)
    {
        screenShooter.ClearBulletHoles();
    }

    private Vector2 Rotate(Vector2 v, float rad) {
        return new Vector2(v.x*Mathf.Cos(rad)-v.y*Mathf.Sin(rad), v.x*Mathf.Sin(rad)+v.y*Mathf.Cos(rad));
    }

    private void PerformZero(InputAction.CallbackContext obj)
    {
        var rotate_rad = rotate.action.ReadValue<float>() * Mathf.PI;

        // offset[weaponIndex] = new Vector2(mag*Mathf.Cos(rotate_rad+Mathf.PI), mag*Mathf.Sin(rotate_rad+Mathf.PI));
        offset[weaponIndex] = Rotate(new Vector2(0.5f, 0.5f) - normalPoint, -rotate_rad);
    }

    // Start is called before the first frame update
    void Start()
    {
        screenShooter = GetComponent<ScreenShooter>();
        atsCfg = GetComponent<ATSCfg>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnGUI() {
        if (showCrosshair) {
            var rotate_rad = rotate.action.ReadValue<float>() * Mathf.PI;
            var _offset = offset[weaponIndex];

            var screenOffset = Rotate(_offset, rotate_rad);

            GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
            Vector2 screenPointNormal = normalPoint + screenOffset;
            Vector2 screenPoint = new Vector2(screenPointNormal.x * Screen.width, Screen.height - screenPointNormal.y * Screen.height);
            GUIUtility.RotateAroundPivot(rotate.action.ReadValue<float>() * 180, new Vector2(screenPoint.x, Screen.height - screenPoint.y));
            GUI.DrawTexture(new Rect(screenPoint.x - 96 / 2, Screen.height - screenPoint.y - 80 / 2, 96, 80), crosshairTexture, ScaleMode.StretchToFill, true, 0);
            GUI.EndGroup();
        }
        // GUI.DrawTexture(new Rect(screenPoint.x-48/2, Screen.height-screenPoint.y-40/2, 48, 40), crosshairTexture, ScaleMode.StretchToFill, true, 0);
    }
}
