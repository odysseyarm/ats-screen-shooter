using Radiosity.OdysseyHubClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using ohc = Radiosity.OdysseyHubClient;

public class ScreenGUI : MonoBehaviour
{
    private UIDocument ui;

    [SerializeField]
    private InputHandlers inputHandlers;

    [SerializeField]
    private InputActionReference toggleUI;

    [SerializeField]
    private OdysseyHubClient client;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ui = GetComponent<UIDocument>();
        ui.rootVisualElement.Query<Button>("Reset").First().clicked += inputHandlers.PerformReset;
        ui.rootVisualElement.Query<Button>("ToggleCrosshairs").First().clicked += inputHandlers.ToggleCrosshairs;
        ui.rootVisualElement.Query<Button>("ToggleZeroTarget").First().clicked += inputHandlers.ToggleZeroTarget;
    }

    private void OnEnable()
    {
        toggleUI.action.performed += ToggleUI;
    }

    private void OnDisable()
    {
        toggleUI.action.performed -= ToggleUI;
    }

    private void ToggleUI(InputAction.CallbackContext obj) {
        ui.enabled = !ui.enabled;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
