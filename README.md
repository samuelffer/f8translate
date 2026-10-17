<p align="center"><img src="assets/logo_1.png" alt="F8 Translate Logo" width="180"></p>

# F8 Translate

Aplicativo **C# / .NET 10 / Windows Forms** para traduzir texto selecionado por um atalho global (F8 por padrão).

Esta é uma nova implementação da proposta original em Python. O código Python e o `iniciar.bat` citados na documentação antiga não estavam neste checkout. As dependências Python não são mais necessárias.

## Recursos

- Atalho configurável de F6 a F12, com detecção de conflito.
- Idiomas de origem/destino: português (`pt`), inglês (`en`), espanhol, francês, alemão, italiano e japonês.
- Modo padrão: exibir a tradução sem substituir o texto original.
- Modo opcional de substituição via Ctrl+V, desativado a cada abertura.
- Bandeja do Windows: abrir, pausar e sair. Fechar a janela não encerra o aplicativo.
- Tentativa de preservar o clipboard, proteção contra traduções simultâneas e timeout de rede.
- Configurações em `%LOCALAPPDATA%\F8Translate\settings.json`, sem histórico de textos.

## Preparar o PC, executar e compilar

Veja **[o guia passo a passo para Windows](docs/DESENVOLVIMENTO.md)**.

Com o **SDK .NET 10** instalado, na raiz do repositório:

```powershell
dotnet run --project src/F8Translate/F8Translate.csproj
```

Gerar executável Windows x64 com o runtime incluído:

```powershell
dotnet publish src/F8Translate/F8Translate.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/win-x64
```

O arquivo principal será `artifacts/win-x64/F8Translate.exe`. O usuário final não precisa instalar Python nem .NET. Isso é uma distribuição portátil, não um instalador, e ainda não é assinada digitalmente.

## Como usar

1. Abra o aplicativo, escolha os idiomas, leia o aviso de privacidade e autorize o serviço externo.
2. Clique em **Salvar e ativar**.
3. Em outro aplicativo, selecione texto e pressione o atalho. Não altere a seleção durante o processamento.
4. Por padrão, a janela mostra o resultado. Clique em **Copiar tradução** para copiá-lo.
5. Se preferir, ative a substituição automática antes de acionar o atalho. O resultado será enviado por Ctrl+V ao aplicativo original.

## Privacidade e provedor

Esta versão usa a API pública do **MyMemory**, sem chave. O texto selecionado é enviado pela internet, como parâmetro de uma requisição HTTPS. Não envie informações pessoais, confidenciais ou senhas. O aplicativo não grava os textos em logs, mas não controla retenção ou uso pelo provedor.

- [Documentação e cotas do MyMemory](https://mymemory.translated.net/doc/spec.php)
- [Política de privacidade do provedor](https://mymemory.translated.net/doc/en/privacy.php)

Há limite de **500 bytes UTF-8 por trecho** (não necessariamente 500 caracteres), além das cotas do serviço, que podem mudar. Não há detecção automática do idioma: escolha a origem. Qualidade, disponibilidade e continuidade do serviço não são garantidas. `TranslationService` isola a integração para permitir outro provedor no futuro.

## Limitações e segurança

- Windows desktop; não funciona no Linux/macOS. Não há OCR nem tradução de texto não selecionável.
- Depende de Ctrl+C / Ctrl+V. Aplicativos elevados, jogos, terminais e campos protegidos podem não funcionar. Evite executar o tradutor como administrador.
- O app verifica a janela ativa e o clipboard antes de colar, mas **não consegue garantir que a seleção/cursor dentro da mesma janela continue igual**. Não edite enquanto aguarda; prefira o modo de pré-visualização.
- O clipboard é restaurado se não houver mudanças posteriores. Formatos especiais ou dados fornecidos sob demanda por outro aplicativo podem não ser preservados perfeitamente.
- Colagem é assíncrona e varia por aplicativo. O envio de Ctrl+V não confirma que o destino aceitou o texto. Ctrl+Z depende do aplicativo de destino; não existe restauração garantida do documento.
- Não há instalador, atualização automática, assinatura digital nem início automático com o Windows nesta versão.

## Validação

O workflow `Build Windows` compila e publica o aplicativo em um runner Windows. Compilação não substitui testes reais de foco, clipboard e atalhos; use o roteiro do guia. Esta implementação ainda precisa de validação interativa em Windows antes de uma release pública.

## Licença

MIT. Consulte [LICENSE](LICENSE).

## Pacotes de fontes para download

Cada entrega com alterações inclui um ZIP novo em `zips/` na branch da sessão Arena. Ele contém os fontes e documentação, **não um executável compilado**, e mantém os arquivos do projeto diretamente na raiz. Extraia em uma pasta limpa e siga o guia de desenvolvimento. O usuário transfere manualmente os arquivos desejados para a `main`; os ZIPs de teste ficam apenas na branch da sessão.

Para gerar uma exportação como mantenedor: `python3 scripts/build_zip.py` (ou `py scripts/build_zip.py` no Windows). Python é necessário apenas para esse script de exportação, não para executar ou compilar o aplicativo. Consulte [AGENTS.md](AGENTS.md) para o fluxo completo.
