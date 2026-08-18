import fs from "node:fs/promises";
import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const source = "C:/Users/JayBryanCAbaoag/Documents/VuexJaysWayFile/VuexJaysWayFile/Project/ER System/ER System 3.0/outputs/ERF System Introduction with Updated Short Voice-Over Notes.pptx";
const outDir = "C:/Users/JayBryanCAbaoag/Documents/VuexJaysWayFile/VuexJaysWayFile/Project/ER System/ER System 3.0/.codex-tmp/voiceover-update";

const presentation = await PresentationFile.importPptx(await FileBlob.load(source));
const snapshot = await presentation.inspect({
  kind: "slide,textbox,shape,image,notes,layout",
  include: "id,slide,name,title,text,textPreview,bbox,alt,isPlaceholder,placeholders",
  maxChars: 100000,
});
await fs.writeFile(`${outDir}/deck-inspect.ndjson`, snapshot.ndjson, "utf8");
for (const [index, slide] of presentation.slides.items.entries()) {
  const number = String(index + 1).padStart(2, "0");
  const png = await presentation.export({ slide, format: "png", scale: 1 });
  await fs.writeFile(`${outDir}/slide-${number}.png`, new Uint8Array(await png.arrayBuffer()));
}
const montage = await presentation.export({ format: "webp", montage: true, scale: 1 });
await fs.writeFile(`${outDir}/montage.webp`, new Uint8Array(await montage.arrayBuffer()));
console.log(`slides=${presentation.slides.items.length}`);
