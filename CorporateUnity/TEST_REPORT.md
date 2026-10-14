# Validação — Corporate Avatar Studio

Data: 14/10/2026. Windows, Unity 6000.4.5f1, UniVRM 0.131.3,
MPFB 2.0.18, Blender 5.2.2, MediaPipe Python 0.10.21.

## Resultados

- Build Windows: PASS, `CorporateUnity/Build/CorporateAvatarStudio.exe`.
- Humano MakeHuman real: 13 malhas, 52 ossos humanoides, 52 controles ARKit,
  15 visemes e três morphs de identidade. Texturas incorporadas no VRM 1.0.
- Estilos realista/estilizado, três cabelos, barba, óculos e materiais: PASS.
- Layout e limites dos botões em 390, 768, 1366 e 2560 pixels: PASS.
- 24 direções de braços e 16 de pernas, incluindo ambos os lados e os dois
  modos de espelhamento: PASS. Cada posição é verificada após estabilização;
  este teste não mede latência nem precisão de reconhecimento humano.
- 30 articulações dos dedos nos dois modos de espelhamento: 60 verificações PASS.
- Identificação de uma mão isolada, piscadas independentes, rotação da cabeça
  e calibração neutra: PASS.
- 52 canais faciais exercitados individualmente por UDP: PASS. Retorno ao
  neutro após perda de dados: PASS.
- Foto de referência processada localmente e aplicada ao MakeHuman: PASS.
  A foto adapta cores e proporções; não reconstrói a identidade em 3D exato.
- Perfil JSON: PASS. Biblioteca e arquivos usam armazenamento separado do
  aplicativo anterior. O website e seus cookies anteriores foram preservados.
- Câmera virtual real: Unity → JPEG/TCP 5060 → pyvirtualcam/OBS → receptor
  DirectShow. PASS: 541 quadros em 18,00 s, 1280×720, 30,06 FPS no receptor.
- Janela oculta e troca para tela limpa: PASS. Renderização explícita corrige
  a imagem preta; conversão linear/sRGB preserva as cores do vídeo.
- Webcam física aberta e pacotes locais recebidos: PASS. Encerramento normal
  libera os processos Python do ambiente virtual e o dispositivo.
- Nenhuma imagem da webcam física foi salva; a evidência visual contém somente
  o avatar renderizado. O teste de tela limpa usa prévia animada.

## Limites observados

No teste inicial, não houve rosto, corpo ou mãos detectados na webcam.
Assim, a precisão dos gestos de uma pessoa ao vivo ainda precisa de validação
com alguém devidamente enquadrado. Os testes de pose/mãos/expressões acima
verificam o mapeamento dos dados para o rig, usando dados de referência.

O rastreador fornece 51 controles faciais correspondentes ao ARKit e o neutro;
não fornece `tongueOut`. Os visemes estão no modelo, mas não há reconhecimento
fonético a partir do áudio. A voz da chamada vem do microfone selecionado no
Teams/Meet/Zoom. Não foi executada uma reunião real nessas plataformas.

A câmera virtual transporta vídeo opaco. Chroma key permite recorte no OBS.
FPS observado no receptor não equivale à taxa de inferência de pose; Full e
Heavy dependem da CPU. Latência completa e precisão de reconhecimento não foram
quantificadas. Os resultados não constituem garantia de rastreamento de 100%.

## Reprodução e evidências

`CorporateAvatarStudio.exe --corporate-smoke -logFile <arquivo>` executa os testes
sintéticos. `Tracker/.venv/Scripts/python.exe Tools/e2e_corporate_camera.py`
executa a validação com webcam 0 e OBS Virtual Camera 1.

Logs locais: `Validation/corporate-build.log`, `Validation/corporate-player.log`,
`Validation/corporate-live-player.log`, `Validation/corporate-camera-result.json`.
Imagens locais: `CorporateUnity/Validation/corporate-realistic.png`,
`CorporateUnity/Validation/corporate-stylized.png`,
`Validation/corporate-virtual-output.png`. Logs, imagens de teste e builds
permanecem fora do Git.

SHA-256 do modelo:
`298e21442c74ef8653b9f369b0d1bf4c1e1d2c835447929f1d653ff4c98288ae`.

## Reexecução do E2E — 14/10/2026

- Modelo MakeHuman/MPFB confirmado no projeto Unity: 13 malhas, 52 ossos e
  67 expressões customizadas (52 ARKit + 15 visemes).
