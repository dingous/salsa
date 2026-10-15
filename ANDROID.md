# Exportação Unity para Android

Execute `Tools/build-android.ps1` com Unity 6000.4.5f1 e Android Build Support instalados. O APK é gerado em `CorporateUnity/Build/Android/DingousAvatar.apk` e copiado para `publish/Android/DingousAvatar.apk`. Identificador: `br.com.dingous.avatar`. IL2CPP, ARM64 para celulares e x86_64 para emuladores; Android mínimo 8/API 26, orientação paisagem.

Modelos CorporateHuman/MakeHuman e Seed-san/Cartoon são empacotados no StreamingAssets. No Android são extraídos com UnityWebRequest para armazenamento privado antes da importação UniVRM. Mesma interface e rig do projeto principal.

O APK inclui serviço Android de câmera/microfone em primeiro plano, reconhecimento offline Vosk em português, MediaPipe Face/Pose/Hands e síntese pela voz portuguesa instalada no Android. O serviço tem notificação e ação Parar; inicia somente pela interface com permissões concedidas pelo usuário. O mesmo rastreamento alimenta os rigs Cartoon e Corporativo. A presença desses componentes não confirma qualidade dos gestos: o E2E assistido precisa validar cada modelo.

A IA roda no computador com Ollama. Execute `Tracker/.venv/Scripts/python.exe Tracker/android_bridge.py`, conecte o emulador por `adb reverse tcp:8767 tcp:8767` e use Parear IA local com `http://127.0.0.1:8767` e o código privado em `%LOCALAPPDATA%/Dingous/Partner/android-pair-token.txt`. Não publique o código. A ponte recebe apenas texto, exige autenticação e permanece em loopback. Não é inferência Ollama dentro do celular nem integração autenticada ao admin ChatTrade.

A ponte força proteção de ambiente compartilhado: respostas de IA ficam em texto, sem memória pessoal ou fala automática. Testar voz local reproduz apenas uma frase fixa sem dados pessoais. Câmera identifica até três rostos e AudioManager verifica modo de chamada; esses sinais não garantem identificação de reuniões ou pessoas fora do enquadramento. Geração por foto e câmera virtual Windows continuam dependendo dos serviços de computador.

O APK usa assinatura de desenvolvimento padrão do Unity; publicação em loja requer configuração de assinatura própria. Build/install/startup devem ser confirmados pelo relatório em Validation, sem tratar exportação como E2E completo de voz, gestos e background.
