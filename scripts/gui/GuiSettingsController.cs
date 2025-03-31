// TODO: Settings 

// using Godot;
// using System;

// public partial class SettingsController : UIController
// {
//     [Export] Slider MasterVolume;
//     [Export] Slider MouseSensitivity;
//     [Export] Button KeybindButton;

//     public override void _Ready()
//     {
//         MasterVolume.Value = AudioServer.GetBusVolumeDb(0);
//         MouseSensitivity.Value = PlayerController.MouseSensitivity;

//         MasterVolume.ValueChanged += OnMasterVolumeChanged;
//         MouseSensitivity.ValueChanged += OnMouseSensitivityChanged;
//     }

//     private void OnMasterVolumeChanged(double value)
//     {
//         AudioServer.SetBusVolumeDb(0, (float)value);
//     }

//     private void OnMouseSensitivityChanged(double value)
//     {
//         PlayerController.MouseSensitivity = (float)value;
//         SaveSystem.SaveSetting("mouse_sensitivity", (float)value);
//     }

//     private void OnKeybindPressed()
//     {
//         StartKeybindProcess();
//     }

//     private async void StartKeybindProcess()
//     {
//         KeybindButton.Text = "Press any key...";
//         var inputEvent = await InputManager.WaitForInput();

//         if (inputEvent is InputEventKey keyEvent)
//         {
//             InputMap.ActionEraseEvents("move_forward");
//             InputMap.ActionAddEvent("move_forward", keyEvent);
//             KeybindButton.Text = keyEvent.AsText();
//         }
//     }
// }