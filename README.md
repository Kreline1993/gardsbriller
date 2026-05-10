# Gårdsbriller

An XR / mixed-reality application for **Meta Quest** that lets the user walk through a real garden and see a *digital twin* of it overlaid on the world via passthrough. Plants and rows are visualised with extra information (health, growth, moisture, notes), and the user can switch between several interaction *modes*: getting an overview of the garden, picking ripe plants by species, or marking plants to protect while weeding.

The project is the active iteration of the *Gårdsbriller* ("farm glasses") project and lives in the [`XR_Lab/`](./XR_Lab) folder. The two other folders in this repo (`Simple digital twin/` and `XR_lab_multiset/`) are older prototypes kept for reference only.

<img width="2559" height="1439" alt="image" src="https://github.com/user-attachments/assets/a5f525d9-b247-4cd5-b782-0f2345f80901" />


## Contents

- [Gårdsbriller](#gårdsbriller)
  - [Contents](#contents)
  - [Features](#features)
  - [Technology](#technology)
  - [Getting started](#getting-started)
    - [Prerequisites](#prerequisites)
    - [Open the project](#open-the-project)
    - [Run in the Editor](#run-in-the-editor)
    - [Build to Quest](#build-to-quest)
    - [Configure MultiSet (optional)](#configure-multiset-optional)
  - [How the app is structured](#how-the-app-is-structured)
    - [Twin (`Twin/`)](#twin-twin)
    - [Mode system (`ModeSystem/`)](#mode-system-modesystem)
    - [UI (`UI/`)](#ui-ui)
    - [Visuals](#visuals)
  - [Plant data (JSON)](#plant-data-json)

## Features

- **Passthrough MR on Quest**: the digital twin is rendered on top of the real world using Meta's passthrough camera.
- **Spatial localization with MultiSet VPS**: the twin is anchored to the real garden using a pre-scanned MultiSet map, with a UI toast that reports localization progress, success, failure or retries.
- **Digital twin loaded from JSON**: rows, plants and their attributes (species, size, growth, health, moisture, notes, dates) are loaded at runtime from `Assets/StreamingAssets/PlantData.json`.
- **Mode system** with four modes (see `Assets/Scripts/ModeSystem/`):
  - **Default**: plain twin, no extra visualisation.
  - **Overview**: bounding boxes over rows that have low ground moisture, plus distance-based icons (LOD + clustering) for plants that are unhealthy, ripe (`growth >= 100`), or have a warning note.
  - **PlantPicking**: highlights mature plants of a chosen species (Tomato, Carrot, Radish) so they're easy to pick. Selection is proximity-based: an overlay prefab snaps onto plants as you walk close to them.
  - **Weeding**: marks all real plants as "protected" near the user, so they aren't pulled by mistake while weeding.
- **Per-plant info panel** that pops up next to a plant and shows its data, with tabs and a coloured header that changes if the plant has a warning note (`Assets/Scripts/UI/Info Panel Scripts/`).
- **Wrist-anchored mode menu** with a left-controller / hand-pinch toggle (`MenuToggle.cs`), and a panel that follows the wrist (`WristPanelFollow.cs`).

## Technology

| Piece | Version / package |
|---|---|
| Unity Editor | **6000.3.9f1** (Unity 6) |
| Render pipeline | BRP |
| XR | OpenXR (`com.unity.xr.openxr` 1.16.1) + Meta XR / Oculus SDK |
| VPS / localization | [MultiSet Quest SDK](https://github.com/MultiSet-AI/multiset-quest-sdk) |
| Passthrough camera | `Assets/PassthroughCameraApi` |
| Build target | Android (Quest), `minSdk` / `targetSdk` 34 |

## Getting started

### Prerequisites

- **Unity Hub** with Unity Editor **6000.3.9f1** installed, including the **Android Build Support** module (SDK / NDK / OpenJDK).
- A **Meta Quest** headset (Quest 2/3/Pro) with developer mode enabled, plus a USB-C cable for sideloading.
- Optional but recommended: **MultiSet** account + a scanned map of the physical space you want to localize against (see the [MultiSet docs](https://docs.multiset.ai/)). Without a map, everything except VPS localization still works.

### Open the project

1. Clone this repo.
2. In Unity Hub: **Add → Add project from disk → select the `XR_Lab/` folder**.
3. Open it. The first import takes a while because Unity needs to download packages and re-import all assets.
4. Open the scene **`Assets/Scenes/XR_Lab_Scene.unity`**.

### Run in the Editor

The scene works in the Editor for UI / data testing, but XR features (passthrough, hand tracking, VPS) require the headset. For quick iteration on the digital twin and modes, just press Play.

### Build to Quest

1. **File → Build Profiles** → switch to **Android**.
2. Connect the headset over USB and confirm the *Allow USB debugging* prompt inside the headset.
3. Pick the device under **Run Device** and click **Build And Run**. The resulting `.apk` lands in `XR_Lab/Builds/` (gitignored).

### Configure MultiSet (optional)

If you want VPS localization to work on your own location:

1. Open the scene and find the GameObject with `MapLocalizationManager` (from the MultiSet SDK).
2. Set your **Map ID / Map Code** there.
3. The `MultisetUpdater` component controls how often `LocalizeFrame()` is called (default every 300 s) and whether localization auto-starts. The `LocalizationToastController` shows in-progress / success / failure messages on a world-space canvas.

## How the app is structured


### Twin (`Twin/`)


- **`TwinDataLoader.cs`**: loads `PlantData.json` from `StreamingAssets`, both in the Editor and from inside the APK on Android (via `UnityWebRequest`).
- **`TwinGenerator.cs`**: at startup, instantiates one *interaction prefab* per plant at the correct local position and scale, parented to itself. Each instance gets a `PlantIdentity` component carrying the `plantId`. Also draws Gizmos for rows and plants in the Scene view.
- **`TwinDatabase.cs`**: singleton-style runtime index of all plants by `plantId`, with helpers like `GetPlantsBySpecies`, `GetPlantsWhere(predicate)`, `GetRowsWhere(...)`, and date comparison utilities. Mode states query the database instead of scanning the scene.

### Mode system (`ModeSystem/`)

A small **state machine** with one state per mode:

- `AppMode`: the enum: `Default`, `Overview`, `PlantPicking`, `Weeding`.
- `IModeState` / `ModeStateBase` — `Enter` / `Tick` / `Exit` lifecycle.
- `ModeContext`: bundles the references each state needs (`TwinDatabase`, `PlantVisualRegistry`).
- `ModeController`: owns one instance of each state, exposes `SwitchMode(...)`, `SwitchModeByName(...)`, `TogglePickingSpecies(...)`, `ClearPickingHighlights()`, and fires `ModeChanged` / `PickingSpeciesToggled` / `PickingSelectionCleared` events that UI scripts subscribe to.
- `OverviewRules.cs`: central place for the magic numbers used by Overview mode (low-moisture threshold, ripe-growth threshold, LOD distances, cluster radius, etc.).

`OverviewModeState` is the most involved: it spawns row-overlay boxes over rows below the moisture threshold, then registers up to three icon layers (bad health / warning notes / ripe plants) into a `PlantIconLODController` that swaps individual icons for clustered icons based on distance from `Camera.main`.

`PlantPickingModeState` filters mature plants and uses a `PickingProximityController` to spawn species-specific overlay prefabs as the user walks near selected plants.

`WeedingModeState` does the inverse: it tells the proximity controller to mark *all* plants as protected near the user.

### UI (`UI/`)

- `Menu/MenuToggle.cs`: opens / closes the wrist menu on the Quest **Start** button (or pinch-menu gesture).
- `Overview/`: `OverviewPanelBinder` + `OverviewPanelDataProvider` build the overview wrist panel (counts of unhealthy / ripe / warned plants per row), with section toggles.
- `Info Panel Scripts/InfoPanelSpawner.cs` + `InfoPanelBinder.cs`: spawn and populate the per-plant info panel.
- `LocalizationToastController.cs`: fades a toast in/out for the four VPS states. Hooked up to MultiSet's `MapLocalizationManager` UnityEvents.
- `ModeIndicator.cs`: shows the current mode name on a HUD label.

### Visuals

- `PlantHighlighting/PlantRuleOutlineController.cs` and `PlantEdgeWireframeRenderer.cs`: outline / wireframe effects for selected plants.

## Plant data (JSON)

The twin is driven entirely by `Assets/StreamingAssets/PlantData.json`. Schema (matches `TwinModels.cs`):

```json
{
  "rows": [
    {
      "rowId": "row_A",
      "location":      { "x": 0, "y": 0, "z": 0 },
      "size":          { "width": 0.75, "length": 4.033 },
      "groundMoisture": 20,
      "lastWateredDate": { "day": 3, "month": 3, "year": 2026 },
      "plants": [
        {
          "plantId": "plant_001",
          "plantName": "Tomato Plant 1",
          "species": "Tomato",
          "position":   { "x": 0.325, "y": 0.1, "z": 0.57 },
          "size":       { "diameter": 0.126, "height": 1.261 },
          "growth": 97,
          "plantedDate":          { "day": 25, "month": 12, "year": 2025 },
          "estimatedHarvestDate": { "day":  5, "month":  3, "year": 2026 },
          "lastWateredDate":      { "day":  3, "month":  3, "year": 2026 },
          "lastPesticide":        { "day": 25, "month":  1, "year": 2026 },
          "nextPesticide":        { "day": 25, "month":  4, "year": 2026 },
          "healthStatus": "ok",
          "notes": { "textNote": "...", "noteTag": "warning" }
        }
      ]
    }
  ]
}
```

Notes:

- `position` and `location` are in metres in the row's / world's local space.
- `healthStatus` triggers icons for the literal value `"bad"` (see `OverviewRules.BadHealthStatus`).
- `notes.noteTag == "warning"` triggers warning icons. Use `"notes": null` to mark a plant as having no notes.
- The species names recognised by Picking mode are `Tomato`, `Carrot`, `Radish` (case-insensitive). Add more by extending the species → tint and species → overlay dictionaries in `ModeController.Awake`.

---

For the older prototypes, see [`Simple digital twin/`](./Simple%20digital%20twin) and [`XR_lab_multiset/`](./XR_lab_multiset). They are kept for history and **not maintained**.
