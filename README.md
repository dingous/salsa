# Salsa Avatar Studio

**MakeHuman/MPFB no Windows:** `Iniciar Avatar Studio.cmd` agora abre o Corporate
Avatar Studio com o humano corporativo. A versão anterior continua em
`Iniciar Avatar Studio Anterior.cmd`. Na interface antiga, abra **Modelo VRM →
Abrir MakeHuman / MPFB**. O projeto Unity novo está na pasta `CorporateUnity`.

**MakeHuman na web local:** recarregue a página e escolha **Modelo de avatar →
MakeHuman / MPFB**. Seed-san continua disponível no mesmo seletor. A versão do
domínio ainda precisa receber essa atualização; não foi publicada nesta etapa.

Aplicativo Unity 6 para usar **avatares profissionais VRM com UniVRM**, ou criar um **avatar procedural inspirado em uma foto**, e animá-los pela webcam. A saída de vídeo contém somente o personagem e o cenário, sem a foto de referência ou a interface.

**Versão web para chamadas:** abra `Iniciar Avatar Studio Web.cmd`, inicie o rastreamento e ative a câmera virtual. No Teams, selecione **OBS Virtual Camera**. A interface web renderiza modelos VRM usando three-vrm e uma ponte local entrega o canvas ao driver OBS, sem transmitir a interface ou a imagem bruta da webcam. [Detalhes e limites da versão web](Web/README.md).

O Studio abre com **Seed-san**, um modelo VRM 1.0 real com texturas, rig Humanoid, expressões e física de cabelo, por VirtualCast, Inc. A tela **Modelo VRM** permite importar arquivos `.vrm` 0.x ou 1.0, voltar à demonstração ou escolher o gerador procedural. Ao salvar um VRM na biblioteca, o arquivo é copiado junto do perfil e carregado novamente na próxima abertura. As expressões são mapeadas para os presets presentes no modelo, incluindo boca, piscadas, emoções e olhar; expressões customizadas com nomes MediaPipe/ARKit recebem os valores correspondentes. Dedos, braços, quadril, coluna, pernas e cabeça usam o rig normalizado do UniVRM. Um modelo com menos expressões não ganha automaticamente as 52 formas faciais do avatar procedural.

Seed-san © VirtualCast, Inc., sob VRM Public License 1.0, com crédito obrigatório. Fonte e licença acompanham o aplicativo em `StreamingAssets/Avatars`. O framework UniVRM permanece sob MIT. A opção de aparência Cartoon/Realístico edita o gerador procedural; a aparência de um VRM vem de suas malhas e texturas, podendo ser substituída pela importação de outro modelo. UniVRM não gera uma reconstrução profissional do rosto a partir da foto.

Escolha o estilo no seletor no topo da tela, antes ou depois de gerar pela foto. A biblioteca salva essa escolha com as proporções e cores. Trocar o estilo preserva os ajustes: Cartoon usa sombreado ilustrado; Realístico usa materiais físicos, cabeça mais proporcional e olhos menores. Ambos compartilham as 52 formas faciais e o controle de mãos e corpo. O modo Realístico é uma interpretação procedural da foto, não uma reconstrução fotogramétrica nem uma promessa de aparência fotográfica.

A interface se reorganiza ao redimensionar a janela: em telas amplas, editor e prévia aparecem lado a lado; em telas estreitas, os painéis ficam empilhados com rolagem vertical. Navegação, escolha de estilo e controles da prévia mudam de posição sem perder os valores editados. A câmera mantém proporção 16:9 e o layout respeita a área segura da tela. A responsividade da interface não equivale a suporte Android/iOS: o rastreador Python e a câmera virtual continuam destinados ao Windows.

## Abrir o aplicativo

Execute `Iniciar Avatar Studio.cmd` ou `Build/AvatarStudio.exe`. O executável e o ambiente Python foram preparados nesta máquina. Mantenha o executável, suas pastas auxiliares e a pasta `Tracker` dentro da estrutura deste projeto.

Para editar, adicione `D:\Git\Diego\Salsa` ao Unity Hub e abra `Assets/Scenes/AvatarStudio.unity` com **Unity 6000.4.5f1**. A cena antiga `WebcamAvatar.unity` continua disponível como demonstração técnica. O menu **Webcam Avatar > Criar Avatar Studio** recria a cena principal.

## Criar seu avatar

1. Em **Criar avatar**, clique em **Escolher foto** e selecione um PNG ou JPEG de até 25 MB. Use um retrato frontal, com apenas um rosto e boa iluminação.
2. Informe o nome e clique em **Gerar meu avatar**. A análise local extrai proporções faciais e cores de pele, cabelo e olhos para montar o personagem.
3. Em **Aparência**, ajuste as cores, o comprimento do cabelo e os óculos.
4. Use **Salvar avatar**. Os personagens ficam na biblioteca local e podem ser carregados novamente pelo seletor abaixo da prévia.

