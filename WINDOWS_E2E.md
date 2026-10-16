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

## Contato com rosto e mãos juntas — 15/10/2026

Correções compartilhadas pelos rigs Cartoon e Corporativo no aplicativo Windows:

- Atribuição conjunta das duas mãos: duas detecções próximas deixam de sobrescrever o mesmo lado do pacote.
- Referência facial baseada nos olhos e na orientação da cabeça do VRM, com proteção aproximada de palma/punho. Pontas de dedos incertas não afastam toda a mão da bochecha.
- Limite de rotação do punho e de articulações dos dedos; retenção breve da última pose durante oclusão perto do rosto ou de outra mão.
- Resolução de proximidade pelas palmas do modelo, com margem baseada na espessura aproximada da mão, mantendo comprimentos dos ossos.
- Indício de mãos unidas calculado na imagem, independente da profundidade monocular dos punhos. Esse indício fecha o vão entre palmas quando elas estão unidas na webcam; tem retenção de 250 ms e exige detecção das duas mãos.
- Ocultação do acessório `robo_arm` do Seed-san, que não pertence aos dois braços do rig humanoide. Ele também deixa de participar do enquadramento do modelo.

### Webcam real assistida

Webcam 0, dois modelos, 40 segundos por modelo na rodada final. Somente estatísticas agregadas foram registradas; não foi salvo vídeo nem fotografia da webcam.

| Modelo | Pacotes amostrados | Ambas as mãos | Punho perto do rosto | Punhos próximos | Malha inválida |
| --- | ---: | ---: | ---: | ---: | ---: |
| Cartoon | 786 | 415 | 576 | 78 | 0 |
| Corporativo | 770 | 313 | 589 | 24 | 0 |

O usuário confirmou na conferência assistida final que as palmas passaram a ficar juntas. Os contadores indicam cobertura dos gestos e finitude da geometria; não certificam ausência de qualquer interpenetração, exatidão milimétrica do contato facial ou reconhecimento de todos os dedos sob oclusão. O contato usa aproximações geométricas, não colisão exata entre todos os triângulos da pele.

Evidências locais: `Validation/windows-prayer-live.log` e `Validation/windows-prayer-contact.log`. O teste sintético verifica separação das palmas, fechamento do vão causado por profundidade incorreta, alcance do IK e geometria finita, com espelhamento ligado/desligado nos dois modelos.

O encerramento gráfico ainda falhou depois dos asserts funcionais: código final `0xc000041d`, com eventos Windows também registrando `0xc0000005` em `UnityPlayer.dll`. Portanto `CORPORATE_CONTACT_LIVE_RESULT=True` não equivale a aprovação integral do processo gráfico. Não houve publicação nesta rodada. O novo indício de contato foi integrado ao rastreador Windows; não declarar E2E Android/web a partir desta validação.

### Regressão e Agente na compilação final

Todos os testes abaixo terminaram com código 0:

- `--contacts-only`: contato de palmas/face, margem, fechamento do vão por indício da imagem e exclusão do acessório robótico do Seed-san. Log `Validation/windows-prayer-contact.log`.
- `--agent-isolation`: Cartoon e Corporativo, rejeição da webcam no Agente, expressões, gesto por comando, sinal de fala, idle com piscada/bocejo, interrupção do bocejo pela fala e retorno à Webcam. Log `Validation/windows-release-agent-isolation.log`.
- `--partner-only`: pergunta real ao modelo local, conclusão do processo de resposta e comparação integral do texto exibido com o resultado nos dois modelos. A captura visual é dispensada apenas quando o teste roda explicitamente sem dispositivo gráfico. Log `Validation/windows-release-partner-only.log`.
- `--rig-parity`: **459 verificações aprovadas**, matriz Cartoon/Corporativo × Webcam/Agente, incluindo dedos, alcance, espelhamento, expressões e vínculos dos ossos. Log `Validation/windows-release-rig-parity.log`.
- 19 testes Python de atribuição/contato das mãos e preferências/política de voz.
- Amostra pública de TTS Windows pela voz selecionada, Microsoft Maria Desktop, concluída sem erro. Isso verifica o processo de síntese, não reconhecimento de uma frase humana pelo microfone.

Esses resultados aprovam os fluxos funcionais exercitados. A falha gráfica ao encerrar, o reconhecimento real de comando falado e as integrações autenticadas de produção continuam fora dessa aprovação.

## Regressão autônoma do pacote exportado — 15/10/2026

Executado `publish/Windows/AvatarStudio.exe` sem interface gráfica, sequencialmente, com limite de 180 segundos por processo. Todos os cinco processos terminaram com código 0 e marcador de aprovação, sem `CORPORATE_FAIL`:

| Caso | Verificações aprovadas | Evidência local |
| --- | ---: | --- |
| 12 presets, bases masculina/feminina, estilo e serialização | 88 | `Validation/overnight-presets-only.log` |
| Rig feminino em Webcam e Agente | 229 | `Validation/overnight-female-rig.log` |
| Paridade Cartoon/Corporativo × Webcam/Agente | 459 | `Validation/overnight-rig-parity.log` |
| Contato de mãos/rosto | — | `Validation/overnight-contacts-only.log` |
| Isolamento do Agente, gestos, fala sinalizada e idle | — | `Validation/overnight-agent-isolation.log` |

Também passaram 84 testes Python do Tracker, dois testes de catálogo/modelos, os testes JavaScript de presets e TTS, a verificação de sintaxe dos 15 módulos JavaScript e dos arquivos Python do projeto. A checagem Python exclui o ambiente virtual: um arquivo legado Python 2 de uma dependência (`olefile2.py`) não deve ser tratado como código do aplicativo. O Ollama `qwen3.5:4b` respondeu a uma saudação pública local em 4,74 segundos, sem reprodução de áudio (`Validation/overnight-ollama.json`). Isso valida o serviço local, não a integração completa do Agente com o ChatTrade.

Assembly do pacote testado, SHA-256: `DE048444D70A4593E217600706B23E1910079A17F7F2D54D8D73D096F038BAA6`.

Repetição: `powershell -File Tools/test-windows-release.ps1`. O novo executor guarda logs e resultado estruturado, aplica timeout e exige saída zero além dos asserts. Sua sintaxe PowerShell foi validada; esta rodada utilizou os mesmos cinco casos em execução sequencial direta.

