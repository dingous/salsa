# Dingous Alimentação

Especialista `dingous-nutrition`, cadastrado pelo Dingous ChatTrade na empresa autenticada ao habilitar **Parceiro Dingous → Dingous Alimentação** e salvar. O cadastro é idempotente, preserva outros vínculos e usa o modelo local configurado. Não é ativado por padrão.

O perfil administrativo inclui maioridade, restrições/condições, padrão alimentar e preferências. Campos desconhecidos permanecem pendentes: o objetivo de emagrecimento não confirma ausência de alergias ou doenças. O agente fornece organização alimentar educativa, ideias de refeições e compras, sem dieta clínica, calorias, medicamentos, jejum ou promessa de resultado. Referência: [Guia Alimentar do Ministério da Saúde](https://www.gov.br/saude/pt-br/composicao/saps/promocao-da-saude/guias-alimentares).

O Avatar atende perguntas sobre dieta, alimentação e cardápio pelo mesmo backend compartilhado entre modelos. Consultas explícitas sobre treino continuam no Fitness. O runtime exige vínculo e perfil ativo vindo do admin. Não envia essas consultas ao Ollama quando consegue responder pelas regras locais. Em ambiente compartilhado, retorna somente proteção de privacidade e bloqueia fala. O Android conserva essa proteção por padrão.

Preferências não são interpretadas como instruções nem geram prescrições automaticamente. Para uma dieta individualizada, é necessário nutricionista.

Validação local: testes Python cobrem roteamento, privacidade antes de ler o perfil, ausência de vínculo, perfil pendente e organização vegana sem prescrição. O smoke do ChatTrade verifica criação no tenant autenticado, idempotência, remoção isolada do vínculo e validação do perfil, com dependências em memória. Esses testes não comprovam publicação nem execução no emulador. A ativação na empresa de produção e o E2E Android continuam pendentes.
