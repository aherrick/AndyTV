<img src="https://raw.githubusercontent.com/aherrick/AndyTV/main/AndyTV.png" alt="AndyTV logo" width="128"/>

[![Build](https://github.com/aherrick/AndyTV/actions/workflows/build.yml/badge.svg)](https://github.com/aherrick/AndyTV/actions/workflows/build.yml)
[![Publish](https://github.com/aherrick/AndyTV/actions/workflows/publish.yml/badge.svg)](https://github.com/aherrick/AndyTV/actions/workflows/publish.yml)
[![Scan](https://github.com/aherrick/AndyTV/actions/workflows/scan.yml/badge.svg)](https://github.com/aherrick/AndyTV/actions/workflows/scan.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)


**AndyTV** is a lightweight, open-source IPTV player for Windows.

## ✨ Features
- 📡 Plays IPTV streams from an M3U playlist  
- 🖥️ Simple and clean Windows UI  
- ⭐ Create and manage **Favorites** for one-click channel switching  
- 🕑 Remembers your most recently watched channels  
- 🔄 Auto-update support via GitHub Releases  

> ⚠️ **Note:** AndyTV does **not** include or ship with any IPTV channels, playlists, or URLs.  
> You must provide your own M3U URL from your IPTV provider.

## 🖱️ Quick controls
- **Left mouse:** click and hold for **1 second** → switch to **previous channel**
- **Middle mouse:** click → **Mute / Unmute**
- **Right mouse:** click → open **Menu**
- **Scroll wheel:** cycle through **recent channels**

## 📥 Download

Download the latest installer from the [Releases page](https://github.com/aherrick/AndyTV/releases/latest)  
or directly via:

- 💿 **Installer (recommended):**  
  [Download AndyTV Setup.exe](https://github.com/aherrick/AndyTV/releases/latest/download/com.ajh.AndyTV-win-Setup.exe)

- 📦 **Portable ZIP:**  
  [Download AndyTV Portable.zip](https://github.com/aherrick/AndyTV/releases/latest/download/com.ajh.AndyTV-win-Portable.zip)

## 🖥️ Requirements
- 🪟 Windows 10 or later  
- 🌐 A valid M3U URL from an IPTV provider  

## 📺 Playlist configuration

Use **Settings → Manage Playlists** in the WinForms app to control how playlists show up in the menu:

- **ShowInMenu** – whether this playlist appears in the CHANNELS menu.
- **GroupByFirstChar** – group channels under A/B/C… based on their name.
- **UrlFind / UrlReplace** – optional regex pair applied to each channel URL (for fixing or normalizing stream URLs).
- **NameFind / NameReplace** – optional regex pair applied to each channel title (for example, strip prefixes like `HD :` or remove `S01E01` when grouping episodic channels).

## 🤝 Contributing
Contributions, issues, and feature requests are welcome!  
Feel free to check out the [issues page](../../issues) to get started.  

If you’d like to add new features, improve the UI, or just fix a typo, PRs are always appreciated.  

## 📚 Tech Stack
AndyTV is built with a modern .NET toolchain and a few carefully chosen libraries:

- ⚡ **.NET 10 / C# 14 Preview** — bleeding-edge performance and language features  
- 🖼️ **WinForms** — classic, lightweight Windows desktop UI  
- 🎵 **LibVLCSharp** — handles reliable media playback  
- 📦 **Velopack** — simple auto-updates & packaging  
- 🗂️ **System.Text.Json** — lightweight and fast JSON handling  
- 🛠️ **GitHub Actions** — CI/CD for build, test, and publish workflows  

## 📱 AndyTV.Maui (Mobile Companion)
`AndyTV.Maui` is a .NET MAUI companion app (in development) for iOS and Android that brings AndyTV-style playlist browsing to mobile.

Planned highlights:
- Browse and filter your IPTV playlists on phone/tablet.
- Quickly launch or hand off channels to your preferred mobile player.
- Share channels/devices with the desktop AndyTV app.

This project is experimental and under active development; APIs and features may change.

## ▶️ AndyTV.VLC (Companion)
`AndyTV.VLC` is a lightweight Blazor Server companion that lets you browse an IPTV playlist and launch any channel directly in VLC. Use it when you prefer VLC's player or want fast filtering/grouping.

**Highlights**
- Parse M3U (Title, Logo, Group, Stream URL)
- Instant search + paging (MudBlazor table)
- Dark mode
- One‑click launch in VLC

**Requires**
- .NET 10 SDK (or change target to `net8.0`)
- VLC installed (`C:/Program Files/VideoLAN/VLC/vlc.exe` default)

**Configure VLC path** (`appsettings.json`):
```json
"VLC": { "Path": "D:/Apps/VLC/vlc.exe" }
```

**Run locally**
```powershell
cd AndyTV.VLC
dotnet restore
dotnet run
```
Open https://localhost:5001 and paste your M3U URL.

> Security: Only the selected stream URL is passed to VLC; keep usage local/private.


## 📜 License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

[![Visitors](https://api.visitorbadge.io/api/VisitorHit?user=aherrick&repo=AndyTV&label=VISITORS&labelColor=%23222222&countColor=%23007ec6)](https://visitorbadge.io/status?path=https%3A%2F%2Fgithub.com%2Faherrick%2FAndyTV)  

Made with ❤️ for the Windows community.
