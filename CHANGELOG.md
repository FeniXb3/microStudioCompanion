# CHANGELOG

### 2.3.0 - October 26, 2022

- Updated by HomineLudens to match features added to microStudio since the last release

### 2.2.0 - January 12, 2021

- Added new mode - `push`

### 2.1.0 - January 11, 2021

- Disabled cleaning project subdirectories before watching or pulling by default - made it optionally enabled
- Added command line argument:
    - `--clean-start` or `-c` - enables removing project subdirectories before watching or pulling

### 2.0.1 - Jan 9, 2021

- Fixed bug with projects having title different than slug

### 2.0.0 - Jan 8, 2021

- Added watch mode - real-time sync between local and remote project copy
- Added command line arguments:
    - `--mode`  or `-m` - defines mode the app should work in; possible values: `watch` , `pull`
    - `--slug` or `-s` - defines slug of the project to pull or watch; possible values: any valid slug of your project
    - `--timestamps` or `-t` - enables showing timestamps for each message
    - `--no-color` - disable coloring messages in the console window

### 1.0.0 Dec 30, 2020

- Initial release
- Pulling current version from microStudio