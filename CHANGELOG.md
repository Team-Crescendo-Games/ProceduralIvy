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