- E2E do editor/rig: 356 verificações PASS, processo encerrado com código 0.
- Primeira inicialização do teste de hardware: crash nativo do Mono durante
  `System.Xml.XmlTextReaderImpl` / inicialização de `System.Net.Sockets`.
  Evidência preservada em `Validation/corporate-live-crash.log`.
- Repetição do hardware: PASS, 541 quadros em 18 segundos, 720p a 30,06 FPS.
  Desta vez houve detecção de rosto e pose corporal; não houve detecção de mãos.
- O crash não se reproduziu na repetição e sua causa não foi corrigida ou
  isolada. Portanto a estabilidade de inicialização não está 100% validada.
- A versão web ainda carrega Seed-san; o MakeHuman pertence ao projeto Unity
  corporativo e não foi integrado nem publicado no website.

## Integração visível Windows/web

O início padrão do Windows agora abre Corporate Avatar Studio. O início anterior
permanece em `Iniciar Avatar Studio Anterior.cmd`, e o Studio antigo ganha um
botão para abrir MakeHuman/MPFB em Modelo VRM. A web local tem seletor MakeHuman
ou Seed-san e controles de estilo, cabelo, barba e óculos, com preferências
guardadas em cookies. O modelo agora usa metadados VRM 1.0 aceitos pelo loader web.

PASS: importação no Three-VRM, 46 verificações no rig MakeHuman, estilos e
acessórios, regressões de filtros/cookies/mãos e 15 verificações do servidor.
O teste Node usa decodificação de texturas simulada e não valida renderização.
A inspeção do navegador local foi bloqueada pela política da ferramenta.
Essa versão web ainda não foi publicada no domínio.

## Windows arm and finger correction — 2026-10-14

Corporate Windows player rebuilt successfully. Arms now hold brief occlusions and ease into a bent seated pose when wrists leave view, with torso-relative fallback and a 150-degree elbow limit after direction smoothing. Finger rest twist now follows wrist roll; finger directions are smoothed, and non-finite landmarks are rejected. Native synthetic smoke run: 356 checks passed, exit 0, CORPORATE_RESULT=True. Log: Validation/corporate-hands-player.log. Real-user webcam gesture accuracy remains to be validated; this run did not open the physical camera.

## Seated/head reach IK — 2026-10-14

Windows arms now solve a two-bone wrist target scaled by shoulder width and actual avatar limb lengths. Near-face gestures use the avatar head anchor. Overfolded observations use the visible elbow to stabilize the closest anatomically reachable target. Existing UDP tests now verify wrist reach; added analytic seated/head targets and elbow limits. Synthetic smoke uses isolated port 15059 so an open user session on 5059 cannot consume fixtures. Final native run: 362 checks passed, exit 0, CORPORATE_RESULT=True (Validation/corporate-ik-release.log). Webcam gesture accuracy and physical hand/head contact remain pending real-user validation. The already running player must be restarted to load the new assemblies.

## Real webcam gesture E2E — 2026-10-14

Command: `Tracker/.venv/Scripts/python.exe Tools/e2e_corporate_camera.py --gestures`.
The user performed seated head-reach, arm raise/lower and finger open/close gestures. This mode disables synthetic preview and records aggregate landmark/rig measurements plus avatar output only; no raw webcam images are saved.

The first real run failed head reach despite detecting face, body and both hands. Arm IK incorrectly required a visible elbow, causing a relaxed fallback when the elbow was occluded near the head. IK now accepts a visible wrist with shoulders, using the last reliable upper-arm direction as its bend pole when the elbow is hidden. A second capture also exposed one-wrist hand assignment: it previously required both pose wrists. A nearby single visible pose wrist now takes precedence over the selfie category; a distant wrist cannot steal the other hand. Five Python assignment regression tests passed.

Final real run: PASS. OBS DirectShow received 1801 frames in 60 seconds, 1280 x 720, 30.02 FPS. Face, body and hands were detected. The minimum tracked wrist-to-nose distances were 0.187 / 0.097 meters; minimum avatar wrist-to-head-center distances during near-face observations were 0.258 / 0.143 meters. Observed wrist-height excursions were 0.263 / 0.335 meters. Tracked finger curl excursions were 65.3 / 52.3 degrees; avatar curl excursions were 66.8 / 66.1 degrees.

