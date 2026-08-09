# Offline Debug Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a single-command, deterministic offline verification loop shared by Codex, developers, and GitHub Actions.

**Architecture:** Add a focused xUnit project and thin seams around time, file storage, and HTTP without introducing a DI container. Preserve the existing public APIs through facades and production adapters; tests call internal deterministic overloads and fakes. Route local and CI verification through one PowerShell script.

**Tech Stack:** .NET 8 WPF, C# 12, xUnit 2.9.3, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 18.8.1, PowerShell 7, GitHub Actions on Windows.

## Global Constraints

- Target `net8.0-windows10.0.22621.0` on Windows.
- Do not use a real HoYoLab API, Cookie, UID, Windows registry key, or existing Application Data in tests.
- An unregistered test URL must fail; it must never fall back to the network.
- Preserve production storage paths, file formats, API contracts, and UI behavior.
- Do not add a DI container or perform the new/old UI migration.
- Use `scripts/verify.ps1` as the canonical local and CI verification entry point.
- Keep every test deterministic by injecting time, storage roots, and HTTP responses.
- The warning-zero work is executed by the follow-up plan `Docs/superpowers/plans/2026-08-09-warning-zero.md`.

---

## File Structure

### Create

- `tests/GenshinChecker.Tests/GenshinChecker.Tests.csproj` — test dependencies and project reference.
- `tests/GenshinChecker.Tests/Core/JsonCheckerTests.cs` — JSON contract tests.
- `tests/GenshinChecker.Tests/Core/ServerTimeTests.cs` — deterministic reset-boundary tests.
- `tests/GenshinChecker.Tests/Core/FileUserDataStoreTests.cs` — isolated compressed and plain storage tests.
- `tests/GenshinChecker.Tests/Core/WebRequestTests.cs` — HTTP status and response-body tests.
- `tests/GenshinChecker.Tests/Core/HoYoLabResponseParserTests.cs` — API data/error parsing tests.
- `tests/GenshinChecker.Tests/TestDoubles/FakeHttpTransport.cs` — deny-by-default HTTP fake.
- `tests/GenshinChecker.Tests/TestDoubles/TemporaryDirectory.cs` — disposable test directory.
- `tests/GenshinChecker.Tests/Fixtures/hoyolab-game-records-ok.json` — fictional successful response.
- `tests/GenshinChecker.Tests/Fixtures/hoyolab-error.json` — fictional non-zero retcode response.
- `Genshin Checker/Core/General/Storage/IUserDataStore.cs` — narrow storage contract.
- `Genshin Checker/Core/General/Storage/FileUserDataStore.cs` — production filesystem adapter.
- `Genshin Checker/Core/General/Http/IHttpTransport.cs` — request transport contract.
- `Genshin Checker/Core/General/Http/HttpClientTransport.cs` — production HttpClient adapter.
- `Genshin Checker/Core/HoYoLab/HoYoLabResponseParser.cs` — generic data/error parser.
- `scripts/verify.ps1` — canonical restore/build/test command.
- `Docs/Development.md` — developer and Codex workflow.
- `AGENTS.md` — repository-local verification and safety rules.

### Modify

- `Genshin Checker.sln` — include the test project.
- `Genshin Checker/AssemblyInfo.cs` — expose internal seams only to the test assembly.
- `Genshin Checker/Core/General/Convert/Server.cs` — accept `TimeProvider` in internal overloads.
- `Genshin Checker/Core/General/AppData.cs` — delegate file operations to `FileUserDataStore`.
- `Genshin Checker/Core/General/WebRequest.cs` — delegate sending to `IHttpTransport` overloads.
- `Genshin Checker/Core/HoYoLab/ApiEndpoint.cs` — use the response parser for `GetGameRecords` and a testable authentication guard.
- `.gitignore` — share common `.vscode` files while ignoring personal overrides.
- `.vscode/launch.json` — launch the actual .NET 8 assembly.
- `.vscode/tasks.json` — expose build, test, and canonical verify tasks.
- `.github/workflows/dotnet-desktop.yml` — use .NET 8 and the canonical script.