Não foram solicitados gestos humanos nem capturados webcam/microfone nesta rodada. Não houve publicação no domínio, push Git ou E2E Android. A falha conhecida de encerramento gráfico do Unity e as integrações autenticadas continuam pendentes; o encerramento sem dispositivo gráfico passou e não comprova a correção do encerramento com renderização.

## Corporativo 3 · Avaturn — 15/10/2026

Implementados seletor com os nomes dos frameworks, painel Avaturn, demonstração oficial T1, importação GLB/VRM Windows e download GLB HTTPS Windows/Android. A web incorpora o SDK oficial 1.1.0, recebe exportação, adapta GLB ao rig VRM e permite baixar o VRM. Corporativo 2 indica o pacote Real-Time Full Body Tracking ausente; não foi simulada instalação.

Validação final do runtime Windows: **224 verificações aprovadas**, zero `CORPORATE_FAIL`, processo com saída 0 e `CORPORATE_AVATURN_RESULT=True`. Evidência: `Validation/windows-avaturn-capabilities.log`. A referência Avaturn real foi carregada pelo UniVRM e exercitada em Webcam/Agente, com ambos os espelhamentos, braços, dedos, cabeça, tronco e pernas. Quatro avisos `CORPORATE_CAPABILITY` registram que os testes de expressão não se aplicam ao modelo T1 sem blendshapes. Pesos de controles internos sem vínculos à malha não são contados como animação facial real. O teste anterior de 232 asserts foi substituído por essa detecção correta de capacidade.

A conferência gráfica Windows comprovou seletor, carregamento do avatar Avaturn real, painel alinhado ao layout responsivo, correção do brilho metálico de pele/tecido e aviso **Rosto estático · use Avaturn T2 para expressões**. Evidência para entrega: `Validation/avaturn-windows.png`. A correção de material ocorre em runtime; o GLB original permanece idêntico ao modelo de referência.

O pacote Windows exportado contém o mesmo assembly testado, SHA-256 `FAD4217CCBEBD19B9132564ED685240D619A5A54901BCDF8CD74577410981D01`. O APK final foi compilado e seu `AvaturnBase.glb` tem hash idêntico ao original; não foi realizado E2E Android assistido. Os 12 presets também passaram na regressão desta integração (`Validation/windows-final-presets-only.log`), antes dos ajustes finais exclusivos de painel/material/detecção Avaturn.

Web: testes de conversão, preservação de dados, validação de URL/rig/arquivo; referência real pelo GLTFLoader/three-vrm com **42 verificações de corpo**; 19 testes Python da ponte; regressões de presets, MakeHuman e TTS aprovadas. O servidor entregou interface, CSP do criador e GLB de 3.735.784 bytes com HTTP 200. Os testes Node substituem decodificação de texturas e não validam pixels. A ferramenta de navegador bloqueou a aquisição da aba de erro por política de URL; não foi contornada. A validação visual web, geração/exportação online com conta Avaturn e um avatar T2 real permanecem pendentes. Nenhuma foto pessoal foi enviada e não houve publicação no domínio ou push Git nesta rodada.

## Corporativo 4 · MetaHuman preparado — 15/10/2026

Adicionados opção/painel MetaHuman no Windows, Android e web, importação de VRM preparado, download HTTPS Windows/Android, validação de corpo/dedos antes da troca e restauração da categoria ao recarregar o arquivo salvo. Nenhum personagem MetaHuman real está incluído; não foi encontrado um export nas pastas locais verificadas. O runtime não executa DNA/RigLogic/FBX bruto.

O teste de compatibilidade `--metahuman-file` usa o VRM MakeHuman já existente **somente como fixture**. Foram aprovadas **234 verificações**, sem falhas, saída 0 e `CORPORATE_METAHUMAN_PREPARED_RESULT=True` em `Validation/windows-option4-metahuman.log`. A rodada cobre painel sem troca prematura do avatar, importação, contexto 4, rig Webcam/Agente, persistência da origem e retorno ao MakeHuman. Não comprova renderização, cabelo, corretivos nem expressões de um MetaHuman final. O teste web preserva explicitamente o nome original `Corporate Human`; a categoria declarada pelo usuário não é certificação da procedência do asset.

Regressões: 12 presets aprovados (`Validation/windows-option4-presets-only.log`) e Avaturn aprovado (`Validation/windows-option4-avaturn.log`), ambos com saída 0. `node Tools/test-metahuman.mjs` validou rig completo, recusa de ossos ausentes/ambíguos, VRM não preparado, dependências externas, preservação dos dados originais e detecção de rosto estático. Também passaram testes Avaturn, presets e TTS web e os 19 testes Python da ponte. A interface e o adaptador MetaHuman foram entregues pelo HTTP local com sucesso; não houve E2E visual de navegador nesta rodada.

A interface Windows foi conferida visualmente: o seletor exibe **Corporativo 4 · MetaHuman** e o painel informa que o personagem não está incluído, com importação, link HTTPS e guia oficial. Abrir o painel mantém o avatar anterior e sua identificação. Evidência: `Validation/metahuman-option.png`. O assembly exportado corresponde ao build testado, SHA-256 `C37F408347723EC5E4C33E412F5081F7A3851A41CD674F74C8FEDA9C516FCC72`.

APK recompilado; o metadata IL2CPP contém a integração MetaHuman. Não houve E2E Android assistido, teste de webcam real com MetaHuman, publicação no domínio ou push Git. Para validar o personagem final, ainda é necessário um MetaHuman preparado para este runtime, com Humanoid completo e blendshapes ARKit vinculados. As limitações de encerramento gráfico registradas anteriormente continuam fora desta aprovação.


## SALSA real — 15/10/2026

Player Windows recompilado e pacote atualizado com SALSA LipSync Suite 2.5.6.150, restaurado do cache licenciado local. Teste dedicado de lip sync aprovado com 16 verificações, nos modelos reais Seed-san (Cartoon) e MakeHuman (Corporativo). Verificou áudio real sintetizado no Windows, reprodução por AudioSource no Unity, controles SALSA/QueueProcessor, boca quieta sem áudio, movimento durante fala, malha finita, fechamento e cancelamento. Também validou o percurso real do botão de prévia TTS até o SALSA e confirmação de término. Processo terminou com exit 0.

