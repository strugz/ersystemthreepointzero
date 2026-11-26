"""Blurs names and record numbers in the screenshots before the video is built.

Boxes are (left, top, right, bottom) in 800x450 space, scaled to larger screenshots. Reads shots/, writes shots_redacted/.
"""
import os
import shutil
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "shots")
DST = os.path.join(HERE, "shots_redacted")

# Filing video, captured sharp at 1920x1080 like the v- shots (employee Loid; From CRM shots as Alexander).
AVATAR = (6, 412, 40, 446)
NAME_LIST = (590, 9, 656, 31)
NAME_EDITOR = (467, 9, 532, 31)
NAME_NEW = (671, 9, 736, 31)
ROUTE_SIDE = (564, 382, 742, 408)
ROUTE_BEHIND = [(628, 95, 742, 113), (628, 115, 742, 133)]
LINE_DIALOG = [AVATAR] + ROUTE_BEHIND
BOXES = {
    "02-dashboard.jpg": [NAME_LIST, AVATAR, (168, 93, 207, 114)],
    "03-new-report.jpg": [NAME_NEW, AVATAR],
    "03b-new-report-type.jpg": [NAME_NEW, AVATAR],
    "03c-new-report-department.jpg": [NAME_NEW, AVATAR],
    "04-editor.jpg": [NAME_EDITOR, AVATAR, ROUTE_SIDE],
    "05-cash-advance.jpg": [NAME_EDITOR, AVATAR, ROUTE_SIDE],
    "06-line-categories.jpg": LINE_DIALOG,
    "07-meals.jpg": LINE_DIALOG,
    "08-line-added.jpg": LINE_DIALOG,
    "09-transport.jpg": LINE_DIALOG,
    "09b-crm-pick.jpg": [AVATAR, (630, 147, 742, 161)] + [(206, y - 8, 352, y + 8) for y in (174, 248, 323, 397)]
                        + [(206, y - 7, 300, y + 7) for y in (202, 276, 351)],
    "09c-crm-filled.jpg": [AVATAR, (630, 147, 742, 161), (180, 232, 330, 252), (408, 305, 492, 325)],
    "09d-or-vat-service.jpg": LINE_DIALOG,
    "10-same-visit.jpg": LINE_DIALOG,
    "11-lines.jpg": [NAME_EDITOR, AVATAR, (564, 143, 742, 169)],
    "11b-totals.jpg": [NAME_EDITOR, AVATAR, ROUTE_SIDE],
    "12-checklist.jpg": [NAME_EDITOR, AVATAR, (564, 277, 742, 303)],
    "13-receipts.jpg": [NAME_EDITOR, AVATAR],
    "14-file-confirm.jpg": [NAME_EDITOR, AVATAR, ROUTE_SIDE, (272, 185, 442, 199)],
    "15-my-reports.jpg": [NAME_LIST, AVATAR],
    "20-toll.jpg": LINE_DIALOG,
    "21-lodging.jpg": LINE_DIALOG,
    "22-gas-reason.jpg": LINE_DIALOG,
    "23-gas.jpg": LINE_DIALOG,
    "24-vehicle.jpg": LINE_DIALOG,
    "25-representation.jpg": LINE_DIALOG,
    "26-sponsorship.jpg": LINE_DIALOG,
    "27-demo.jpg": LINE_DIALOG,
    "28-others.jpg": LINE_DIALOG,
}