---

### Task 1: Bootstrap the Test Project and JSON Contract

**Files:**
- Create: `tests/GenshinChecker.Tests/GenshinChecker.Tests.csproj`
- Create: `tests/GenshinChecker.Tests/Core/JsonCheckerTests.cs`
- Modify: `Genshin Checker.sln`
- Modify: `Genshin Checker/AssemblyInfo.cs`

**Interfaces:**
- Consumes: `JsonChecker<T>.Check(string)`, `JsonChecker<T>.IsValid(string?)`, `JsonChecker<T>.format(string)`.
- Produces: test assembly `GenshinChecker.Tests` with internal access to `Genshin Checker`.

- [ ] **Step 1: Create the test project with pinned stable packages**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.8.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Genshin Checker\Genshin Checker.csproj" />
  </ItemGroup>
</Project>
```

Add it with:

```powershell
dotnet sln 'Genshin Checker.sln' add 'tests\GenshinChecker.Tests\GenshinChecker.Tests.csproj'
```

- [ ] **Step 2: Write JSON behavior tests**

```csharp
using Genshin_Checker.Core.General;
using System.IO;

namespace GenshinChecker.Tests.Core;

public sealed class JsonCheckerTests
{
    private sealed class Payload
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Check_ValidJson_ReturnsModel()
    {
        var result = JsonChecker<Payload>.Check("{\"Value\":42}");
        Assert.Equal(42, result.Value);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    public void Check_InvalidOrNullJson_ThrowsInvalidDataException(string json)
    {
        Assert.Throws<InvalidDataException>(() => JsonChecker<Payload>.Check(json));
    }

    [Fact]
    public void IsValid_Null_ReturnsFalse()
    {
        Assert.False(JsonChecker<Payload>.IsValid(null));
    }
}
```

- [ ] **Step 3: Run the new tests and record the first result**

Run:

```powershell
dotnet test 'tests\GenshinChecker.Tests\GenshinChecker.Tests.csproj' --configuration Debug --filter FullyQualifiedName~JsonCheckerTests
```

Expected: tests pass. If resource initialization makes them fail, fix only the test bootstrapping; do not weaken the assertions.

- [ ] **Step 4: Grant internal access for later seam tests**

Append to `Genshin Checker/AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GenshinChecker.Tests")]
```

- [ ] **Step 5: Run the whole solution test command**

Run:

```powershell
dotnet test 'Genshin Checker.sln' --configuration Debug --no-restore
```

Expected: all JSON tests pass and no test project warning is emitted.

- [ ] **Step 6: Commit the test bootstrap**

```powershell
git add -- 'Genshin Checker.sln' 'Genshin Checker/AssemblyInfo.cs' 'tests/GenshinChecker.Tests'
git commit -m "test: add offline test project"
```

---

### Task 2: Make Server Time Deterministic

**Files:**
- Modify: `Genshin Checker/Core/General/Convert/Server.cs`
- Create: `tests/GenshinChecker.Tests/Core/ServerTimeTests.cs`

**Interfaces:**
- Consumes: `Account.Servers`, `TimeProvider`.
- Produces: internal overloads `GameServerDate(Servers, TimeProvider)`, `GameDailyReset(Servers, TimeProvider)`, `NextWeeklyResetDate(Servers, TimeProvider)`, and `GetRegionTime(Servers, DateTime)`.

- [ ] **Step 1: Write failing reset-boundary tests**

```csharp
using Genshin_Checker.Core.General.Convert;
using Genshin_Checker.Core.HoYoLab;

namespace GenshinChecker.Tests.Core;

public sealed class ServerTimeTests
{
    [Theory]
    [InlineData("2026-08-09T19:59:59Z", 9)]
    [InlineData("2026-08-09T20:00:00Z", 10)]
    public void AsiaGameServerDate_ChangesAtFourLocalTime(string utc, int expectedDay)
    {
        var clock = new FakeTimeProvider(DateTimeOffset.Parse(utc));
        var result = Server.GameServerDate(Account.Servers.os_asia, clock);
        Assert.Equal(expectedDay, result.Day);
    }

