# MAUI Blazor Integration for AndyTV

## Summary

I've successfully integrated MAUI Blazor into your WinForms application, allowing you to display the TV Guide in a native web control. The Blazor components have been extracted to a shared library for reuse.

## What Was Done

### 1. Created AndyTV.Guide.Shared Project
- **Location**: `AndyTV.Guide.Shared/`
- **Purpose**: Shared Blazor component library that can be used by both the standalone web app and the WinForms app
- **Contents**:
  - `Components/GuideComponent.razor` - The TV guide schedule component (extracted from AndyTV.Guide)
  - `_Imports.razor` - Shared Blazor imports
  - Project references to AndyTV.Data and Syncfusion packages

### 2. Updated AndyTV.Guide Project
- Modified `Pages/Guide.razor` to use the shared `GuideComponent`
- Added project reference to `AndyTV.Guide.Shared`
- Simplified to just render the shared component

### 3. Enhanced AndyTV WinForms Project
- **Added NuGet Packages**:
  - `Microsoft.AspNetCore.Components.WebView.WindowsForms` (10.0.0-rc.1.25452.6)
  - `Syncfusion.Blazor.Schedule` (31.1.21)
  - `Syncfusion.Blazor.Themes` (31.1.21)
- **Added Project Reference**: `AndyTV.Guide.Shared`
- **Created New Files**:
  - `UI/GuideForm.cs` - Windows Form that hosts the Blazor web view
  - `wwwroot/index.html` - HTML host page for the Blazor component
- **Modified Form1.cs**: Added "Guide" menu item in the Channels menu (at the top)

### 4. Updated Solution File
- Added `AndyTV.Guide.Shared` project to the solution

## How to Use

### Opening the Guide
1. Run the AndyTV application
2. Right-click to open the context menu
3. Navigate to **Channels → Guide**
4. A new window will open displaying the TV guide schedule

### Architecture Benefits
1. **Code Reuse**: The guide component is shared between the web app and desktop app
2. **Single Source of Truth**: Updates to the guide component automatically apply to both apps
3. **Native Integration**: The guide runs natively in the WinForms app (no browser needed)
4. **Consistent UI**: Same look and feel across web and desktop

## Technical Details

### GuideForm.cs
- Uses `BlazorWebView` control from MAUI
- Configures Syncfusion Blazor services
- Registers HttpClient for data fetching
- Hosts the `GuideComponent` in a 1400x900 window

### Data Flow
1. GuideComponent fetches guide data from GitHub (https://raw.githubusercontent.com/aherrick/AndyTV/guide/guide.json)
2. Data is processed and displayed in a Syncfusion schedule control
3. Shows grouped by Category → Channel with timeline view

## Files Modified
- `AndyTV/AndyTV.csproj` - Added MAUI Blazor packages and project reference
- `AndyTV/Form1.cs` - Added Guide menu item
- `AndyTV.Guide/AndyTV.Guide.csproj` - Added shared project reference
- `AndyTV.Guide/Pages/Guide.razor` - Simplified to use shared component
- `AndyTV.sln` - Added shared project

## Files Created
- `AndyTV.Guide.Shared/AndyTV.Guide.Shared.csproj`
- `AndyTV.Guide.Shared/_Imports.razor`
- `AndyTV.Guide.Shared/Components/GuideComponent.razor`
- `AndyTV/UI/GuideForm.cs`
- `AndyTV/wwwroot/index.html`

## Next Steps (Optional)

### Potential Enhancements
1. **Click to Play**: Add click handler in GuideComponent to notify parent form when user clicks a show
2. **Themed UI**: Customize the Syncfusion theme to match AndyTV's look
3. **Caching**: Cache the guide data to reduce network calls
4. **Refresh Button**: Add ability to refresh guide data without reopening
5. **Keyboard Shortcuts**: Add keyboard navigation in the guide

### Example: Adding Click-to-Play
To make shows clickable and play them in AndyTV, you could:
1. Add an event callback to `GuideComponent`
2. Handle click events on schedule items
3. Pass the channel URL back to `GuideForm`
4. Have `GuideForm` communicate with `Form1` to play the selected channel

## Build Status
✅ AndyTV.Data - Build Successful
✅ AndyTV.Guide.Shared - Build Successful  
✅ AndyTV - Build Successful (with 1 warning about WindowsBase version conflict - safe to ignore)
✅ AndyTV.Guide - Build Successful

The WindowsBase warning is a dependency resolution notice from the WebView2 component and does not affect functionality.
