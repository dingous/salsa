# Android — E2E assistido em 14/10/2026

Resultado: **incompleto; não aprovado para publicação como versão validada**.

## Confirmado

- Unity Android IL2CPP ARM64/x86_64: build concluído; APK em `publish/Android/DingousAvatar.apk`.
- Instalação e abertura do aplicativo no Pixel 7/API 35.
- CorporateHuman/MakeHuman importado e renderizado pelo UniVRM.
- Webcam 0 real: MediaPipe detectou rosto, pose e duas mãos; atualização continuou por centenas de frames. Observados cabeça, braços e gesto com dois dedos no corporativo.
- Vosk português inicializado e contador de áudio avançando no aplicativo Android.
- Testes Python: autenticação/ponte Android (4), privacidade (4), atribuição anatômica das mãos (5): aprovados.
- Build Windows também concluído após alteração do controlador compartilhado.

## Falhas encontradas e alterações

- Travamento na preparação dos modelos: isolamento do lock de cópia; posteriormente removidos locks de publicação de estado e consulta de modo de chamada dentro dessa publicação. Última correção compilada, mas ainda sem reteste assistido completo.
- Distorção de braço no Seed-san: reprodução visual confirmada. Malhas agora preservam quatro pesos de skinning em todas as plataformas; correção compilada, **efeito visual ainda não confirmado**.
- Emulador apresentou falha de driver Vulkan. APK configurado com OpenGL ES 3 e emulador dedicado preparado com SwiftShader.
- Pixel 7 original ficou sem espaço. Criado `DingousAvatarE2E`, API 35, 4 GB RAM/16 GB armazenamento, preservando o AVD anterior. APK instalado nesse novo dispositivo (`emulator-5556`).
- Captura da nova janela falha com `window crop is outside captured monitor`; recuperação por ativação, rotação e tela cheia não resolveu. Usuário também não conseguiu posicioná-la.

## Pendências de validação

- Pareamento autenticado pelo aplicativo e resposta real Ollama no Android: não concluídos. Não foram usados grants de permissões ou injeção de código secreto pela linha de comando.
- Voz: motor Google TTS presente no AVD original, mas sem voz portuguesa local pronta. Síntese pt-BR não passou no teste.
- Palavra de ativação falada, mão na cabeça, abertura/fechamento de todos os dedos, piscadas independentes e comparação integral dos dois modelos: não aprovados.
- Escuta ao ir para Home, recuperação, parada pelo usuário e testes de múltiplos rostos/chamada: ainda não verificados ponta a ponta.

A ponte de IA mantém respostas em texto por proteção de ambiente compartilhado. O botão de voz só permite frase fixa sem dados pessoais. Não existe comprovação de ausência de bugs, desempenho em celular físico ou E2E completo.

## Reteste assistido — 14/10/2026 (horário de São Paulo)

APK recompilado, instalado com atualização preservando dados no DingousAvatarE2E/API 35. A janela passou a ser capturável com o framebuffer dedicado. Sem grants de permissões ou injeção de segredo pela linha de comando.

| Caso | Resultado observado |
|---|---|
| Corporativo · Webcam | Captura real: rosto, pose e mão detectados; contador acima de 400 frames. Não certifica precisão de todos os gestos. |
| Corporativo · Agente | Avatar em tela cheia; Vosk pronto, escuta ativa e áudio avançando. |
| Rotação | Agente e Studio adaptaram a interface entre paisagem e retrato, após transição do sensor. |
| Home / segundo plano | Home pressionado pela interface. Serviço continuou com listening=true, asrReady=true, 1727 frames e 3620 blocos de áudio. |
| Retorno | Atividade retornou ao Agente em tela cheia. |
| Agente → Webcam | Tela de Transmissão apareceu; estado do serviço foi removido após parada explícita da escuta/câmera. |
| Cartoon | Carregou Seed-san no seletor e no Agente em tela cheia; distorção de braço ainda visível. **Falhou.** A alteração anterior Bone4 não resolveu no AVD. |
| Voz | voiceReady=false; serviço solicita instalação de voz portuguesa local. **Não passou.** |
| IA / agenda | Pareamento não salvo neste AVD; consulta real e lembretes não aprovados. |
| Teclado e respostas longas | Não houve validação assistida completa de digitação/resposta real. |

Correções nesta revisão: controles Android em grade (duas colunas em telas estreitas), espaço reservado para a barra inferior no Studio, HUD do Agente respeitando safe area, resposta com rolagem e quebra de linha, orientação automática habilitada no build. O AVD customizado reporta safe area de 960×404 em framebuffer 960×540; a área excluída explica o grande recuo superior. Não foi ignorada a área segura do dispositivo.

**Resultado geral: E2E parcial, reprovado para aprovação integral.** Não publicar ou anunciar como sem bugs. Permanecem distorção do Cartoon, voz pt-BR, pareamento/IA/agenda e teste completo de teclado, privacidade em chamada/múltiplas pessoas e dispositivo físico.
