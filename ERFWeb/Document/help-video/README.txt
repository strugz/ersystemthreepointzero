ERFWeb Help video: how to rebuild it
=====================================

The Help page (/help) plays Frontend/erfweb-client/public/help/erfweb-filing-guide.mp4 and jumps
to the start times in Frontend/erfweb-client/src/features/help/topics.ts. This folder builds that
video from screenshots of the live site plus a narration read by Microsoft's online voices.

Files
  scenes.py      one entry per scene: (screenshot file, caption, narration text), in play order
  redact.py      blur boxes per screenshot (names, approver, ERF no., CRM client, serial no.)
  build.py       makes the voice clips, frames and the final video, and writes chapters.json
  chapters.json  start time (seconds) of every scene in the last build

Not in git (they hold real names): shots/, shots_redacted/, out/.

Needs: Python 3 with Pillow and edge-tts (pip install --user pillow edge-tts), ffmpeg and ffprobe
on PATH (or set FFMPEG / FFPROBE), Windows Segoe UI fonts, internet for the voice.
The narration text is sent to Microsoft's speech service; keep it generic (no names or data).

Steps
  1. Put the screenshots (800x450, from the built-in browser at a 1024x576 viewport) in shots/
     with the file names used in scenes.py.
  2. Check redact.py covers every name, initial, approver, ERF no., client and serial no. on
     screen; adjust the boxes and look at shots_redacted/ before building.
  3. python redact.py
  4. python build.py   (delete out\*.mp3 first when the narration text changed)
     It writes the video straight into the SPA's public/help folder and updates chapters.json.
  5. Copy the new start times from chapters.json into src/features/help/topics.ts.
  6. Bump the SPA version and deploy as usual (README "Versions" rule).

Voice: en-US-AndrewNeural, rate -8%. Video: 1280x720 at 5 fps (H.264 CRF 30), mono AAC 32 kbps;
about 3.8 MB for 6:45. Keep it small: users on slow or mobile links download only what they play.
