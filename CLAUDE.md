# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

2D Top-Down RPG in Unity 6000.0.43f1 featuring graph-based dialogue and quest systems, inventory, and character combat. The dialogue and quest systems share a unified graph execution framework.

## Build & Test

This is a Unity project — there are no CLI build commands. Open in Unity 6000.0.43f1. Tests run via Unity Test Runner (Window → General → Test Runner). The test framework package is `com.unity.test-framework` (1.4.6).

When working via UnityMCP, always call `read_console` after creating or modifying scripts to check for compilation errors before using new types.

## Architecture

### Core Graph Framework

Both dialogue and quest systems share this stack:

- **`GraphAsset`** (abstract `ScriptableObject`): Serialized graph data — `NodeData` (typeId, ports, fields, position), `EdgeData` (port-to-port connections), `Blackboard`.
- **`GraphRunner`** (base `MonoBehaviour`): Coroutine-driven walker. Clones the blackboard at start for per-run isolation, dispatches nodes to registered `IGraphNodeHandler` implementations, and handles built-in nodes (Start, End, Condition, SetVariable, Sequence, Selector, RunSubgraph).
- **`IGraphNodeHandler`**: Interface for custom node execution. `Execute(NodeData, GraphRunContext)` is a coroutine; call `ctx.Follow("PortName")` to advance or `ctx.End()` to stop.

### Dialogue System

- **`DialogueGraphAsset`** extends `GraphAsset`. Create via Assets → Create → Dialogue Graph.
- **`DialogueRunner`** extends `GraphRunner`, adds nodes: DialogueLine, ChoiceBranch, TriggerEvent, WaitForEvent.
- DialogueLine fires `OnDialogueLine(speaker, text, portrait)` and waits for `Advance()`.
- ChoiceBranch fires `OnChoicesPresented(List<string>)` and waits for `SelectChoice(index)`.
- Node types are discovered via `[DialogueNode(typeId, displayName, category)]` attribute on `IGraphNodeHandler` classes.

### Quest System

- **`QuestGraphAsset`** extends `GraphAsset`. Has a `Kind` property:
  - **Single**: Self-contained quest with Objective/Reward/CompleteQuest/FailQuest nodes.
  - **Chain**: Dependency graph of quest references (RunSubgraph nodes).
- **`QuestRunner`** extends `GraphRunner` for Single quests. Key API: `StartQuest()`, `CompleteObjective(guid)`, `FailObjective(guid)`, `AbortQuest()`. Key events: `OnObjectiveStarted/Completed/Failed`, `OnRewardGranted`, `OnQuestEnded(result)`.
- **`QuestChainRunner`** (independent `MonoBehaviour`) for Chain graphs. Maintains entry states (Locked → Available → Active → Completed/Failed). Topological frontier determines available quests. Key API: `StartChain()`, `StartQuest(nodeGuid)`, `NotifyExternalQuestCompleted/Failed()`. Fires `OnAvailableQuestsChanged` when the frontier changes.
- **`QuestAsset`** (simple `ScriptableObject`): flag-based quest without graph logic. Used as leaf nodes inside Chain graphs.
- Custom quest nodes use `[QuestNode(typeId, displayName, category)]` attribute. `QuestNodeRegistry.ForKind(kind)` filters the palette by graph type.

### Blackboard

- **`Blackboard`**: Variable registry. `Clone(sharedSource)` deep-clones for per-run isolation; Shared variables skip cloning and are written back to the asset (broadcast pattern).
- **`BlackboardVariable<T>`**: Typed container. Concrete sealed subclasses are required — the serialization system resolves `SerializedProperty` paths by concrete type name, so raw generics don't work. Supported: `Bool`, `Int`, `Float`, `String`, `Vector2`, `Vector3`, `Color`, `GameObject`, `Transform`, `Sprite`, `AudioClip`.
- Node fields can be inline (literal value) or linked (blackboard variable GUID resolved at runtime via `GraphRunContext`).

### Character & Combat

- **`Entity`**: Base class with `Health`, `Mana`, `Stamina` resources (all derive from `Resource`). Resources fire `onValueChanged` and `onResourceReachesZero`.
- **`PlayerController`** extends `Entity`: movement (WASD/analog), aiming (mouse/right stick), attack (LMB/trigger), interaction (E → `Interactor.InteractWithClosest()`).
- **Weapon hierarchy**: `Weapon` (base) → `Melee` (collision hit), `Projectile` (spawned physics entity). `ComboWeapon` + `Combo` + `ComboStep` for multi-hit sequences.
- **`Stat` / `StatEffect`**: Scaling system used by resources.

### Interaction System

- **`Interactable`**: Base for interactive objects. Registers/deregisters itself with the nearby `Interactor` via `OnTriggerEnter2D`/`OnTriggerExit2D`. `Interact()` fires `OnInteract`.
- **`Interactor`**: Player-side component. `InteractWithClosest()` calls the nearest interactable.

### Inventory System

- **`InventoryManager`**: Singleton. Loads item prefabs from `Resources/Items/`. `GetItem(id)` retrieves by ID.
- **`InventoryGrid`**: 2D grid storage with shape-based item placement and drag-drop. `InventoryGridEditor` generates grid UI from layout.
- **`InventoryList`**: Linear item list with `InventoryListUI`.
- **`ItemShape`**: 2D occupancy pattern (list of grid coordinates) for irregular item shapes.

## Key Execution Flows

**Dialogue**: Interactor → `Interactable.Interact()` → `DialogueRunner.StartDialogue()` → walk nodes → `OnDialogueLine` → UI waits → `Advance()` → continue → End.

**Single Quest**: `QuestRunner.StartQuest()` → Objective node fires `OnObjectiveStarted` → game logic detects condition → `CompleteObjective(guid)` → Reward node fires `OnRewardGranted` → CompleteQuest terminal → `OnQuestEnded`.

**Quest Chain**: `QuestChainRunner.StartChain()` → compute frontier → `OnAvailableQuestsChanged` → player picks quest → `StartQuest(guid)` → spawns child `QuestRunner` (or calls `QuestAsset.Start()`) → on end → `RecomputeFrontier()` → repeat until chain done.

## Adding Custom Nodes

```csharp
[DialogueNode("my_node", "My Node", "Custom")]  // or [QuestNode(...)]
public class MyNodeHandler : IGraphNodeHandler
{
    public string NodeTypeId => "my_node";

    public IEnumerator Execute(NodeData node, GraphRunContext ctx)
    {
        // work...
        ctx.Follow("Out");
        yield break;
    }
}
```

For scene-driven nodes (e.g. quest goals that need `MonoBehaviour`), extend `GraphNodeBehaviour` instead.

## Important Constraints

- Blackboard variable concrete subclasses must be **sealed** and non-generic for `SerializedProperty` resolution to work.
- `RunSubgraph` creates a child `GameObject` with a new runner; parent handlers are forwarded to the child runner.
- The Quest node palette filters by `QuestKind` via `QuestNodeRegistry.ForKind(kind)` — Single-only nodes (Objective, Reward, CompleteQuest, FailQuest) do not appear in Chain graphs.
- `QuestChainRunner` entry states are not persisted across sessions by default — persistence must be added separately if needed.
