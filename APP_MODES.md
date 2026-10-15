# Modos do aplicativo

## Galeria de presets

O botão **Avatares** no topo do Unity abre uma galeria com filtros Todos/Masculinos/Femininos e prévia ao aplicar. Há seis presets masculinos (Rafael, André, Lucas, Marcos, Henrique e Caio) e seis femininos (Beatriz, Camila, Sofia, Júlia, Helena e Lara). Os presets combinam pele, cabelo, olhos, proporções faciais, óculos, barba e roupas sociais/casuais/esportivas. Depois da escolha, os controles de Aparência continuam disponíveis.

São duas bases MakeHuman/MPFB reais: `CorporateHuman.vrm` e `CorporateWoman.vrm`, ambas com 52 ossos humanoides e 67 expressões. O catálogo não troca apenas o nome do avatar. Os presets preservam o modo Webcam/Agente; o seletor Cartoon/Corporativo muda o visual sem descartar o personagem escolhido. **Salvar avatar** registra a aparência na biblioteca local Windows. A galeria está também no código e APK Android; não implica E2E assistido Android.

Na web, **Avatares prontos · 12 presets** usa o mesmo catálogo, com filtro e botão **Usar avatar escolhido**. Modelo, cores e parâmetros ficam em cookies do navegador, respeitando o bloqueio de armazenamento do usuário. O estilo fica em **Personalizar MakeHuman → Aparência**. Não há upload de foto obrigatório nem serviço pago para escolher os presets.

O catálogo canônico é `CorporateUnity/Assets/Resources/AvatarPresets.json`. `Tools/build_corporate_human.py --female` (como argumento de script do Blender) gera a base feminina; `Tools/glb_to_corporate_vrm.py --female` empacota o VRM. `Tools/prepare_preset_web_assets.py` copia o catálogo e prepara as duas bases web com texturas até 1024 px, preservando ossos, pesos, geometria e vínculos faciais. O pacote Windows valida a presença/hash de ambas as bases, além do Seed-san.

Validação dos presets em 15/10/2026: os 12 personagens passaram no teste do executável Windows exportado (`CORPORATE_PRESETS_RESULT=True`, saída 0). O rig feminino passou nos testes sintéticos de braços, dedos e expressões em Webcam/Agente. A seleção feminina, o filtro e a restauração após recarregar foram conferidos na web. O APK contém a base feminina com hash idêntico ao arquivo de origem; não foi realizado E2E assistido Android nesta entrega.

## Funcionamento dos modos

### Frameworks e Corporativo 3 · Avaturn

O seletor identifica `Cartoon · UniVRM`, `Corporativo · MakeHuman/MPFB`, `Corporativo 2 · Real-Time Full Body Tracking`, `Corporativo 3 · Avaturn` e `Corporativo 4 · MetaHuman`. Corporativo 2 abre uma indicação de pacote ausente: o repositório indicado contém apenas README e não foi integrado um plugin inexistente. A entrada não troca o avatar ativo nem afirma que esse rastreador esteja funcionando.

Corporativo 3 abre a integração Avaturn. Há um modelo oficial de demonstração T1 incluído, importação GLB/VRM no Windows e download de GLB por URL HTTPS no Windows/Android. O criador é aberto no navegador externo nesses aplicativos. A web usa o SDK oficial `@avaturn/sdk` 1.1.0 em diálogo incorporado, com callback de exportação, importação GLB e download VRM para uso no aplicativo. A URL padrão `https://demo.avaturn.dev` é demonstrativa; produção deve usar um projeto próprio. Não foi criado projeto/conta Avaturn, nem ativada API paga.

A criação por fotografia ocorre no serviço online Avaturn, com envio escolhido pelo usuário; não é geração local. Arquivos importados passam por adaptação local de bindings humanoides e faciais para o runtime UniVRM, mantendo geometria, pesos, materiais e metadados originais. O serviço online não recebe automaticamente webcam, memória, agenda ou credenciais da ponte Dingous. No Windows, o arquivo escolhido é copiado para a pasta local `Avaturn` antes de salvar o perfil. Na web, o VRM convertido utiliza a persistência IndexedDB já existente.

