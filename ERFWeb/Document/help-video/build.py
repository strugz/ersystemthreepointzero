"""Builds a narrated video for the ERFWeb Help page: python build.py [filing|approving|finance|verifier] [--hd]. Steps: README.txt here."""
import asyncio, importlib, json, os, shutil, subprocess, sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import edge_tts

HERE = os.path.dirname(os.path.abspath(__file__))
SHOTS = os.path.join(HERE, "shots_redacted")
PUBLIC_HELP = os.path.normpath(os.path.join(HERE, "..", "..", "Frontend", "erfweb-client", "public", "help"))
# name: (scenes module, work folder, video file, chapters file, title-card subtitle, guide named on the end card)
VIDEOS = {
    "filing": ("scenes", "out", "erfweb-filing-guide.mp4", "chapters.json", "Employee filing guide",
               "ERFWeb Employee Filing Guide"),
    "approving": ("scenes_approving", os.path.join("out", "approving"), "erfweb-approving-guide.mp4",
                  "chapters_approving.json", "Signatory guide: Level 1 and Level 2", "ERFWeb Signatory Guide"),
    "finance": ("scenes_finance", os.path.join("out", "finance"), "erfweb-finance-guide.mp4",
                "chapters_finance.json", "Finance guide", "ERFWeb Finance Guide"),
    "verifier": ("scenes_verifier", os.path.join("out", "verifier"), "erfweb-verifier-guide.mp4",
                 "chapters_verifier.json", "Expense Verifier guide", "ERFWeb Expense Verifier Guide"),
}
ARGS = [a for a in sys.argv[1:] if not a.startswith("--")]
NAME = ARGS[0] if ARGS else "filing"
# --hd: the 1920x1080 download copy (Download on the Help page). Reuses the voice clips of the normal build, so run that first.
HD = "--hd" in sys.argv
SCENES_MODULE, OUT_DIR, VIDEO_FILE, CHAPTERS_FILE, SUBTITLE, GUIDE = VIDEOS[NAME]
SCENES = importlib.import_module(SCENES_MODULE).SCENES
VOICE_DIR = os.path.join(HERE, OUT_DIR)
OUT = os.path.join(VOICE_DIR, "hd") if HD else VOICE_DIR
VIDEO = os.path.join(PUBLIC_HELP, VIDEO_FILE.replace(".mp4", "-hd.mp4") if HD else VIDEO_FILE)
FFMPEG = os.environ.get("FFMPEG") or shutil.which("ffmpeg") or "ffmpeg"
FFPROBE = os.environ.get("FFPROBE") or shutil.which("ffprobe") or "ffprobe"
VOICE = "en-US-AndrewNeural"
S = 1.5 if HD else 1.0
W, H = int(1280 * S), int(720 * S)
BG = (11, 16, 28)
ACCENT = (160, 190, 255)
FONT = r"C:\Windows\Fonts\segoeui.ttf"
FONT_B = r"C:\Windows\Fonts\segoeuib.ttf"
os.makedirs(OUT, exist_ok=True)


async def tts(text, path):
    for attempt in range(4):
        try:
            await edge_tts.Communicate(text, VOICE, rate="-8%").save(path)
            return
        except edge_tts.exceptions.NoAudioReceived:
            if os.path.exists(path):
                os.remove(path)
            await asyncio.sleep(3 * (attempt + 1))
    raise RuntimeError(f"no audio for: {text[:40]}")


def duration(path):
    r = subprocess.run([FFPROBE, "-v", "error", "-show_entries", "format=duration", "-of", "json", path],
                       capture_output=True, text=True, check=True)
    return float(json.loads(r.stdout)["format"]["duration"])