    [Fact]
    public void AsiaDailyReset_OneSecondBeforeReset_ReturnsOneSecond()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.Parse("2026-08-09T19:59:59Z"));
        Assert.Equal(TimeSpan.FromSeconds(1), Server.GameDailyReset(Account.Servers.os_asia, clock));
    }

    [Fact]
    public void AsiaWeeklyReset_OnSundayBeforeReset_TargetsMondayFourLocalTime()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.Parse("2026-08-09T12:00:00Z"));
        var result = Server.NextWeeklyResetDate(Account.Servers.os_asia, clock);
        Assert.Equal(DateTime.Parse("2026-08-09T20:00:00Z").ToUniversalTime(), result);
    }
}
```

Add this test-only provider at the bottom of `ServerTimeTests.cs`; do not add another package:

```csharp
private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
```

- [ ] **Step 2: Run the tests to verify the missing overload failure**

Run:

```powershell
dotnet test 'tests\GenshinChecker.Tests\GenshinChecker.Tests.csproj' --filter FullyQualifiedName~ServerTimeTests
```

Expected: compile failure because the `TimeProvider` overloads do not exist.

- [ ] **Step 3: Add deterministic overloads and correct UTC conversion at reset boundaries**

Keep public signatures as wrappers over `TimeProvider.System`. Implement the core using this shape:

```csharp
public static DateTime GameServerDate(Account.Servers server) =>
    GameServerDate(server, TimeProvider.System);

internal static DateTime GameServerDate(Account.Servers server, TimeProvider clock)
{
    var localNow = GetRegionTime(server, clock.GetUtcNow().UtcDateTime);
    return localNow.AddHours(-GameDailyResetTime).Date;
}

internal static DateTime GetRegionTime(Account.Servers server, DateTime utcNow) =>
    utcNow - ServerUTCOffset(server);

private static DateTime NextDailyResetUtc(Account.Servers server, TimeProvider clock)
{
    var localNow = GetRegionTime(server, clock.GetUtcNow().UtcDateTime);
    var localReset = localNow.Date.AddHours(GameDailyResetTime);
    if (localNow >= localReset)
        localReset = localReset.AddDays(1);
    return DateTime.SpecifyKind(ConvertUTCTime(server, localReset), DateTimeKind.Utc);
}
```

Implement weekly reset by finding the next Monday at 04:00 in server-local time, then converting that value to UTC before returning it.

- [ ] **Step 4: Run boundary and existing conversion tests**

Run:

```powershell
dotnet test 'tests\GenshinChecker.Tests\GenshinChecker.Tests.csproj' --filter FullyQualifiedName~ServerTimeTests
```

Expected: all reset tests pass for Asia. Add table rows for America, Europe, and TW/HK/MO offsets before committing.

- [ ] **Step 5: Commit deterministic time handling**

```powershell
git add -- 'Genshin Checker/Core/General/Convert/Server.cs' 'tests/GenshinChecker.Tests/Core/ServerTimeTests.cs'
git commit -m "test: make server reset time deterministic"
```

---

### Task 3: Isolate User Data Storage

**Files:**
- Create: `Genshin Checker/Core/General/Storage/IUserDataStore.cs`
- Create: `Genshin Checker/Core/General/Storage/FileUserDataStore.cs`
- Create: `tests/GenshinChecker.Tests/Core/FileUserDataStoreTests.cs`
- Create: `tests/GenshinChecker.Tests/TestDoubles/TemporaryDirectory.cs`
- Modify: `Genshin Checker/Core/General/AppData.cs`

**Interfaces:**
- Consumes: a caller-supplied root directory and explicit `compress` flag.
- Produces: `IUserDataStore.Exists`, `LoadAsync`, `SaveAsync`, and `GetRandomPath`; `AppData` remains a compatibility facade.

- [ ] **Step 1: Define the storage contract in the failing tests**

```csharp
internal interface IUserDataStore
{
    string RootPath { get; }
    string GetRandomPath();
    bool Exists(string path);
    Task<string?> LoadAsync(string path, bool compress, bool throwOnError = true);
    Task<bool> SaveAsync(string path, string data, bool compress);
}
```

Write tests that instantiate `FileUserDataStore(temp.Path)` and assert:

```csharp
[Theory]
[InlineData(false)]
[InlineData(true)]
public async Task SaveThenLoad_RoundTripsUtf8(bool compress)
{
    using var temp = new TemporaryDirectory();
    var store = new FileUserDataStore(temp.Path);
    Assert.True(await store.SaveAsync("sample.misaki_gsc", "原神-data", compress));
    Assert.Equal("原神-data", await store.LoadAsync("sample.misaki_gsc", compress));
}