A foto é uma referência para um personagem estilizado. O gerador usa um modelo 3D paramétrico próprio; ele não reproduz exatamente a identidade, o penteado ou acessórios da imagem. Cabelo e óculos são ajustados na tela. Não há cobrança de API nem envio de fotos a serviços externos.

## Usar no lugar da webcam

1. Instale o [OBS Studio](https://obsproject.com/) com o componente de câmera virtual. Esse driver é necessário uma única vez.
2. Em **Transmissão**, selecione sua webcam e clique em **Iniciar rastreamento**. O processo é aberto automaticamente, sem uma janela mostrando seu rosto.
3. Olhe para a câmera e clique em **Calibrar**, abaixo da prévia.
4. Clique em **Ativar câmera virtual** no Studio. Mantenha a saída de câmera virtual do OBS desligada: o Studio envia diretamente os frames ao driver.
5. Na chamada de Meet, Zoom, Teams ou Discord, escolha **OBS Virtual Camera** como câmera.

O áudio continua vindo do microfone selecionado no aplicativo da chamada. Se a câmera virtual não aparecer, confira a instalação do OBS e reinicie o aplicativo da chamada. Se outro programa estiver usando a saída virtual do OBS, desligue essa saída antes de ativá-la no Studio.

Como alternativa, use **Tela limpa** e capture a janela do aplicativo no OBS; nesse fluxo, ative a câmera virtual **no OBS**. Use **Esc** ou **F11** para voltar à tela de edição.

## Controles

### Avatar avançado

O personagem principal agora usa uma malha facial esculpida com **52 blendshapes**, cavidade oral, dentes individuais e pálpebras deformáveis. A cabeça, o rosto e os lábios são superfícies 3D; as expressões deformam os vértices da malha. O cabelo usa mechas modeladas e movimento secundário, e a roupa tem geometria e acabamentos próprios.

As mãos possuem **30 juntas nos dedos**, além dos pulsos, e são animadas pelos 21 landmarks de cada mão. Para usar esse rastreamento no aplicativo, mantenha as mãos visíveis para a webcam. Na execução manual do rastreador, acrescente `--hands`.

Por padrão, rosto, corpo e mãos usam o mesmo frame e timestamp da webcam. O script `start.ps1` também ativa mãos. Na execução manual, `--hand-stride 2` reduz a frequência das mãos para máquinas mais lentas; o aplicativo usa a frequência completa.

Em **Aparência > Ajustes avançados do rosto**, altere nove proporções: rosto, olhos, distância entre olhos, nariz, volume e largura dos lábios, maxilar, bochechas e cabelo. **Desfazer** restaura as proporções que estavam ativas ao abrir o painel. **Concluir ajustes** aplica a edição; use **Salvar avatar** para persistir o resultado.

O seletor abaixo da prévia demonstra sorriso, surpresa, seriedade e fala. Isso ativa a **Prévia animada**, identificada na tela. Para acompanhar a webcam, inicie o rastreamento novamente ou desligue essa prévia.

O rig inclui a deformação `tongueOut` para fontes externas; o MediaPipe desta implementação não estima a língua. A foto continua sendo referência para proporções e cores de um personagem estilizado.

- **Prévia animada**: demonstra piscadas, sorriso, fala e movimento de cabeça sem abrir a webcam. O rastreamento desliga essa prévia automaticamente.
- **Calibrar**: define a orientação neutra da cabeça, quando há rosto detectado.
- **Espelhar movimentos**: ajusta o sentido dos movimentos.
- **Intensidade das expressões**: regula o quanto o personagem reage.
- **Fundo verde**: permite recorte por chroma key quando usado em composição no OBS.
- **Parar rastreamento**: libera a webcam física.
- **Desativar câmera virtual**: encerra a saída virtual.

A saída é **1280 × 720, até 30 fps**. A webcam real fornece somente os movimentos. A renderização é feita por uma câmera Unity separada da interface.

## Instalação em outra máquina

O aplicativo precisa de Python 3.11 ou 3.12, da pasta `Tracker` e do driver OBS para a saída virtual. Copie também as pastas auxiliares do executável, como `AvatarStudio_Data` e `MonoBleedingEdge`. Não copie somente o `.exe`.

Na nova máquina, execute uma vez:

```powershell
./Tracker/setup.ps1 -Python 'C:/caminho/python.exe'
```

O ambiente `.venv` é local à instalação. Não o trate como um runtime portátil. A configuração baixa os modelos oficiais do Google. Depois desse preparo, geração e rastreamento funcionam sem conexão com a internet.

## Dados locais

Os perfis e as pequenas fotos de referência ficam em `Application.persistentDataPath`, normalmente `%USERPROFILE%/AppData/LocalLow/Salsa/Avatar Studio`. As fotos originais permanecem no lugar escolhido pelo usuário. O rastreamento não grava imagens da webcam.

A comunicação local usa UDP `127.0.0.1:5055` para movimentos e TCP `127.0.0.1:5056` para a saída virtual. Somente uma instância do aplicativo deve usar essas portas.

## Qualidade do rastreamento

O personagem acompanha piscar, direção do olhar, abertura da boca, sorriso, sobrancelhas, bochechas, cabeça, braços, pulsos, dedos, quadril, tronco, coxas, joelhos, tornozelos e pés. Escolha **Corpo inteiro** no seletor abaixo da prévia para visualizar o rig completo; **Busto** mantém o enquadramento para chamadas.

O indicador da prévia informa rosto, número de mãos e corpo completo ou parcial. Partes com baixa confiança retornam suavemente ao repouso; dedos fora do quadro não são usados para calcular flexão. O quadril e a coluna usam os landmarks do corpo, independentemente da rotação da cabeça.

O deslocamento lateral e vertical usa a posição do quadril na imagem, com referência neutra definida automaticamente e atualizada em **Calibrar**. É uma estimativa em imagem, sem posicionamento absoluto em profundidade. Os dedos acompanham flexão e abertura lateral, além da orientação dos pulsos.

Um asterisco junto de **MÃOS** indica landmarks parciais. A confiança usada para as mãos é a informação de classificação fornecida pelo modelo, combinada com o enquadramento; ela não é uma medida validada da precisão de cada dedo nem garante ausência de oclusão.

Uma webcam monocular estima profundidade; oclusão, iluminação ruim e enquadramento incompleto afetam a pose. O corpo precisa estar enquadrado da cabeça aos pés para rastrear pernas e pés. A presença de controles e testes para todas as partes não significa precisão de 100%. Não há validação de precisão contra um sistema de captura de referência.

## Desenvolvimento e validação

- `Assets/WebcamAvatar/StudioUI.cs`: telas, biblioteca, criação e controles de transmissão.
- `CartoonAvatar.cs`: geometria 3D e expressões do personagem.
- `AdvancedFace.cs`: malha facial com blendshapes e aberturas dos olhos e boca.
- `AdvancedGeometry.cs`: mechas, superfícies da roupa e geometria dos dedos.
- `AvatarTracking.cs`: recepção, suavização, calibração e retorno ao neutro.
- `Tracker/generate_avatar.py`: análise da foto e geração do perfil.
- `Tracker/virtual_camera.py`: envio ao driver OBS via `pyvirtualcam`.
- `Assets/WebcamAvatar/Editor/StudioValidation.cs`: validação de integração e build Windows.

```powershell
./Tracker/.venv/Scripts/python.exe ./Tracker/test_backend.py
./Tracker/.venv/Scripts/python.exe ./Tracker/validate.py --camera 0
```

O teste de geração usa, quando presente, `Validation/reference.png`: a foto pública da astronauta Eileen Collins distribuída como fixture no scikit-image. Essa foto não faz parte dos assets do aplicativo e não é usada como avatar padrão.

## Tecnologia e licenças

[MediaPipe Face Landmarker](https://ai.google.dev/edge/mediapipe/solutions/vision/face_landmarker/python) e [Pose Landmarker](https://ai.google.dev/edge/mediapipe/solutions/vision/pose_landmarker/python) fazem a análise. MediaPipe é Apache 2.0. A saída usa [pyvirtualcam](https://github.com/letmaik/pyvirtualcam), GPL-2.0, em um processo Python separado, e o driver OBS; respeite os termos dessas dependências se distribuir o aplicativo.

O rastreamento de dedos usa [MediaPipe Hand Landmarker](https://ai.google.dev/edge/mediapipe/solutions/vision/hand_landmarker/python). A topologia facial foi criada para este projeto; `Tracker/build_face_topology.py` permite recriar o asset original.

SALSA não foi importado: nenhum pacote licenciado foi fornecido, e o fluxo implementado utiliza expressões obtidas diretamente da webcam. O nome do projeto foi preservado como marca do Studio; não há dependência da API SALSA.

## Aparência do UniVRM no Unity

Com um VRM ativo, as paletas de Aparência alteram os materiais de pele, cabelo, olhos e roupa do modelo, sem trocar para o avatar procedural. A coloração multiplica as texturas existentes: detalhes e desenhos da roupa permanecem. O modelo Seed-san possui materiais reconhecidos; outros modelos precisam usar nomes identificáveis (hair, eye, skin, cloth ou outfit). Em Modelo VRM, restaure as cores originais ou selecione apresentação Profissional/Corporativo. As cores e o perfil corporativo são guardados junto do avatar ao salvar na biblioteca. Penteado, óculos e proporções são definidos pela geometria do VRM.
