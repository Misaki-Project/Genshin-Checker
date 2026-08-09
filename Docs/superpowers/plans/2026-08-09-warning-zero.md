# Warning-Zero Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce the Genshin Checker Release build from the observed 422 warnings to zero without changing API payload semantics or generated resource files.

**Architecture:** Use the offline verification foundation as a safety net, then fix warnings by category: generated resource naming, serializer-owned DTO initialization, and hand-written nullable/deprecation issues. Suppress only CS8981 with a documented project-level exception because the six lowercase resource types are generated from compatibility-sensitive `.resx` names; all other warnings are fixed in source. Enable Release warnings-as-errors only after the clean build is proven.

**Tech Stack:** .NET 8 WPF, C# 12 nullable reference types, Newtonsoft.Json DTOs, xUnit offline tests, MSBuild.

## Global Constraints

- Execute `Docs/superpowers/plans/2026-08-09-offline-debug-foundation.md` first.
- Target `net8.0-windows10.0.22621.0` on Windows.
- Do not edit any generated `*.Designer.cs` file directly.
- Preserve JSON property names, public type names, resource manifest names, and existing API response formats.
- Use `string.Empty` only when an absent string is safely equivalent to empty; use `null!` for serializer-required complex objects whose absence must still be detected at the API boundary.
- Use an empty collection only when callers already treat a missing collection as empty; otherwise keep the property nullable and validate it.
- Add a characterization test before any null-handling change that can alter runtime behavior.
- Finish with a clean, non-incremental Release build and `pwsh -File scripts/verify.ps1`.

---

## File Structure

### Modify: generated-name policy

- `Genshin Checker/Genshin Checker.csproj` — document and suppress only CS8981 for generated lowercase resource types.

### Modify: serializer-owned Model warnings

- `Genshin Checker/Model/GitHub/latest.cs`
- `Genshin Checker/Model/HoYoLab/CalculatorComputeBatchGet.cs`
- `Genshin Checker/Model/HoYoLab/CharacterDetail.cs`
- `Genshin Checker/Model/HoYoLab/CharacterDetailResult.cs`
- `Genshin Checker/Model/HoYoLab/HardChallenge.cs`
- `Genshin Checker/Model/HoYoLab/MainMaterial.cs`
- `Genshin Checker/Model/HoYoLab/StellarJourney.cs`

### Modify: hand-written warnings

- `Genshin Checker/MainTray.cs`
- `Genshin Checker/InternalTools/MusicInfoSetter.cs`
- `Genshin Checker/GUI/UserInterface/CentralPanel/Component/InfoDisplay.xaml.cs`
- `Genshin Checker/GUI/BrowserApp/Announcement.cs`
- `Genshin Checker/GUI/Window/WebApp/Announcement.cs`
- `Genshin Checker/GUI/old/CharacterCalculator.cs`
- `Genshin Checker/GUI/old/SettingWindow.cs`
- `Genshin Checker/GUI/old/SpiralAbyss.cs`
- `Genshin Checker/GUI/old/TravelersDiaryDetailList.cs`

### Create tests

- `tests/GenshinChecker.Tests/Model/ModelDeserializationTests.cs`
- `tests/GenshinChecker.Tests/Core/NullBehaviorTests.cs`

---

### Task 1: Record the Warning Inventory and Handle Generated Resource Names

**Files:**
- Modify: `Genshin Checker/Genshin Checker.csproj`

**Interfaces:**
- Consumes: compiler warning CS8981 from six generated resource classes: `font`, `icon`, `namecard`, `css`, `html`, and `javascript`.
- Produces: documented CS8981 suppression; no suppression for nullable, obsolete, hiding, duplicate-using, or unused-field warnings.

- [ ] **Step 1: Reproduce and group the baseline**

Run:

```powershell
dotnet build 'Genshin Checker.sln' --configuration Release --no-restore --no-incremental 2>&1 |
  Select-String -Pattern 'warning CS[0-9]+' |
  ForEach-Object { if ($_.Line -match 'warning (CS[0-9]+)') { $Matches[1] } } |
  Group-Object | Sort-Object Count -Descending
```

Expected baseline categories, counting WPF temporary and final compilation duplicates: CS8618, CS8602, CS8981, CS0108, CS8604, CS0105, CS0169, and CS0618.

- [ ] **Step 2: Prove all CS8981 sources are generated resources**

Run:

```powershell
dotnet build 'Genshin Checker.sln' --configuration Release --no-restore --no-incremental 2>&1 |
  Select-String -Pattern 'warning CS8981'
```

Expected files only:

```text
Resource/font.Designer.cs
Resource/icon.Designer.cs
Resource/namecard.Designer.cs
Resource/WebStatic/css.Designer.cs
Resource/WebStatic/html.Designer.cs
Resource/WebStatic/javascript.Designer.cs
```

