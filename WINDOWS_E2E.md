# Windows — rodada de E2E de 14/10/2026

## Resultado

**Funcionalidades verificadas, aprovação integral bloqueada por falha nativa no encerramento.** Não foi publicada uma versão como “100% aprovada”. Android deixou de ser o alvo do E2E por solicitação do usuário.

| Verificação | Evidência |
|---|---|
| Webcam 0 real | 60 pacotes recebidos: rosto em 55, pose em 60, mão esquerda em 15 e direita em 4; processo terminou com código 0. Sem gravação de vídeo. |
| Agente isolado da webcam | Nos dois modelos, pacotes da câmera não conectam o rig; expressões com origem `agent` chegam ao modelo. |
| Gestos por comando | Comando “acene” moveu o rig de Cartoon e Corporativo; retorno ao modo Webcam restaurou a fonte de entrada. |
| Matriz completa de rig | `--rig-parity -batchmode -nographics`: código 0, `CORPORATE_RIG_PARITY_RESULT=True`, Cartoon/Corporativo × Webcam/Agente; braços, dedos, cabeça, piscadas, expressões e limites da malha com entradas de teste. Não certifica imagem gráfica nem rastreamento de todos os dedos reais. |
| Janela do Windows | Modo Webcam e entrada no Agente em tela cheia observados. Uso simultâneo do computador interferiu nas demais ações assistidas; não houve aprovação visual completa da bandeja e de todos os gestos/dedos. |
| Visão local real | Webcam capturada sob consulta; qwen3.5:4b retornou 354 caracteres em 26,3 s. Imagem apenas em memória e enviada ao Ollama loopback. Não comprova identidade nem reconhecimento contínuo de atividades. |
| Voz local | Microsoft Daniel gerou WAV de 7,09 s, RMS 1583,3 e sinal não vazio. Isso verifica síntese, não a audição pelo usuário. |
| ASR | No mesmo áudio de teste gerado, base transcreveu “acene” como “assine”; small transcreveu “Dingous, acene para mim.”. Small instalado localmente e selecionável no admin. Não equivale a aprovação com microfone real em diferentes ruídos. |
| Entrada real de microfone | 16000 amostras recebidas sem overflow; RMS 0,00001 e pico 0,00006 no intervalo observado. Nenhum áudio foi salvo. Não houve fala humana suficiente para aprovar a palavra de ativação real. |
| Boca durante a fala do Agente | Sinais START/END da síntese acionam a mandíbula sem câmera nos dois modelos; teste funcional com código 0. Animação básica por estado de fala, não visemas fonéticos nem sincronização labial de precisão. |
| Privacidade | Testes Python mantêm respostas protegidas sem fala; imagem não é persistida e não há identificação de pessoas. |

## Falha de encerramento

Todos os asserts funcionais do teste de isolamento passaram, mas o processo terminou com **0xC0000005 / -1073741819**, após `CodeReloadManager destroyed`. Windows registra `UnityPlayer.dll` como módulo da falha. A falha continuou com Direct3D 11 e com a janela visível.

Foi criado um projeto **mínimo separado**, sem UniVRM, Avatar Studio ou pacotes adicionais: `Validation/MinimalExit`. Uma única cena vazia e um MonoBehaviour aguardam um segundo e chamam `Application.Quit(0)`. Esse executável também terminou com o mesmo código. Isso isola a falha para o runtime/ambiente Unity 6000.4.5f1 utilizado nesta máquina, sem estabelecer ainda qual componente do ambiente a causa. Não foi identificado um hotfix oficial específico nem homologada troca de versão/driver.

O executável mínimo encerrou com código **0** em `-batchmode -nographics`. Com janela gráfica, falhou também ao ser lançado pela ferramenta nativa do Windows, com BitBlt/D3D11 e renderização direta. Uma compilação mínima separada com Vulkan falhou na inicialização. Esses testes não foram usados para alterar as configurações gráficas do produto, nem para trocar ou desabilitar drivers do usuário.

O teste funcional final `--agent-isolation -batchmode -nographics` concluiu com **código 0** e confirmou o modelo real carregado, isolamento da câmera, expressão, gesto por comando e restauração de Webcam nos dois modelos. Isso não aprova renderização. A repetição gráfica com o renderizador de software Microsoft WARP passou nos asserts, mas manteve a falha de encerramento. Portanto ela não foi usada como uma “correção” ou configuração padrão.

Reprodução e logs locais:

- `Validation/MinimalExit/Assets/Quit.cs`
- `Validation/minimal-runtime.log`
- `Validation/windows-agent-disposal.log`
- `Validation/windows-agent-isolation.log`

## Correções implementadas

- Fonte de movimento do Agente separada da câmera: câmera não comanda cabeça, corpo ou expressões do avatar nesse modo.
- Gestos limitados e controlados pelo comando/resposta, compartilhando o mesmo rig dos dois modelos.
- Percepção Windows sob consulta, usando somente o modelo local com suporte a visão; nenhuma fotografia é salva. Presença/observação não são autenticação nem reconhecimento de identidade.
- Whisper small e parâmetro administrativo `AsrModel`; instalação reproduzível em `Tools/setup-local-asr.ps1`.
- Sinalização de fala própria para o rig, limpeza ao interromper e execução da voz sem janela de console. A proteção de privacidade continua bloqueando a fala quando configurada.
- Restauração do modo Agente aguarda o carregamento do VRM; estado Android acompanha o serviço persistente em vez de mostrar valores locais desatualizados.
- UniVRM: liberação do Control Rig destrói seu GameObject e desassocia o Avatar do Animator. O código anterior tentava destruir um Transform, causando erro ao liberar um modelo ainda vivo. Spring bones usam o runtime standalone para terminar seus jobs no processamento do avatar, alinhado ao retargeting manual.

## Pendências

Corrigir e retestar o encerramento nativo do Unity neste ambiente; validar frase falada no microfone real, todos os dedos e gestos em ambos os modelos, bandeja, múltiplas pessoas e reuniões. Agenda e agentes de produção continuam dependendo da conexão autenticada ao ChatTrade; não foram simulados compromissos. Reconhecimento de identidade requer um cadastro e validação próprios e não foi implementado. A percepção Android não usa a câmera do computador como se fosse a do celular.
