# AGENTS.md — como trabalhar neste repositório

Leia este arquivo antes de alterar o projeto. Estas regras preservam o fluxo de entrega combinado com o usuário entre sessões.

## Projeto

**F8 Translate** é um aplicativo Windows em C# / .NET 10 / Windows Forms. Traduz texto selecionado por atalho global, com bandeja do sistema e integração com o clipboard. Comunicação e documentação em português (Brasil).

- `src/F8Translate/`: aplicativo e projeto .NET.
- `docs/DESENVOLVIMENTO.md`: preparar Windows, executar, testar e publicar executável.
- `scripts/build_zip.py`: exportação dos fontes, com biblioteca padrão do Python.
- `.github/workflows/build.yml`: compilação/publicação em Windows.

## Fluxo obrigatório para cada entrega com alterações

1. Faça e verifique as alterações na **branch da sessão Arena**. Nesta sessão: `arena/91c71699-f8translate`. Nunca troque de branch para entregar arquivos.
2. Atualize a documentação pertinente e explique o que foi realmente testado. Não afirme ter compilado ou testado no Windows sem fazê-lo.
3. Gere **um ZIP novo ao concluir cada pedido com modificações**, depois de todas as edições:
   ```bash
   python3 scripts/build_zip.py
   ```
   No Windows, o mantenedor pode usar `py scripts/build_zip.py`. Python é ferramenta de exportação, não dependência do aplicativo C#.
4. O ZIP fica em `zips/f8translate-source-AAAAMMDD-rN.zip`. Nunca sobrescreva nem apague pacotes anteriores. A revisão é automática e não altera a versão do aplicativo. Só altere a versão quando o usuário pedir explicitamente.
5. Confira a integridade e o conteúdo. O pacote deve conter **todos os arquivos de projeto necessários**, incluindo fontes, documentação, assets, scripts, arquivos ocultos de configuração e este `AGENTS.md`, diretamente na raiz, sem pasta intermediária. Não inclua `.git`, ZIPs anteriores, builds, caches, dependências locais, credenciais ou arquivos pessoais. O script usa arquivos rastreados e novos não ignorados pelo Git; revise a lista antes de publicar.
6. Commite e envie **código e ZIP juntos**, exclusivamente para a branch da sessão:
   ```bash
   git add -A
   git add -f zips/<novo-pacote>.zip
   git commit -m "Descrição da entrega"
   git push origin arena/91c71699-f8translate
   ```
   O nome acima corresponde a esta sessão; em outra sessão use apenas a branch Arena autorizada nela. `zips/` fica ignorado; force a adição somente do pacote novo.
7. Entregue o link de download do ZIP no GitHub e um resumo curto. Se o push falhar, informe e disponibilize o arquivo local; não diga que publicou.

## Papel do GitHub e do ZIP

A branch da sessão é a ponte de download. O usuário baixa o pacote e copia os arquivos manualmente para a `main`. **Não faça push na `main`, não abra PR, não faça merge e não crie releases sem pedido explícito.** Os ZIPs de teste não devem ser copiados para a `main`.

O pacote contém **fontes**, não um executável compilado. O usuário compila seguindo o guia. Não chame o ZIP de instalador nem prometa que ele funciona sem .NET SDK para compilação.

Ao atualizar uma cópia existente, verifique também arquivos removidos: extrair um ZIP por cima não apaga arquivos obsoletos. Prefira uma pasta limpa. Nesta migração, `requirements.txt` foi removido; o antigo `iniciar.bat`, se existir na máquina do usuário, também não é mais usado.

## Cuidados técnicos

- Não registre nem versione textos traduzidos, clipboard ou segredos.
- Preserve o consentimento para envio de texto ao provedor externo.
- Modo de substituição deve permanecer opcional; cuidado com foco, clipboard e operações simultâneas.
- Não prometa compatibilidade universal com aplicativos. Teste com texto descartável.
- Não edite o mesmo arquivo em chamadas paralelas.
