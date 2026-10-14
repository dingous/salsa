# Publicação do Avatar no Dingous

Execute `Deployment/package.ps1` para gerar `Validation/AvatarDeploy`, contendo somente assets públicos.

A imagem deriva do digest confirmado do container `chattrade-all` em produção. Ela acrescenta `/Games/Avatar/` e valida o Nginx, preservando os serviços e o código existentes. Esse fluxo evita incluir alterações locais de outras aplicações.

```powershell
az acr build --registry chattradeacr --image chattrade-all:avatar-candidate --file Validation/AvatarDeploy/Dockerfile.azure-all --build-arg BASE_IMAGE=chattradeacr.azurecr.io/chattrade-all@sha256:DIGEST Validation/AvatarDeploy
```

Confirme o resultado do build e que `latest` continua no digest usado como base. Guarde uma tag de rollback antes de promover a imagem candidata para `latest`. Depois reinicie `az webapp restart -g dingous -n dingous` e confira HTTPS, redirecionamento, JS/MJS/WASM, modelos e carregamento do avatar no endereço público.

O site público faz rastreamento no navegador, não chama a API Python local. A câmera virtual exige OBS com captura da janela ou o aplicativo local instalado. A pasta `publish` permanece ignorada no Git.
