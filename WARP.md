# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

This is a Unity 6000.1.0f1 project called ATS Screen Shooter - an advanced training system for shooting range simulation with Odyssey hardware integration. It features qualification and reactive target modes with real-time tracking and hit detection.

## Common Development Commands

### Unity Editor Operations
```bash
# Open Unity project (Windows)
"C:\Program Files\Unity\Hub\Editor\6000.1.0f1\Editor\Unity.exe" -projectPath "E:\repos\ats-screen-shooter"

# Build for Windows standalone
"C:\Program Files\Unity\Hub\Editor\6000.1.0f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "E:\repos\ats-screen-shooter" -buildWindows64Player "Builds\ATS.exe"

# Run Unity tests
"C:\Program Files\Unity\Hub\Editor\6000.1.0f1\Editor\Unity.exe" -batchmode -nographics -runTests -projectPath "E:\repos\ats-screen-shooter" -testResults results.xml -testPlatform EditMode
```

### Visual Studio Project Management
```bash
# Regenerate Visual Studio solution files (if .csproj files are missing)
"C:\Program Files\Unity\Hub\Editor\6000.1.0f1\Editor\Unity.exe" -batchmode -quit -projectPath "E:\repos\ats-screen-shooter" -executeMethod UnityEditor.SyncVS.SyncSolution

# Build C# solution with MSBuild
msbuild ats-screen-shooter.sln /p:Configuration=Release

# Clean build artifacts
msbuild ats-screen-shooter.sln /t:Clean
```

### Version Control
```bash
# Git LFS is used for binary assets (images, models, audio)
git lfs pull

# Check LFS tracked files
git lfs ls-files
```

## Architecture Overview

### Core Systems

**Scene Architecture**
- Two main gameplay scenes: `IndoorRange.unity` and `OutdoorRange.unity`
- Mode switching between Qualification and Reactive target modes

**Hardware Integration Layer**
- `OdysseyHubClient`: Manages connection to Odyssey Hub hardware, handles real-time tracking events and shot detection
- `InputHandlers`: Processes tracking data from Odyssey devices (guns/helmets), manages multiple player crosshairs, and converts coordinate systems
- Uses Unity's Job System and async/await patterns for performance

**Target System**
- `AppModeManager`: Central mode controller switching between target types
- `QualificationModeManager`/`QualificationTargetController`: Static paper target simulation
- `ReactiveModeManager`/`ReactiveTarget`: Animated knockdown targets with physics responses
- `ScreenShooter`: Raycast-based hit detection and bullet hole instantiation

**Projection & Calibration**
- `ProjectionPlane`/`ProjectionPlaneCamera`: Screen-to-world coordinate mapping for projection systems
- `TrackerBase`: Base class for position tracking with 6DOF support
- Coordinate transformation from Odyssey (center-origin, y-up) to Unity (y-down)

**UI Management**
- Modal-based UI system (`ModalManager`, `ExitModalManager`)
- Distance and mode selection menus
- `ScreenGUI`: Real-time device status overlay

### Key Data Flow

1. **Tracking Pipeline**: Odyssey Hub → `OdysseyHubClient` → `InputHandlers` → `TrackerBase` → Camera transform
2. **Shot Detection**: Impact event → `TrackingHistory` lookup → Screen coordinate conversion → `ScreenShooter` raycast → Target hit/bullet hole
3. **Mode Switching**: Input (X key) or UI → `AppModeManager` → Enable/disable mode GameObjects → Update UI state

### External Dependencies

- **Radiosity.OdysseyHubClient** (v6.0.0-alpha): Native interop library for Odyssey hardware
- **Unity Input System**: Modern input handling with action maps
- **ProceduralWorlds Flora**: Environmental asset package (in .gitignore)
- **UnityMainThreadDispatcher**: Thread marshaling for async operations

### Configuration

- **AppConfig**: Persistent JSON configuration in `Application.persistentDataPath`
  - Stores helmet UUIDs for tracking
- **Shot Delay Calibration**: Per-device millisecond offset for shot synchronization
- **Projection Bounds**: Screen corner calibration stored in `ScreenInfo`

## Development Tips

- Input actions are defined in `Assets/Input Actions/AppControls.cs`
- Scenes are configured in `ProjectSettings/EditorBuildSettings.asset`
- The project uses Git LFS for large binary files - ensure LFS is installed
- Bullet holes are parented to hit objects for proper transform tracking
- Debug shots can be triggered with configured input action for testing without hardware
