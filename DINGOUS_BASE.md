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
