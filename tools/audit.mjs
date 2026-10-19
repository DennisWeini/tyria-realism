import fs from 'node:fs';import path from 'node:path';
const root=path.resolve(import.meta.dirname,'..');
const excluded=new Set(['.git','bin','obj','dist','runtime','reports','captures']);
let count=0;
function walk(dir){for(const e of fs.readdirSync(dir,{withFileTypes:true})){if(excluded.has(e.name))continue;const f=path.join(dir,e.name);if(e.isDirectory()){walk(f);continue;}const rel=path.relative(root,f);if(/\.(dll|exe|bin|addon64|addon32|dmp|log|csv|zip)$/i.test(f))throw new Error('Forbidden publication file '+rel);const s=fs.readFileSync(f,'utf8');if(/C:\\Users\\|E:\\Codex\\|S:\\Games\\|gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{30,}/.test(s))throw new Error('Private path or credential pattern '+rel);count++;}}
walk(root);
const p=JSON.parse(fs.readFileSync(path.join(root,'data/project.json'),'utf8'));
if(!p.tested.length||!p.compatibility.some(x=>x.confidence.includes('untested'))||!p.modelSources.length)throw new Error('Missing evidence/source matrix');
for(const name of ['Tyria-Universal-Lite.ini','Tyria-Elora-Balanced.ini','Tyria-Elora-Quality-DepthTest.ini','Tyria-NVIDIA-RTX50-Neural.ini']){const s=fs.readFileSync(path.join(root,'presets',name),'utf8');if(!s.includes('Techniques='))throw new Error('Missing preset techniques');}
for(const name of ['Install-NvidiaRtx50.ps1','Uninstall-NvidiaRtx50.ps1','Show-HardwareSupport.ps1']){if(!fs.existsSync(path.join(root,'scripts',name)))throw new Error('Missing variant script '+name);}
console.log(`Publication boundary audit passed: ${count} text/source files. No proprietary binary payloads or private paths detected.`);
