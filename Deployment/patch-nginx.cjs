const fs=require('fs'),crypto=require('crypto'),path=require('path');
const configs=['/etc/nginx/nginx.conf'];
for(const dir of ['/etc/nginx/conf.d','/etc/nginx/sites-enabled'])if(fs.existsSync(dir))for(const name of fs.readdirSync(dir))if(fs.statSync(path.join(dir,name)).isFile())configs.push(path.join(dir,name));
const candidates=configs.map(file=>({file,source:fs.readFileSync(file,'utf8')})).filter(x=>/\blocation\s+\/\s*\{/.test(x.source));
if(!candidates.length)throw new Error('No nginx root location found in '+configs.join(', '));
const selected=candidates.find(x=>/chattrade_mvc|8081/.test(x.source))||candidates[0];
const file=selected.file;let source=selected.source;
const route=`
        location = /Games/Avatar { return 301 /Games/Avatar/; }
        location ^~ /Games/Avatar/ {
            alias /app/mvc/wwwroot/Games/Avatar/;
            index index.html;
            types {
                text/html html;
                text/css css;
                application/javascript js mjs;
                application/wasm wasm;
                application/json json;
                application/octet-stream vrm task;
                image/png png;
                image/jpeg jpg;
            }
            add_header Cache-Control "no-cache" always;
            add_header X-Content-Type-Options "nosniff" always;
            add_header Referrer-Policy "no-referrer" always;
            add_header Permissions-Policy "camera=(self), microphone=()" always;
            add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self'; img-src 'self' data: blob:; connect-src 'self' blob:; worker-src 'self' blob:; frame-ancestors 'self'" always;
        }
`;
if(!source.includes('location ^~ /Games/Avatar/')){
  const starts=[...source.matchAll(/\bserver\s*\{/g)].map(m=>m.index);
  const servers=starts.map((start,i)=>({start,text:source.slice(start,starts[i+1]??source.length)})).filter(s=>/\blocation\s+\/\s*\{/.test(s.text));
  let server=servers.find(s=>/proxy_pass\s+http:\/\/chattrade_mvc\b/.test(s.text));
  if(!server)server=servers.find(s=>/server_name\s+(?:[^;]*\s)?dingous\.com\.br(?=\s|;)/.test(s.text));
  if(!server)throw new Error('Cannot identify Dingous MVC server: '+servers.map(s=>(s.text.match(/server_name[^;]+;/)||['unnamed'])[0]).join(' | '));
  if((server.text.match(/\blocation\s+\/\s*\{/g)||[]).length!==1)throw new Error('Ambiguous root inside Dingous server');
  const offset=server.start+server.text.search(/\blocation\s+\/\s*\{/);
  source=source.slice(0,offset)+route+'\n        '+source.slice(offset);
  fs.writeFileSync(file,source);
}
const root='/app/mvc/wwwroot/Games/Avatar';
for(const name of ['index.html','studio.js','render-worker.js','browser-tracking-worker.js','assets/Seed-san.vrm','assets/face.task','assets/pose.task','assets/hand.task','vendor/mediapipe/package/vision_bundle.mjs'])if(!fs.existsSync(path.join(root,name)))throw new Error('Missing deployed asset '+name);
const hash=crypto.createHash('sha256');hash.update(fs.readFileSync('/app/deployment-fingerprint'));hash.update(source);
function files(dir){for(const name of fs.readdirSync(dir).sort()){const full=path.join(dir,name);if(fs.statSync(full).isDirectory())files(full);else{hash.update(path.relative(root,full));hash.update(fs.readFileSync(full));}}}
files(root);fs.writeFileSync('/app/deployment-fingerprint',hash.digest('hex')+'\n');
console.log('Avatar static route and assets verified; existing services preserved.');
