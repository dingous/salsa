# Modos do aplicativo

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
