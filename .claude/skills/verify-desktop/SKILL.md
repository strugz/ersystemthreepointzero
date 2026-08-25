---
name: verify-desktop
description: Verify the ER System VB.NET WinForms desktop solution (ER System.sln, .NET Framework 4.8) by restoring NuGet packages, building with MSBuild, and running the MSTest suite via vstest.console.exe. Use after changing any .vb file, .vbproj, or anything in ER System/, ERSystem.Domain/, ERSystem.AppServices/, ERSystem.Infrastructure/, or ERSystem.Tests/.
---

# Verify the ER System desktop solution

Runs the desktop verification sequence required by `AGENTS.md`. This is legacy .NET Framework 4.8 tooling — the .NET SDK CLI does **not** work here. Report honestly which steps ran and which were blocked; `AGENTS.md` explicitly requires stating environment limitations rather than claiming success.

## Step 1 — resolve the toolchain

Visual Studio paths differ per machine and per edition, so resolve them with `vswhere` instead of hardcoding. Run this once and reuse the results:

```powershell
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$vs = & $vswhere -latest -property installationPath
$msbuild = Join-Path $vs 'MSBuild\Current\Bin\MSBuild.exe'
$vstest  = Join-Path $vs 'Common7\IDE\Extensions\TestPlatform\vstest.console.exe'
$devenv  = Join-Path $vs 'Common7\IDE\devenv.com'
"msbuild: $(Test-Path $msbuild)"; "vstest: $(Test-Path $vstest)"; "devenv: $(Test-Path $devenv)"
```

If `vswhere.exe` is absent, Visual Studio is not installed and none of the remaining steps can run. Say that and stop — do not fall back to `dotnet build`.

## Step 2 — restore NuGet packages

The desktop projects use `packages.config`, **not** SDK-style `PackageReference`. `dotnet restore` cannot restore them. Use `nuget.exe`, and **pass `-MSBuildPath` explicitly**:

```powershell
$nuget = (Get-Command nuget.exe -ErrorAction SilentlyContinue).Source
if (-not $nuget) { $nuget = "$env:USERPROFILE\tools\nuget\nuget.exe" }
& $nuget restore "ER System.sln" -MSBuildPath (Join-Path $vs 'MSBuild\Current\Bin')
```

`-MSBuildPath` is required, not cosmetic. Without it, `nuget.exe` auto-detects the .NET Framework MSBuild at `C:\Windows\Microsoft.NET\Framework64\v4.0.30319`, which cannot parse the `Version` attribute on `PackageReference` and fails with `MSB4066` plus a warning that projects were skipped. The `packages.config` restore still happens, but the noise looks like a real failure and any non-`packages.config` project is silently skipped.

If `nuget.exe` cannot be found, check whether `packages/` is already populated — a previously restored tree is usually enough to build. Report that you skipped restore and why.

## Step 3 — build the solution

```powershell
& $msbuild "ER System.sln" /p:Configuration=Debug /p:Platform="Any CPU" /v:minimal /nologo
```

- Valid solution configurations are `Debug` and `Release`, each with `Any CPU` and `x86`. Use the one relevant to the task; `Debug` / `Any CPU` is the default choice.
- **`ERSystem3.5Setup` is not built by this command and should not be.** It is a Visual Studio deployment project (`.vdproj`) that MSBuild cannot build. The solution has no `Build.0` entry for it under these configurations, so MSBuild skips it **silently** — there is no `MSB4078` and no warning. Do not read the clean output as proof the installer was validated; it was not built at all.
- Known-good baseline: **exit code 0, five projects built** — ERSystem.Domain, ERSystem.Infrastructure, ERSystem.AppServices, ER System.exe, ERSystem.Tests — with 0 warnings and 0 errors.
- Installer validation is a separate, explicit step and only when the task calls for it:
  ```powershell
  & $devenv "ER System.sln" /build "Debug"
  ```

## Step 4 — run the tests

`ERSystem.Tests` targets .NET Framework 4.8 with MSTest 4.x. **`dotnet test` does not work.** Use the VS test platform against the built assembly, and **pass `/TestAdapterPath`**:

```powershell
$adapter = (Get-ChildItem "packages" -Directory -Filter "MSTest.TestAdapter.*" |
            Sort-Object Name -Descending | Select-Object -First 1).FullName
$adapter = Join-Path $adapter 'buildTransitive\net462'
& $vstest "ERSystem.Tests\bin\Debug\ERSystem.Tests.dll" /TestAdapterPath:$adapter /Logger:console
```

`/TestAdapterPath` is required. MSTest 4.x does not copy its adapter into the project output — `bin\Debug` contains only `MSTest.TestFramework.dll` — so without it `vstest.console.exe` discovers nothing.

**Do not trust the exit code here.** When `vstest.console.exe` discovers no tests it prints `No test is available in ...` and still **exits 0**. Always confirm the run reported a real test count (`Total tests: N` with `N > 0`) before calling the suite green. Known-good baseline: **10 tests, all passing**.

If the assembly is missing, step 3 did not succeed — fix the build first rather than reporting tests as passing.

## Rules and expected dependency direction

Keep this direction; a change that reverses it is a defect even if it compiles:

```text
ER System (WinForms UI) -> ERSystem.AppServices -> ERSystem.Domain
                                                -> ERSystem.Infrastructure -> ERSystem.Domain
```

Domain must not reference Infrastructure, AppServices, or WinForms, and no WinForms control types belong in Domain, Infrastructure, or application-service interfaces.

Also per `AGENTS.md`:

- New `.vb` files must be added to the correct `.vbproj` explicitly — these legacy projects have no wildcard file inclusion. A file that exists on disk but is not in the project silently does not compile.
- Do not hand-edit `.Designer.vb`, `.resx`, or `My Project/` generated files. Editing those prompts for confirmation by design.
- Preserve form names, partial-class relationships, and `DependentUpon` metadata when moving or renaming form files.
- Do not introduce .NET Core / .NET 5+ only APIs — the target is `net48`.

## Form work

A green build does not prove a form still loads. For any change touching a form, also confirm the form opens in the Visual Studio designer and exercise the changed workflow. If you could not do that, say so.

## If a step cannot run

Missing Visual Studio, missing `nuget.exe`, missing SQL Server for data-access tests, or a non-Windows environment are all limitations to report explicitly. Complete every step that can run, then list what was skipped and why.
