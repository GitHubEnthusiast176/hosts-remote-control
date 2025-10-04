# hosts-remote-control

Small Windows utility that manages access to websites by editing the `hosts` file.  
The state (`block` or `allow`) is controlled remotely via a `status.txt` file stored in a GitHub repository.

## Features
- Blocks or unblocks websites defined in `config.json`.
- Uses `hosts` file (`C:\Windows\System32\drivers\etc\hosts`) for blocking.
- Remote control via GitHub `status.txt`:
  - `0` = Block websites
  - `1` = Allow websites (comment out entries)
- Keeps a local `local_status.txt` to avoid unnecessary changes.
- Logs all actions to `control.log` with automatic log rotation.
- Automatically flushes DNS cache after every change.

## Installation

1. Clone this repository
2. Copy `config.json.example` to `config.json`
3. Edit `config.json` with your GitHub credentials
4. Build the project using the commands below
5. Run as Administrator (required for hosts file access)

## Build Commands

```bash
# Clean previous builds
dotnet clean

# Build the project
dotnet build

# Run the application
dotnet run

# Publish as self-contained executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Build Options Explained:
- `-c Release` → Release mode build
- `-r win-x64` → Build for Windows 64-bit
- `--self-contained true` → Executable works even if .NET is not installed
- `-p:PublishSingleFile=true` → Package everything into a single exe file

## Required Files

The `hosts-remote-control.exe` requires two files for operation:
- `config.json` — Configuration file with GitHub credentials and settings
- `local_status.txt` — Local status tracking file (created automatically)

Additional files:
- `control.log` — Log file for application events (auto-rotated based on `logRetentionDays` setting in config.json)

## How it works
1. The app checks `status.txt` in your GitHub repository.
2. If remote status differs from local one, it updates the `hosts` file:
   - `0` → adds `127.0.0.1 host # BLOCKED-BY-APP`
   - `1` → comments those lines (`# 127.0.0.1 host # BLOCKED-BY-APP`)
3. Saves new status to `local_status.txt`.
4. Writes logs to `control.log`.

## Example config.json
```json
{
  "token": "ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "owner": "YOUR_GITHUB_USERNAME",
  "repo": "hosts-remote-control",
  "filePath": "status.txt",
  "intervalSeconds": "30",
  "logRetentionDays": "7",
  "hostsEntries": [
    "youtube.com",
    "www.youtube.com",
    "m.youtube.com",
    "youtu.be",
    "img.youtube.com",
    "s.ytimg.com",
    "music.youtube.com",
    "gaming.youtube.com", 
    "studio.youtube.com", 
    "kids.youtube.com", 
    "youtube-nocookie.com", 
    "ytimg.com", 
    "i.ytimg.com", 
    "googlevideo.com",    
    "tiktok.com",
    "www.tiktok.com",
    "discord.com"
  ]
}
```

### Configuration Options:
- `token` — GitHub personal access token
- `owner` — GitHub username or organization name
- `repo` — Repository name (without full URL)
- `filePath` — Path to the status file in the repository
- `intervalSeconds` — How often to check for status changes
- `logRetentionDays` — Number of days to keep log files
- `hostsEntries` — Array of domains to block/allow

## Security Notes
- **Requires Administrator privileges** to modify the hosts file
- **Never commit `config.json`** with real GitHub tokens
- Keep GitHub token permissions minimal (only repo access needed)
- The application modifies system files - ensure you trust the source