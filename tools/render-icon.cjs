// Regenerate the PNG and Windows ICO from the vector master. Requires Node.js + sharp.
// Run from the repository root: node tools/render-icon.cjs
const fs = require('node:fs');
const path = require('node:path');
const sharp = require('sharp');
async function main() {
  const root = path.resolve(__dirname, '..');
  const svg = fs.readFileSync(path.join(root, 'assets', 'dubbox.svg'));
  const sizes = [16, 20, 24, 32, 40, 48, 64, 96, 128, 256];
  const frames = [];
  for (const size of sizes) frames.push(await sharp(svg, {density: 384}).resize(size, size).png().toBuffer());
  const header = Buffer.alloc(6 + sizes.length * 16);
  header.writeUInt16LE(1, 2); header.writeUInt16LE(sizes.length, 4);
  let offset = header.length;
  for (let index = 0; index < sizes.length; index++) {
    const pos = 6 + index * 16;
    header[pos] = header[pos + 1] = sizes[index] === 256 ? 0 : sizes[index];
    header.writeUInt16LE(1, pos + 4); header.writeUInt16LE(32, pos + 6);
    header.writeUInt32LE(frames[index].length, pos + 8); header.writeUInt32LE(offset, pos + 12);
    offset += frames[index].length;
  }
  fs.writeFileSync(path.join(root, 'Source', 'SteamIdleManager', 'logo.ico'), Buffer.concat([header, ...frames]));
  await sharp(svg, {density: 192}).resize(512, 512).png().toFile(path.join(root, 'assets', 'dubbox.png'));
  console.log('Generated transparent PNG and ICO: ' + sizes.join(', ') + ' px.');
}
main().catch(error => { console.error(error); process.exitCode = 1; });
