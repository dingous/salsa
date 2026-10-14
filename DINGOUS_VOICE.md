# Dingous por voz e ChatTrade

## Voz confortável, local

Windows usa Microsoft Daniel em pt-BR, instalada nesta máquina, com velocidade -1, volume 85% e pausa de 280 ms entre parágrafos. Se Daniel não estiver instalada, usa outra voz local pt-BR; se nenhuma existir, informa o erro. Não usa serviço pago de síntese. Texto é escapado por PromptBuilder e não interpretado como SSML. O mesmo script atende Cartoon e Corporativo no modo Parceiro. O script aceita -VoiceName e -Rate para ajuste e -OutputPath para gerar uma amostra WAV.

Web prefere Daniel entre as vozes locais pt-BR do navegador, com velocidade 0,92 e volume 85%. A disponibilidade e o timbre dependem do navegador; a escuta pela ponte usa a voz Windows. Respostas do parceiro foram ajustadas para frases claras, passos concretos, menos bordões e honestidade sobre informações e ações indisponíveis.

Ao escolher Parceiro Dingous, a escuta do microfone padrão deste computador inicia automaticamente. Desmarque Escutar Dingous para desligar. Diga “Dingous, sua pergunta” numa frase, ou “Dingous” seguido da pergunta na janela de ativação. Funciona enquanto o aplicativo/ponte estiver aberto, não é serviço permanente após fechar Windows.

ASR Whisper base int8 em CPU, 4 threads. Modelo e runtime foram instalados nesta máquina e copiados para publish/Windows/Tracker. Áudio de microfone fica em memória; arquivos de resposta contêm texto. Não há envio de áudio para serviços de transcrição. O reconhecimento da palavra é baseado na transcrição e aceita Dingous/dingus/dingos/dingo; não é detector de palavra personalizado treinado. Microfone fica pausado enquanto transcreve/responde/fala, evitando ouvir a própria resposta. Um bloqueio local impede duas instâncias simultâneas de escuta. Web usa o microfone da máquina da ponte local, não o microfone de outro dispositivo remoto; a sessão pertence à aba autenticada e expira sem polling por 20 segundos.

Teste realizado: carregamento offline + captura real do microfone por 5 segundos, sem gravação, exit 0. Frase em voz Windows sintetizada passou por Whisper e ativou o parser. Ainda não há E2E da frase com voz humana. A precisão depende de ruído, pronúncia e dispositivo; use botão de pergunta digitada se necessário.

## Integração do ChatTrade
Código existente examinado em D:/Git/Diego/ChatTrade/ChatTrande. API existente api/chat/start e api/chat/send recebe ChatRequestDto, responde ChatResponseDto e passa pela orquestração da empresa. O adaptador não altera permissões, AppSettings, agentes ou banco do ChatTrade e não assume acesso a todos os agentes administrativos.

Para conexão real são necessários URL, empresa e conta autenticada. Aguardando essas informações do usuário. Não há conexão real ativa nesta entrega. Localmente, o adaptador lê %LOCALAPPDATA%/Dingous/chat-trade.json com url, companyId, localAiVerified (somente depois de verificar que o provedor da empresa é local). DINGOUS_CHATTRADE_TOKEN deve vir do login válido no ambiente local; não coloque token no Git ou no chat. DINGOUS_PARTNER_BACKEND=chattrade seleciona a integração; o padrão continua Ollama. Não há fallback silencioso para agentes quando falta autenticação. Ações externas continuam sujeitas às permissões/políticas do ChatTrade e à autorização do usuário para a tarefa concreta; “Dingous” ativa a escuta, não concede privilégio irrestrito.

Não rodei o startup/migrations do ChatTrade nem ativei fornecedores pagos. Ainda falta autenticar e validar quais agentes e aprovações estão habilitados para a empresa. Testes do cliente usam respostas simuladas, não provam todos os agentes conectados.
