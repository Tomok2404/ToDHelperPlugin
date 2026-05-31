# ToDHelper

**ToDHelper** is a Dalamud plugin designed to streamline the hosting of **Truth or Dare** games within FINAL FANTASY XIV. It automates the tedious parts of game management, allowing hosts to focus on the social experience.

## Features

*   **Automatic Roll Monitoring**: Automatically detects `/random` or `/dice` rolls in chat to determine winners or turn order.
*   **Player Balancing**: Tracks player participation to ensure everyone gets a fair turn and prevents "stacking" on specific players.
*   **Round Management**: Tools to track current participants, manage the round state, and keep the game moving smoothly.
*   **Customizable Settings**: Configure chat channels to watch, roll modes, and more.

## Installation

To install **ToD Helper**, you can add the custom plugin repository to Dalamud in-game.

### 1. In-Game Installation (Recommended)
1. Open FINAL FANTASY XIV and type `/xlsettings` in your chat window to open the Dalamud Settings.
2. Navigate to the **Experimental** tab.
3. Scroll down to the **Custom Plugin Repositories** section.
4. Copy and paste the following URL into the empty slot:
   ```
   https://raw.githubusercontent.com/Tomok2404/TomokPlugins/main/pluginmaster.json
   ```
5. Click the **+** (Add) button and then click **Save and Close** at the bottom.
6. Open `/xlplugins` in chat, search for **ToD Helper**, and click **Install**!

### 2. Developer / Manual Version (For Devs)
1. Build the plugin in your IDE or run `dotnet build`.
2. Open `/xlsettings` in-game and go to the **Experimental** tab.
3. Under **Dev Plugin Locations**, add the absolute path to your compiled `ToDHelperPlugin.dll`.
4. Open `/xlplugins` and enable the plugin from the **Installed Dev Plugins** category.

## Usage
Use `/tod` or `/todhelper` to open the main interface.

---



