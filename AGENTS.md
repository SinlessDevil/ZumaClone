# AGENTS.md

This file provides guidance to OpenAI Codex when working in this repository.

## Project

ZumaClone is a Unity remake of the arcade game Zuma, with spline-based ball chains, match-3 shooting, combos, and win/lose conditions. Use Unity **6000.5.3f1**, pinned in `ProjectSettings/ProjectVersion.txt`. The project uses **URP 17.5.0**, **Extenject** (Zenject APIs), **UniTask**, **DOTween**, and **uGUI**. Android is the current project target.

## Git workflow and commit messages

- `main` is the default branch. Check the current worktree and local and remote branches before branch operations. For the feature-cycle workflow, start from the requested base or `main` and use `feature/<short-name>`.
- Use a concise English commit title in the existing `<prefix>: <short description>` style. The history uses `feat:` for features, `add:` for additions, `edit:` for edits, `fix:` for fixes, and `upd:` for updates.

## Commands

- **Run the game:** open `Assets/Scenes/Initial.unity` and enter Play Mode. The enabled scenes in `ProjectSettings/EditorBuildSettings.asset` are `Initial`, `Menu`, and `Game`.
- **Live Editor:** from the project root, use `unity status --project-path .` and `unity command editor status --project-path .`. After relevant code changes, check compilation and the Console through the open Editor.
- **Tests:** use *Window → General → Test Runner* or *Tools → Tests Tool Window*. Tests live in `Assets/Tests/{EditMode,PlayMode}` under the `Tests.EditMode` and `Tests.PlayMode` asmdefs. For a closed checkout, use `Unity.exe -runTests -batchmode -projectPath . -testPlatform EditMode -testResults results.xml` or select `PlayMode`. Use `-testFilter` to target a fixture or test.
- **CI:** `build.cake` and `.github/workflows/unity-build.yml` contain the Android build workflow. Its container still references Unity 2023.2.1f1. Use the pinned project version for local verification.
- **Editor tools:** the *Tools* menu contains the Tests Tool Window, Localization Editor Window, AudioVibrationKit libraries, and Save System Kit tools.

## Architecture

- **Composition root:** `Assets/Code/Infrastructure/Installers/BootstrapInstaller.cs` uses Extenject to bind services, factories, and states. It loads the static data services during binding, caches audio during initialization, and enters `BootstrapState`. Put new bindings in the relevant existing `Bind*` method.
- **Game flow:** `IStateMachine<IGameState>` coordinates startup, menu navigation, and gameplay. Startup proceeds through `BootstrapState`, `LoadProgressState`, `BootstrapAnalyticState`, and `PreLoadGameState`. From there the game enters the menu or loads a level before `GameLoopState`. Keep transitions in the state classes under `Assets/Code/Infrastructure/StateMachine/Game/States`.
- **Scenes and assets:** `SceneLoader` calls `SceneManager.LoadSceneAsync` through `ICoroutineRunner`. The factories use `IInstantiator.InstantiatePrefabResource`. Keep Resources paths in `Code.Services.Factories.ResourcePath`. ScriptableObject configs live under `Assets/Resources/StaticData`.
- **Save system:** `UnifiedSaveLoadFacade` selects PlayerPrefs, JSON, or XML using `SaveMethodType`. Player progress is provided by `IPersistenceProgressService`.
- **Assemblies:** gameplay lives under `Assets/Code` in `Game`. Editor tools live under `Assets/Code/Editor` in `GameEditor`. Infrastructure also has a `Game.Editor` assembly. Keep dependencies aligned with adjacent code.

## Development rules

### Code style

- Use Allman braces, explicit local types, `_camelCase` private fields, and `readonly` where possible. Use expression-bodied members for straightforward one-liners.
- Order members as constants and static readonly fields, serialized and private fields, properties, events, constructors, public methods, then private methods.
- Let missing required bindings and inspector references surface. Guard null only where it represents an expected state.
- Put reusable extensions in `Assets/Code/Extensions` in a relevant `*Extensions` class.

### Services and UI

- Use interface-first services injected through Extenject and bind them alongside related services in `BootstrapInstaller`. Follow existing service and factory conventions, including the `Constructor(...)` initializer where that pattern is already used. Prefer UniTask for new async work and follow the established lifecycle when extending coroutine-based code.
- Use uGUI for game UI and UI Toolkit for editor windows. Follow the template's MVVM convention for new UI by keeping display behavior in the view and presentation logic in the view model. Preserve the existing `BaseWindow` and `StateWindow` patterns when extending existing windows.

### Rendering

- Use URP 17.5.0 Forward. The configured pipeline asset is `Assets/Resources/URP/URP Asset.asset`. Opaque texture, depth texture, and HDR are disabled. Main light shadows are enabled.
- Follow the surrounding shader conventions under `Assets/Shaders`. For new handwritten URP shaders, use HLSL and URP ShaderLibrary includes, keep material properties in `UnityPerMaterial` for SRP Batcher compatibility, use local shader keywords, and choose precision and shader targets appropriate to the feature. Consider overdraw and texture reads on mobile.
- Check the cost of extra render passes and full-screen effects in Unity when changing rendering. Use the Frame Debugger or device profiling when the task requires performance evidence.

### Changes and verification

- Keep changes scoped to the task. Use the Editor to modify serialized Unity assets such as scenes, prefabs, materials, and settings. Let Unity generate `.meta` files and preserve their GUIDs when moving or renaming assets.
- Run existing checks that cover the change. Add tests where the task calls for them or where an existing test cannot catch a specific regression. Report which checks ran and what still requires Editor, device, or visual verification.
