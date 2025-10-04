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
- Logs all actions to `control.log`.
- Automatically flushes DNS cache after every change.

## Files
- `Program.cs` — source code
- `config.example.json` — configuration template
- `.gitignore` — excludes sensitive files (like `config.json`)
- `status.txt` — remote control file (stored in a separate GitHub repo)

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
  "token": "YOUR_GITHUB_TOKEN",
  "owner": "YOUR_USERNAME",
  "repo": "yt-control-state",
  "filePath": "status.txt",
  "intervalSeconds": "30",
  "hostsEntries": [
    "youtube.com",
    "www.youtube.com",
    "tiktok.com",
    "discord.com"
  ]
}