97 testes Python aprovados. Evidências em `Validation/lipsync/salsa-e2e-final.log` e `Validation/lipsync/result.json`. Não houve teste de webcam/microfone real, Android ou web nesta alteração. Detalhes em `LIPSYNC_SALSA.md`.


## E2E Windows completo — 15/10/2026, 02h45–03h19

**Aprovação integral bloqueada pelo encerramento nativo na transmissão real.** Foram aprovados 13 casos funcionais, com 2014 verificações nas rodadas finais de cada caso. Também passaram 113 testes Python do Tracker e dois testes de catálogo/modelos. Builds sucessivos foram usados durante as correções; isso não representa execução de toda a matriz num único hash. O último build passou no estúdio gráfico completo e no startup do pacote principal.

| Caso | Verificações | Saída | Log local |
| --- | ---: | ---: | --- |
| presets-only | 88 | 0 | `Validation\release-20261015-024854\presets-only.log` |
| female-rig | 229 | 0 | `Validation\release-20261015-024854\female-rig.log` |
| rig-parity | 459 | 0 | `Validation\release-20261015-024854\rig-parity.log` |
| contacts-only | 26 | 0 | `Validation\release-20261015-024854\contacts-only.log` |
| agent-isolation | 19 | 0 | `Validation\release-20261015-030253\agent-isolation.log` |
| avaturn | 224 | 0 | `Validation\release-20261015-024854\avaturn.log` |
| metahuman-fixture | 234 | 0 | `Validation\release-20261015-024854\metahuman-fixture.log` |
| face-only | 9 | 0 | `Validation\release-20261015-030253\face-only.log` |
| partner-only | 7 | 0 | `Validation\release-20261015-030253\partner-only.log` |
| lab-only | 7 | 0 | `Validation\release-20261015-030614\lab-only.log` |
| lipsync-only | 16 | 0 | `Validation\release-20261015-030614\lipsync-only.log` |
| full-studio | 695 | 0 | `Validation\release-20261015-031137\full-studio.log` |
| startup-only | 1 | 0 | `Validation\release-20261015-031802\startup-only.log` |

### Correções e execução

- SALSA controla a boca somente durante áudio: expressões por comando e bocejo idle deixam de ser apagados no silêncio. O sinal de fala interrompe o bocejo sem inventar movimento labial.
- Busca do Tracker exige módulos essenciais e Python, evitando selecionar uma pasta antiga incompleta. A distribuição de teste inclui seu próprio Tracker, ambiente virtual e modelos.
- O executor registra hash e modo gráfico; exige renderização nos casos que capturam imagens/áudio. Teste do Agente verifica existência da resposta antes de abri-la, evitando travamento por exceção.
- A fixture do teste completo posicionava o nariz na cintura e acionava incorretamente contato facial ao baixar braços. A posição foi corrigida e a rodada completa repetida com sucesso; não foi alterado o limite de alcance para esconder a falha.
- Conferidos visualmente: pergunta digitada e resposta local real, galeria/filtros, laboratório, logo na camiseta e layout estreito. O teste completo cobre roupas, fundo, foto de referência, materiais, 52 canais faciais e layouts de 390 a 2560 pixels. A personalização de roupas/logo foi exercitada no MakeHuman, não certifica cobertura em todos os modelos importados.
- Whisper small reconheceu palavra de ativação/comando em WAV sintetizado localmente (`Validation/windows-full-e2e/asr.json`). Não é fala humana. Microfone: 16.000 amostras, RMS 0,00001465, sem áudio persistido.

### Webcam física e OBS

DirectShow confirmou `ACER HD User Facing` no índice 0 e `OBS Virtual Camera` no índice 1. O executor valida o nome antes de salvar qualquer evidência, para não confundir vídeo físico com saída virtual. Cada avatar entregou 541 quadros em 18 segundos de amostragem, cerca de 30,06 FPS no receptor, a 1280×720. Rastreamento físico produziu pacotes e a ponte TCP conectou. A segunda parte usa prévia animada para verificar continuidade da saída; não valida gestos reais. Somente a imagem renderizada do avatar foi salva.

Ambos os logs terminaram com `CORPORATE_LIVE_RESULT=True`, mas os processos retornaram falha. Eventos Windows 1000 registraram `UnityPlayer.dll`, exceções `0xc0000005` e `0xc000041d`, após os testes. **Marcador interno positivo não equivale a E2E aprovado.** O executor foi ampliado para registrar também o código de saída em futuras execuções; os JSON desta captura anterior à alteração já registram FAIL.

Não houve detecção de rosto, pose ou mãos nesta rodada. Não aprovar mão no rosto/cabeça, oração, todos os dedos ou piscadas humanas a partir destes resultados. A tentativa adicional com o projeto mínimo existente foi inconclusiva: seu build Vulkan não inicializou e não continha shaders D3D11; foi encerrado por timeout. Isso não estabelece a causa do crash do Avatar nesta rodada.

### Pacote e limites

O assembly corrigido foi atualizado em `publish/Windows/AvatarStudio_Data/Managed/WebcamAvatar.dll`, idêntico ao export de teste `publish/WindowsE2E`. SHA-256: `CFC19CECD0FB1AC578E0CBAD1434A1E8062748FC96FC4C82B8452D1C2625C285`. Os módulos Python locais também foram sincronizados. O processo anteriormente aberto continua com o código carregado em memória: use **Sair** na bandeja e abra novamente para carregar as correções.

Avaturn foi validado com T1 de rosto estático. MetaHuman foi validado somente com fixture MakeHuman para o caminho de importação, sem personagem MetaHuman real. Corporativo 2 pago continua indisponível. Não foram aprovados serviços autenticados de agenda/agentes do ChatTrade, monitoramento remoto do admin, reconhecimento humano de voz/gestos, privacidade em reunião real ou todos os controles da bandeja.

Resumo estruturado: `Validation/windows-full-e2e/summary.json`. Evidências visuais: `Validation/windows-full-e2e/laboratory.png`, `Validation/corporate-virtual-output.png`, `Validation/cartoon-virtual-output.png` e `publish/Validation/corporate-390.png`.


