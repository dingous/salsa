# Compatibilidade local UniVRM 0.131.3

`VRM10/Runtime/Components/Vrm10Runtime/ControlRig/Vrm10RuntimeControlRig.cs`: o descarte desassocia o Avatar gerado do Animator e destrói o GameObject do Control Rig. O original tentava destruir seu Transform isoladamente, operação rejeitada pelo Unity 6000.4.5f1 ao descarregar um modelo ainda vivo. Preservar ou reavaliar esta correção ao atualizar o pacote.

O controlador do aplicativo usa `Vrm10FastSpringboneRuntimeStandalone`, cuja implementação termina o job em `Process`. Isso mantém a simulação sincronizada com o retargeting manual de um único avatar.

Essas alterações não resolveram a falha gráfica de encerramento encontrada nesta máquina. Um projeto Unity mínimo sem estes pacotes também a reproduziu. Consulte `WINDOWS_E2E.md`; não atribuir a correção dessa falha a este patch.
