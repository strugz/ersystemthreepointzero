import fs from "node:fs/promises";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = "../../outputs/weekly-summary-20260820";
await fs.mkdir(outputDir, { recursive: true });

const workbook = Workbook.create();
const sheet = workbook.worksheets.add("Weekly Summary");
sheet.showGridLines = false;

sheet.mergeCells("A1:C1");
sheet.getRange("A1").values = [["WEEKLY WORK SUMMARY"]];
sheet.mergeCells("A2:C2");
sheet.getRange("A2").values = [["Committed work only | August 17–20, 2026"]];

sheet.getRange("A4:C7").values = [
  ["Operational Highlights", "Challenge / Issue / Risk", "Action Plan"],
  [
    "Implemented employee-specific SMTP credential resolution and decryption for approval reminders, including caching, failure handling, integration updates, and comprehensive tests. (c84e978)",
    "Reminder email delivery now depends on valid employee SMTP records and decryptable legacy credentials; missing or invalid configuration can prevent delivery.",
    "Validate SMTP account coverage in the target database, monitor provider resolution failures after deployment, and maintain the new unit/integration tests as credential rules evolve."
  ],
  [
    "Replaced the legacy sp_Notify / D:\\ERSHARE SMS flow with direct HTTP calls to the MDMPI api_sendsms endpoint. Added manager-only targeting, duplicate prevention, startup validation, bounded timeouts, error handling, migration/rollback scripts, documentation, and tests. Backend build passed and all 68 tests passed. (05df3ec)",
    "The reminder service now relies on API availability, URL configuration, network connectivity, and correct rollout of the SmsApi database channel. Failed requests must not create false delivery claims or expose sensitive data in logs.",
    "Deploy the reviewed database script and worker configuration together, confirm endpoint connectivity and timeout settings, monitor HTTP failures and delivery claims, and retain the rollback script for controlled recovery."
  ],
  [
    "Completed and committed the ER System introduction presentation, including the final 'Roles and Next Steps' slide, detailed voice-over notes, layouts, and rendered review assets. (c84e978)",
    "Presentation render/intermediate files were committed under .codex-tmp, which may add repository noise and make the authoritative deliverable less obvious.",
    "Confirm the final PPTX is the approved source, archive or remove temporary render assets in a separate reviewed cleanup, and keep future presentation outputs in a clearly documented deliverables location."
  ]
];

sheet.getRange("A1:C1").format = {
  fill: "#17365D",
  font: { bold: true, color: "#FFFFFF", size: 18 },
  horizontalAlignment: "center",
  verticalAlignment: "center"
};
sheet.getRange("A2:C2").format = {
  fill: "#D9EAF7",
  font: { italic: true, color: "#334E68", size: 10 },
  horizontalAlignment: "center",
  verticalAlignment: "center"
};
sheet.getRange("A4:C4").format = {
  fill: "#00A6A6",
  font: { bold: true, color: "#FFFFFF", size: 11 },
  horizontalAlignment: "center",
  verticalAlignment: "center",
  wrapText: true,
  borders: { preset: "outside", style: "medium", color: "#137878" }
};
sheet.getRange("A5:C7").format = {
  font: { color: "#243B53", size: 10 },
  verticalAlignment: "top",
  horizontalAlignment: "left",
  wrapText: true,
  borders: {
    insideHorizontal: { style: "thin", color: "#B8C6D1" },
    insideVertical: { style: "thin", color: "#D5DEE5" },
    top: { style: "thin", color: "#B8C6D1" },
    bottom: { style: "thin", color: "#B8C6D1" },
    left: { style: "thin", color: "#B8C6D1" },
    right: { style: "thin", color: "#B8C6D1" }
  }
};
sheet.getRange("A5:C5").format.fill = "#F7FAFC";
sheet.getRange("A6:C6").format.fill = "#EAF4F4";
sheet.getRange("A7:C7").format.fill = "#F7FAFC";

sheet.getRange("A1:C1").format.rowHeight = 34;
sheet.getRange("A2:C2").format.rowHeight = 22;
sheet.getRange("A3:C3").format.rowHeight = 10;
sheet.getRange("A4:C4").format.rowHeight = 32;
sheet.getRange("A5:C5").format.rowHeight = 100;
sheet.getRange("A6:C6").format.rowHeight = 150;
sheet.getRange("A7:C7").format.rowHeight = 110;
sheet.getRange("A:A").format.columnWidth = 48;
sheet.getRange("B:B").format.columnWidth = 42;
sheet.getRange("C:C").format.columnWidth = 44;
sheet.freezePanes.freezeRows(4);

const preview = await workbook.render({
  sheetName: "Weekly Summary",
  range: "A1:C7",
  scale: 1.5,
  format: "png"
});
await fs.writeFile(`${outputDir}/weekly-summary-preview.png`, new Uint8Array(await preview.arrayBuffer()));

const check = await workbook.inspect({
  kind: "table",
  range: "Weekly Summary!A1:C7",
  include: "values,formulas",
  tableMaxRows: 10,
  tableMaxCols: 5
});
console.log(check.ndjson);

const errors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 100 },
  summary: "final formula error scan"
});
console.log(errors.ndjson);

const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(`${outputDir}/ER_System_Weekly_Work_Summary_2026-08-20.xlsx`);
