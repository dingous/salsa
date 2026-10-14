# Exportação Unity para Android

Execute `Tools/build-android.ps1` com Unity 6000.4.5f1 e Android Build Support instalados. O APK é gerado em `CorporateUnity/Build/Android/DingousAvatar.apk` e copiado para `publish/Android/DingousAvatar.apk`. Identificador: `br.com.dingous.avatar`. IL2CPP, ARM64 para celulares e x86_64 para emuladores; Android mínimo 8/API 26, orientação paisagem.

Modelos CorporateHuman/MakeHuman e Seed-san/Cartoon são empacotados no StreamingAssets. No Android são extraídos com UnityWebRequest para armazenamento privado antes da importação UniVRM. Mesma interface e rig do projeto principal.

Este primeiro APK exporta o estúdio, a prévia e a edição disponíveis no Unity. Serviços LocalJob continuam exclusivos do Windows: IA Ollama, transcrição Whisper, ativação por voz, rastreamento MediaPipe externo, geração por foto e câmera virtual ainda exigem adaptação/conexão móvel. O APK informa a limitação em vez de procurar python.exe. Não contém serviço Android de microfone em background nem conexão autenticada ao admin.

O APK usa assinatura de desenvolvimento padrão do Unity; publicação em loja requer configuração de assinatura própria. Build/install/startup devem ser confirmados pelo relatório em Validation, sem tratar exportação como E2E completo de voz, gestos e background.
