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