- [ ] **Step 3: Add the narrow documented suppression**

Add to the main project property group:

```xml
<!-- These six lowercase types are generated from compatibility-sensitive .resx names.
     Do not hand-edit generated Designer.cs files; handwritten lowercase type names remain prohibited by review. -->
<NoWarn>$(NoWarn);CS8981</NoWarn>
```

- [ ] **Step 4: Verify CS8981 disappears and no other code is suppressed**

Run the non-incremental Release build and confirm no CS8981 line is present while CS8618 and the hand-written warnings remain visible.

- [ ] **Step 5: Commit the generated-code warning policy**

```powershell
git add -- 'Genshin Checker/Genshin Checker.csproj'
git commit -m "build: document generated resource warning policy"
```

---

### Task 2: Characterize and Initialize API Models

**Files:**
- Create: `tests/GenshinChecker.Tests/Model/ModelDeserializationTests.cs`
- Modify: `Genshin Checker/Model/GitHub/latest.cs`
- Modify: `Genshin Checker/Model/HoYoLab/CalculatorComputeBatchGet.cs`
- Modify: `Genshin Checker/Model/HoYoLab/CharacterDetail.cs`
- Modify: `Genshin Checker/Model/HoYoLab/CharacterDetailResult.cs`
- Modify: `Genshin Checker/Model/HoYoLab/HardChallenge.cs`
- Modify: `Genshin Checker/Model/HoYoLab/MainMaterial.cs`
- Modify: `Genshin Checker/Model/HoYoLab/StellarJourney.cs`

**Interfaces:**
- Consumes: Newtonsoft.Json `[JsonProperty]` mappings in the seven warning-producing Model files.
- Produces: DTOs with explicit initialization and unchanged serialized property names.

- [ ] **Step 1: Add representative deserialization tests before mechanical edits**

Create one theory covering the seven warning-producing Model files. It must deserialize a minimal response and reserialize it to confirm the original JSON property name remains:

```csharp
public static TheoryData<Type, string, string> ModelCases => new()
{
    { typeof(Genshin_Checker.Model.GitHub.Latest.Root), "{\"tag_name\":\"v1\"}", "\"tag_name\"" },
    { typeof(Genshin_Checker.Model.HoYoLab.CalculatorComputeBatchGet.Root), "{\"retcode\":0,\"message\":\"OK\",\"data\":{}}", "\"retcode\"" },
    { typeof(Genshin_Checker.Model.HoYoLab.CharacterDetail.Root), "{\"retcode\":0,\"message\":\"OK\",\"data\":{}}", "\"retcode\"" },
    { typeof(Genshin_Checker.Model.HoYoLab.CharacterDetailResult.Root), "{\"retcode\":0,\"message\":\"OK\",\"data\":{}}", "\"retcode\"" },
    { typeof(Genshin_Checker.Model.HoYoLab.HardChallenge.Root), "{\"retcode\":0,\"message\":\"OK\",\"data\":{}}", "\"retcode\"" },
    { typeof(Genshin_Checker.Model.HoYoLab.MainMaterial.Root), "{\"retcode\":0,\"message\":\"OK\",\"data\":{}}", "\"retcode\"" },
    { typeof(Genshin_Checker.Model.HoYoLab.StellarJourney.Root), "{\"retcode\":0,\"message\":\"OK\",\"data\":{}}", "\"retcode\"" },
};

[Theory]
[MemberData(nameof(ModelCases))]
public void Root_DeserializesAndPreservesJsonNames(Type type, string json, string expectedProperty)
{
    var root = JsonConvert.DeserializeObject(json, type);
    Assert.NotNull(root);
    Assert.Contains(expectedProperty, JsonConvert.SerializeObject(root));
}
```

- [ ] **Step 2: Run the characterization tests**

Run:

```powershell
dotnet test 'tests\GenshinChecker.Tests\GenshinChecker.Tests.csproj' --filter FullyQualifiedName~ModelDeserializationTests
```

Expected: tests pass before warning edits.

- [ ] **Step 3: Apply explicit initialization rules to every CS8618 property in the seven files**

Use these exact transformations according to the JSON contract:

```csharp
public string name { get; set; } = string.Empty;
public List<Item> items { get; set; } = [];
public Dictionary<string, Item> lookup { get; set; } = [];
public Item[] entries { get; set; } = [];
public RequiredNestedObject data { get; set; } = null!;
public OptionalNestedObject? optional_data { get; set; }
```

Do not add `required`: Newtonsoft.Json does not enforce C# required-member construction. Do not remove or rename `[JsonProperty]` attributes. For every `null!`, verify that a parser or caller already rejects missing data; if it does not, add an assertion at that API boundary in the same file change.

- [ ] **Step 4: Run Model tests and inspect remaining CS8618 files**