Acceptance criteria: both sides observed, tracked near-face wrist under 0.23 m with avatar head proximity under 0.30 m, arm-height excursion over 0.25 m, tracked finger curl excursion over 20 degrees and avatar curl excursion over 15 degrees. Finger audit uses the mean index/middle/ring/little proximal bend; thumb and individual joints are covered by synthetic checks, not isolated real-camera gestures. These aggregate thresholds validate gesture transmission and reach; they do not establish exact hand/head contact, perfect finger fidelity, absence of mesh collisions, latency, or accuracy in all lighting/occlusion conditions. Avatar distances refer to a rig head center, not a physical surface contact point. Saved sample counters are rendering-loop observations and include repeated tracking frames.

Artifacts: `Validation/corporate-camera-result.json`, `CorporateUnity/Validation/corporate-gesture-audit.json`, `Validation/corporate-gesture-before.json`, `Validation/corporate-live-player.log` (local ignored diagnostic files).

Final rebuilt player regression after the real-camera fixes: 362 checks passed, exit 0, CORPORATE_RESULT=True (Validation/corporate-final-regression.log).

## Adaptive smoothing — 2026-10-14

One Euro filtering now uses the derivative between consecutive raw samples instead of displacement from the filtered output. Both 21-point hands receive a dedicated filter (minimum cutoff 1.5 Hz, speed gain 3); body uses 1.2 Hz and 0.65. UDP drainage applies filtering once to the newest valid packet, preventing artificial same-time filter updates for queued packets. A missing hand observation is held for up to 150 ms and then released; a gap over 400 ms resets filter history. Existing angular speed limits and direction smoothing remain active. Head-anchor blending uses SmoothStep across the 0.30-to-0.23 m proximity band, removing an abrupt target switch.

Technique source: https://gery.casiez.net/1euro/ and the authors' CHI 2012 paper https://gery.casiez.net/publications/CHI2012-casiez.pdf. Implementation is local; no new paid runtime dependency.

Synthetic tests: resting noise energy reduced at least 75%; a 20 cm intentional step reached within 2 cm of target in 500 ms; short loss holds, long loss releases. The previous real webcam gesture PASS predates this smoothing update. These benchmarks do not establish zero webcam jitter or measured real-world latency after the update.
Native rebuilt player: 366 checks passed, exit 0, CORPORATE_RESULT=True. Log: Validation/corporate-smoothing.log.

## Windows wardrobe — 2026-10-14

Added six official CC0 MakeHuman garments (male_casualsuit01 through 06) alongside male_elegantsuit01. Editor choices: suit/tie, blue shirt with jeans, long-sleeved knit top, striped shirt with jeans, blue T-shirt, casual jacket, white T-shirt. Each option has its own fitted skinned mesh and embedded texture. clothingStyle 0..6 is persisted and sanitized; older profiles default to the existing suit. Selector fits within the Appearance card and is enabled only for the corporate model.

The generator bakes only the intersection of skin regions hidden by all seven garments, plus the shared shoe mask. This fixes visible skin protrusions without permanently cutting out arms needed by short sleeves. Accessory head masks remain unbaked. Inspected actual rendered captures for all seven choices (CorporateUnity/Validation/wardrobe-0.png through wardrobe-6.png). The full native suite passed 428 checks, including exclusive garment visibility, selection serialization, original facial/limb regressions, shader support and responsive views. Log: Validation/corporate-wardrobe-final.log. No real-camera collision test was performed for every garment and extreme pose.

Rebuilt Windows application and updated bundled model/license (82,524,092-byte VRM). The web model remains its previous smaller version; this wardrobe update targets CorporateUnity Windows.

## Direct avatar selector — 2026-10-14

Windows header now switches Cartoon (procedural avatar) / Corporativo (realistic MakeHuman) inside the same application. Corporate reuse retains loaded model and clothing selection. Header synchronizes with actual active model, and selection is blocked during loading. Rebuilt player regression: 430 checks passed, exit 0. Log: Validation/corporate-mode-selector.log.

## Cartoon VRM selector correction — 2026-10-14

Cartoon now loads the bundled Seed-san VRM, replacing the prior procedural shortcut. Corporate loads MakeHuman. Header synchronizes by loaded model type rather than merely whether any VRM is active. Seed model and license included in Windows distribution. Updated player: 430 checks passed, exit 0; both model transitions verified. Log: Validation/corporate-cartoon-vrm.log.

