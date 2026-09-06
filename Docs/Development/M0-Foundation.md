# M0 — Foundation

## Purpose

M0 gives every later feature a stable home and establishes a repeatable way to
prove that the project still opens, validates, and builds. It deliberately
avoids player physics and combat; those systems start in M1 and M2.

## Apply the milestone

1. Close Unity and VS Code.
2. Copy the contents of the patch's `files` directory into the Unity project
   root, preserving paths and replacing `.gitignore` and `.gitattributes`.
3. Delete the Unity template files listed in the patch's `DELETE.md`.
4. Reopen the project with Unity 6.6.0f1 and wait for compilation/import.
5. Run **Tools > Sphere Corridor > M0 > Apply Foundation Setup**.

The setup command is idempotent: it creates missing scenes but never overwrites
an existing scene. It then writes the required scene order to Build Settings.

## Why scenes are generated in the Editor

Unity scene files are serialized graphs containing stable file identifiers.
Generating the initial scenes through Unity APIs is safer and easier to audit
than hand-authoring YAML. After generation, commit every new scene and `.meta`
file Unity creates.

## Validate

Run **Tools > Sphere Corridor > M0 > Validate Foundation**. A successful check
prints one summary log and no warnings or errors.

Then open **Window > General > Test Runner**, select **EditMode**, and run all
tests. The tests verify:

- all required scenes exist;
- Build Settings contains them in the correct order;
- the project-specific input action asset exists;
- Unity uses Force Text serialization and Visible Meta Files.

## Debugger smoke test

1. Open `GameBootstrap.cs` from Unity.
2. Place a breakpoint inside `Awake`.
3. In VS Code press **F5** and attach to the Unity Editor.
4. Open `Assets/_Project/Scenes/Bootstrap.unity` and enter Play mode.
5. Continue execution after the breakpoint.

Bootstrap should log its initialization and load MainMenu. The temporary menu
can open the Gameplay sandbox, which displays geometric placeholders.

## Build smoke test

Run **Tools > Sphere Corridor > M0 > Build Windows Development**. The build is
written to:

```text
Builds/WindowsDevelopment/SphereCorridor.exe
```

The `Builds` directory is intentionally ignored by Git. Run the executable and
confirm Bootstrap, MainMenu, and Gameplay work outside the Editor.

## Logging standard

Logs use the following prefix:

```text
[SphereCorridor][Subsystem] Message
```

Use informational logs for meaningful lifecycle or state transitions, warnings
for recoverable unexpected states, and errors when the requested operation
cannot continue. Do not log every frame or every physics tick: repeated logs
hide useful evidence and consume CPU and storage.

Development-only diagnostic logs use `AppLog.Development`; they compile only in
the Editor or a Development Build.

## Commit checklist

Before committing, run:

```powershell
git status
git diff --check
git lfs status
```

Confirm that `Library`, `Temp`, `Logs`, `UserSettings`, `Builds`, and project ZIP
archives are absent. Then commit:

```powershell
git add .
git status
git commit -m "Establish M0 project foundation"
git push
```
