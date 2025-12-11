# Procedural Ivy

A procedural ivy generator for Unity, forked from [Real Ivy 2 Procedural Ivy Generator](https://assetstore.unity.com/packages/tools/modeling/real-ivy-2-procedural-ivy-generator-181402) and cleaned up for modern Unity development.

## Overview

Procedural Ivy is a Unity package that allows you to generate realistic ivy and vines procedurally in both the editor and at runtime. Create organic, natural-looking vegetation that grows along surfaces with customizable parameters.

## Features

### Editor Tools
- **Interactive Editor Window** - Create and edit ivy directly in the Unity editor
- **Multiple Editing Modes**:
  - Add Leaves
  - Cut branches
  - Delete points
  - Move points
  - Optimize mesh
  - Paint mode
  - Refine growth
  - Shave branches
  - Smooth curves
- **Preset System** - Save and load ivy configurations
- **Real-time Preview** - See your ivy grow in real-time

### Runtime Features
- **Procedural Growth** - Generate ivy dynamically at runtime
- **Baked Mesh Support** - Pre-bake meshes for optimal performance
- **Configurable Parameters**:
  - Growth speed and lifetime
  - Branch probability and maximum branches
  - Distance to surface controls
  - Gravity and stiffness
  - Leaf placement and probability
  - Radius variation
  - UV mapping
- **Object Pooling** - Efficient runtime ivy generation with pooling support

### Technical Features
- Unity 6000.0+ compatible
- Lightmap UV generation support
- Customizable material support
- Layer mask collision detection
- Random seed support for reproducible results

## Installation

### Via Unity Package Manager (Git)

1. Open Unity Package Manager (Window > Package Manager)
2. Click the `+` button
3. Select "Add package from git URL"
4. Enter the repository URL or local path (https://github.com/Team-Crescendo-Games/ProceduralIvy.git)

### Manual Installation

1. Clone or download this repository
2. Place the `ProceduralIvy` folder in your Unity project's `Assets` folder
3. The package will be available in your project

## Quick Start

### Editor Usage

1. Open the Procedural Ivy window: `Window > Procedural Ivy`
2. Click "Create New Ivy" to start
3. Adjust parameters in the window
4. Use the editing tools to refine your ivy
5. Save as a preset for reuse

### Runtime Usage

1. Add an `IvyController` component to a GameObject
2. Configure `IvyParameters` and `RuntimeGrowthParameters`
3. Call `StartGrowth()` to begin procedural generation
4. Use `IvyCaster` for spawning multiple ivy instances with pooling

```csharp
// Example: Start ivy growth
IvyController ivy = GetComponent<IvyController>();
ivy.StartGrowth();
```

## Package Structure

```
ProceduralIvy/
├── Editor/          # Editor tools and windows
├── Runtime/         # Core runtime systems
│   ├── Baked/       # Baked mesh support
│   └── Procedural/  # Runtime procedural generation
├── Sample/          # Example scenes and prefabs
└── GuiSkins/        # Editor UI assets
```

## Requirements

- Unity 6000.0 or later
- Compatible with Windows, Mac, and Linux
- Compatible with URP only

## Credits

This project is a fork of [Real Ivy 2 Procedural Ivy Generator](https://assetstore.unity.com/packages/tools/modeling/real-ivy-2-procedural-ivy-generator-181402) by 3Dynamite, originally released on the Unity Asset Store.

**Fork Maintainer:** Team Crescendo (Ninghua Xu)

## License

See [LICENSE](LICENSE) file for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history and updates.
