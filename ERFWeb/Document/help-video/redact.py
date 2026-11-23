"""Blurs names and record numbers in the screenshots before the video is built.

Boxes are (left, top, right, bottom) in the 800x450 screenshot. Reads shots/, writes shots_redacted/.
"""
import os
import shutil
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "shots")
DST = os.path.join(HERE, "shots_redacted")

AVATAR = (8, 402, 48, 442)
NAME_EDITOR = (372, 12, 466, 38)
NAME_WIDE = (526, 12, 622, 38)
NAME_NEW = (626, 12, 722, 38)

BOXES = {
    "02-dashboard.jpg": [NAME_WIDE, AVATAR, (204, 114, 274, 146), (100, 352, 362, 372)],
    "03-new-report.jpg": [NAME_NEW, AVATAR],
    "03b-new-report-type.jpg": [NAME_NEW, AVATAR],
    "04-editor.jpg": [NAME_EDITOR, AVATAR],
    "05-cash-advance.jpg": [NAME_EDITOR, AVATAR],
    "06-line-categories.jpg": [AVATAR],
    "07-meals.jpg": [AVATAR],
    "08-line-added.jpg": [AVATAR],
    "09-transport.jpg": [AVATAR],
    "10-same-visit.jpg": [AVATAR],
    "11-lines.jpg": [NAME_EDITOR, AVATAR],
    "11b-totals.jpg": [NAME_EDITOR, AVATAR],
    "12-checklist.jpg": [NAME_EDITOR, AVATAR, (98, 268, 202, 290)],
    "13-receipts.jpg": [NAME_EDITOR, AVATAR, (98, 148, 202, 170)],
    "14-file-confirm.jpg": [NAME_EDITOR, AVATAR, (98, 148, 222, 170), (282, 173, 478, 194)],
    "15-my-reports.jpg": [NAME_WIDE, AVATAR, (92, 196, 146, 250)],
    "09b-crm-pick.jpg": [(236, 238, 324, 256), (156, 256, 282, 278), (226, 292, 292, 311)],
    "09c-crm-filled.jpg": [(116, 168, 246, 190), (124, 254, 250, 276)],
    "09d-or-vat-service.jpg": [(306, 282, 378, 303), (492, 282, 588, 303)],
}


def main():
    os.makedirs(DST, exist_ok=True)
    for name in os.listdir(SRC):
        src, dst = os.path.join(SRC, name), os.path.join(DST, name)
        boxes = BOXES.get(name)
        if not boxes:
            shutil.copyfile(src, dst)
            continue
        im = Image.open(src).convert("RGB")
        for box in boxes:
            im.paste(im.crop(box).filter(ImageFilter.GaussianBlur(7)), box[:2])
        im.save(dst, quality=92)
    print("redacted", len(BOXES), "of", len(os.listdir(SRC)))


if __name__ == "__main__":
    main()
