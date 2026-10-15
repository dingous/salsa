# Modos do aplicativo

Os botões **Modo Webcam** e **Modo Agente** ficam no topo do Unity, com indicação da opção selecionada. O seletor Cartoon/Corporativo escolhe a aparência independentemente do modo. O layout adapta os controles para telas menores, mantendo rolagem.

- **Webcam:** abre Transmissão para iniciar o rastreamento e a saída de câmera virtual. Encerra a escuta e as respostas do Agente. Não ativa a webcam automaticamente ao trocar de modo.
- **Agente:** abre a conversa com Dingous, escuta local, respostas, memória explícita e aprovação de exemplos. Interrompe o rastreador Windows da webcam e desativa a saída virtual. No Android, o serviço usa câmera/microfone autorizados para rastreamento e escuta do Agente.

Ao iniciar o aplicativo, o modo é Webcam. Abrir o modo Agente exige ação do usuário. A integração real aos especialistas depende do pareamento e dos vínculos/permissões do ChatTrade; a interface não implica que toda a orquestração do ecossistema esteja concluída.

O modo Agente abre o avatar em tela cheia, com status, resposta e controles discretos de escuta. A escuta continua ao sair da tela cheia para configurar. Voltar ao modo Webcam encerra a escuta. Android usa seu serviço em primeiro plano, com notificação; permissões e restrições do sistema continuam aplicáveis.

Windows faz uma saudação fixa ao iniciar a escuta; Android faz uma saudação diária quando a voz está disponível, há um rosto detectado e não há ligação. O resumo diário Windows só consulta o ChatTrade no horário administrativo se a agenda estiver configurada e a proteção de ambiente compartilhado não bloquear. Não há consulta direta ao Google nesta implementação, nem sincronização validada ou alertas individuais de eventos no Android. Não considere os lembretes concluídos até validar a integração real. A saudação fixa não contém dados pessoais.

Na versão web, o seletor existente usa os mesmos nomes **Modo Webcam** e **Modo Agente**. A câmera virtual do Windows depende dos componentes locais já utilizados pelo projeto.