## Webcam real assistida — 15/10/2026, rodada da manhã

Usuário confirmou disponibilidade e recebeu aviso após cada marcador de captura pronta. Webcam 0 e receptor OBS nomeado; sem persistir imagens da câmera física. Três capturas de gestos: 75 segundos de auditoria por avatar/rodada, com 60 segundos de amostragem do receptor, 1801 quadros a aproximadamente 30,02 FPS em cada execução. Rosto, pose e mãos foram detectados em todas as rodadas.

| Rodada | Pacotes | Mão esquerda/direita | Auditoria de corpo | Piscada esquerda/direita no avatar |
| --- | ---: | ---: | --- | ---: |
| Cartoon inicial | 1382 | 118 / 832 | Reprovada | 19 / 0 amostras |
| Corporativo | 1300 | 290 / 620 | Aprovada | 45 / 0 amostras |
| Cartoon repetição, somente corpo | 1420 | 370 / 378 | Reprovada | Não medida |

No Corporativo, o critério confirmou mãos próximas da cabeça, amplitude dos dois braços, abertura/fechamento dos dedos observados e resposta do rig. A auditoria de dedos usa a média de curvatura de indicador a mínimo; não certifica todos os segmentos e polegares individualmente nem naturalidade visual contínua. O gesto de oração foi solicitado, mas esta auditoria não mede colisão ou contato exato entre as palmas.

No Cartoon repetido, o punho esquerdo saiu do quadro em 1128/1420 amostras (79,4%); a amplitude detectada desse braço foi de 0,110 m, abaixo do critério de 0,25 m. A menor distância rastreada mão/cabeça foi 0,232 m, sem preencher o critério de contato de 0,23 m. O valor sentinela 99 em avatarHeadDistance significa ausência de amostra elegível, não distância física de 99 m. As duas mãos apresentaram variação de curvatura no rig, mas isso não aprova todo o movimento do braço esquerdo.

As piscadas unilaterais não foram aprovadas: apenas um dos lados acumulou amostras válidas nos dois avatares. Diagnóstico adicional direto do rastreador recebeu rosto em 600/600 quadros: picos eyeBlinkLeft=0,681 e eyeBlinkRight=0,696; nenhum quadro satisfez fechamento independente (um acima de 0,5 com outro abaixo de 0,3), e 12 quadros registraram ambos acima de 0,5. Isso mostra o sinal recebido nessa coleta; não demonstra, isoladamente, defeito de blendshape ou que o usuário não executou o gesto. Não foram alterados limiares para converter a falha em aprovação.

**Resultado integral: reprovado.** As três execuções terminaram com código 1 por asserts de gestos, não constituem reprodução do crash nativo registrado de madrugada nem comprovam sua correção. Testes sintéticos anteriores não substituem estas falhas de captura real. A cobertura visual de interpenetrações e a piscada unilateral continuam pendentes.

O executor agora preserva cada rodada em `--output-dir` e registra a auditoria corporal mesmo quando mede piscadas; reprova o resultado caso o corpo não passe. Evidências em `Validation/assisted-20261015/summary.json`, JSON/logs por rodada e `eye-diagnostic.json`. Arquivos locais ignorados pelo Git; somente saída do avatar salva como imagem.

## Agente Windows — mensagens e segundo plano (15/10/2026)

- Corrigido o compositor: limpa a pergunta depois que o processo local aceita o envio; falha ao iniciar preserva o texto. O caminho Android usa a mesma limpeza, sem certificação Android nesta rodada.
- Build Unity Windows concluído; DLL atualizada em publish/Windows e publish/WindowsE2E (hash idêntico ao build). Aplicativo já aberto precisa ser reiniciado para usar a correção.
- 113 testes Python aprovados. E2E com Ollama local aprovou resposta integral, arquivo de resposta e limpeza do compositor para Cartoon e Corporativo.
- E2E de isolamento aprovado no pacote WindowsE2E: câmera não anima o Agente, comandos movem o rig, idle pisca/boceja e retorno ao modo Webcam funciona. Log: Validation/agent-isolation-20261015.log.
- Segundo plano ainda não certificado integralmente: houve uma rodada Corporativo com 217 frames sem foco, mas rodadas seguintes não observaram perda de foco pelo Application.isFocused mesmo com outra janela ativada. O teste permanece reprovado; runInBackground=true sozinho não constitui E2E aprovado. Logs: Validation/agent-background-e2e.log, agent-background-hidden-e2e.log, agent-background-final-e2e.log.
- Proteção de ambiente compartilhado ativa: bloqueia áudio nesta configuração. Resposta por texto aprovada; não foi certificado wake word real, microfone contínuo, lembretes ou funcionamento prolongado na bandeja nesta rodada. Não alterar proteção para forçar aprovação.

## Microfone Intel Smart Sound — roteamento corrigido (15/10/2026)

As entradas processadas MME/WASAPI entregavam praticamente zero, mas a entrada WDM-KS “Grupo de microfones 3 ()” possui sinal real nos canais 3/4. Configuração local por nome/API, canal de índice 2, 16 kHz e seis canais; não modifica o registro nem os drivers. A captura seleciona o canal antes de converter para PCM mono do ASR.

Teste físico com frase TTS fixa pelos alto-falantes: pico 0,24585, 119 blocos ativos de 120, transcrição e palavra de ativação reconhecidas pelo Whisper small. Nenhum áudio captado foi salvo; nenhuma transcrição do ambiente foi impressa. Isso valida o caminho de captura/ASR com a frase de teste e não certifica a voz do usuário ou resposta completa do ChatTrade. Nove testes de voz aprovados. Scripts atualizados nos três pacotes locais; a escuta em execução precisa ser reaberta para carregar o novo roteamento.

## E2E assistido do microfone — revisão do canal (15/10/2026)

A seleção anterior do canal 3 foi insuficiente: reconheceu a reprodução pelos alto-falantes, mas não a voz do usuário. Na comparação assistida sem playback, os canais 1 e 2 reconheceram fala e a palavra de ativação; o canal 3 não reconheceu fala. Perfil local corrigido para canal 1 (índice 0), API WDM-KS e ganho limitado de até 16x antes do Whisper/VAD. Pacotes locais atualizados. Isso substitui a recomendação anterior de canal 3.