O modelo oficial T1 possui rosto estático: não oferece piscadas ou boca animada. Para essas funções, exporte **T2 com ARKit/visemes**, conforme https://docs.avaturn.me/docs/integration/bodies/. O aplicativo informa quando não há blendshapes; não fabrica expressões ausentes. Corpo/mãos, modos Webcam/Agente e gestos continuam no controlador compartilhado. Roupa e cabelo Avaturn são escolhidos no criador do fornecedor; os controles específicos de malhas MakeHuman ficam desativados.

Referência incluída: `AvaturnBase.glb`, proveniente de `Samples~/Runtime/_Data/Models/Avaturn-BaseModel.glb` do repositório oficial https://github.com/avaturn/avaturn-unity-webview-sdk. Licença MIT do repositório acompanha os assets em `LICENSE-Avaturn.txt`; direitos dos avatares pessoais exportados continuam sujeitos aos termos do fornecedor. A adaptação não concede direitos de redistribuição sobre arquivos enviados pelo usuário.

### Corporativo 4 · MetaHuman

O seletor inclui **Corporativo 4 · MetaHuman** no Unity e na web. A opção abre um painel próprio e mantém o avatar atual até uma importação válida. Nenhum personagem de outro fornecedor é renomeado ou incluído como se fosse um MetaHuman. Não há personagem MetaHuman exportado disponível nas pastas locais verificadas.

Windows importa um MetaHuman previamente convertido para `.vrm`, copia o arquivo para a pasta persistente `MetaHuman` e valida corpo e todas as articulações dos dedos antes de substituir o avatar ativo. Windows/Android também recebem um link HTTPS para esse VRM preparado. O caminho do arquivo persistido permite restaurar a seleção ao carregar o perfil salvo. Os modos Webcam e Agente usam o mesmo controlador de rig já existente. Os controles de roupa/cabelo específicos do MakeHuman não representam um editor de MetaHuman.

Na web, **Importar MetaHuman preparado .vrm** exige VRM 1.0 com corpo, dedos, texturas e buffers incorporados, até 150 MB. O adaptador preserva o nome original, os metadados VRM, geometria, materiais e dados binários; adiciona apenas a identificação do framework em `asset.extras.dingousFramework` para restaurar a categoria no IndexedDB. A categoria é declarada pelo usuário ao importar e não certifica a procedência artística do arquivo.

Prepare o personagem a partir do fluxo oficial Epic DCC/FBX e configure Humanoid e expressões ARKit no Unity/UniVRM antes de exportar VRM. Documentação de exportação: https://dev.epicgames.com/documentation/metahuman/metahuman-creator-export-tool-in-unreal-engine. Este aplicativo não executa DNA, RigLogic, arquivos `.uasset`, grooms do Unreal nem FBX bruto em runtime. Qualidade facial, cabelo, materiais e corretivos precisam ser preparados para Unity/web/Android; o runtime não reproduz automaticamente todos os recursos de renderização do Unreal. Um arquivo sem blendshapes vinculados tem rosto estático, e a interface informa essa limitação.

Os testes de compatibilidade usam o MakeHuman existente como fixture, preservando seu nome original. Aprovação desses testes não comprova um personagem MetaHuman real nem autorização/licença de um arquivo enviado pelo usuário. A validação visual e de captura real com um MetaHuman final permanece dependente desse asset preparado.

### Comportamento dos modos

O escopo do produto inclui webcam/stream, Avatar Agente pessoal e Avatar Agente de atendimento integrado via web ao web chat do Dingous Chat Trade. O atendimento é um contexto do modo Agente, com agentes e fluxos da empresa/aplicativo e sem acesso às memórias privadas do agente pessoal. Esse escopo está registrado em `AGENTS.md`; a integração de atendimento web ainda exige implementação e validação próprias, não sendo comprovada pelos testes Windows.

Os botões **Modo Webcam** e **Modo Agente** ficam no topo do Unity, com indicação da opção selecionada. O seletor Cartoon/Corporativo escolhe a aparência independentemente do modo. O layout adapta os controles para telas menores, mantendo rolagem.

