# Dingous · base de laboratório de avatar

## Abrir
Windows: Modelo VRM → Laboratório Dingous · IA. Web: Dingous · Laboratório de IA, com a ponte local reiniciada. Ambos animam Cartoon e Corporativo.

Digite: “Sorria, pisque o olho esquerdo e acene com a mão direita para cumprimentar”. Gerar e executar chama Ollama local. Demonstração programada usa um plano fixo, sem IA. Parar teste cancela a reprodução. O teste substitui a webcam; não combina duas fontes de movimento.

## IA local
Ollama + `qwen3.5:4b`. Instalados nesta máquina. Em outra máquina: instale Ollama e execute `ollama pull qwen3.5:4b`. Use DINGOUS_AI_MODEL para outro modelo Ollama já instalado. Apenas texto vai ao endpoint loopback 127.0.0.1:11434; imagens/vídeo não entram na geração. O download inicial do runtime/modelo necessita internet, a inferência ocorre localmente.

## Contrato extensível
Tracker/dingous_lab.py separa geração, validação e execução. O modelo gera JSON versão 1 com até 12 passos, action/duration/intensity. Duração limitada a 0,5–5 segundos; intensidade 0–1. Nenhum código, caminho ou comando de shell gerado pelo modelo é executado. Ações disponíveis: neutral, smile, surprise, wink_left, wink_right, nod, wave_left, wave_right, raise_arms, open_hands, close_hands.

A IA compõe ações de uma biblioteca procedural. Esta base não gera animação esquelética inédita por difusão nem treina um modelo de motion capture. Novos movimentos podem ser adicionados ao executor mantendo o contrato. Rigs VRM e MediaPipe permanecem substituíveis para o futuro personagem Dingous.

Windows salva planos em Laboratory dentro do armazenamento local do aplicativo. Web oferece Salvar plano. Replay pela CLI: `Tracker/.venv/Scripts/python.exe Tracker/dingous_lab.py --plan meu-plano.json --output replay.json --port 5059`. Para exportar um plano sem reproduzir: use --generate-only com --prompt.

## Validação
Oito testes Python cobrem validação, limites, finitude, pacotes e piscadas independentes. Teste nativo --corporate-smoke --lab-only reproduz a demonstração nos dois rigs reais; --ai-lab reproduz o plano real da IA em Validation/dingous-ai-plan.json. O teste é de reprodução procedural, distinto do E2E de webcam real. A interface web recebeu integração; validação visual no navegador ainda necessária. Para web hospedada, é necessária a ponte local: a página não expõe o Ollama à internet.


## Configuração desta máquina
Intel Core i9-14900HX, RTX 4070 Laptop 8 GB, aproximadamente 32 GB RAM. Modelo padrão qwen3.5:4b (3,3 GB instalado), contexto 2048, máximo 768 tokens, temperature 0.2, think=false e keep_alive=0. A geração é assíncrona e descarrega o modelo ao concluir, liberando VRAM para render e webcam. Teste local: plano válido em 31,05 segundos incluindo carregamento/descarga; pico total de VRAM 5221 MiB, base 1512 MiB, execução 100% GPU. Este é um teste representativo, não uma comparação exaustiva de modelos nem garantia de FPS sob toda carga. qwen2.5:3b foi preservado como alternativa via DINGOUS_AI_MODEL. Evidência: Validation/dingous-qwen35-benchmark.json.


## Dois modos: Webcam e Parceiro
Windows: Modelo VRM → Modo → Webcam ou Parceiro Dingous. Webcam segue seu rosto/corpo. Parceiro interrompe o rastreamento e recebe perguntas digitadas; responda com Perguntar e ouvir. Os dois modelos usam o mesmo backend. Windows reproduz usando a voz pt-BR instalada via System.Speech; web usa voz pt-BR local do navegador e informa quando ela não estiver disponível. Não há reconhecimento de fala de entrada nem sincronização fonética medida nesta etapa.

Guardar preferência armazena somente o texto escolhido em %LOCALAPPDATA%/Dingous/Partner/memory.json. Memória é contexto usado nas respostas; não modifica pesos do modelo. Aprovar para fine-tuning adiciona a pergunta/resposta ao arquivo approved-training.jsonl nesse mesmo diretório. Não são adicionados automaticamente exemplos não aprovados.

Treinamento real: Tools/train_dingous_lora.py oferece pipeline experimental QLoRA para Qwen/Qwen3.5-4B. Use ambiente CUDA separado com torch, transformers, datasets, peft, trl, bitsandbytes. --validate-only valida o conjunto sem carregar modelo. Execução de treinamento, consumo de VRAM e importação do adaptador no Ollama não foram validados; o desempenho de inferência de 4B não garante treinamento dentro dos 8GB. Treine depois de aprovar exemplos úteis, avalie em perguntas separadas e só então converta/ative o resultado. O script não substitui automaticamente o modelo atual. Nenhum fine-tuning foi realizado nesta implementação.

Acompanhamento diário às 09h (São Paulo) criado no Codex nesta conversa. Não é alarme do executável Windows e não inicia/fala o avatar sozinho. A tarefa revê prioridades e pendências, sem autorizar operações externas ou treinamento automático.


## Operação local sem API paga
Dingous usa somente 127.0.0.1:11434 e modelos instalados. local_ai.py verifica disponibilidade, rejeita tags cloud/URLs e metadados de modelo remoto. Se o serviço estiver desligado, inicia o Ollama instalado em janela oculta, com loopback, OLLAMA_NO_CLOUD=1 e uma geração paralela. Nenhum modelo é baixado automaticamente. Qwen3.5:4b já está instalado nesta máquina. Não há chave, assinatura de API ou cobrança por tokens nesta inferência. Isso não altera nem desativa globalmente todos os recursos do Ollama já aberto. O acompanhamento agendado permanece uma tarefa do Codex, separado da inferência do aplicativo.