Confirmação final com o mesmo adaptador do aplicativo: pico 0,02875; 14 blocos acima do limiar; fala transcrita, mas palavra de ativação NÃO reconhecida. Resultado integral REPROVADO. Não certificar resposta do agente nem conexão ChatTrade a partir deste teste. A checagem de duplicidade no executor é uma regressão sobre o texto reconhecido, não uma certificação de eco físico. Evidências locais: Validation/microphone-assisted-channels.json e Validation/microphone-channel1-final.json. Áudio e transcrições não foram persistidos.

## E2E autônomo Windows — agenda, voz e visão (15/10/2026)

Executor reproduzível: `Tracker/.venv/Scripts/python.exe Tools/e2e-agent-autonomous.py`. Gera quatro frases TTS fixas com Microsoft Daniel, usa Whisper small local e o roteamento real do aplicativo. As quatro frases reconheceram a ativação e chegaram aos bloqueios corretos para capacidades não implementadas. Não simula acusticamente distância, ruído ou microfone físico.

Passaram: quatro fluxos de áudio/ASR, regressão de comandos duplicados, contrato HTTP isolado de consulta/criação/consulta de agenda com recusa de credencial inválida e detecção MediaPipe de um rosto sintético/nenhum rosto. O servidor HTTP da agenda é somente fixture local; nenhum evento Google ou dado de produção foi criado. O rosto de teste foi gerado por IA e não foi matriculado em cadastro de identidade.

WindowsE2E aprovou isolamento da animação do Agente nos modelos Cartoon/Corporativo, resposta do rig a comandos, idle e retorno ao modo Webcam (saída 0). Suíte Python: 119 testes aprovados. Corrigidos os guardas para consulta de agenda local e para cadastro facial/abertura de acesso: não podem inventar sucesso.

**E2E integral REPROVADO:** falta vínculo autenticado do Avatar com ChatTrade; integração real Google Agenda não validada; cadastro/reconhecimento de identidade facial e controlador de porta não implementados. Um detector de rosto/pessoa não substitui identificação, consentimento, autorização ou prova de vida. Nenhuma porta foi acionada. A proteção de privacidade permanece ativa.

Evidências locais: `Validation/agent-autonomous/report.json`, `run.log`, `unit-tests.log`, `windows-avatar-isolation.log` e `api-preflight.json`. O executor retorna código 1 enquanto houver esses bloqueios, mesmo com as simulações aprovadas. Cadastro facial positivo e operação real como porteiro não foram certificados.

## Captura para voz baixa e coexistência (15/10/2026)

Mantido Intel/WDM-KS, canal 1, calibrado com voz humana; filtro passa-altas de 80 Hz, normalização por energia de blocos com ganho máximo 16x e pré-captura de 300 ms para preservar o início da palavra de ativação. Processamento somente local; não muda volume, mute ou dispositivo padrão do Windows. Não há alegação de cancelamento de eco de reprodução de terceiros.

Passaram: abertura simultânea de captura WASAPI explicitamente compartilhada e WDM-KS; reconhecimento de Dingous em frase TTS reduzida para 6% da amplitude com ruído senoidal de 50 Hz; preservação de silêncio, remoção de DC, limitação de nível e ausência de NaN/Inf. Suíte total: 122 testes aprovados. Isso não certifica alcance físico de 1/2/3 metros nem compatibilidade com todos os aplicativos ou modo exclusivo. Pacotes Python atualizados; requer reabrir a escuta para carregar os módulos.

## E2E completo Windows solicitado — compromissos, música, áudio e webcam (15/10/2026)

Resultado integral: **REPROVADO**, sem alegação de 100%. Relatório consolidado local: `Validation/agent-autonomous/full-windows-report.json`.

- Cinco comandos sintetizados por TTS Windows passaram pelo Whisper e pelo roteamento real: consulta/criação de agenda, música, cadastro facial e abertura de acesso. Capacidades não implementadas foram bloqueadas corretamente.
- Simulador HTTP isolado do contrato ChatTrade aprovou consulta/criação/consulta da agenda, persistência da conversa e recusa de credencial inválida. Fluxo de música aprovou política administrativa autenticada de teste, escopo, preferência por jazz e sugestão consistente no mesmo dia. Esses resultados não são Google Agenda ou agentes da empresa real.
- Webcam física ACER HD User Facing abriu e entregou 45 quadros válidos a 640x480. Nenhuma imagem física foi salva. Não havia rosto/pose/mãos detectados no teste virtual; isso não certifica tracking humano nesta rodada autônoma.
- Unity WindowsE2E aprovou paridade de rig Cartoon/Corporativo nos modos Webcam/Agente, saída 0. OBS/DirectShow aprovou transmissão dos dois modelos a 1280x720, aproximadamente 30 FPS. O movimento usado na transmissão era a prévia animada.
- Teste acústico real pelos alto-falantes e microfone calibrado reconheceu fala, mas NÃO reconheceu a palavra de ativação em duas frases (compromissos e música). Ambos reprovados. Áudio injetado em arquivo aprovado não substitui esta reprovação física.
- Suíte Python: 122 testes aprovados. Detecção MediaPipe de rosto sintético e imagem vazia aprovada; nenhuma identidade cadastrada.
- API local respondeu 401 sem credencial; Admin local exige sua sessão. A sessão do navegador estava autenticada, porém o Avatar não tem arquivo de vínculo nem credencial. A busca autenticada de empresas não encontrou Dingous Avatar. Não alterei a empresa ativa nem criei permissões para contornar isso.

Bloqueios: ativação acústica consistente e vínculo autenticado empresa/aplicativo/usuário com ChatTrade. Não foi certificado uso real de Google Agenda, nem acesso à agenda pessoal, nem execução do agente musical da empresa. Nenhum evento real, cadastro de pessoa ou abertura de porta foi executado.

## Configuração real de empresa/aplicativo (15/10/2026)

