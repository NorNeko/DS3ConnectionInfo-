# Regression checks

Build with modern Visual Studio MSBuild (the .NET desktop build tools and .NET Framework 4.7.2 targeting pack are required):

```powershell
MSBuild.exe tests/DS3ConnectionInfo.Tests.csproj /p:Configuration=Debug
tests/bin/Debug/DS3ConnectionInfo.Tests.exe "$PWD" "$PWD/tests/bin/Debug/overlay-preview.png"
```

The checks use the actual application assembly and simulated memory blocks, verify legacy settings migration, failed reads, stale player clearing, XAML column ordering, and render the compiled overlay with sample data. They do not start Steam, join sessions, or save application settings.

Optional read-only verification against an already running game (run elevated when needed):

```powershell
tests/bin/Debug/DS3ConnectionInfo.Tests.exe --live
```

This uses query/read process permissions and prints basic character attributes, without changing game memory. An empty remote-player list is not a successful remote-player test.


Field option tests exercise actual WPF controls: visibility synchronization, drag-reorder handling and restoration, per-field color selection, restoring inherited colors, and rendered cell colors. The update-checker type must be absent from the built assembly. Screenshots include both languages and the selected-field color controls.

Use `--save-fields` followed by `--check-fields` in separate test processes to check persistence under the test executable's own settings identity. These test settings are separate from the tool's normal user settings.
