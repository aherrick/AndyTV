# AndyTV.VLC

Blazor Server IPTV browser and VLC launcher (Dark Mode MudBlazor).

## Features
- Supply an IPTV M3U playlist URL
- Parse channels via M3UManager (Title, Logo, Group, MediaUrl)
- Display in MudBlazor table with paging & instant search (type to filter by Title or Group)
- Launch VLC with selected channel stream (passes MediaUrl to VLC)
- Dark theme via MudBlazor

## Requirements
- .NET 10 SDK (preview) or adjust `TargetFramework` in `AndyTV.VLC.csproj` to `net8.0` if .NET 10 not installed
- VLC installed (default path: `C:/Program Files/VideoLAN/VLC/vlc.exe`)

## Configuration
Edit `appsettings.json` to change VLC path:
```json
"VLC": { "Path": "D:/Apps/VLC/vlc.exe" }
```

## Run
```powershell
cd "c:\Users\andrew.j.herrick\Desktop\New folder\AndyTV.VLC"
dotnet restore
dotnet build
dotnet run
```
Navigate to https://localhost:5001 or http://localhost:5000.

Paste your playlist M3U URL and click Load Playlist. Use the search box to filter. Click the TV icon to launch VLC.

## Notes
- Direct usage of `M3UManager.ParseFromUrlAsync` for playlist parsing.
- Launching external processes only works when hosting locally (not in sandboxed environments).
- Security: Only the provided stream URL is passed to VLC; do not expose this publicly without validation.
