# Preparar seu PC Windows

## 1. Instale as ferramentas

Use Windows 10/11 compatível com .NET 10, atualizado, e instale:

1. **Git para Windows**, em https://git-scm.com/downloads/win.
2. **SDK do .NET 10**, em https://dotnet.microsoft.com/download/dotnet/10.0. Escolha SDK, não apenas Runtime. Na maioria dos PCs a arquitetura é x64; para Windows ARM escolha ARM64.
3. Editor opcional: **Visual Studio Code** com a extensão **C# Dev Kit**, da Microsoft. Também pode usar uma versão do Visual Studio compatível com .NET 10 e a carga de trabalho de desenvolvimento para desktop com .NET.

Feche e reabra o PowerShell depois da instalação. Confira:

```powershell
git --version
dotnet --info
dotnet --list-sdks
```

A lista deve mostrar um SDK `10.0.xxx`. Se `dotnet` não for encontrado, confira a instalação/PATH e reabra o terminal. Não é necessário Python, Node.js, C++ nem instalar pacotes NuGet manualmente.

## 2. Obtenha esta versão do código

As alterações desta sessão precisam estar publicadas no GitHub para o clone abaixo contê-las. Caso ainda não estejam, baixe/copie os arquivos desta sessão para seu PC.

Depois de publicar a branch da sessão:

```powershell
git clone --branch arena/91c71699-f8translate https://github.com/samuelffer/f8translate.git
cd f8translate
```

Se essa branch ainda não existir no GitHub, o comando falhará: não significa que há um problema no .NET.

## 3. Abra e execute

Abra a pasta no editor ou use o PowerShell na raiz do projeto:

```powershell
dotnet restore src/F8Translate/F8Translate.csproj
dotnet build src/F8Translate/F8Translate.csproj -c Debug
dotnet run --project src/F8Translate/F8Translate.csproj
```

`restore` baixa os componentes necessários; precisa de internet no primeiro uso. `run` também restaura e compila se necessário.

Para testar uma alteração: salve o código, escolha **Sair** na bandeja e execute `dotnet run` novamente. O X da janela apenas a esconde. Só uma instância pode ficar aberta por sessão do Windows.

Arquivos principais:

- `Program.cs`: inicialização e instância única.
- `MainForm.cs`: interface, bandeja, fluxo da tradução e clipboard.
- `Native.cs`: atalhos e integração com o Windows.
- `TranslationService.cs`: chamada ao provedor online.
- `Settings.cs`: configurações locais.

## 4. Teste manual antes de distribuir

Use texto descartável no Bloco de Notas, nunca um documento importante.

- [ ] Ao abrir sem consentimento, F8 não envia texto ao serviço.
- [ ] Escolha pt → en, autorize e salve; selecione uma frase e pressione F8.
- [ ] No modo padrão, o documento permanece igual e o resultado aparece na janela.
- [ ] Copiar tradução funciona; antes desse clique, o clipboard anterior deve ser restaurado.
- [ ] Ative substituição: a seleção deve ser trocada. Teste Ctrl+Z no Bloco de Notas.
- [ ] Sem seleção: mensagem útil, sem colar conteúdo antigo.
- [ ] Sem internet: erro claro, sem alterar o documento.
- [ ] Mais de 500 bytes: mensagem de limite, sem enviar a tradução.
- [ ] Idiomas iguais: erro claro.
- [ ] Pressione F8 repetidamente: não deve iniciar várias requisições.
- [ ] Durante uma tradução lenta, mude de janela: não deve colar em outra janela.
- [ ] Durante uma tradução lenta, copie outro texto: não deve sobrescrever esse novo clipboard.
- [ ] Teste clipboard vazio, texto, imagem e conteúdo formatado antes de usar F8; confira a restauração.
- [ ] Mude o atalho e salve; teste um atalho ocupado por outro aplicativo.
- [ ] Pause pela bandeja; o atalho deixa de atuar. Despause e teste novamente.
- [ ] Feche pelo X, reabra pela bandeja e finalize por Sair.
- [ ] Reabra: idiomas/atalho/consentimento persistem; substituição volta desativada.
- [ ] Teste a distribuição em outro PC sem SDK instalado.

Não há suíte automatizada de testes nesta primeira versão. O workflow faz build/publicação, mas não verifica interação de desktop nem disponibilidade/qualidade da API.

## 5. Gere o executável

Para Windows x64:

```powershell
dotnet publish src/F8Translate/F8Translate.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/win-x64
```

Abra `artifacts\win-x64\F8Translate.exe`. Distribua essa pasta em ZIP após testar. O `.pdb`, se gerado, contém símbolos de depuração e não é necessário ao usuário final. Para ARM64, troque `win-x64` por `win-arm64` e use outra pasta de saída.

O executável inclui o runtime e pode ser maior do que você espera. Componentes nativos podem ser extraídos temporariamente pelo .NET ao abrir. Isso não é um instalador/MSI.

O SmartScreen pode alertar porque o aplicativo é novo e não tem assinatura. Não instrua usuários a desativar o antivírus; antes de distribuir amplamente, planeje assinatura de código, uma release verificável e testes em máquina limpa.

## Problemas comuns

- **F8 não funciona:** confira consentimento, clique em Salvar e ativar, verifique se está pausado e teste outro atalho. Alguns teclados exigem Fn+F8.
- **Não copia/cola em determinado programa:** teste no Bloco de Notas. Programas elevados ou que interceptam atalhos podem bloquear a automação.
- **Clipboard ocupado:** feche ferramentas de clipboard conflitantes e tente novamente.
- **Tradução recusada:** verifique internet, tamanho do trecho e cotas do MyMemory.
- **Quero zerar configurações:** saia pela bandeja e exclua `%LOCALAPPDATA%\F8Translate\settings.json`.