## Windows publication routing fix — 2026-10-14

Root Unity Windows export now routes through Tools/publish-windows.ps1 and the CorporateUnity build. Root Build/AvatarStudio.exe and publish/Windows/AvatarStudio.exe include both models and the direct selector. Packaging copies every native build dependency, including D3D12, dstorage.dll and dstoragecore.dll; omission of these libraries prevented the initial packaged player from initializing. Model and UI assembly SHA256 checks detect stale/incomplete copies. The published executable was tested from publish/Windows: 430 synthetic checks passed, exit 0, CORPORATE_RESULT=True, including actual Cartoon/Corporate transitions. Root exporter scripts compiled successfully in the isolated Unity validation project. Existing legacy build preserved under Build/Anterior. The Python venv is prepared for this machine; a different machine must prepare Tracker dependencies and OBS separately. Artifacts: Validation/windows-published-e2e.log and Validation/windows-export-compile.log.

## Both-model movement E2E — 2026-10-14

The native suite now exercises the loaded Seed-san Cartoon and MakeHuman Corporate rigs independently with both mirror settings: head reach with occluded elbows, arm raise/lower, elbow limits, all 30 finger joints open/close, jaw opening, unilateral blinking, head rotation, each leg forward/back and torso lean. The preexisting MakeHuman suite continues checking six arm directions, leg directions, 52 facial channels, materials, wardrobe and UI. Final run: 622 checks passed, exit 0, CORPORATE_RESULT=True. Log: Validation/cartoon-corporate-full-e2e.log. Web rig/model tests and five Python hand-assignment tests passed too.

The first expanded run exposed Cartoon wrists remaining below the shoulder on an upward pose. Limb reference vectors now use named anatomical endpoints rather than the first transform child. Limb aiming follows the current segment in the parent coordinate space, preserving imported transforms, with existing angular speed limits; it avoids an extra quaternion low-pass after already-filtered landmarks/directions. Added wrist target and arm alignment diagnostics. Updated CorporateUnity/Build, Build/AvatarStudio.exe and publish/Windows/AvatarStudio.exe; package assembly/model hashes verified by the exporter.

Limits: arm raise/lower tests assert the direction of wrist height, not exact endpoint error. Seed-san still shows forearm direction/endpoint deviation in the bent upward fixture (observed target wrist height around 0.19 m vs actual around 0.05–0.10 m above shoulder during debugging). Thus this result must not be described as perfect IK, exact head contact or flawless motion. Hidden-elbow head reach threshold is 0.30 m from rig head pivot. Extreme pose mesh collisions, real stationary webcam jitter, latency and isolated real thumb fidelity remain unverified in this run. A new two-model real-webcam test was requested, but user participation has not yet been confirmed; no new physical-camera gesture run was performed for this update.

Real camera runner supports `--gestures --cartoon` for Cartoon and `--gestures` for Corporate, 75 seconds each, no synthetic preview. Model-specific log/result/avatar-output files preserve the evidence separately; raw webcam images are not stored.

## Endpoint precision and geometry audit — 2026-10-14

Fixed the residual Cartoon forearm reach defect reported in the previous run. Tracked arm IK now filters camera landmarks once, rather than smoothing upper and lower solved directions independently. The forearm targets the actual wrist goal from the current elbow; goals are clamped to the reachable chain length and 150-degree bend envelope. Forearm angular compensation allows up to 720 deg/s vs the upper arm's 360 deg/s, so a moving parent does not consume the entire child rotation budget. Fallback occlusion direction smoothing remains active. No acceptance threshold was relaxed.

New native checks require wrist endpoint error under 4 cm for raises/lowers, bake the visible skinned meshes and reject non-finite vertices or vertices beyond a 3 m radius from the hip, and verify that normalized bone local positions remain unchanged. Both actual VRM models, both arms and mirror modes passed. In the final deterministic fixtures, wrist endpoint errors were around float precision; this is simulation accuracy, not measured physical webcam accuracy.

Final rebuilt application: 658 checks passed, exit 0, CORPORATE_RESULT=True. Log: Validation/motion-geometry-final.log. Updated CorporateUnity/Build, Build/AvatarStudio.exe and publish/Windows/AvatarStudio.exe, with model/assembly hashes checked by the publisher. A cleanup of generated duplicate executable/data files was rejected by automatic review without a detailed reason; the copies were preserved and updated instead.

