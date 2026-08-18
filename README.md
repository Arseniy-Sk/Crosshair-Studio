# Crosshair Studio

Cross-platform overlay for Windows, macOS and Linux (Ubuntu, Fedora, Debian, SteamOS).

## Run

```bash
dotnet run --project src/CrosshairStudio/CrosshairStudio.csproj -c Release
```

## Publish

```bash
dotnet publish src/CrosshairStudio/CrosshairStudio.csproj -c Release -r win-x64 --self-contained true
dotnet publish src/CrosshairStudio/CrosshairStudio.csproj -c Release -r linux-x64 --self-contained true
dotnet publish src/CrosshairStudio/CrosshairStudio.csproj -c Release -r osx-arm64 --self-contained true
```

On Linux the overlay uses X11 / XWayland click-through. Some pure Wayland fullscreen games will not show foreign overlays.