[Fact]
public async Task Load_MissingFile_ThrowsWithoutTouchingApplicationData()
{
    using var temp = new TemporaryDirectory();
    var store = new FileUserDataStore(temp.Path);
    await Assert.ThrowsAsync<FileNotFoundException>(() => store.LoadAsync("missing", false));
}
```

Also test corrupt gzip, automatic directory creation, and `throwOnError: false` returning `null`.

- [ ] **Step 2: Run tests to verify the storage types are missing**

Run:

```powershell
dotnet test 'tests\GenshinChecker.Tests\GenshinChecker.Tests.csproj' --filter FullyQualifiedName~FileUserDataStoreTests
```

Expected: compile failure for missing `FileUserDataStore`.

- [ ] **Step 3: Implement `FileUserDataStore` with one path resolver**

Use a private resolver that prevents relative paths from escaping the supplied root:

```csharp
private string ResolvePath(string path)
{
    if (Path.IsPathRooted(path))
        return Path.GetFullPath(path);

    var fullPath = Path.GetFullPath(Path.Combine(RootPath, path));
    var root = Path.GetFullPath(RootPath) + Path.DirectorySeparatorChar;
    if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        throw new InvalidDataException("User data path escapes the configured root.");
    return fullPath;
}
```

Use `await using` for file streams, `GZipStream` for compressed data, and UTF-8 readers/writers. `SaveAsync` returns `false` only after logging an exception; `LoadAsync` follows `throwOnError` exactly.

- [ ] **Step 4: Convert `AppData` into a behavior-preserving facade**

Create a single production store rooted at `UserDataPath`. Preserve the existing DEBUG rule before delegating:

```csharp
#if DEBUG
compress = false;
#endif
return await Store.LoadAsync(path, compress, ReturnException);
```

Delegate `IsExistFile(path)` to `Store.Exists(path)` and delegate `SaveUserData(path, data, compress)` to `Store.SaveAsync(path, data, compress)` after applying the existing DEBUG `compress = false` rule. Keep `AppDataDirectry`, `UserDataPath`, and all existing method signatures intact.

- [ ] **Step 5: Run storage tests and a Release build**

Run:

```powershell
dotnet test 'tests\GenshinChecker.Tests\GenshinChecker.Tests.csproj' --filter FullyQualifiedName~FileUserDataStoreTests
dotnet build 'Genshin Checker.sln' --configuration Release --no-restore
```

Expected: all storage tests pass and the application compiles with unchanged callers.

- [ ] **Step 6: Commit isolated storage**

```powershell
git add -- 'Genshin Checker/Core/General/AppData.cs' 'Genshin Checker/Core/General/Storage' 'tests/GenshinChecker.Tests/Core/FileUserDataStoreTests.cs' 'tests/GenshinChecker.Tests/TestDoubles/TemporaryDirectory.cs'
git commit -m "refactor: isolate user data storage"
```

---

### Task 4: Add Deny-by-Default HTTP and API Response Parsing

**Files:**
- Create: `Genshin Checker/Core/General/Http/IHttpTransport.cs`
- Create: `Genshin Checker/Core/General/Http/HttpClientTransport.cs`
- Create: `Genshin Checker/Core/HoYoLab/HoYoLabResponseParser.cs`
- Create: `tests/GenshinChecker.Tests/TestDoubles/FakeHttpTransport.cs`
- Create: `tests/GenshinChecker.Tests/Core/WebRequestTests.cs`
- Create: `tests/GenshinChecker.Tests/Core/HoYoLabResponseParserTests.cs`
- Create: `tests/GenshinChecker.Tests/Fixtures/hoyolab-game-records-ok.json`
- Create: `tests/GenshinChecker.Tests/Fixtures/hoyolab-error.json`
- Modify: `Genshin Checker/Core/General/WebRequest.cs`
- Modify: `Genshin Checker/Core/HoYoLab/ApiEndpoint.cs`

**Interfaces:**
- Consumes: `IHttpTransport.SendAsync(HttpRequestMessage, CancellationToken)` and `Model.HoYoLab.Root<T>`.
- Produces: internal HTTP overloads accepting a transport; `HoYoLabResponseParser.RequireData<T>(string)`; `ApiEndpoint.EnsureAuthenticated(bool, int)`.

- [ ] **Step 1: Write the deny-by-default fake and failing HTTP tests**

```csharp
internal interface IHttpTransport
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}
```

`FakeHttpTransport` stores responses by `(HttpMethod, absolute URL)`. Its `SendAsync` throws `InvalidOperationException($"No fake response registered for {request.Method} {request.RequestUri}")` when no exact registration exists.

Test the internal `WebRequest.HoYoGetRequest` overload with a registered 200 JSON response, a registered 500 valid HoYoLab error JSON, and an unregistered URL. The last assertion must expect `InvalidOperationException`, proving there is no real-network fallback.

- [ ] **Step 2: Run HTTP tests to verify the seam is missing**

Run:

```powershell
dotnet test 'tests\GenshinChecker.Tests\GenshinChecker.Tests.csproj' --filter FullyQualifiedName~WebRequestTests
```

Expected: compile failure for `IHttpTransport` or the internal overload.

- [ ] **Step 3: Implement the production transport and internal overloads**

`HttpClientTransport` owns one `HttpClient` and forwards `SendAsync`. Public methods create requests and delegate to an internal overload:

```csharp
public static Task<string> HoYoGetRequest(string url, string cookie, CultureInfo? culture = null) =>
    HoYoGetRequest(url, cookie, HttpClientTransport.Shared, culture, CancellationToken.None);

