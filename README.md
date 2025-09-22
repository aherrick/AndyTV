<img src="https://raw.githubusercontent.com/aherrick/AndyTV/main/AndyTV.png" alt="AndyTV logo" width="128"/>

[![Build](https://github.com/aherrick/AndyTV/actions/workflows/build.yml/badge.svg)](https://github.com/aherrick/AndyTV/actions/workflows/build.yml)
[![Publish](https://github.com/aherrick/AndyTV/actions/workflows/publish.yml/badge.svg)](https://github.com/aherrick/AndyTV/actions/workflows/publish.yml)
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

## 📜 License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

Made with ❤️ for the Windows community.
