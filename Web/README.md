# Avatar Studio Web → Teams

Abra **Iniciar Avatar Studio Web.cmd** na raiz do projeto. O atalho inicia a ponte local e abre http://127.0.0.1:8765 no navegador. Não requer que o Unity esteja aberto.

1. Escolha um modelo VRM, ou use Seed-san.
2. Clique em **Iniciar rastreamento**. A webcam física é capturada pelo rastreador MediaPipe local; o navegador recebe somente os pontos e expressões.
3. Clique em **Ativar câmera virtual**. O canvas do avatar é enviado como JPEG 1280×720 para a ponte local, que entrega o vídeo ao driver OBS Virtual Camera a 30 fps. A taxa de atualização efetiva depende do hardware e da renderização.
4. No Teams, selecione **OBS Virtual Camera** como câmera. No aplicativo desktop, fica em Configurações → Dispositivos → Câmera. Na versão web, use os controles de câmera da tela de entrada ou da reunião. Mantenha o microfone usual; esta ponte não envia áudio.
5. Mantenha a página aberta. Ao terminar, desligue a câmera virtual e o rastreamento. Fechar a página tenta liberar os dispositivos imediatamente; a ponte também os libera após 15 segundos sem atividade do cliente.

A renderização e o envio usam OffscreenCanvas em um Web Worker para continuar quando outra aba recebe foco. Use Chrome ou Edge atualizados. Suspensão do computador, congelamento/descarte da aba ou modos de economia do navegador ainda podem interromper a atualização. Após 1,2 segundo sem imagem nova, a ponte envia um quadro preto, evitando deixar uma imagem antiga em transmissão.

O OBS Studio precisa estar instalado para registrar o dispositivo no Windows, mas não precisa ficar aberto. Deixe a câmera virtual do próprio OBS desligada quando esta ponte estiver ativa: só um produtor pode usar o mesmo driver. O OBS 32.2.2 foi instalado e o driver respondeu nesta máquina.

A versão local usa a ponte Python no Windows. A versão hospedada usa MediaPipe Tasks Vision 0.10.21 (Apache-2.0) em um worker, com modelos e WASM locais ao site. Frames da webcam não são enviados ao servidor. Um site não registra um dispositivo de câmera no sistema: para Teams, use Tela limpa e captura de janela no OBS ou abra o aplicativo local. A ponte continua escutando somente 127.0.0.1 e rejeitando outras origens.

Bibliotecas locais: Three.js 0.186.1 e @pixiv/three-vrm 3.5.5 (MIT). O renderer web usa three-vrm, enquanto o projeto Unity usa UniVRM; ambos importam modelos VRM. As importações de Three.js em três-vrm e nos utilitários GLTF foram ajustadas para caminhos locais, pois Web Workers não herdam import maps. Os pacotes originais e suas licenças ficam em vendor. Seed-san © VirtualCast, Inc., VRM Public License 1.0; origem e licença em assets.

Validação: 14 testes do servidor, 6 casos de regressão de mãos e 9 execuções E2E nativas aprovadas. O teste real cobre webcam física, avatar, driver OBS, receptor DirectShow, perda de sinal, recuperação de duas quedas de conexão, cores/orientação, parada/reinício, isolamento entre abas e reconexão após reiniciar a ponte. O relatório completo e os comandos para repetir ficam em `E2E_REPORT.txt`. A entrada em uma reunião do Teams não foi testada.

Cada aba controla seus próprios dispositivos. Uma segunda aba exibe “em uso em outra aba” e não envia frames nem encerra o dispositivo pertencente à primeira. Os frames carregam um identificador de sessão, impedindo que uploads atrasados de uma transmissão anterior contaminem a nova. Falhas transitórias de envio são recuperadas; falhas prolongadas levam a uma mensagem de erro e parada controlada. O atalho verifica a revisão do servidor e substitui apenas a ponte antiga deste projeto.

## Perfis de apresentação

Escolha **Profissional · Studio** ou **Corporativo · Reuniões**. O perfil corporativo aplica um cenário de escritório abstrato, luz neutra, enquadramento em busto e emoções moderadas, preservando articulação da boca e piscadas. A escolha é lembrada neste navegador. Cenário e enquadramento podem ser ajustados depois. O perfil não altera a roupa do arquivo VRM; importe um avatar com traje social para uma aparência corporativa completa.

Testes: `node Web/rig.test.mjs`, `node Web/tracking-utils.test.mjs` e `Tracker/.venv/Scripts/python.exe Web/test_server.py`.

## Sentido dos movimentos e E2E

**Espelhar movimentos** troca o lado anatômico de destino e reflete os pontos de forma consistente para braços, pernas, mãos, expressões e cabeça. Desligado preserva o lado anatômico. O checkbox indica espelho estilo selfie, não inversão acidental.

`/e2e-motion.html` executa 42 verificações usando o modelo Seed-san real: braços e antebraços, avanço, pernas, tronco, cabeça, lateralidade das piscadas, boca, cinco dedos, ligações do esqueleto e retorno após perda de sinal/calibração. `/e2e-browser-tracker.html` verifica o carregamento de WASM/modelos e inferência sem falsas detecções em imagem vazia. Esses testes determinísticos não garantem precisão perfeita de uma webcam em todas as condições. Os arquivos de teste não fazem parte do pacote público.

## Personalização sem login

O cookie `avatar_appearance_v1` guarda cores, apresentação, cenário, enquadramento e espelhamento por 180 dias (SameSite=Lax e Secure em HTTPS), restrito ao caminho do Avatar no site. Preferências antigas do perfil de apresentação são migradas. Arquivos VRM importados ficam no IndexedDB deste navegador, depois de carregados com sucesso, sem upload ao servidor. Ao escolher o modelo de demonstração, o avatar importado salvo é removido. Limpar dados do navegador remove a personalização; outros navegadores e dispositivos não compartilham essas configurações.

## Pose avançada

Full é o modelo padrão de pose no navegador e no rastreador Python (incluindo Unity). Heavy é selecionável na web ou com `--pose-quality heavy` no Python. Ambos usam os modelos oficiais MediaPipe float16 v1. Troque a precisão com a webcam parada; a escolha fica no cookie. Heavy exige mais processamento. O avatar web aplica filtragem One Euro por landmark, rejeição de saltos impossíveis, limites de flexão dos cotovelos (150°) e joelhos (160°), orientação do quadril e retorno suave ao repouso. Partes fora do enquadramento não conduzem o deslocamento do corpo. A filtragem e os limites novos são do rig web; o Unity recebe o modelo Full por meio do rastreador Python e mantém seu retargeting nativo.
