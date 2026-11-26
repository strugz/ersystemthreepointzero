ERFWeb Help videos: how to rebuild them
=======================================

The Help page (/help) plays the videos in Frontend/erfweb-client/public/help/ and jumps to the start
times in Frontend/erfweb-client/src/features/help/topics.ts. This folder builds them from screenshots
of the live site plus a narration read by Microsoft's online voices.

  name       scenes               video file                   chapters                  shown to
  filing     scenes.py            erfweb-filing-guide.mp4      chapters.json             everyone
  approving  scenes_approving.py  erfweb-approving-guide.mp4   chapters_approving.json   Level 1 and Level 2
  finance    scenes_finance.py    erfweb-finance-guide.mp4     chapters_finance.json     Finance
  verifier   scenes_verifier.py   erfweb-verifier-guide.mp4    chapters_verifier.json    Expense Verifier

Files
  scenes*.py     one entry per scene: (screenshot file, caption, narration text), in play order
  redact.py      blur boxes per screenshot (names, initials, approver, ERF no., CRM client, serial no.);
                 approving shots start with a-
  build.py       makes the voice clips, frames and the final video, and writes the chapters file
  chapters*.json start time (seconds) of every scene in the last build

Not in git (they hold real names): shots/, shots_redacted/, out/.

Needs: Python 3 with Pillow and edge-tts (pip install --user pillow edge-tts), ffmpeg and ffprobe
on PATH (or set FFMPEG / FFPROBE), Windows Segoe UI fonts, internet for the voice.
The narration text is sent to Microsoft's speech service; keep it generic (no names or data).

Steps
  1. Put the screenshots (800x450 from the built-in browser: filing at a 1024x576 viewport,
     approving at 1200x675) in shots/ with the file names used in the scenes file.
  2. Check redact.py covers every name, initial, approver, ERF no., client and serial no. on
     screen; adjust the boxes and look at shots_redacted/ before building.
  3. python redact.py
  4. python build.py filing   or   python build.py approving
     (delete that video's out\*.mp3 first when the narration text changed; approving uses out\approving)
     It writes the video straight into the SPA's public/help folder and updates the chapters file.
  5. Copy the new start times from the chapters file into src/features/help/topics.ts, and raise the
     ?v= number of that video in HELP_VIDEOS there so browsers fetch the new file, not a cached copy.
     A url in HELP_VIDEOS may also be a full https:// URL (file server or CDN) instead of the SPA file.
  6. Bump the SPA version and deploy as usual (README "Versions" rule).

Voice: en-US-AndrewNeural, rate -8%. Video: 1280x720 at 5 fps (H.264 CRF 30), mono AAC 32 kbps;
about 4.0 MB for the 7:00 filing video, 3.2 MB for the 6:06 approving video and 2.0 MB for the 3:26 Finance video and 1.2 MB for the 2:25 Expense Verifier video. Keep them small:
users on slow or mobile links download only what they play.

Test data for the approving video (2026-11-24, kept for the Expense Verifier and Finance videos):
ERF-2026-000005 (approved), -000006 (waiting Level 2), -000007 (waiting Level 1), purpose "TEST ...
will be deleted", claimant ALEXANDER H ANCLOTE, Level 1 Frederick T. Galicha, Level 2 Richard B. Garces.
The Finance video (2026-11-26, f- shots, Finance account Rosemarie Asuncion) recorded ERF-2026-000005's receipts
as received and opened one verification request on its Parking line for the Expense Verifier video; it is not cleared.
The Expense Verifier video (2026-11-26, v- shots, verifier account Donna Rose C. Parducho) picked that request up and
recorded it as Verified, so the request is closed and Finance can clear ERF-2026-000005.