Criada pelo Admin autenticado a empresa privada Dingous Avatar, ID 48, e o aplicativo Dingous Avatar Pessoal, chave company-app-48-b1ba4cca44de418192c7681adb4b77da. O parceiro foi habilitado e a persistência conferida após recarregar; motor Ollama local, qwen3.5:4b instalado, privacidade mantida ativa. Corrigida validação MVC que exigia AgendaAccount e Nutrition.Preferences opcionais vazios. Admin recompilado sem erros e iniciado na porta 6300.

Salvo o vínculo sem segredo em LOCALAPPDATA/Dingous/chat-trade.json, API http://127.0.0.1:5100. Essa criação substitui o bloqueio anterior de empresa inexistente; NÃO substitui autenticação. Nenhum token da API está disponível. Cliente agora aceita sessão existente protegida com DPAPI por usuário Windows, além da variável de sessão existente. O auxiliar Tools/configure-chattrade-session.py só salva após verificar empresa e obter validação HTTP da API; não cria token nem contorna login. Testes: 124 aprovados com configuração de teste isolada; autenticação real continua pendente. Captura da configuração: Validation/chattrade-avatar-settings.png.

## 2026-10-15 — Recorte do cabelo / mancha na testa
- Corrigido o recorte alpha dos materiais MakeHuman ao alternar shaders. Shader AlphaCutout em Resources mantém clip explícito no executável, sem depender da variante opcional do Standard.
- Build Windows concluído; smoke --hairline-only passou (3 cabelos), com comparação visual antes/depois em Validation/hairline. A faixa opaca na testa desapareceu; sobrancelhas e cílios também respeitam transparência.
- Esta verificação é de renderização Windows; não constitui E2E Android/web nem nova validação de movimentos.


## 2026-10-15 — Autenticação do Avatar pelo admin
- Implementado fluxo admin autenticado -> consentimento com antiforgery -> callback loopback de uso único -> validação na API -> DPAPI. Empresa e aplicativo são revalidados no servidor e no cliente; sessão de chat temporária, sem papel de admin.
- Admin compilou com zero erros; Python passou py_compile. Não foi possível concluir a autenticação nem testar a API com a credencial: revisão automática bloqueou o comando de reinício do admin/início do pareamento. Admin local ficou parado após a compilação; usar Conectar Avatar ao ChatTrade.cmd para iniciar e autorizar pelo navegador.
- Não declarar conexão autenticada até a validação real da API terminar.


## 2026-10-15 — Retomada do rastreamento no Modo Webcam
- SelectRunMode(false) agora inicia a captura automaticamente após encerrar a percepção do Agente. Selecionar Webcam novamente preserva o tracker existente. Android usa SetTracking(true), sem declaração de teste Android.
- Build Windows passou. Smoke --webcam-transition passou em Cartoon e Corporativo: início automático, pelo menos 5 novos pacotes da webcam física, clique repetido sem desligar e retorno ao Agente liberando o tracker. Log: Validation/webcam-transition.log. Não valida precisão de gestos reais.
- Pacote publish/Avatar Studio.exe atualizado.


## 2026-10-15 — Tela inicial de login e Deslogar
- Tela inicial Windows com Entrar com ChatTrade (autorização pelo admin no navegador) e opção Webcam sem acesso aos agentes. Reutiliza credencial DPAPI somente após validação na API; sem senha/token em PlayerPrefs.
- Deslogar no canto superior direito remove credencial salva e conversationId, encerra escuta/percepção/rastreamento/saída virtual e retorna ao login. Perfil visual é preservado.
- Build Windows passou; 3 testes Python de sessão passaram; smoke --session-ui passou para bloqueio inicial e ausência de escuta/percepção antes do login. Captura Validation/avatar-login.png.
- O login completo pela API atualizada permanece não homologado: início da API/admin atualizados foi bloqueado pela revisão automática na etapa anterior. O processo antigo da API não aceita a nova chave local fixa. Não afirmar autenticação concluída.


## 2026-10-15 — Ambientes e sugestões seguras de login
- Login do admin com labels associados e autocomplete username/current-password para sugestões do gerenciador de senhas do navegador. Senhas não são armazenadas pelo Avatar.
- LocalJob define DINGOUS_ENVIRONMENT a partir de Application.isEditor/Debug.isDebugBuild. Desenvolvimento: API 127.0.0.1:5100 e admin 127.0.0.1:6300; produção: HTTPS dingous.com.br. Sem fallback de produção para localhost. Build and Run do projeto raiz agora preserva a opção Development Build ao exportar o projeto corporativo.
- Credencial DPAPI de produção separada; chave fixa local recusada antes de envio externo; conversationId descartado ao trocar de ambiente.
- 131 testes Python passaram. Builds Windows Development e normal passaram, assim como smoke --session-ui em ambos. Capturas Validation/avatar-login-development.png e avatar-login-production.png. Admin com autofill compilou com zero erros em artifacts/avatar-local-admin-autofill.
- Alterações do admin/autorização ainda não publicadas em dingous.com.br; login real completo e sugestões de credenciais salvas no navegador não foram homologados nesta etapa.


## 2026-10-15 — Link local de pareamento indisponível
- Confirmado: porta 6300 sem listener e callback 58756 encerrado. Link informado expirado, não reutilizável.
- Launcher atualizado para artifacts/avatar-local-admin-autofill. Pareamento agora verifica disponibilidade do admin antes de abrir navegador, com instrução acionável; UI preserva erro específico.
- Dois testes de falha do admin passaram e build Windows passou. Não houve inicialização automática do admin: esse comando foi rejeitado pela revisão automática anteriormente; usar o launcher manual para iniciar e gerar link novo.


## 2026-10-15 — Correção do 404 de pareamento em produção
- Publicado admin com AvatarConnectionController (consentimento autenticado, antiforgery, callback loopback, grant de chat por empresa/aplicativo) e API com contrato de settings/agentes presente no HEAD limpo 22271a8. Artefatos construídos em cópia isolada; alterações locais não relacionadas e appsettings não foram incluídos. Docker base preserva demais serviços.
- ACR cq1f/cq1g concluídos. Digest ativo f4f3478935f6ef9077b320a36800b099f098ed02bdf1dfd8f4d46ecdab8611c6. Rollback preservado na tag avatar-auth-rollback-20261015. Houve aquecimento prolongado com 502/503 após reinícios; disponibilidade final confirmada.
- Verificação HTTPS real: /admin/avatar/status 200 e protocol=1, /admin/login 200, / 200; /api/dingous-partner/settings 401 sem credencial. 135 testes Python e 15 testes de contratos de autorização passaram. A autenticação pessoal completa requer a conta real e não foi declarada concluída.
- Cliente agora valida protocolo antes de abrir navegador, evitando link para servidor sem rota. Um link novo deve ser gerado no Avatar; os desafios antigos expiram.


