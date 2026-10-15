# Memória do projeto Dingous Avatar

## Escopo confirmado pelo usuário

Este projeto atende três usos do mesmo ecossistema:

1. **Webcam / stream:** avatar acompanha rosto, expressões, mãos e corpo da pessoa para transmissão e uso como câmera virtual.
2. **Avatar Agente pessoal:** recebe comandos, conversa por voz, consulta agentes e ajuda com prioridades e agenda. Seus movimentos vêm das respostas e comandos; não deve imitar a webcam. A câmera pode servir à percepção autorizada do ambiente, separada da animação.
3. **Avatar Agente de atendimento web:** avatar de atendimento integrado ao **web chat do Dingous Chat Trade**, executado via web, para conversar com visitantes e clientes usando os agentes e fluxos autorizados para a empresa e o aplicativo.

Os dois modos principais continuam claros: **Modo Webcam** e **Modo Agente**. Atendimento web é um contexto de uso do Agente; não implica criar um terceiro botão no aplicativo sem necessidade de produto.

## Regras para próximas alterações

- Administração, parâmetros, agentes e fluxos pertencem ao admin do Dingous Chat Trade. Respeitar empresa, aplicativo, sessão e permissões em toda integração.
- Compartilhar contratos de rig, expressões, gestos e personalização entre Cartoon e Corporativo, nos dois modos e no atendimento web, respeitando capacidades de cada plataforma.
- O atendimento de clientes não pode receber memórias pessoais, agenda privada, credenciais ou contexto do agente pessoal. Conversas e dados devem permanecer isolados por empresa, aplicativo, usuário e sessão.
- Preferência do usuário: IA local sem API paga. Não expor serviços locais ou credenciais no navegador público. A arquitetura de acesso da versão web deve ser validada antes de publicar.
- Por enquanto, os E2E solicitados pelo usuário devem priorizar **Windows**. Não declarar testes Android/web realizados a partir de testes Windows.
- Esta memória registra o escopo e as decisões; não comprova funcionalidades implementadas. Consultar APP_MODES.md e os relatórios E2E para estado e limitações reais.
- Nunca gravar chaves, tokens ou segredos fornecidos em conversa nos documentos ou no Git.
