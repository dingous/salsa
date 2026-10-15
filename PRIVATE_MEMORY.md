# Memórias pessoais do agente

Fatos fornecidos pelo usuário em frases familiares declarativas reconhecidas ou comandos como “lembre” e “guarde” são armazenados em `private-memory.json`, na pasta local do parceiro. Cada registro contém data de registro e classificação pessoal. Idades são informação datada, não data de nascimento inferida. Dados reais de pessoas não pertencem ao repositório ou exemplos de teste.

Memórias pessoais não entram em prompts com proteção de ambiente compartilhado. Consultas privadas usam somente o Ollama local e produzem respostas escritas, com `speechAllowed=false`. Memória consultada impede áudio e aprovação para treinamento. Não é identificação de pessoas pela webcam. O atendimento de clientes não deve acessar esse armazenamento pessoal.

Os caminhos de TTS do CLI e da escuta respeitam `speechAllowed`. O listener não registra mais a pergunta reconhecida em stdout. Logs anteriores não são removidos automaticamente. Arquivos locais de resposta ainda podem conter a pergunta e a resposta: são dados pessoais locais, não telemetria administrativa.

A identificação de frases para armazenamento usa regras, não compreensão universal. Use “Guarde: …” para registrar explicitamente uma informação. Ambiente desconhecido continua protegido por padrão; não se afirma que uma reunião ou outra pessoa presente tenha sido detectada sem evidência real. O armazenamento usa o perfil local do Windows e não oferece criptografia própria nem autenticação biométrica. Não reutilizar esse diretório para um serviço compartilhado entre clientes.

Validação: 95 testes Python, cinco novos sobre captura de memória, bloqueio em ambiente compartilhado, resposta privada sem áudio, bloqueio de envio ao ChatTrade e exclusão do treinamento. Os arquivos Python foram atualizados no pacote Windows. Sem E2E de voz real nesta alteração.
