# UniVRM 0.131.3

Framework oficial do VRM Consortium, licença MIT (LICENSE.txt nesta pasta).

Origem: https://github.com/vrm-c/UniVRM/releases/tag/v0.131.3
Arquivo original: Downloads/UniVRM-v0.131.3.zip

Pacotes locais instalados no manifest do Unity:
- Packages/UniGLTF: com.vrmc.gltf
- Packages/VRM: com.vrmc.univrm, VRM 0.x
- Packages/VRM10: com.vrmc.vrm, VRM 1.0

Inclui importação/exportação de avatares, materiais MToon e recursos de expressões e spring bones. Amostras oficiais estão em Samples~ dos pacotes; os modelos possuem suas próprias condições de uso.

O aplicativo agora usa VrmAvatarController para carregar modelos VRM em tempo de execução, criar o rig normalizado UniVRM, aplicar pose/dedos/cabeça, mapear expressões e processar a física de cabelo. A tela Modelo VRM importa arquivos 0.x e 1.0, permite voltar ao gerador procedural e a biblioteca copia o arquivo VRM junto do perfil. O padrão é Seed-san, da VirtualCast, com atribuição e licença próprias em StreamingAssets/Avatars. A geração procedural pela foto continua como alternativa explícita; UniVRM não reconstrói modelos profissionais a partir de fotos.