## 2026-10-15 — E2E de login localhost solicitado
- Avatar Windows recompilado em Development, publicado em publish/Avatar Studio.exe e reaberto. Usa API 127.0.0.1:5100 e admin 127.0.0.1:6300.
- API local real validada: /health 200; /api/dingous-partner/settings 401 sem credencial.
- Admin local sem listener em 6300. Tentativa de iniciar por Start-Process foi rejeitada pela revisão automática (blocked by policy). Solicitado ao usuário executar Conectar Avatar ao ChatTrade.cmd; enquanto isso, não é possível concluir login/pareamento autenticado.
- Smoke --session-ui agora suporta --session-authenticated para exigir validação real da credencial pela API e desbloqueio da interface; não foi declarado aprovado nesta etapa.


## 2026-10-15 — Nova tentativa de iniciar admin localhost
- Removidos do LoginController os caminhos de login administrativo automático em localhost e as credenciais embutidas usadas por eles. Login GET preserva sessão autenticada existente; login inválido não busca identidade de desenvolvimento.
- Build artifacts/avatar-local-admin-secure concluído; 5 testes LocalAdminLogin.Smoke passaram. Launcher local passou a apontar para esse artefato.
- Tentativa explícita de iniciar a versão corrigida em 127.0.0.1:6300 foi novamente rejeitada pela revisão automática (blocked by policy), sem motivo adicional. Não foi tentado outro mecanismo para contornar essa rejeição. Serviço continua desligado e E2E local autenticado não concluído. Nenhuma publicação em produção foi realizada nesta etapa.


## 2026-10-15 — E2E real de login Windows executado
- Executado CorporateAvatarStudio.exe --corporate-smoke --session-ui --session-authenticated; exit 1. Interface permaneceu no login; exigência de sessão validada pela API falhou (sem credencial aceita). Log Validation/local-login-e2e.log e captura Validation/avatar-login.png.
- HTTP real: localhost:5100/health 200, settings 401 sem credencial, localhost:6300 sem serviço. Domínio público /admin/avatar/status e /admin/login 200. Nenhuma autenticação pessoal completa foi declarada.
- Preflight real do pareamento local falhou antes de abrir navegador, com motivo específico; 4 testes de indisponibilidade/protocolo passaram. Mensagem do produto deixou de solicitar execução de CMD.
- Não foi repetida a ação de inicialização rejeitada pela revisão automática.


## 2026-10-15 — Login local pelo botão do Avatar
- Entrar com ChatTrade em Development/Editor agora prepara o admin local após clique explícito da pessoa e abre o pareamento após protocol=1. Produção mantém o fluxo HTTPS existente. A abertura do aplicativo e as consultas de sessão não iniciam serviços locais.
- Novo helper local_chattrade_admin.py reutiliza serviço disponível, valida fingerprint do artefato sem login automático, inicia somente em loopback, aguarda prontidão e mantém diagnóstico no AppData. Caminho do projeto configurado apenas no arquivo privado local.
- 140 testes Python passaram (incluindo 5 testes do helper com Popen simulado); build Development e smoke real --session-ui passaram. Pacote Windows atualizado. O teste não acionou o botão que inicia o backend; não é um E2E de serviço autenticado.
- A execução direta do backend pela ferramenta permanece rejeitada pela revisão automática. A inicialização real pelo clique da pessoa ainda precisa ser confirmada; não foi declarada executada ou homologada.



## 2026-10-15 — Dingous Aplicativos

Executor local adicionado ao modo Agente, com perfis JSON no admin /admin/dingous-parceiro (Launcher). Cada perfil contém name, applicationKey, executable e arguments (lista, sem shell ou interpolação de texto do usuário). Comando explícito: “Dingous, abra notas”. Parâmetros personalizados são definidos pelo administrador em cada perfil; comandos livres e scripts não são executados. Habilitar e salvar cadastra/vincula dingous-app-launcher na empresa. Endpoint launcher-context exige usuário, empresa, aplicativo habilitado, escopo app, agente ativo e política habilitada. Falha nessa autorização bloqueia o executor sem interromper os outros agentes. Proteção de ambiente compartilhado bloqueia abertura.

Validação: 145 testes Python passaram, incluindo cinco do executor com fronteira de processo simulada. Builds Admin e API passaram (avisos preexistentes). Helpers copiados aos diretórios Tracker das exportações Windows existentes; não houve alteração C# ou necessidade de recompilar Unity neste passo. Não foi realizado lançamento real de aplicativo autenticado, salvamento no admin real nem publicação do servidor. Não é certificação E2E.


## 2026-10-15 — diagnóstico do login local
Admin 6300/status e API 5100/health responderam 200. A credencial legada local foi recusada com HTTP 401; a sessão do navegador havia expirado. Nenhuma permissão foi ampliada e nenhuma credencial foi fabricada. Mensagens do Avatar agora distinguem 401 (autenticar novamente), 403 (vínculos), indisponibilidade e política desabilitada. Quatro testes de sessão passaram; helper atualizado nas exportações Windows existentes. Login autenticado continua pendente de entrada pela conta no navegador.


## 2026-10-15 — login local autenticado validado
Login administrativo feito pelo formulário normal; empresa Dingous Avatar selecionada pela UI. Pareamento autorizado para o aplicativo pessoal existente. API aceitou a sessão e a credencial foi salva somente por DPAPI. avatar_session retornou AUTH_OK. E2E Windows real --session-ui --session-authenticated: exit 0, CORPORATE_SESSION_UI_RESULT=True; sessão salva validada pela API e tela desbloqueada. Evidência: Validation/authenticated-login-windows.log e Validation/avatar-connected-local.png. Aplicativo principal reaberto para carregar a sessão. Não foram alterados papéis ou ampliadas permissões.


