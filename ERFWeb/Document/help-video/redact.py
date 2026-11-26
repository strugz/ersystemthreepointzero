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
    "03c-new-report-department.jpg": [NAME_NEW, AVATAR],
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

# "Approving reports" video (prefix a-), captured at a 1200x675 viewport.
A_HEAD_REVIEW = (358, 10, 452, 32)
A_HEAD_LIST = (636, 10, 734, 32)
A_ACTIVITIES_TOP = (194, 80, 430, 170)
A_TIMELINE = (106, 222, 200, 274)
BOXES.update({
    "a-01-dashboard.jpg": [(550, 10, 650, 32), (180, 96, 268, 126), (86, 288, 200, 318), (86, 331, 200, 361), (86, 374, 200, 404)],
    "a-02-inbox.jpg": [A_HEAD_LIST, (80, 168, 136, 198), (166, 168, 240, 224), (166, 234, 240, 290), (166, 300, 240, 356)],
    "a-03-review-top.jpg": [A_HEAD_REVIEW, (72, 60, 188, 86)],
    "a-04-lines.jpg": [A_HEAD_REVIEW, (152, 338, 304, 354)],
    "a-05-crm-calendar.jpg": [A_HEAD_REVIEW, (194, 200, 430, 450)],
    "a-06-timeline-receipts.jpg": [A_HEAD_REVIEW, A_ACTIVITIES_TOP, A_TIMELINE],
    "a-07-check-receipts.jpg": [(118, 21, 232, 35)],
    "a-08-lower-amount.jpg": [A_HEAD_REVIEW, (72, 60, 245, 86)],
    "a-09-approve-dialog.jpg": [A_HEAD_REVIEW, (72, 60, 274, 86)],
    "a-10-return-dialog.jpg": [A_HEAD_REVIEW, (72, 60, 274, 86)],
    "a-11-reject-dialog.jpg": [A_HEAD_REVIEW, (72, 60, 274, 86)],
    "a-12-r47-approve.jpg": [(250, 8, 324, 38), (116, 138, 274, 156), (328, 228, 432, 242)],
    "a-13-acted-on.jpg": [A_HEAD_LIST, (160, 168, 236, 224)],
    "a-14-delegations.jpg": [A_HEAD_LIST, (86, 139, 166, 158)],
    "a-15-new-delegation.jpg": [A_HEAD_LIST, (230, 130, 332, 150), (86, 139, 166, 158)],
    "a-16-budgets.jpg": [(578, 10, 666, 32)],
    "a-17-summary.jpg": [(554, 10, 650, 32), (80, 196, 186, 224)],
    "a-18-summary-export.jpg": [(554, 10, 650, 32), (80, 196, 186, 224)],
    "a-19-l2-inbox.jpg": [(644, 10, 732, 32), (80, 168, 136, 198), (156, 168, 234, 224), (156, 234, 234, 290),
                          (156, 300, 234, 356), (592, 302, 692, 330)],
    "a-20-queries.jpg": [(644, 10, 732, 32)],
    "a-21-l2-delegation.jpg": [(644, 10, 732, 32), (230, 130, 342, 150), (230, 188, 328, 208)],
    "a-22-final-approved.jpg": [(572, 10, 660, 32), A_ACTIVITIES_TOP, A_TIMELINE],
    "a-23-override.jpg": [(506, 10, 594, 32), (72, 60, 274, 86)],
})
# "Finance" video (prefix f-), captured at a 1200x675 viewport. Real reports in the queue lose their whole left block.
F_HEAD_QUEUE = (632, 10, 734, 32)
F_HEAD_REVIEW = (560, 10, 658, 32)
BOXES.update({
    "f-01-queue.jpg": [F_HEAD_QUEUE, (84, 272, 330, 318), (84, 355, 330, 371), (84, 402, 330, 448)],
    "f-02-receipts.jpg": [F_HEAD_QUEUE, (352, 103, 478, 120), (84, 202, 248, 250), (84, 267, 248, 315), (84, 332, 248, 380)],
    "f-03-follow-up.jpg": [F_HEAD_QUEUE, (266, 145, 386, 162), (264, 203, 404, 221), (84, 202, 240, 250), (84, 267, 240, 315),
                           (84, 332, 240, 380)],
    "f-04-report.jpg": [F_HEAD_REVIEW, (72, 60, 272, 86)],
    "f-05-verification-card.jpg": [F_HEAD_REVIEW, (108, 311, 202, 328), (108, 344, 202, 361)],
    "f-06-ask-verify.jpg": [F_HEAD_REVIEW, (108, 311, 230, 328), (108, 344, 230, 361)],
    "f-07-signatures.jpg": [F_HEAD_QUEUE],
    "f-08-meal-claims.jpg": [(568, 10, 666, 32)] + [(80, y, 212, y + 26) for y in (224, 292, 360, 428)],
    "f-09-summary.jpg": [(553, 10, 652, 32), (80, 194, 200, 386)],
    "f-10-budgets.jpg": [(568, 10, 666, 32)],
    "f-11-received.jpg": [F_HEAD_QUEUE, (84, 202, 330, 248), (84, 285, 330, 303), (84, 314, 330, 330), (84, 346, 330, 394)],
    "f-12-clear.jpg": [F_HEAD_QUEUE, (348, 156, 478, 174), (84, 190, 248, 236), (84, 271, 248, 324), (84, 332, 248, 382)],
    "f-13-waiting-verifier.jpg": [F_HEAD_REVIEW, (94, 314, 212, 334)],
    "f-14-under-verification.jpg": [F_HEAD_QUEUE, (84, 272, 330, 318), (84, 354, 330, 372), (84, 384, 330, 400), (84, 402, 330, 450)],
})
# "Expense Verifier" video (prefix v-), captured at a 1200x675 viewport.
V_HEAD_LIST = (616, 10, 732, 32)
V_HEAD_REVIEW = (542, 10, 658, 32)
BOXES.update({
    "v-01-queue.jpg": [V_HEAD_LIST, (144, 166, 254, 181), (180, 211, 258, 224)],
    "v-02-report.jpg": [V_HEAD_REVIEW, (72, 60, 272, 86)],
    "v-03-card.jpg": [V_HEAD_REVIEW, (96, 155, 210, 175), (108, 297, 224, 313), (108, 330, 212, 346)],
    "v-04-picked-up.jpg": [V_HEAD_REVIEW, (96, 155, 210, 175), (116, 205, 208, 219), (154, 236, 304, 251),
                           (108, 317, 224, 333), (108, 350, 212, 366)],
    "v-05-receipts.jpg": [(118, 21, 232, 35)],
    "v-06-finding.jpg": [V_HEAD_REVIEW, (96, 135, 210, 155), (116, 186, 230, 200), (110, 217, 230, 232),
                         (108, 297, 224, 313), (108, 330, 224, 346)],
    "v-07-recorded.jpg": [V_HEAD_REVIEW, (96, 135, 210, 155), (116, 184, 208, 198), (96, 205, 226, 225),
                          (108, 357, 224, 373), (108, 390, 212, 406)],
    "v-08-my-findings.jpg": [V_HEAD_LIST, (86, 166, 192, 181), (120, 211, 204, 224)],
})


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