Run:

```powershell
dotnet test 'tests\GenshinChecker.Tests\GenshinChecker.Tests.csproj' --filter FullyQualifiedName~ModelDeserializationTests
dotnet build 'Genshin Checker.sln' --configuration Release --no-restore --no-incremental 2>&1 | Select-String -Pattern 'warning CS8618'
```

Expected: Model characterization tests pass. The only remaining CS8618 may be `InfoDisplay.PropertyChanged`, which is handled in Task 3.

- [ ] **Step 5: Commit the Model initialization cleanup**

```powershell
git add -- 'Genshin Checker/Model/GitHub/latest.cs' 'Genshin Checker/Model/HoYoLab' 'tests/GenshinChecker.Tests/Model/ModelDeserializationTests.cs'
git commit -m "fix: initialize deserialized API models"
```

Review the staged diff before committing to ensure only the seven listed HoYoLab files are staged from that directory.

---

### Task 3: Fix Component and Simple Hand-Written Warnings

**Files:**
- Modify: `Genshin Checker/GUI/UserInterface/CentralPanel/Component/InfoDisplay.xaml.cs`
- Modify: `Genshin Checker/MainTray.cs`
- Modify: `Genshin Checker/InternalTools/MusicInfoSetter.cs`

**Interfaces:**
- Consumes: WPF dependency property names and tray menu event handler.
- Produces: explicit member hiding/nullability and no duplicate or unused members.

- [ ] **Step 1: Fix `InfoDisplay` without changing its XAML property name**

Make the event nullable and explicitly mark the intentional WPF member hiding:

```csharp
public event PropertyChangedEventHandler? PropertyChanged;
public new static readonly DependencyProperty ToolTipProperty = /* existing registration */;
public new string ToolTip
{
    get => (string)GetValue(ToolTipProperty);
    set => SetValue(ToolTipProperty, value);
}
```

Do not rename `ToolTip`; existing XAML binds to that name.

- [ ] **Step 2: Remove purely mechanical warnings**

In `MainTray.cs`, remove the duplicate `using Genshin_Checker.GUI.Window.PopupWindow;` line. In `MusicInfoSetter.cs`, remove the unused `MusicStream` field after confirming `rg -n "MusicStream"` returns only its declaration.

- [ ] **Step 3: Resolve the obsolete `UpdateNotice` call intentionally**

Replace `new Window.PopupWindow.UpdateNotice().ShowDialog()` at `MainTray.cs:161` with the fully qualified WPF window call `new GUI.Window.PopupWindow.UpdateNotice().ShowDialog()`. The WPF constructor already reads update data from `AppUpdater` during its load event, so no constructor or XAML change is needed. Preserve modal display and do not suppress CS0618.

- [ ] **Step 4: Build and confirm four warning codes are gone**

Run:

```powershell
dotnet build 'Genshin Checker.sln' --configuration Release --no-restore --no-incremental 2>&1 |
  Select-String -Pattern 'warning CS(0105|0108|0169|0618|8618)'
```

Expected: no output, assuming Task 2 removed Model CS8618.

- [ ] **Step 5: Commit the component cleanup**

```powershell
git add -- 'Genshin Checker/GUI/UserInterface/CentralPanel/Component/InfoDisplay.xaml.cs' 'Genshin Checker/MainTray.cs' 'Genshin Checker/InternalTools/MusicInfoSetter.cs' 'Genshin Checker/GUI/Window/PopupWindow/UpdateNotice.xaml.cs'
git commit -m "fix: remove component compiler warnings"
```

If `UpdateNotice.xaml.cs` did not change, omit it from `git add`.

---

### Task 4: Fix Nullable UI Control Flow

**Files:**
- Create: `tests/GenshinChecker.Tests/Core/NullBehaviorTests.cs`
- Modify: `Genshin Checker/GUI/BrowserApp/Announcement.cs`
- Modify: `Genshin Checker/GUI/Window/WebApp/Announcement.cs`
- Modify: `Genshin Checker/GUI/old/CharacterCalculator.cs`
- Modify: `Genshin Checker/GUI/old/SettingWindow.cs`
- Modify: `Genshin Checker/GUI/old/SpiralAbyss.cs`
- Modify: `Genshin Checker/GUI/old/TravelersDiaryDetailList.cs`

**Interfaces:**
- Consumes: WebView initialization, exception aggregation, selected UI items, and nullable API archive data.
- Produces: explicit early-return/error behavior instead of null-forgiving operators.

- [ ] **Step 1: Classify each warning before editing**

Inspect the exact warning lines from a non-incremental Release build. For each nullable value, select one of these verified behaviors:

```csharp
if (value is null)
    return; // only for optional UI events where no selection/navigation is valid

if (value is null)
    throw new InvalidDataException("Specific domain value was not available.");

if (exception is not null)
    errors.Add(exception);
```