# "Approving reports" video (prefix a-), captured sharp at 1920x1080 like the v- shots.
A_HEAD_REVIEW = (385, 9, 472, 31)
A_HEAD_LIST = (645, 9, 738, 31)
A_HEAD_L2 = (655, 9, 736, 31)
A_TITLE = (68, 57, 253, 80)
A_APPROVERS = [(586, 206, 690, 219), (586, 237, 656, 250)]
A_ROWS = [(156, 160, 242, 184), (156, 223, 242, 247), (156, 286, 242, 310)]
BOXES.update({
    "a-01-dashboard.jpg": [(568, 9, 656, 31), (166, 93, 248, 114), (82, 272, 184, 286)],
    "a-02-inbox.jpg": [A_HEAD_LIST, (172, 159, 268, 173)],
    "a-03-review-top.jpg": [A_HEAD_REVIEW, A_TITLE] + A_APPROVERS,
    "a-04-lines.jpg": [(578, 9, 665, 31), (144, 297, 274, 310)] + A_APPROVERS,
    "a-05-crm-calendar.jpg": [(578, 9, 665, 31), (144, 44, 274, 57), (182, 240, 376, 450)] + A_APPROVERS,
    "a-06-timeline-receipts.jpg": [(578, 9, 665, 31), A_TITLE] + A_APPROVERS,
    "a-07-check-receipts.jpg": [(114, 19, 212, 32)],
    "a-08-lower-amount.jpg": [A_HEAD_REVIEW, A_TITLE] + A_APPROVERS,
    "a-09-approve-dialog.jpg": [A_HEAD_REVIEW, A_TITLE] + A_APPROVERS,
    "a-10-return-dialog.jpg": [A_HEAD_REVIEW, A_TITLE] + A_APPROVERS,
    "a-11-reject-dialog.jpg": [A_HEAD_REVIEW, A_TITLE] + A_APPROVERS,
    "a-12-r47-approve.jpg": [(271, 9, 357, 31), (174, 131, 270, 145), (336, 220, 404, 231)],
    "a-13-acted-on.jpg": [A_HEAD_LIST] + A_ROWS,
    "a-14-delegations.jpg": [A_HEAD_LIST, (82, 133, 152, 147)],
    "a-15-new-delegation.jpg": [A_HEAD_LIST, (82, 133, 152, 147)],
    "a-16-budgets.jpg": [(585, 9, 670, 31)],
    "a-17-summary.jpg": [(572, 9, 657, 31), (76, 184, 170, 198)],
    "a-18-summary-export.jpg": [(572, 9, 657, 31), (76, 184, 170, 198)],
    "a-19-l2-inbox.jpg": [A_HEAD_L2, (584, 286, 674, 310)] + A_ROWS,
    "a-20-queries.jpg": [A_HEAD_L2],
    "a-21-l2-delegation.jpg": [A_HEAD_L2],
    "a-22-final-approved.jpg": [(588, 9, 664, 31), A_TITLE] + A_APPROVERS,
    "a-23-override.jpg": [(525, 9, 609, 31), A_TITLE] + A_APPROVERS,
})
# "Finance" video (prefix f-), captured sharp at 1920x1080 like the v- shots. Real reports in the queue lose their names.
F_HEAD_QUEUE = (644, 9, 738, 31)
F_HEAD_REVIEW = (575, 9, 666, 31)
F_APPROVERS = [(586, 206, 690, 219), (586, 237, 656, 250)]
F_ROWS_SCROLLED = [(80, 222, 160, 236), (80, 283, 180, 297), (124, 310, 200, 322), (80, 357, 134, 371)]
BOXES.update({
    "f-01-queue.jpg": [F_HEAD_QUEUE, (80, 272, 160, 286), (80, 333, 180, 347), (124, 360, 200, 372), (80, 407, 134, 421)],
    "f-02-receipts.jpg": [F_HEAD_QUEUE, (358, 111, 446, 125)] + F_ROWS_SCROLLED,
    "f-03-follow-up.jpg": [F_HEAD_QUEUE, (276, 151, 362, 165), (288, 205, 381, 219)] + F_ROWS_SCROLLED,
    "f-11-received.jpg": [F_HEAD_QUEUE, (80, 272, 180, 286), (124, 300, 200, 312)],
    "f-12-clear.jpg": [F_HEAD_QUEUE, (356, 162, 469, 176)] + F_ROWS_SCROLLED,
    "f-04-report.jpg": [F_HEAD_REVIEW, (68, 57, 253, 80)] + F_APPROVERS,
    "f-05-verification-card.jpg": [F_HEAD_REVIEW, (90, 132, 198, 151), (109, 179, 236, 193), (90, 196, 210, 214),
                                   (90, 363, 198, 382), (109, 410, 236, 424), (90, 427, 210, 445)] + F_APPROVERS,
    "f-06-ask-verify.jpg": [F_HEAD_REVIEW, (90, 308, 198, 327), (109, 355, 236, 369), (90, 373, 210, 391),
                            (586, 205, 690, 219), (586, 236, 656, 250)],
    "f-13-waiting-verifier.jpg": [F_HEAD_REVIEW, (90, 53, 198, 72), (109, 100, 236, 114), (90, 118, 210, 136),
                                  (90, 284, 198, 303), (109, 331, 236, 345), (90, 348, 210, 366),
                                  (586, 180, 690, 194), (586, 211, 656, 225)],
    "f-07-signatures.jpg": [F_HEAD_QUEUE],
    "f-08-meal-claims.jpg": [(581, 9, 673, 31)] + [(76, y - 7, 172, y + 19) for y in (217, 281, 345, 409)],
    "f-09-summary.jpg": [(568, 9, 660, 31), (76, 184, 172, 362)],
    "f-10-budgets.jpg": [(581, 9, 673, 31)],
})
# "Expense Verifier" video (prefix v-), captured sharp at 1920x1080 (CSS 1280x720 at 1.5x) in Chrome.
V_HEAD_LIST = (628, 9, 736, 31)
V_HEAD_REVIEW = (558, 9, 666, 31)
V_APPROVERS_TOP = [(586, 117, 688, 130), (586, 148, 656, 161)]
BOXES.update({
    "v-01-queue.jpg": [V_HEAD_LIST, (80, 155, 182, 169), (111, 197, 183, 212)],
    "v-02-report.jpg": [V_HEAD_REVIEW, (68, 57, 253, 80), (586, 206, 688, 219), (586, 237, 656, 250)],
    "v-03-card.jpg": [V_HEAD_REVIEW, (90, 127, 198, 146), (109, 174, 236, 189), (90, 192, 210, 210),
                      (90, 358, 198, 377)] + V_APPROVERS_TOP,
    "v-04-picked-up.jpg": [V_HEAD_REVIEW, (90, 108, 198, 127), (109, 155, 236, 170), (90, 173, 210, 191),
                           (90, 339, 198, 358), (109, 386, 236, 401), (148, 416, 288, 431)] + V_APPROVERS_TOP,
    "v-05-receipts.jpg": [(114, 19, 212, 32)],
    "v-06-finding.jpg": [V_HEAD_REVIEW, (90, 53, 198, 72), (109, 99, 236, 114), (90, 117, 210, 135),
                         (90, 283, 198, 302), (109, 330, 236, 345), (148, 360, 240, 375),
                         (586, 61, 688, 74), (586, 92, 656, 105)],
    "v-07-recorded.jpg": [V_HEAD_REVIEW, (90, 53, 198, 72), (109, 100, 236, 115), (90, 118, 210, 136),
                          (90, 284, 198, 303), (109, 331, 236, 346), (90, 348, 210, 366),
                          (586, 180, 688, 194), (586, 211, 656, 225)],
    "v-08-my-findings.jpg": [V_HEAD_LIST, (80, 155, 182, 169), (111, 198, 183, 211), (80, 302, 182, 316),
                             (111, 345, 183, 358)],
})

