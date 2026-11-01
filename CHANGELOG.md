# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-12-10

### Added
- Forked from [Real Ivy](https://assetstore.unity.com/packages/tools/modeling/real-ivy-2-procedural-ivy-generator-181402?srsltid=AfmBOoo9sIfXnK32mkfmRMLBY3wh1AxP0j_ELkbEOQY5l7KaAeZWiKJV) and cleaned up.

## [1.1.0] - 2025-12-13

### UI Toolkit
- Added new inspector UI for better control over the ivy generation. This new UI is using UI Toolkit.
### Performance
- Improved performance by optimizing the generation process using parallel mesh builder and throttling control.
- Removed unnecessary serializations to the scene file which made scenes huge.
### Bug Fixes
- Fixed many scene GUI and editor window bugs
- Made EditorMeshBuilder and EditorGrowthController context-based static classes, not serialized scriptable objects. This improves readibility and maintainability.
- Fixed many null reference exceptions with ivy containers.
- Changed how the preset system works by removing the need for GUI parameter serializer classes and maintaining preset versions.

## [1.1.1] - 2025-12-20
### Bug Fixes
- Fixed a bug where the project does not build.

## [1.1.2] - 2026-11-01
### Added
- Parameters that differ from the selected preset are highlighted in blue in the Procedural Ivy window.
- Right-click any parameter field and choose "Reset to Preset" to restore that value from the selected preset (undoable).
- Paint tool and Place Seed Mode show a surface-conforming point cloud cursor with a precise crosshair, instead of a fixed-size sphere. Its size follows the overlay's Radius field.
### Changed
- Editing a parameter in the Procedural Ivy window or the Inspector rebuilds the ivy mesh immediately (throttled by Mesh Rebuild Throttle). Undo/redo also rebuilds.
- All ivy parameters are clamped to valid ranges (e.g. Sides 1-32, Leaves Probabilities 0-1, Max >= Min for paired values) in the window, the Inspector, and preset assets.
- Leaves Probabilities always has one entry per leaf prefab; its size follows Leaves Prefabs.
- Removed the unused `leafCenter` field from leaf points and cleaned up unused imports.
- Removed Asset Store `AssetOrigin` metadata from `.meta` files.
### Bug Fixes
- Fixed the "Half Section" toggle not being bound to its parameter.
- Fixed the paint cursor staying at the last surface point after the mouse left all surfaces.
- Fixed growth throwing an index error when there were more leaf prefabs than leaf probabilities.
- Fixed a division by zero when Sides or Leaf Interval was set to 0.