- **Webcam:** abre Transmissão para iniciar o rastreamento e a saída de câmera virtual. Encerra a escuta e as respostas do Agente. Não ativa a webcam automaticamente ao trocar de modo.
- **Agente:** abre a conversa com Dingous, escuta local, respostas, memória explícita e aprovação de exemplos. Interrompe o rastreador Windows da webcam e desativa a saída virtual. No Android, o serviço usa câmera/microfone autorizados para rastreamento e escuta do Agente.

Ao iniciar o aplicativo, o modo é Webcam. Abrir o modo Agente exige ação do usuário. A integração real aos especialistas depende do pareamento e dos vínculos/permissões do ChatTrade; a interface não implica que toda a orquestração do ecossistema esteja concluída.

O modo Agente abre o avatar em tela cheia, com status, resposta e controles discretos de escuta. A escuta continua ao sair da tela cheia para configurar. Voltar ao modo Webcam encerra a escuta. Android usa seu serviço em primeiro plano, com notificação; permissões e restrições do sistema continuam aplicáveis.

Windows faz uma saudação fixa ao iniciar a escuta; Android faz uma saudação diária quando a voz está disponível, há um rosto detectado e não há ligação. O resumo diário Windows só consulta o ChatTrade no horário administrativo se a agenda estiver configurada e a proteção de ambiente compartilhado não bloquear. Não há consulta direta ao Google nesta implementação, nem sincronização validada ou alertas individuais de eventos no Android. Não considere os lembretes concluídos até validar a integração real. A saudação fixa não contém dados pessoais.

Na versão web, o seletor existente usa os mesmos nomes **Modo Webcam** e **Modo Agente**. A câmera virtual do Windows depende dos componentes locais já utilizados pelo projeto.

## Disponibilidade em segundo plano

Windows: no modo Agente, **Avatar no canto** posiciona uma janela compacta sobre as outras no canto inferior direito. **Segundo plano** oculta a janela e mantém o processo/escuta. O ícone na bandeja permite restaurar, abrir em tela cheia, pausar e encerrar. Fechar a janela em modo Agente mantém o aplicativo na bandeja, desde que o processo da bandeja esteja disponível. Em Webcam, fechar encerra normalmente. O processo auxiliar usa apenas controles do próprio aplicativo. O aplicativo precisa ter sido iniciado; não é instalado como serviço nem registrado para iniciar com o Windows.

Android: fechar a atividade não encerra o serviço de escuta já iniciado pelo usuário. A notificação persistente abre novamente o avatar e permite parar o serviço. Ao reabrir, o último modo Agente escolhido é restaurado. Não há sobreposição sobre outros aplicativos nem inicialização automática no boot. Forçar parada, permissões revogadas, políticas de bateria e restrições do Android podem interromper o serviço. Tela cheia corresponde ao aplicativo aberto; segundo plano corresponde ao serviço com notificação. Não há garantia de escuta ininterrupta após o sistema matar o processo.

Windows e Android restauram o modo Agente selecionado anteriormente; a primeira execução continua no modo Webcam. Parar explicitamente ou voltar ao modo Webcam deve ser respeitado.

## Percepção e animação independentes

No modo Agente, pacotes da webcam não movimentam o avatar. A animação recebe uma fonte própria e responde com gestos limitados: acenar, piscar, sorrir, levantar braços e assentir. O mesmo controlador atende Cartoon e Corporativo. Isso não equivale a gerar qualquer animação corporal solicitada.

No Windows, perguntas como “O que estou fazendo?” ou “O que você vê?” capturam uma imagem atual da Webcam 0 e consultam o modelo local com visão. A imagem fica em memória, não vira arquivo nem é enviada à nuvem. É uma observação sob consulta, não monitoramento visual contínuo ou identificação biométrica. O modelo deve expressar incerteza e não inferir atributos sensíveis. No Android, essa consulta não usa indevidamente a câmera do computador; a transmissão da imagem do telefone ainda precisa ser integrada.

O reconhecimento de voz Windows usa Whisper small local, configurável no código do admin ChatTrade por `AsrModel`. Instale pelo script `Tools/setup-local-asr.ps1`; nenhum modelo é baixado silenciosamente durante a escuta. A alteração no admin ainda não foi publicada em produção.