# "System Administrator" video (prefix ad-), captured sharp at 1920x1080 as admin; forms filled and cancelled.
AD_CRUMB = (210, 10, 312, 30)
AD_TITLE = (176, 55, 364, 81)
AD_PROFILE_TOP = [(176, 78, 234, 93), (188, 169, 297, 185), (371, 169, 430, 185), (188, 260, 342, 276),
                  (371, 260, 450, 276), (371, 305, 507, 334)]
BOXES.update({
    "ad-02-users.jpg": [(184, 172, 334, 450)],
    "ad-05-user.jpg": [AD_CRUMB, AD_TITLE] + AD_PROFILE_TOP,
    "ad-06-roles.jpg": [AD_CRUMB, (188, 65, 297, 81), (371, 65, 430, 81), (188, 156, 342, 172), (371, 156, 450, 172),
                        (371, 201, 507, 230)],
    "ad-07-grant-role.jpg": [AD_CRUMB, (188, 77, 342, 93), (371, 77, 450, 93), (371, 122, 507, 151)],
    "ad-08-reset-password.jpg": [AD_CRUMB, AD_TITLE, (386, 155, 504, 169), (176, 78, 234, 93), (188, 169, 250, 185), (188, 260, 250, 276)],
    "ad-09-deactivate.jpg": [AD_CRUMB, AD_TITLE, (276, 205, 392, 219), (176, 78, 234, 93), (188, 169, 256, 185), (188, 260, 256, 276),
                            (371, 305, 507, 334)],
    "ad-10-scope.jpg": [(184, 192, 268, 450), (312, 192, 446, 450)],
    "ad-11-new-assignment.jpg": [(184, 192, 206, 450)],
    "ad-12-coverage.jpg": [(184, 194, 302, 450), (398, 280, 762, 450)],
    "ad-18-who-gets-what.jpg": [(570, 87, 692, 103), (560, 164, 670, 179)],
    "ad-37-rules-notifications.jpg": [(348, 221, 544, 237)],
    "ad-38-rules-signin.jpg": [(348, 66, 544, 82)],
    "ad-42-audit.jpg": [(486, 158, 727, 450)],
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
        k = im.width / 800  # boxes are in 800x450 space; sharp shots are larger
        for box in boxes:
            box = tuple(round(v * k) for v in box)
            im.paste(im.crop(box).filter(ImageFilter.GaussianBlur(7 * k)), box[:2])
        im.save(dst, quality=92)
    print("redacted", len(BOXES), "of", len(os.listdir(SRC)))


if __name__ == "__main__":
    main()
