# Olhos do Avatar

## Perfil leve

YOLOX Nano com entrada 416×416, CPU limitada a uma thread e OpenCL desativado para não disputar GPU com o avatar. Padrão de uma detecção por segundo; o intervalo continua configurável no admin. Solicita captura de 640×480 a 10 FPS (o driver pode ignorar essa solicitação). O detector não roda a cada frame de renderização. Mantém somente um quadro em memória e comprime JPEG apenas quando há uma prévia ou solicitação real de percepção.

Medição local em imagem pública, 30 inferências após aquecimento: mediana61,36 ms, p9564,16 ms e média59,90 ms de CPU por inferência. A 1 Hz, aproximadamente6% de um núcleo para detecção; não representa consumo total do aplicativo nem medição com webcam real. Resultados dependem da máquina e da carga simultânea. Evidência local: `Validation/avatar-eyes-performance.json`.

O modo Agente Windows inicia `Tracker/avatar_eyes.py`. Sua câmera só é aberta quando a política autenticada do aplicativo habilita o monitoramento. A animação continua independente da câmera. Voltar ao modo Webcam encerra esse processo e libera a câmera para o rastreamento. Android e web ainda não produzem este fluxo de visão: não anuncie a webcam do PC como câmera do telefone.

## Administração

No ChatTrade, abra **Parceiro Dingous → Olhos do Avatar**, ou `/admin/olhos-avatar`. Selecione a empresa no admin e seu aplicativo vinculado. A tela usa o dispositivo conectado; não solicita a câmera do navegador.

Configure monitoramento, índice da câmera, confiança, intervalo, prévia, ocultação das pessoas, linha de passagem e retenção de eventos. A prévia começa fechada, exige permissão e uma concessão de leitura de 20 segundos, renovada somente com a tela visível. Sem conexão, um quadro antigo não permanece apresentado como atual. Apenas o último JPEG solicitado é mantido no servidor; não há gravação de vídeo ou histórico de quadros. TTL é lógico, e dados podem existir em WAL/backups conforme a política do servidor. Máscaras dependem da detecção e não garantem anonimização de pessoas não detectadas.

## Conexão Windows

Execute `Tools/setup-avatar-eyes.ps1`. O detector YOLOX Nano usa OpenCV DNN na CPU, sem API paga ou PyTorch. Modelo oficial Apache-2.0: https://github.com/Megvii-BaseDetection/YOLOX ; licença em `Tracker/YOLOX-LICENSE.txt`. SHA256 ONNX: `c789161ed43c8269fcd4e67c67eeeb4e80c622da2eb296a20bc6007bd18a0b7d`. Modelo ignorado no Git e incluído ao publicar a pasta Tracker.

Na configuração local existente `%LOCALAPPDATA%/Dingous/chat-trade.json`, adicione `applicationKey` com a chave real do aplicativo habilitado. Preserve `url`, `companyId` e `localAiVerified`. O token existente vem de `DINGOUS_CHATTRADE_TOKEN`, com perfil Admin/Root e empresa correta. Não coloque credenciais no Git, URL ou JavaScript. O servidor obtém a empresa dos claims, nunca do corpo da telemetria. Esta configuração não concede acesso a outras empresas.

API de ingestão: `/api/dingous-partner/eyes/{applicationKey}/policy/{deviceUuid}` e `/telemetry`. Limites de tamanho, sequência, frequência e quantidade de dispositivos são aplicados. Logs têm códigos operacionais e tempo de inferência; não incluem raciocínio interno, conversas ou tokens.

Porta 8768 é exclusivamente loopback, com token efêmero em arquivo local, sem CORS. Serve o quadro em memória para a percepção sob demanda reutilizar a câmera. Porta 8767 continua sendo a ponte de IA já existente; não é a API do ChatTrade. Quando o monitoramento não estiver conectado, a percepção sob demanda mantém o comportamento anterior.

## Passagens

Rastreamento temporário por distância espacial, sem biometria. IDs duram a sessão, expiram após perda de detecção e não são cadastro de visitantes. Conta apenas pessoas cruzando a linha, com margem contra tremulação e intervalo entre eventos. Desaparecer da imagem não conta como saída. Cruzamentos com oclusões, pessoas sobrepostas ou baixa frequência podem falhar; não é controle de acesso nem alarme de segurança certificado.

## Validação desta entrega

- 90 testes Python, incluindo seis de percepção/passagem/máscara e inferência real em imagem pública.
- Admin/API compilados; pacote Windows recompilado.
- PostgreSQL separado, somente loopback porta 55439, testou rollback e repetição do SQL, isolamento, eventos idempotentes, concessão de prévia, expiração e desligamento.
- Host de fixture em `ChatTrande/tests/AvatarEyes.Smoke` renderiza a view real com imagem pública e dados explicitamente de teste. Navegador validou salvar e abrir prévia. Não é captura real nem validação da implantação.
- Migração `20261015_045124_avatar_eyes_monitoring.sql` permanece pendente no ChatTrade; não foi aplicada ao banco real. Ledger/checksum e concorrência do SqlMigrationRunner ainda precisam de homologação no ambiente de implantação.
- Não foi feito E2E com câmera real ou publicação em dingous.com.br nesta entrega.