internal static async Task<string> HoYoGetRequest(
    string url,
    string cookie,
    IHttpTransport transport,
    CultureInfo? culture = null,
    CancellationToken cancellationToken = default)
{
    using var request = BuildHoYoGetRequest(url, cookie, culture);
    using var response = await transport.SendAsync(request, cancellationToken);
    return await ReadHoYoResponse(response, url, cancellationToken);
}
```

Extract request/header construction and response reading without changing header names or the existing generated error JSON. Apply the same transport seam to HoYo POST and general GET; image/cache behavior remains outside the initial tests.

- [ ] **Step 4: Write parser and authentication tests**

```csharp
[Fact]
public void RequireData_Success_ReturnsData()
{
    const string json = "{\"retcode\":0,\"message\":\"OK\",\"data\":{\"value\":7}}";
    Assert.Equal(7, HoYoLabResponseParser.RequireData<Payload>(json).Value);
}

[Fact]
public void RequireData_Error_ThrowsApiException()
{
    const string json = "{\"retcode\":-100,\"message\":\"invalid\",\"data\":null}";
    var error = Assert.Throws<HoYoLabAPIException>(() => HoYoLabResponseParser.RequireData<Payload>(json));
    Assert.Contains("invalid", error.Message);
}

[Fact]
public void EnsureAuthenticated_False_ThrowsWithoutNetwork()
{
    Assert.Throws<UserNotAuthenticatedException>(() => ApiEndpoint.EnsureAuthenticated(false, 800000001));
}
```

- [ ] **Step 5: Implement and use the parser in `GetGameRecords`**

```csharp
internal static class HoYoLabResponseParser
{
    internal static T RequireData<T>(string json)
    {
        var root = JsonChecker<Model.HoYoLab.Root<T>>.Check(json);
        return root.Data ?? throw new HoYoLabAPIException(root.Retcode, root.Message);
    }
}
```

Change only `ApiEndpoint.GetGameRecords` in this task to call `EnsureAuthenticated(Account.IsAuthed, Account.UID)` and `HoYoLabResponseParser.RequireData<Model.HoYoLab.GameRecords.Data>(json)`. All other endpoint methods remain byte-for-byte unchanged in this task.

- [ ] **Step 6: Run all offline HTTP and parser tests**

Run:

```powershell
dotnet test 'tests\GenshinChecker.Tests\GenshinChecker.Tests.csproj' --filter 'FullyQualifiedName~WebRequestTests|FullyQualifiedName~HoYoLabResponseParserTests'
```

Expected: all tests pass and no test contacts the network.

- [ ] **Step 7: Commit the HTTP boundary**

```powershell
git add -- 'Genshin Checker/Core/General/Http' 'Genshin Checker/Core/General/WebRequest.cs' 'Genshin Checker/Core/HoYoLab/ApiEndpoint.cs' 'Genshin Checker/Core/HoYoLab/HoYoLabResponseParser.cs' 'tests/GenshinChecker.Tests'
git commit -m "test: isolate HTTP response handling"
```

---

### Task 5: Create the Canonical Verification Script

**Files:**
- Create: `scripts/verify.ps1`
- Modify: `tests/GenshinChecker.Tests/Core/JsonCheckerTests.cs` temporarily for the negative check, then restore it.

**Interfaces:**
- Consumes: `dotnet restore`, `dotnet build`, `dotnet test`.
- Produces: `scripts/verify.ps1 [-NoRestore]` and `TestResults/*.trx`; exits non-zero on any failure.

- [ ] **Step 1: Implement strict command execution**

Use this command shape:

```powershell
[CmdletBinding()]
param([switch]$NoRestore)

$ErrorActionPreference = 'Stop'
$solution = Join-Path $PSScriptRoot '..\Genshin Checker.sln'
$results = Join-Path $PSScriptRoot '..\TestResults'

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
}

if (-not $NoRestore) { Invoke-DotNet @('restore', $solution) }
Invoke-DotNet @('build', $solution, '--configuration', 'Release', '--no-restore')
Invoke-DotNet @('test', $solution, '--configuration', 'Release', '--no-build', '--logger', 'trx', '--results-directory', $results)
```

Use `New-Item -ItemType Directory -Force` for `TestResults`; do not delete any directory outside the resolved repository path.

- [ ] **Step 2: Run the canonical script**

Run:

```powershell
pwsh -File 'scripts\verify.ps1'
```

Expected: exit code 0, Release build succeeds, all tests pass, and at least one `.trx` exists under `TestResults`.

- [ ] **Step 3: Prove a failing test makes the script fail**

Temporarily change `Assert.Equal(42, result.Value)` to `Assert.Equal(41, result.Value)`, then run:

```powershell
pwsh -File 'scripts\verify.ps1' -NoRestore
```

Expected: non-zero exit code and a failed-test message naming `Check_ValidJson_ReturnsModel`.

- [ ] **Step 4: Restore the assertion and re-run**

Restore `Assert.Equal(42, result.Value)` using `apply_patch`, then run the canonical command again. Expected: exit code 0.

- [ ] **Step 5: Ignore generated test output and commit**

Add `/TestResults/` to `.gitignore`, then commit:

```powershell
git add -- '.gitignore' 'scripts/verify.ps1'
git commit -m "build: add canonical verification script"
```

---

### Task 6: Share Codex and VS Code Development Instructions

**Files:**
- Create: `AGENTS.md`
- Create: `Docs/Development.md`
- Modify: `.gitignore`
- Modify: `.vscode/launch.json`
- Modify: `.vscode/tasks.json`

**Interfaces:**
- Consumes: `scripts/verify.ps1`, actual Debug output `Genshin Checker.dll`.
- Produces: one documented setup and one working VS Code launch configuration.

- [ ] **Step 1: Make common VS Code files trackable**

Replace the blanket `.vscode/` ignore with:

```gitignore
.vscode/*.local.json
.vscode/settings.json
```

Keep `launch.json` and `tasks.json` tracked.

- [ ] **Step 2: Correct the .NET 8 launch target**

Set the launch program to:

```json
"program": "${workspaceFolder}/Genshin Checker/bin/Debug/net8.0-windows10.0.22621.0/Genshin Checker.dll",
"cwd": "${workspaceFolder}/Genshin Checker",
"preLaunchTask": "build-debug"
```

Keep the attach configuration. Rename the launch entry to `Genshin Checker (Debug)`.

- [ ] **Step 3: Define build, test, and verify tasks**

Create tasks named `build-debug`, `test-offline`, and `verify` where `verify` runs:

```json
{
  "label": "verify",
  "type": "process",
  "command": "pwsh",
  "args": ["-File", "${workspaceFolder}/scripts/verify.ps1"],
  "problemMatcher": "$msCompile"
}
```

- [ ] **Step 4: Write repository-local Codex rules**

`AGENTS.md` must state:

```markdown
# Development verification

- Run `pwsh -File scripts/verify.ps1` after code changes.
- Tests must not use real HoYoLab credentials, real API calls, Windows registry state, or existing Application Data.
- Use fixtures under `tests/GenshinChecker.Tests/Fixtures` and temporary directories.
- Do not edit generated `*.Designer.cs` files directly.
- Treat a failing verification command as unresolved work; report the exact failing test or build error.
```

- [ ] **Step 5: Write `Docs/Development.md`**

Document Windows, .NET 8 SDK, PowerShell 7, first restore, canonical verify, filtered `dotnet test`, VS Code F5, TRX location, fixture safety, and GitHub Actions diagnosis. Use copy-pasteable commands from earlier tasks.

- [ ] **Step 6: Validate shared configuration and commit**

Run:

```powershell
pwsh -File 'scripts\verify.ps1' -NoRestore
git check-ignore -v '.vscode\launch.json'
```

Expected: verification succeeds and `git check-ignore` prints nothing for `launch.json`.

Commit:

```powershell
git add -- '.gitignore' '.vscode/launch.json' '.vscode/tasks.json' 'AGENTS.md' 'Docs/Development.md'
git commit -m "docs: add reproducible development workflow"
```

---

### Task 7: Make GitHub Actions Use the Same Verification Path

**Files:**
- Modify: `.github/workflows/dotnet-desktop.yml`

**Interfaces:**
- Consumes: `scripts/verify.ps1 -NoRestore` and `TestResults/*.trx`.
- Produces: one Release validation job on `windows-latest` for pushes and pull requests to `main`.

- [ ] **Step 1: Replace the stale workflow with a minimal .NET 8 job**

```yaml
name: .NET Desktop

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

permissions:
  contents: read

jobs:
  verify:
    runs-on: windows-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v6

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v6
        with:
          dotnet-version: 8.0.x

      - name: Restore
        shell: pwsh
        run: dotnet restore 'Genshin Checker.sln'

      - name: Verify
        shell: pwsh
        run: ./scripts/verify.ps1 -NoRestore

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v7
        with:
          name: test-results
          path: TestResults/**/*.trx
          if-no-files-found: error
```

Do not enable setup-dotnet caching in this task because the repository does not contain NuGet lock files.

- [ ] **Step 2: Validate the exact workflow command locally**

Run:

```powershell
dotnet restore 'Genshin Checker.sln'
pwsh -File 'scripts\verify.ps1' -NoRestore
```

Expected: both commands exit 0 and TRX files exist.

- [ ] **Step 3: Commit the workflow update**

```powershell
git add -- '.github/workflows/dotnet-desktop.yml'
git commit -m "ci: verify .NET 8 build and offline tests"
```

- [ ] **Step 4: Run final foundation verification**

Run:

```powershell
pwsh -File 'scripts\verify.ps1'
git status --short
```

Expected: verification succeeds and the worktree is clean.

- [ ] **Step 5: Verify the real GitHub check after push authorization**

After the user authorizes pushing the implementation branch, push it and inspect the corresponding Actions run with `gh run list` and `gh run view --log-failed`. Completion requires the `verify` job to be green. Do not claim remote CI success before this step has actually run.