Geometry tests detect broken attachments, invalid values and gross skinning explosions. They do not detect every mesh self-intersection, cloth/skin penetration, occlusion error, or camera/lighting condition. The new real-camera gesture test still requires the user's participation; no new physical-camera motion PASS is claimed for these changes.

## Real camera follow-up — 2026-10-14

Both physical-camera runs used current binaries, 75-second gesture windows, no synthetic preview. Each OBS DirectShow receiver captured 1801 frames over 60 seconds at 1280x720 / 30.02 FPS; face, pose and hands were detected. Both complete gesture audits returned FAIL and are not approved.

Cartoon: one tracked wrist approached the nose at 0.194 m and avatar head pivot at 0.150 m; the other wrist/head gesture and arm excursion were not recorded with sufficient confidence. Detected finger curl excursions were only 11.6 / 3.0 degrees, below the required 20 degrees. Corporate: both head-reach gestures recorded (avatar head-pivot distances 0.285 / 0.194 m). One arm excursion was only 0.127 m (minimum 0.25 m); one hand curl excursion was only 12.7 degrees (minimum 20 degrees). The other arm/hand met those criteria.

These results do not establish whether missing coverage arose from framing, detector confidence, gesture execution or side assignment. No threshold was relaxed and no detection data was invented. Wider framing and a side-by-side repeat were requested. Artifacts: Validation/cartoon-camera-result.json, Validation/corporate-camera-result.json, Validation/cartoon-live-player.log, Validation/corporate-live-player.log. Raw webcam images were not saved.

## Wider-framing repeat — 2026-10-14

User authorized another real capture for both models. Each virtual receiver again delivered 1801 frames at 1280x720 / 30.02 FPS. Both complete audits remained FAIL; no thresholds were relaxed.

Cartoon: both near-head observations passed (avatar head distances 0.160 / 0.095 m) and both finger curl excursions passed (29.9 / 37.2 degrees tracked). Arm height excursions were 0.192 / 0.209 m, below 0.25 m.

Corporate: both finger curl excursions passed (50.2 / 42.5 degrees tracked). One near-head observation passed; the other tracked wrist-to-nose minimum was 0.240 m vs a 0.23 m threshold, so avatar head proximity was not accepted for that side. Arm height excursions were 0.118 / 0.121 m, below 0.25 m.

The runner uses StudioUI's default Webcam 0. User was asked to confirm whether this is the camera used for their gestures before another run or tracker changes. Missing amplitude does not by itself establish a rig bug; camera source, framing and pose detection confidence remain unresolved. Model-specific logs/results contain the repeated run; first-run results were preserved locally as Validation/cartoon-camera-first-result.json and Validation/corporate-camera-first-result.json. No raw webcam images saved.


## Webcam 0 diagnostic and framing preview — 2026-10-14

User confirmed Webcam 0. The latest Corporate real-camera run remained FAIL: 1614 unique tracking packets, with 1576 / 1123 wrist estimates outside normalized image bounds and only 9 / 474 hand observations. These are detector estimates, not proof of actual physical framing. OBS output delivered 1801 frames at 720p / 30.02 FPS. No raw camera images were stored.

Added optional Transmissão → Ver enquadramento da webcam. The local mirrored OpenCV preview marks shoulders, elbows and wrists green when in bounds and confidence >= 0.5, red otherwise. It does not record video. Windows packages include the updated tracker. Gesture audits now count each packet once, record image/model arm heights and confidence/coverage, begin the 75-second window only after camera/tracker readiness, and sample after the VRM LateUpdate (execution order 13000 vs 12000). Acceptance thresholds remain unchanged. Runner supports --preview for guided capture.

Rebuilt Windows synthetic run: 658 checks PASS and one FAIL (photo reference customizes actual MakeHuman model), exit 1, CORPORATE_RESULT=False. Arm, finger, mirror, endpoint and mesh geometry checks passed for both actual models; complete suite is not approved. Log: Validation/motion-preview-audit.log. Python compilation and five hand-assignment regression tests passed. Real gesture E2E remains unapproved pending visible two-sided framing and repeat.


## Guided Webcam 0 capture after preview confirmation — 2026-10-14

