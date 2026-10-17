# Preparar seu PC Windows

**Quer apenas usar/testar?** Não precisa instalar ferramentas de desenvolvimento. Baixe um artefato do Actions: `portable` funciona sem instalar .NET; `compact` exige apenas **Desktop Runtime 10 x64** uma vez. O SDK abaixo só é necessário para editar/compilar localmente.

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
- `SelectionWatcher.cs`: observação local de seleções, em thread separada.
- `SuggestionPopup.cs`: sugestão temporária que não rouba foco.

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
- [ ] Teste `compact` em PC com Desktop Runtime 10, mas sem SDK.
- [ ] Teste `portable` em PC sem SDK e sem Runtime instalado.
- [ ] Sem consentimento, selecionar texto não mostra sugestões nem gera requisições.
- [ ] Configure Inglês → Português, autorize e ative sugestões. Selecione uma frase no Bloco de Notas e em um navegador: em aplicativos compatíveis, um botão aparece sem roubar foco.
- [ ] Clicar na sugestão mostra o resultado, sem tocar no documento ou clipboard, mesmo com substituição marcada.
- [ ] Feche a janela pelo X e repita a seleção: o app continua ativo.
- [ ] Selecione em campo de senha: não deve aparecer sugestão.
- [ ] Desmarque sugestões: elas param imediatamente. Atalho continua funcionando.
- [ ] Pause na bandeja: nem sugestões nem atalho atuam. Retome e teste.
- [ ] Mude a seleção/janela, dispense o botão e espere oito segundos: ele não deve ficar preso na tela nem reaparecer repetidamente para a mesma seleção.
- [ ] Confira layout em 100%, 150% e 200% de escala, inclusive em monitores diferentes.
- [ ] Mantenha o app aberto por alguns minutos e observe CPU/memória; provedores de acessibilidade lentos não devem bloquear a interface.

O script de exportação tem testes automatizados (`python -m unittest discover -s scripts/tests`). O workflow compila e publica o aplicativo, mas não verifica interação de desktop nem disponibilidade/qualidade da API. Não há testes automatizados de interface nesta versão.

## 5. Gere o executável

**Compacto** — requer .NET Desktop Runtime 10 x64 instalado no PC do usuário:

```powershell
dotnet publish src/F8Translate/F8Translate.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o artifacts/compact
```

**Portátil comprimido** — inclui o runtime, não exige instalação separada:

```powershell
dotnet publish src/F8Translate/F8Translate.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o artifacts/portable
```

O arquivo principal é `F8Translate.exe` dentro da pasta escolhida. Distribua a pasta completa, com as instruções correspondentes de `docs/PACOTE-*.txt`. Para ARM64, troque `win-x64` por `win-arm64` e use outra pasta de saída. Os pacotes do Actions são apenas x64.

Não habilite trimming/NativeAOT: Windows Forms/UI Automation dependem de recursos que exigem cuidado e não são um alvo apropriado para uma simples troca de flags. A versão compacta economiza repetição do runtime; a portátil comprimida economiza transferência, mas extrai componentes e pode demorar mais no início. Não é instalador/MSI. Símbolos de depuração não são publicados.

O SmartScreen pode alertar porque o aplicativo é novo e não tem assinatura. Não instrua usuários a desativar o antivírus; antes de distribuir amplamente, planeje assinatura de código, uma release verificável e testes em máquina limpa.

## 6. Downloads temporários no GitHub

O workflow publica compacto, portátil e fontes com validade de **7 dias**. Não commite ZIPs nem executáveis. Para uma entrega só de documentação, que não dispara o build, use **Actions → Build Windows → Run workflow** na branch da sessão se precisar de novos pacotes.

Mudanças de código/empacotamento disparam build. Execuções anteriores da mesma branch são canceladas e o build tem timeout de dez minutos. Depois dele, um job isolado (até dois minutos) tenta apagar pacotes anteriores da mesma branch e workflow, apenas quando os três atuais existem. Outras branches/workflows são preservados. Se a limpeza falhar por permissão, confira o aviso no Actions; a retenção de sete dias dos pacotes novos continua válida. Não há cache nem agendamento. O arquivo já baixado não expira no PC. Essas medidas reduzem uso, mas limites/cobrança da conta precisam ser conferidos nas configurações do GitHub; não há garantia de uso infinito.

## Problemas comuns

- **F8 não funciona:** confira consentimento, clique em Salvar e ativar, verifique se está pausado e teste outro atalho. Alguns teclados exigem Fn+F8.
- **Não copia/cola em determinado programa:** teste no Bloco de Notas. Programas elevados ou que interceptam atalhos podem bloquear a automação.
- **Sugestão não aparece:** confirme consentimento e sugestões ativados e salvos, selecione um trecho curto e aguarde. Não há suporte universal; use F8 como alternativa. A origem é configurada, não detectada.
- **Compacto pede .NET:** instale Desktop Runtime 10 x64 ou escolha o pacote portátil.
- **Clipboard ocupado:** feche ferramentas de clipboard conflitantes e tente novamente.
- **Tradução recusada:** verifique internet, tamanho do trecho e cotas do MyMemory.
- **Quero zerar configurações:** saia pela bandeja e exclua `%LOCALAPPDATA%\F8Translate\settings.json`.
