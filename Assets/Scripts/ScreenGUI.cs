using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class ScreenGUI : MonoBehaviour
{
    private UIDocument ui;

    [SerializeField]
    private InputHandlers inputHandlers;

    [SerializeField]
    private InputActionReference toggleUI;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ui = GetComponent<UIDocument>();
        ui.rootVisualElement.Query<Button>("Reset").First().clicked += inputHandlers.PerformReset;

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
            groupBox.Query<Label>().First().text = BitConverter.ToString(items[i].item.device.UUID).Replace("-", "");
            // groupBox.Query<Button>().First() = items[i];
        };

        listView.makeItem = makeItem;
        listView.bindItem = bindItem;
        listView.itemsSource = items;
        listView.selectionType = SelectionType.None;

        listView.Rebuild();
    }
}
