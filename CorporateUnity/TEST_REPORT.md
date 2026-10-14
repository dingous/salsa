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