## Conversa digitada em tela cheia

O modo Agente oferece um campo editável no rodapé, com botão **Enviar** e envio por **Enter**. A resposta aparece acima, com rolagem. “Pergunte algo para começar” é a mensagem inicial da área de resposta; o campo de pergunta fica separado. A digitação usa o mesmo fluxo local de respostas, gestos e voz da conversa pelo painel. Perguntas vazias e envios simultâneos são bloqueados antes de interromper a escuta.

Validação Windows em 14/10/2026: build concluído, aplicativo aberto em modo Agente, pergunta digitada enviada pela interface, estado de processamento exibido e resposta real da IA local apresentada na tela. Isso valida este fluxo de texto; não substitui o E2E de todas as plataformas ou a investigação da falha de encerramento descrita em WINDOWS_E2E.md.

## Descanso do Agente

Cartoon e Corporativo compartilham o idle do modo Agente: braços relaxados, respiração e oscilação discretas, piscadas a cada 2,8–6 segundos e bocejos silenciosos ocasionais (primeiro após 45–80 segundos, depois a cada 60–110 segundos). O bocejo combina abertura gradual da boca, pálpebras e leve inclinação da cabeça. Fala e novos comandos cancelam o bocejo; gestos e sequências do laboratório têm prioridade. Esse comportamento não substitui o rastreamento no modo Webcam.

Teste Windows `--corporate-smoke --agent-isolation -batchmode -nographics`: saída 0, `CORPORATE_AGENT_ISOLATION_RESULT=True`, incluindo piscadas/bocejo e cancelamento pela fala nos dois VRMs. O teste antecipa os temporizadores para exercitar o bocejo sem aguardar o intervalo normal. Evidência local: `Validation/windows-idle.log`. Aplicativo gráfico recompilado e aberto no modo Agente para conferência da postura; isso não resolve a falha de encerramento gráfico já documentada.

## Escolher voz TTS por dispositivo

- **Windows:** Modo Agente → **Voz TTS** (ou **Escolher voz TTS** no painel). Lista as vozes locais instaladas acessíveis ao motor System.Speech. Clique na voz para salvar, use **Testar voz** e **Voltar**. **Padrão do admin** remove a preferência local e usa `voiceName` administrativo. A preferência fica em `%LOCALAPPDATA%/Dingous/Partner/tts-windows.json` e é consultada também pela escuta e saudações. Nenhuma voz é instalada automaticamente.
- **Android:** Modo Agente → **Voz TTS**. O diálogo nativo oferece vozes instaladas que não exigem rede, com amostra ao selecionar e **Salvar/Cancelar**. A escolha fica nas preferências privadas do aplicativo e é aplicada ao serviço de voz. O motor TTS e seus dados devem estar instalados no Android.
- **Web:** **Conversar com Dingous → Voz TTS deste navegador → Testar voz escolhida**. O navegador lista vozes locais, atualiza a lista quando elas ficam disponíveis e salva a escolha em seu armazenamento local. A preferência usa o identificador da voz; se ela desaparecer, informa indisponibilidade, sem trocar silenciosamente a voz escolhida. Essa seleção atende a síntese feita no navegador nas respostas digitadas; a escuta legada pela ponte Windows ainda sintetiza no computador e usa a seleção Windows.

A escolha é independente da aparência Cartoon/Corporativo. Os parâmetros e a política de privacidade do admin continuam valendo; trocar voz não autoriza divulgação de dados pessoais. Vozes disponíveis variam entre motores, dispositivos e navegadores. A lista Windows pode diferir da lista do navegador no mesmo computador.

Validação em 15/10/2026: 11 testes Python de preferências/política passaram; `node Tools/test-tts-web.mjs` passou; verificação sintática dos módulos web passou; builds Windows e APK Android concluídos. Na interface Windows, Microsoft Maria Desktop foi selecionada, persistida e a amostra terminou sem erro. Na web, Microsoft Maria foi selecionada, mantida após recarregar e o botão de amostra foi acionado. O seletor Android foi compilado, mas não recebeu E2E visual nesta rodada, conforme prioridade atual de testes Windows.