Do not use `!` merely to silence the compiler.

- [ ] **Step 2: Add characterization tests for non-UI helper branches**

Extract only small pure guards where a branch is otherwise hard to test. For example, move exception aggregation to:

```csharp
internal static void AddIfNotNull(ICollection<Exception> errors, Exception? error)
{
    if (error is not null)
        errors.Add(error);
}
```

Test both null and non-null inputs. Do not instantiate WinForms or WPF windows in unit tests.

- [ ] **Step 3: Apply explicit guards at the warning sites**

Required outcomes by file:

- Both `Announcement.cs` files: guard a missing WebView/CoreWebView2 object before dereference; return without navigation when initialization did not complete.
- `CharacterCalculator.cs`: add exceptions only when non-null; guard missing character/skill data with the existing domain error path before updating controls.
- `SettingWindow.cs`: return when the selected account/item is null instead of dereferencing it.
- `SpiralAbyss.cs`: do not call `PanelLoad` when converted archive data is null; surface the existing invalid-file error path.
- `TravelersDiaryDetailList.cs`: guard the nullable selected/detail item before reading it.

- [ ] **Step 4: Run tests and prove nullable warnings are gone**

Run:

```powershell
dotnet test 'tests\GenshinChecker.Tests\GenshinChecker.Tests.csproj' --filter FullyQualifiedName~NullBehaviorTests
dotnet build 'Genshin Checker.sln' --configuration Release --no-restore --no-incremental 2>&1 |
  Select-String -Pattern 'warning CS(8602|8604)'
```

Expected: tests pass and the warning query prints nothing.

- [ ] **Step 5: Commit explicit null handling**

```powershell
git add -- 'Genshin Checker/GUI/BrowserApp/Announcement.cs' 'Genshin Checker/GUI/Window/WebApp/Announcement.cs' 'Genshin Checker/GUI/old/CharacterCalculator.cs' 'Genshin Checker/GUI/old/SettingWindow.cs' 'Genshin Checker/GUI/old/SpiralAbyss.cs' 'Genshin Checker/GUI/old/TravelersDiaryDetailList.cs' 'tests/GenshinChecker.Tests/Core/NullBehaviorTests.cs'
git commit -m "fix: make nullable UI paths explicit"
```

---

### Task 5: Enforce Warning-Free Verification

**Files:**
- Modify: `Genshin Checker/Genshin Checker.csproj`
- Modify: `Docs/Development.md`
- Modify: `AGENTS.md`

**Interfaces:**
- Consumes: a clean non-incremental Release build and the canonical verify script.
- Produces: Release warnings-as-errors and documented zero-warning policy.

- [ ] **Step 1: Prove the build is clean before enabling the gate**

Run:

```powershell
dotnet build 'Genshin Checker.sln' --configuration Release --no-restore --no-incremental
```

Expected final summary: `0 警告` / `0 Warning(s)` and `0 エラー` / `0 Error(s)`.

- [ ] **Step 2: Enable warnings-as-errors for Release**

Add a conditional property group:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

Keep the documented `CS8981` entry in `NoWarn`; do not add any nullable code to `NoWarn` or `WarningsNotAsErrors`.

- [ ] **Step 3: Run the complete canonical verification**

Run:

```powershell
pwsh -File 'scripts\verify.ps1'
```

Expected: restore, Release build, and every offline test pass with exit code 0.

- [ ] **Step 4: Prove a new warning fails the gate, then restore**

Temporarily add an unused private field to a hand-written class:

```csharp
private int WarningGateProbe;
```

Run `pwsh -File scripts\verify.ps1 -NoRestore`. Expected: failure with CS0169. Remove the probe using `apply_patch`, rerun, and require success. Never commit the probe.

- [ ] **Step 5: Document the policy**

Add to `Docs/Development.md` and `AGENTS.md`:

```markdown
- Release builds must remain at zero warnings. Do not suppress a warning unless it is generated code and the project file records the exact warning number and reason.
- Run `pwsh -File scripts/verify.ps1` before declaring a change complete.
```

- [ ] **Step 6: Commit and verify the final state**

```powershell
git add -- 'Genshin Checker/Genshin Checker.csproj' 'Docs/Development.md' 'AGENTS.md'
git commit -m "build: enforce warning-free release verification"
pwsh -File 'scripts\verify.ps1' -NoRestore
git status --short
```

Expected: canonical verification succeeds and the worktree is clean.

- [ ] **Step 7: Confirm the remote workflow after push authorization**

After the user authorizes pushing the implementation branch, inspect the real GitHub Actions `verify` job. If it fails, use its uploaded TRX and `gh run view --log-failed`, fix locally, rerun `scripts/verify.ps1`, then push the fix. Completion requires a green remote check for the final commit.
