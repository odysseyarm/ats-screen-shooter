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

        RebuildListView();
    }

    public void Refresh() {
        RebuildListView();
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

    private void RebuildListView() {
        var listView = ui.rootVisualElement.Query<ListView>("ZeroDeviceChoices").First();

        var items = new List<(int index, InputHandlers.Player item)>(inputHandlers.Players.ToList());

        // The "makeItem" function will be called as needed
        // when the ListView needs more items to render
        Func<VisualElement> makeItem = () => {
            var groupBox = new GroupBox();
            groupBox.style.flexDirection = FlexDirection.Row;
            groupBox.Add(new Label());
            {
                var btn = new Button();
                btn.text = "Zero";
                groupBox.Add(btn);
            }
            {
                var btn = new Button();
                btn.text = "Reset Zero";
                groupBox.Add(btn);
            }
            return groupBox;
        };

        // As the user scrolls through the list, the ListView object
        // will recycle elements created by the "makeItem"
        // and invoke the "bindItem" callback to associate
        // the element with the matching data item (specified as an index in the list)
        Action<VisualElement, int> bindItem = (e, i) => {
            // only vaguely understand why this guard is necessary
            if (i < 0 || i >= items.Count) {
                return;
            }
            var groupBox = e as GroupBox;
            groupBox.Query<Label>().First().text = string.Format("0x{0:X}", new ohc.uniffi.Device(items[i].item.device).Uuid());
            groupBox.Query<Button>().First().clicked += () => {
                client.client.Zero(items[i].item.device, new ohc.uniffi.Vector3f32(0, -0.0635f, 0), new ohc.uniffi.Vector2f32(0.5f, 0.5f));
            };
            groupBox.Query<Button>().AtIndex(1).clicked += () => {
                client.client.ResetZero(items[i].item.device);
            };
        };

        listView.makeItem = makeItem;
        listView.bindItem = bindItem;
        listView.itemsSource = items;
        listView.selectionType = SelectionType.None;

        listView.Rebuild();
    }
}