## 2026-10-15 — falha real de autorização dos Olhos
Sessão real: GET settings 200, GET eyes/{app}/policy/{device} 403. Causa: AvatarEyesController exigia Admin/Root, enquanto pareamento entrega Customer com chat e app exatos. Correção no fonte permite somente policy/telemetry ao Customer autenticado com subject e ambos escopos, preservando Admin/Root e validação da empresa/aplicativo no serviço; não habilita administração ou leitura da prévia. Cinco casos de permissão passaram (Customer correto, aplicativo divergente, escopo ausente, Operator e Admin). API compilada isoladamente em Validation/eyes-api-build, com Program do HEAD para não ativar handler experimental de token local. Substituição/reinício da API foi rejeitada pela revisão automática: blocked by policy, sem justificativa adicional. Nenhuma alteração em execução foi aplicada pelo comando rejeitado. E2E Olhos real continua reprovado com 403; não declarar sistema 100%.


## 2026-10-15 — mensagens de conexão corrigidas no Windows
148 testes Python passaram; Unity Development build e exportação principal atualizados. Agenda agora distingue autenticação, modo local, conector ausente e privacidade; avisos de agenda não inicializam Ollama. Olhos distingue 401/auth_required de 403/access_denied, exibindo permissão recusada em vez de pedir novo login. Teste real local: agenda bloqueada pela política de ambiente compartilhado ativa; Olhos retornou access_denied duas vezes. Isso não comprova consulta de agenda nem funcionamento da câmera. Correção de permissão no servidor ainda não aplicada devido ao bloqueio automático já registrado. Aplicativo principal reaberto.


## 2026-10-15 — repetição E2E login Windows
Credencial anterior recusada; renovação concluída pelo consentimento normal na sessão administrativa existente. API real retornou AUTH_OK. CorporateAvatarStudio --session-ui --session-authenticated terminou exit 0 e CORPORATE_SESSION_UI_RESULT=True, comprovando leitura da sessão salva, validação na API e desbloqueio da interface. Evidências: Validation/login-e2e-latest.log e Validation/e2e-login-renewed.png. Aplicativo principal reaberto. Sessão temporária continua sujeita a expiração; não houve remoção da autenticação ou concessão de administrador ao Avatar. Olhos 403 permanece uma falha separada, sem aplicação da correção bloqueada do servidor.


## 2026-10-15 — prévia webcam Windows
Correção: habilitar Ver enquadramento da webcam inicia Modo Webcam quando o tracker está parado. Antes apenas mudava uma flag e não abria a prévia. Tracker real camera0 --frames8 com janela de prévia terminou exit0. Unity build Development passou. E2E --webcam-transition validou Cartoon e Corporativo: transição inicia captura, pacotes reais recebidos, selecionar Webcam duas vezes preserva tracker, retornar Agente libera captura e marcar prévia com tracker parado inicia captura e entrega frames reais. CORPORATE_WEBCAM_TRANSITION_RESULT=True, exit0. Exportação principal atualizada e reaberta. Evidência: Validation/webcam-preview-e2e.log. Teste não certifica qualidade dos gestos nem o módulo Olhos da API.


## 2026-10-15 — restauração do idle no Modo Agente
ShowPartner agora reativa AgentMotion.IdleEnabled e remove avatar.freeze tanto na entrada quanto ao reabrir painel já ativo. Corrige retorno após logout/laboratório manter animação de repouso desativada. Unity Development compilado e exportação principal atualizada/reaberta. E2E Windows --agent-idle-only terminou exit0, CORPORATE_AGENT_IDLE_RESULT=True, nos dois modelos: iniciou propositalmente idle desabilitado/avatar pausado, entrou no Agente e verificou retomada; frames de piscada, bocejo e pose relaxada; reentrada reativa idle. Temporizadores de piscada/bocejo foram antecipados no teste para validar sem espera aleatória; intervalos naturais de produção preservados. Evidência: Validation/agent-idle-e2e.log.


## 2026-10-15 — CORS Windows/web e Android
Preflight real OPTIONS da API5100 com Authorization/Content-Type: localhost:8765, 127.0.0.1:8765 e https://dingous.com.br retornaram204 com origem exata e credentials=true; https://untrusted.invalid retornou204 sem cabeçalho allow-origin, portanto bloqueado pelo navegador. Código CORS do ChatTrade atualizado para aceitar origem http(s)://10.0.2.2 somente em Development. Sete testes de origem passaram, incluindo rejeição de origem null, domínio externo e emulador em produção sem allowlist. Novo caso Android ainda não aplicado ao servidor devido ao bloqueio da atualização/reinício já registrado. Clientes Unity nativos não dependem de CORS; 401/403 exigem autenticação/escopo. Não houve E2E Android.


## 2026-10-15 — comando abra o notepad
Parser corrigido para artigos o/a e alias bloco de notas. Cinco testes do executor passaram, incluindo argv sem shell, escopo e recusa de acesso. Configuração real retornou launcher ausente, _launcherAuthorized false e sharedEnvironmentProtection true: abertura real não autorizada/não executada. Helpers atualizados nas exportações existentes. Não confundir parsing corrigido com integração administrativa ativada.

## 2026-10-15 — validação acompanhada do agente Aplicativos

- Admin real em http://localhost:6300/admin/agentes: Dingous Aplicativos continua inativo; vínculo desabilitado. Não foi alterado por fora do admin.
- Correções no fonte do admin: ordem checkbox/hidden para Ativo e Padrão; enum como texto no JSON do editor; temperatura HTML interpretada com cultura invariável; ModelState inválido impede salvamento.
- Compilação do admin e teste AgentForm.Smoke: sete casos de temperatura passaram.
- AvatarEyes.Smoke --permissions: cinco casos de autorização por escopo passaram, sem banco/câmera.
- Python: 148 testes passaram. Corrigida a dependência acidental dos testes de memória e parceiro nas configurações reais da sessão; política simulada isolada por teste.
- E2E real interrompido antes da execução: servidor aberto ainda usa versão anterior. A atualização/reinicialização havia sido recusada com blocked by policy; nenhuma tentativa alternativa foi usada para contornar a recusa.
- Não comprovado: ativação real, perfil Notepad salvo no admin, comando por microfone, abertura real do Notepad, autorização de Olhos no servidor atual.
