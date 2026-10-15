# Downloads oficiais dos frameworks

Verificado em 15/10/2026. Downloads não equivalem a integração nem a E2E aprovado.

| Opção | Estado local | Origem e versão |
| --- | --- | --- |
| Corporativo 2 — Real-Time Full Body Tracking | Não adquirido na conta Unity conectada; busca em My Assets sem resultados. Pacote não baixado. Preço observado US$39, sem impostos. | https://assetstore.unity.com/packages/tools/ai-ml-integration/real-time-full-body-tracking-system-350552 |
| Corporativo 3 — Avaturn | SDK oficial baixado em `Downloads/Frameworks/AvaturnUnityWebView`. Modelo GLB já existente no projeto. | https://github.com/avaturn/avaturn-unity-webview-sdk — 2.0.0, commit `e2be3c930826d5eba3bced038cc68d08e53667c4` |
| Corporativo 4 — MetaHuman | Fontes oficiais OpenRigLogic/DNA baixadas em `Downloads/Frameworks/MetaHumanOpenRigLogic`. Não incluem personagem. | https://github.com/EpicGames/OpenRigLogic — branch estável 5.8, commit `55756aca0c4c55adc56ad68a796bcc1c0bf6fa5a` |

## Limites de integração

- O Avaturn WebView SDK declara suporte WebGL e mobile, Unity 2021.3+. Depende de Newtonsoft JSON 3.0.2, glTFast 5.2.0 e Cinemachine 2.6.17; inclui componentes UniWebView. Não foi adicionado automaticamente ao manifesto do projeto Windows. Seu download não comprova suporte Windows nem validação mobile.
- OpenRigLogic é C++ com bindings Python e licença MIT. Precisa de compilação e ponte nativa para Unity; não é um pacote Unity nem um avatar pronto. README e licenças originais permanecem nos downloads.
- O conteúdo de exemplo oficial MetaHuman está em https://www.fab.com/listings/27a81942-69bf-498e-a41f-004d0d2db37b . A EULA Fab foi aceita com autorização do usuário e o conteúdo foi adquirido. O ZIP adicional contém apenas Sample.dna e faceboard.json, sem malha de personagem. O conteúdo possui licença própria, separada da MIT do OpenRigLogic.
- UniVRM 0.131.3, MakeHuman/MPFB, modelo Avaturn e SALSA já estavam presentes. Não foram novamente baixados nem considerados substitutos do plugin pago.

MetaHuman adiado por decisão do usuário em 15/10/2026: manter somente Unity, sem instalar Unreal Engine. A instalação do Unreal 5.8.3 foi cancelada antes do início. OpenRigLogic foi compilado e sua análise offline passou no Windows (Validation/metahuman-dna-windows.log); isso não integra um personagem MetaHuman visível. O conteúdo completo do fluxo Epic exigia um Unreal compatível instalado. Não retomar essa instalação sem nova solicitação. Os modelos Unity existentes continuam como foco.
