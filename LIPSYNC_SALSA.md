# Lip sync Windows com SALSA

Foi integrado o runtime real SALSA LipSync Suite 2.5.6.150, encontrado no cache Unity Asset Store do proprietário. `Tools/install_salsa.py` restaura somente o runtime licenciado; DLL e conteúdo comercial ficam ignorados no Git. Em outra máquina, importe uma cópia licenciada ou use `--package` com o pacote local. Os scripts de build restauram a dependência antes de compilar. Não há compra, serviço de fala pago ou download de cópias de terceiros.

## Caminho da fala

1. O TTS local do Windows gera WAV PCM16 temporário com a voz escolhida.
2. O processo informa `SPEECH_AUDIO` ao Unity e aguarda confirmação. Não toca uma segunda cópia do áudio fora do Unity.
3. Um AudioSource 2D reproduz esse mesmo WAV. O SALSA analisa o clip, com seu QueueProcessor.
4. Três controles suaves de abertura passam pela camada de expressões do avatar. Apenas o UniVRM altera a face visível, evitando conflito de escritores. Cartoon usa os presets vocálicos e Corporativo usa os controles ARKit existentes.
5. Silêncio, fim da fala e cancelamento fecham a boca. O WAV é removido após reprodução; o processo também limpa sua pasta temporária. Flags de “falando” não geram abertura artificial no Windows.

Calibração inicial: intervalo de análise60 ms, transição de abertura55 ms e fechamento90 ms, jitter aleatório desligado, cutoff0,012–0,65 e limite de abertura do controlador0,72 antes do ganho global de expressões. O SALSA faz aproximação por áudio: isso não é reconhecimento fonético exato em português.

Referência da API usada: https://www.crazyminnow.com/docs/salsa-lip-sync/modules/salsa/api/ e configuração de runtime: https://www.crazyminnow.com/docs/salsa-lip-sync/modules/salsa/api-example/ .

## Uso e limites

No Windows: **Modo Agente → Voz TTS → Testar voz e lip sync SALSA**. O teste reproduz somente uma frase fixa pública, sem ler memórias. A fala das respostas continua respeitando `speechAllowed` e a política de privacidade. Webcam mantém os controles faciais da câmera; a voz do computador não deve animar uma pessoa em modo Webcam.

Esta entrega calibra o áudio Windows/Unity. Android ainda usa seu caminho nativo anterior de fala e a web tem seu próprio renderer; não foram calibrados nem validados com SALSA nesta alteração. Todos os avatares Windows utilizam o mesmo caminho, conforme suas expressões disponíveis. A validação dedicada usa Cartoon Seed-san e Corporativo MakeHuman reais.

## Validação

97 testes Python passaram, incluindo criação única do WAV, confirmação de reprodução, limpeza temporária e proteção antes da síntese. O teste do player `--corporate-smoke --lipsync-only --speech-file <WAV>` verifica o SALSA real nos dois modelos, boca quieta sem áudio, movimento durante a fala, malha finita, fechamento, confirmação e cancelamento. A variante final também executa o TTS real do Windows até o Unity/SALSA pela mesma ponte do aplicativo.

Relatórios locais em `Validation/lipsync/`. O teste usa frase pública sintetizada, não voz capturada da webcam ou microfone. A qualidade visual subjetiva deve ser reavaliada com a voz e o avatar escolhidos pelo usuário.