User confirmed both sides green in the updated local preview. Actual Cartoon capture PASS, exit 0: 1089 unique samples; 483 / 487 hand samples; avatar wrist-to-head minima 0.153 / 0.179 m; source arm excursions 0.464 / 0.473 m; tracked finger curl excursions 78.2 / 47.8 degrees, avatar 69.6 / 55.3 degrees. OBS received 1801 frames at 1280x720 / 30.02 FPS. This verifies these aggregate real gesture criteria, not absence of every possible distortion, isolated finger fidelity or measured stationary jitter.

First Corporate capture FAIL: both head proximity and hand curl passed, but one source arm excursion was 0.180 m vs >0.25 m required (other 0.364 m). Avatar head minima 0.213 / 0.248 m. Evidence preserved in Validation/corporate-framing-first-result.json. Second Corporate capture FAIL with no detected hands, 561 pose samples and 557 / 560 estimated wrists outside the image. No accepted arm/head observations were available. This second capture cannot validate the requested gestures. Both receivers delivered 1801 frames at 30.02 FPS.

No movement code or thresholds changed during these captures. No raw webcam images saved. Main/master updated with results; complete two-model physical E2E remains unapproved, as does the previously failed photo-reference synthetic check.


## Corporate real Webcam 0 PASS — 2026-10-14

User requested another physical E2E and main push. Current Corporate runtime passed unchanged thresholds with Webcam 0 and local framing preview: exit 0, CORPORATE_LIVE_RESULT=True, 1026 unique pose packets, 186 / 356 hand samples. Avatar wrist-to-head minima 0.181 / 0.153 m; source arm height excursions 0.479 / 0.409 m; tracked finger curl excursions 76.2 / 66.0 degrees; avatar curl excursions 78.0 / 65.7 degrees. OBS DirectShow received 1801 frames over 60.01 s at 1280x720 / 30.01 FPS. Source wrists still had low-confidence/out-of-frame periods; the run validates aggregate requested gestures under observed conditions, not uninterrupted tracking or perfect isolated fingers. No raw webcam images saved.

Both models now have successful actual physical gesture captures (Cartoon documented above, Corporate in this capture). Their arm/hand retarget implementation is shared; no new rig animation defect was reproduced, so no speculative motion change was made. Existing corrections were validated. This does not resolve the separate photo-reference check failure or certify all distortion/jitter conditions.

Fixed runner evidence bug: Cartoon capture no longer overwrites corporate-camera-result.json; each model writes its own result and failure message references it. Native binaries remain the already updated builds. Evidence: Validation/corporate-camera-result.json and Validation/corporate-live-player.log.


## Movement continuity update — 2026-10-14

Addressed abrupt loss/recovery transitions: individual low-confidence landmarks retain filtered positions up to 150 ms before release, preserving filter continuity during brief occlusion. Wrists/finger joints retain last local pose up to 180 ms after hand loss, then relax with exponential smoothing rate 4/s instead of the normal tracking rate. Elbow pole directions use exponential interpolation at 10/s before analytic IK, preserving the wrist endpoint rather than independently smoothing solved limb halves. State clears on model reload. Both models use these changes. Holding positions is interpolation through missing measurements, not new detector observations; current gesture audit counts may include held observations.

Rebuilt native suite: 661 PASS, no FAIL, exit 0, CORPORATE_RESULT=True (including photo reference on this execution). Added tests that a brief individual occlusion preserves pose and expired occlusion releases it. Geometry/endpoint/rig/mirror/finger suites passed for both actual models. Log: Validation/motion-continuity.log.

New physical Webcam 0 captures with user performing slow gestures both PASS. Corporate: 1215 unique packets, hand samples 454 / 691, avatar head distances 0.232 / 0.229 m, source arm excursions 0.439 / 0.306 m; avatar finger curl excursions 83.1 / 79.0 degrees. Cartoon: 891 packets, hand samples 300 / 428, avatar head distances 0.132 / 0.102 m, source arm excursions 0.629 / 0.638 m; avatar curl excursions 82.0 / 76.4 degrees. Each OBS receiver captured 1801 frames at 1280x720 / 30.02 FPS. No raw camera images saved. Logs/results are model-specific in Validation.

Windows packages CorporateUnity/Build, Build/AvatarStudio.exe, publish/Windows/AvatarStudio.exe and preserved duplicate CorporateAvatarStudio were updated; managed assembly SHA256 verified equal. Naturalness is subjective and the real audit measures aggregate reach/curl/amplitude, not jerk or stationary jitter. The automated dropout tests validate the new continuity behavior, but do not certify perfect fingers, uninterrupted tracking or every distortion scenario.
