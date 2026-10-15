# Dingous Música

Especialista do ecossistema ChatTrade, chave `dingous-music`, papel `music`. A ativação é feita em **Admin → Parceiro Dingous → Dingous Música**. Habilitar e salvar cria o cadastro e vínculo na empresa selecionada, sem duplicar um agente existente. Um cadastro inativo precisa ser reativado no cadastro de agentes. Os demais vínculos são preservados.

No admin ficam catálogo, horário, habilitação diária, janela sem repetição e porcentagem de descoberta. Não há migration nova: a política utiliza o JSON de configurações existente. É necessário implantar Admin/API atualizados e salvar a configuração real; escrever o código não cadastra um agente no banco de produção.

## Aprendizado pessoal

O cliente obtém um contexto opaco da API autenticada, que valida empresa, aplicativo habilitado, usuário e vínculo do agente ativo. O histórico local é separado por esse contexto em `Partner/Music/<contexto>/state.json`, com trava entre processos e escrita atômica. Sem conexão autenticada válida, o módulo não cria um perfil global alternativo.

Comandos:

- “Dingous, sugira uma música.”
- “Gostei dessa música” / “Não gostei dessa música”: avaliam a sugestão do dia.
- “Gosto de jazz e rock” / “Não gosto de rock”: registram preferências explícitas.
- “Apague meu gosto musical”: remove preferências e avaliações, preservando apenas o controle das sugestões recentes.

As avaliações alteram a classificação local das próximas escolhas. Não é fine-tuning, identificação de emoção ou leitura do histórico de Spotify. Não se presume gosto musical sem feedback. A parte de descoberta pode variar os estilos, respeitando rejeições explícitas. Se faltarem faixas elegíveis no catálogo, informa a limitação e pede ampliar o catálogo.

É uma sugestão única por data local. Pedir novamente retorna a mesma faixa; avaliações não geram novas sugestões no mesmo dia. O histórico evita repetição na janela configurada. O catálogo inicial tem oito faixas com referências públicas de artistas/gravadoras; tags são categorias editoriais, e o catálogo pode ser ampliado no admin. Não copia letras nem baixa áudio. O link só é aberto pelo usuário; o agente não inicia reprodução automaticamente.

## Horário e privacidade

O padrão configurável é 09:00, America/Sao_Paulo. A entrega é processada pela escuta do Avatar enquanto o aplicativo está ativo. A janela é de dois minutos; não existe recuperação de notificações horas depois. Reiniciar ou alternar processos não duplica a entrega do dia. Uma consulta manual no dia também evita anúncio automático repetido. Não é uma automação do Codex nem um serviço que acorda um computador desligado.

Proteção de ambiente compartilhado bloqueia consulta, alteração e anúncio do gosto pessoal. Em contexto privado, a fala da recomendação informa somente título e artista, sem histórico, motivos pessoais ou leitura do link. Feedback e preferências não são exportados ao ChatTrade ou ao treinamento; somente a política e o contexto autenticado são consultados. A seleção é local e não chama Ollama nem APIs pagas.

## Entrega e validação

113 testes Python passaram, com 16 novos testes musicais de feedback, rejeição, repetição, reinício, horário/fuso, privacidade, isolamento e catálogo. Testes .NET confirmam cadastro idempotente, vínculo por empresa, preservação de outros agentes, validação do catálogo e contexto por usuário/aplicativo. Admin e API compilados. Foi instalada a dependência IANA `tzdata` no Python Windows, sem a qual ZoneInfo não conseguia consultar São Paulo.

O cliente Python é atualizado também no pacote Windows. Não houve publicação do ChatTrade, salvamento na empresa real, teste de notificação às 09h reais ou E2E Android nesta alteração. Sem preferências informadas pelo usuário, nenhum gosto pessoal foi predefinido.

Referências do catálogo: https://www.thebeatles.com/here-comes-sun ; https://www.coldplay.com/songs/ ; https://www.marisamonte.com.br/ ; https://www.youtube.com/watch?v=PpxLo642Q98 ; https://www.youtube.com/watch?v=5NV6Rdv1a3I ; https://www.milesdavis.com/track/so-what-feat-john-coltrane-cannonball-adderley-bill-evans/ .
