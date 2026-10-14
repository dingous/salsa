# Corporate Avatar Studio

Novo aplicativo Unity nativo, separado do Avatar Studio original e da versão web.
Abra esta pasta no Unity 6000.4.5f1 ou execute `Iniciar Corporate Avatar Studio.cmd`
na raiz após compilar com `Tools/build-corporate.ps1`.

O modelo incluído é um humano MakeHuman real, vestido com roupa social, exportado
com MPFB 2.0.18 e carregado pelo UniVRM 0.131.3. Possui 52 controles ARKit, 15
visemes, rig humanoide com dedos, três cabelos, barba e óculos ajustados à malha.
O editor permite aparência realista ou estilizada, cores, cabelo, barba, óculos
e proporções de rosto/maxilar. A foto fornece referências de cores e proporções;
não produz uma reconstrução fotorealista exata da identidade.

Em Transmissão, inicie a webcam e a câmera virtual. No Teams, Meet ou Zoom,
selecione **OBS Virtual Camera** como câmera e seu microfone habitual como áudio.
A boca acompanha o movimento detectado na webcam, incluindo fala em português;
não há reconhecimento de fonemas nem substituição da sua voz. OBS deve estar
instalado. Apenas uma aplicação pode emitir na câmera virtual OBS por vez.

Os 52 controles ARKit existem no modelo. O MediaPipe fornece 51 controles
correspondentes mais o canal neutro; a língua (`tongueOut`) não é detectada
automaticamente por esse rastreador. Os 15 visemes ficam disponíveis no modelo;
o movimento labial ao vivo usa os controles visuais da boca.

O rastreamento Face/Pose Full/Hands roda localmente via `../Tracker`. Vídeos e
fotos não são enviados a servidores. Modelos podem ser baixados no primeiro uso.
UDP 5059 e TCP 5060 evitam conflito de serviços com a versão antiga. Os perfis
são salvos no armazenamento próprio de Corporate Avatar Studio; no aplicativo
nativo, persistência usa arquivos locais em vez de cookies.

Os fundos incluem um escritório 3D local, estúdio e chroma key. O fundo verde
permite recorte no OBS. A câmera virtual transporta vídeo opaco;
transparência não é transmitida pelo dispositivo OBS Virtual Camera.

Os pacotes Unity são compartilhados com `../Packages`, portanto abra este
projeto dentro do repositório Salsa. A geração reproduzível está em
`../Tools/build_corporate_human.py` e `../Tools/glb_to_corporate_vrm.py`.
Consulte `Assets/StreamingAssets/Avatars/LICENSE-MakeHuman.txt` para licenças.

30 FPS é a meta da saída, sujeita ao hardware. Reuniões reais e a qualidade
de gestos ocultos/fora de enquadramento precisam de validação prática.