def card(title, sub):
    im = Image.new("RGB", (W, H), BG)
    d = ImageDraw.Draw(im)
    f1, f2 = ImageFont.truetype(FONT_B, int(54 * S)), ImageFont.truetype(FONT, int(28 * S))
    d.text((W // 2, H // 2 - int(30 * S)), title, font=f1, fill=(240, 244, 255), anchor="mm")
    d.text((W // 2, H // 2 + int(40 * S)), sub, font=f2, fill=ACCENT, anchor="mm")
    return im


def frame(shot, caption, n, total):
    im = Image.new("RGB", (W, H), BG)
    top, side = int(630 * S), int(80 * S)
    pic = Image.open(os.path.join(SHOTS, shot)).convert("RGB").resize((W - 2 * side, top), Image.LANCZOS)
    pic = pic.filter(ImageFilter.UnsharpMask(radius=1.2 * S, percent=60, threshold=2))
    im.paste(pic, (side, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([0, top, W, H], fill=(16, 22, 38))
    d.rectangle([0, top, W, top + int(3 * S)], fill=ACCENT)
    d.text((side, int(675 * S)), caption, font=ImageFont.truetype(FONT_B, int(30 * S)), fill=(240, 244, 255), anchor="lm")
    d.text((W - side, int(675 * S)), f"{n} / {total}", font=ImageFont.truetype(FONT, int(22 * S)), fill=ACCENT, anchor="rm")
    return im


def main():
    clips, chapters, start = [], [], 0.0
    steps = [s for s in SCENES if s[0] not in ("title", "end")]
    for i, (key, caption, text) in enumerate(SCENES):
        mp3 = os.path.join(VOICE_DIR, f"{i:02d}.mp3")
        if not os.path.exists(mp3):
            if HD:
                raise SystemExit(f"missing {mp3}: run python build.py {NAME} first")
            asyncio.run(tts(text, mp3))
        dur = duration(mp3) + 0.9
        png = os.path.join(OUT, f"{i:02d}.png")
        if key == "title":
            card("ERFWeb: " + caption, SUBTITLE).save(png)
        elif key == "end":
            card(caption, f"Refer to the {GUIDE} for complete details").save(png)
        else:
            frame(key, caption, steps.index((key, caption, text)) + 1, len(steps)).save(png)
        mp4 = os.path.join(OUT, f"{i:02d}.mp4")
        subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-loop", "1", "-framerate", "30", "-i", png, "-i", mp3,
                        "-filter_complex",
                        f"[0:v]fade=t=in:st=0:d=0.35,fade=t=out:st={dur - 0.35:.2f}:d=0.35,format=yuv420p[v];"
                        f"[1:a]adelay=300|300,apad,aresample=48000[a]",
                        "-map", "[v]", "-map", "[a]", "-t", f"{dur:.2f}",
                        "-c:v", "libx264", "-preset", "medium", "-crf", "20", "-tune", "stillimage",
                        "-c:a", "aac", "-b:a", "128k", "-ac", "2", mp4], check=True)
        clips.append(mp4)
        chapters.append({"scene": key, "caption": caption, "start": round(start, 1)})
        start += duration(mp4)
        print(f"{i:02d} {dur:5.1f}s {caption}")
    if not HD:
        with open(os.path.join(HERE, CHAPTERS_FILE), "w", encoding="utf-8") as f:
            json.dump(chapters, f, indent=1)
    lst = os.path.join(OUT, "list.txt")
    with open(lst, "w", encoding="utf-8") as f:
        f.writelines(f"file '{c.replace(os.sep, '/')}'\n" for c in clips)
    raw = os.path.join(OUT, "raw.mp4")
    subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", lst, "-c", "copy", raw], check=True)
    # Streamed copy kept small for slow and mobile links: still screens need only 5 fps, one voice 32 kbps mono.
    # The HD download trades size for cleaner text and voice.
    crf, audio = ("22", ["-b:a", "96k", "-ac", "1", "-ar", "48000"]) if HD else ("30", ["-b:a", "32k", "-ac", "1", "-ar", "24000"])
    subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-i", raw,
                    "-c:v", "libx264", "-preset", "veryslow", "-crf", crf, "-tune", "stillimage", "-r", "5", "-g", "250",
                    "-af", "loudnorm=I=-16:TP=-1.5:LRA=11", "-c:a", "aac", *audio,
                    "-movflags", "+faststart", VIDEO], check=True)
    print("total", round(duration(VIDEO), 1), "s ->", VIDEO)


if __name__ == "__main__":
    main()
