# Shoulder Camera 0.3.1 — source for review

Open-source Windows camera configuration panel for Enshrouded, licensed under MIT.

Mod page: https://www.nexusmods.com/enshrouded/mods/192

## Build from source

Requirements: Windows x64 with .NET Framework 4.8 and its framework C# compiler. No NuGet packages or downloads are needed.

1. Download or clone this repository.
2. Open PowerShell in the repository root.
3. Run:

```powershell
.\source\build.ps1
```

The script first compiles a console self-test executable with the RESEARCH symbol and runs `--self-test`. These tests operate on synthetic memory in their own process, not on the game. They cover restoration, conflicts, profile isolation, numeric bounds, preference migration, configuration slots and translations.

If tests succeed, it builds `ShoulderCamera.exe` in the repository root, without RESEARCH and without debug symbols. The player executable accepts no command-line options. The temporary test output is under `source/obj/`.

The compiler is the Windows .NET Framework compiler at `%WINDIR%/Microsoft.NET/Framework64/v4.0.30319/csc.exe`. References are only System.Windows.Forms, System.Drawing and System.Runtime.Serialization. Compiler flags are explicitly listed in build.ps1.

## Runtime behavior for security review

- User clicks CONNECT TO GAME after loading a world.
- Finds a single local Enshrouded process and verifies its executable SHA-256 against the one supported build.
- Uses OpenProcess, VirtualQueryEx and ReadProcessMemory to locate and validate a bounded camera configuration table.
- Uses VirtualProtectEx temporarily and WriteProcessMemory to change selected camera configuration floats, reads back values, then restores page protection.
- It does not inject code, allocate memory in the game process, install hooks, patch executable instructions, replace game files, modify saves or contact a server.
- Restores original values on normal exit or RESTORE CAMERA. Restarting the game clears changes after forced exit.
- Only persistent runtime writes are shoulder-camera.json and its temporary replacement beside the executable.
- No networking, telemetry, update downloads, installer, administrator requirement or persistent service.

VirtualAlloc and VirtualFree are used for synthetic self-tests in the process itself, not remote allocation in the game.

The Bow / Wand profile controls shared bow camera states; dedicated free-aim wand states remain native. This is a public beta for one verified game executable. Remote multiplayer and a second PC remain untested. The executable is unsigned. All code and analysis scripts were AI-generated with Codex under the author's direction and tested as documented.

## Uploaded release fingerprints

ZIP SHA-256: `ec7f8ed42e7e8d23b4561bdc9d5159c872b82d6ef43e4320780e24d6b5f208a9`

EXE SHA-256: `ed338054ae9c80f2d3c918b673e310db1720e5d5612f8c77de0e05ce97aec125`

Sources copied exactly from the uploaded 0.3.1 ZIP and checked against its manifest. Recompilation with this compiler can produce a different binary hash due to compilation metadata; byte-for-byte reproducibility is not claimed.
