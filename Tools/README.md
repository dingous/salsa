# Ferramentas da versão corporativa

Lip sync Windows: `install_salsa.py` restaura o SALSA real do cache Unity Asset Store licenciado. Consulte `../LIPSYNC_SALSA.md`. A DLL comercial não é enviada ao Git; os builds restauram a dependência local.

`setup-avatar-eyes.ps1` instala o YOLOX Nano oficial, verificando SHA256. A percepção no modo Agente é administrada pelo ChatTrade; consulte `../AVATAR_EYES.md`.

O aplicativo anterior permanece na raiz. O produto novo está em `CorporateUnity`.
Os pacotes UniVRM são compartilhados com o diretório `Packages` original.

Para compilar o novo executável: `powershell -File Tools/build-corporate.ps1`.
O resultado é `CorporateUnity/Build/CorporateAvatarStudio.exe`.

O modelo VRM já está incluído; Blender e MPFB não são necessários para usar o
aplicativo. Para recriar o asset:

1. Baixe o Blender 5.2.2 oficial e clone MPFB em `Downloads/mpfb2`, tag `v2.0.18`.
2. Execute `Tracker/.venv/Scripts/python.exe Tools/download_corporate_assets.py`.
3. Execute Blender com `--background --python Tools/build_corporate_human.py`.
4. Execute `Tracker/.venv/Scripts/python.exe Tools/glb_to_corporate_vrm.py`.
5. Compile o projeto.

O Blender opera como ferramenta externa de geração. O código GPL do MPFB não
é incorporado ao runtime Unity; somente os assets CC0 gerados são distribuídos.
Os arquivos de download, o projeto `.blend` e os builds ficam fora do Git.

Teste sintético do executável: `CorporateAvatarStudio.exe --corporate-smoke`.
Ele valida estilos, acessórios, layout, pose, mãos, expressões e referência
fotográfica, sem ativar a webcam. Use `-logFile` para salvar os resultados.

Para repetir a regressão do pacote Windows exportado, execute
`powershell -File Tools/test-windows-release.ps1`. O script testa os 12 presets,
o rig feminino, paridade dos estilos/modos, contato de mãos/rosto, isolamento
do Agente, o modelo Avaturn T1 e a importação MetaHuman preparada com uma fixture
MakeHuman (não é teste de um personagem MetaHuman real), sem câmera nem síntese de fala. Cada processo tem limite de tempo;
o resultado exige código de saída zero e marcador de aprovação, sem asserts
falhos. Logs e `results.json` ficam em uma pasta datada dentro de `Validation`.
O script não comprova renderização gráfica, hardware real ou integrações externas.

Teste com hardware: `Tracker/.venv/Scripts/python.exe Tools/e2e_corporate_camera.py`.
Requer webcam física no índice 0 e OBS Virtual Camera no índice 1. Abre a webcam
por 30 segundos, verifica o receptor DirectShow e inclui troca para tela limpa
com prévia animada. Salva somente a imagem renderizada do avatar, nunca uma
imagem da webcam real. O microfone da reunião continua sendo selecionado no
Teams/Meet/Zoom.
