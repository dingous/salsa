# Ferramentas da versão corporativa

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

Teste com hardware: `Tracker/.venv/Scripts/python.exe Tools/e2e_corporate_camera.py`.
Requer webcam física no índice 0 e OBS Virtual Camera no índice 1. Abre a webcam
por 30 segundos, verifica o receptor DirectShow e inclui troca para tela limpa
com prévia animada. Salva somente a imagem renderizada do avatar, nunca uma
imagem da webcam real. O microfone da reunião continua sendo selecionado no
Teams/Meet/Zoom.
