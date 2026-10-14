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
