"""Builds the narrated filing video for the ERFWeb Help page. Steps: README.txt in this folder."""
import asyncio, json, os, shutil, subprocess
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import edge_tts
from scenes import SCENES

HERE = os.path.dirname(os.path.abspath(__file__))
SHOTS = os.path.join(HERE, "shots_redacted")
OUT = os.path.join(HERE, "out")
VIDEO = os.path.normpath(os.path.join(HERE, "..", "..", "Frontend", "erfweb-client", "public", "help", "erfweb-filing-guide.mp4"))
FFMPEG = os.environ.get("FFMPEG") or shutil.which("ffmpeg") or "ffmpeg"
FFPROBE = os.environ.get("FFPROBE") or shutil.which("ffprobe") or "ffprobe"
VOICE = "en-US-AndrewNeural"
W, H = 1280, 720
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
    f1, f2 = ImageFont.truetype(FONT_B, 54), ImageFont.truetype(FONT, 28)
    d.text((W // 2, H // 2 - 30), title, font=f1, fill=(240, 244, 255), anchor="mm")
    d.text((W // 2, H // 2 + 40), sub, font=f2, fill=ACCENT, anchor="mm")
    return im


def frame(shot, caption, n, total):
    im = Image.new("RGB", (W, H), BG)
    pic = Image.open(os.path.join(SHOTS, shot)).convert("RGB").resize((1120, 630), Image.LANCZOS)
    pic = pic.filter(ImageFilter.UnsharpMask(radius=1.2, percent=60, threshold=2))
    im.paste(pic, (80, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 630, W, H], fill=(16, 22, 38))
    d.rectangle([0, 630, W, 633], fill=ACCENT)
    d.text((80, 675), caption, font=ImageFont.truetype(FONT_B, 30), fill=(240, 244, 255), anchor="lm")
    d.text((W - 80, 675), f"{n} / {total}", font=ImageFont.truetype(FONT, 22), fill=ACCENT, anchor="rm")
    return im


def main():
    clips, chapters, start = [], [], 0.0
    steps = [s for s in SCENES if s[0] not in ("title", "end")]
    for i, (key, caption, text) in enumerate(SCENES):
        mp3 = os.path.join(OUT, f"{i:02d}.mp3")
        if not os.path.exists(mp3):
            asyncio.run(tts(text, mp3))
        dur = duration(mp3) + 0.9
        png = os.path.join(OUT, f"{i:02d}.png")
        if key == "title":
            card("ERFWeb: " + caption, "Employee filing guide").save(png)
        elif key == "end":
            card(caption, "Refer to the ERFWeb Employee Filing Guide for complete details").save(png)
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
    with open(os.path.join(HERE, "chapters.json"), "w", encoding="utf-8") as f:
        json.dump(chapters, f, indent=1)
    lst = os.path.join(OUT, "list.txt")
    with open(lst, "w", encoding="utf-8") as f:
        f.writelines(f"file '{c.replace(os.sep, '/')}'\n" for c in clips)
    raw = os.path.join(OUT, "raw.mp4")
    subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", lst, "-c", "copy", raw], check=True)
    subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-i", raw, "-c:v", "copy", "-af", "loudnorm=I=-16:TP=-1.5:LRA=11",
                    "-ar", "48000", "-c:a", "aac", "-b:a", "128k", "-movflags", "+faststart", VIDEO], check=True)
    print("total", round(duration(VIDEO), 1), "s ->", VIDEO)


if __name__ == "__main__":
    main()
