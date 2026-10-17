# AGENTS.md — como trabalhar neste repositório

Leia antes de qualquer alteração. Comunicação e documentação em português (Brasil).

## Projeto

F8 Translate: C# / .NET 10 / Windows Forms, exclusivo para Windows. UI Automation é usada apenas para observar seleções localmente em aplicativos compatíveis; não há OCR nem reconhecimento automático de idioma.

- `src/F8Translate/`: aplicativo. `MainForm.cs` gerencia bandeja/atalhos; `MainView.xaml` e `.xaml.cs` desenham a interface WPF; `InterfacePreview.cs` gera prévias sem dados reais.
- `docs/DESENVOLVIMENTO.md`: preparar Windows, executar, testar e publicar.
- `scripts/build_zip.py`: exportar fontes completos (Python padrão, apenas para o mantenedor/CI).
- `.github/workflows/build.yml`: publicar apenas aplicativo compacto e fontes, com retenção de 7 dias e limpeza de pacotes anteriores da mesma branch/workflow.

## Fluxo obrigatório de entrega

1. Trabalhe somente na branch da sessão Arena (`arena/91c71699-f8translate` nesta sessão). Nunca troque de branch.
2. Atualize documentação e verifique as alterações. Diferencie compilação, revisão e teste real no Windows.
3. Gere um ZIP local novo após concluir todas as modificações:
   ```bash
   python3 scripts/build_zip.py
   ```
   No Windows: `py scripts/build_zip.py`. O arquivo `zips/f8translate-source-AAAAMMDD-rN.zip` não sobrescreve anteriores. Versão do app só muda por pedido explícito.
4. Verifique integridade e conteúdo. Fontes, documentação, assets, scripts e configurações (incluindo arquivos ocultos) devem estar na raiz. Não exporte credenciais, builds, caches, `.git` ou ZIPs antigos.
5. Commite o código, **não os ZIPs nem executáveis**, e envie apenas para a branch da sessão:
   ```bash
   git add -A
   git commit -m "Descrição da entrega"
   git push origin arena/91c71699-f8translate
   ```
6. Confira o workflow e entregue os links dos artefatos. O Actions gera dois pacotes principais e prévias visuais com validade de **7 dias**: fontes completos e aplicativo compacto. Não faça promessas de download antes da conclusão bem-sucedida.
7. Mudanças só em documentação não disparam compilação: se precisar de um novo pacote para uma entrega, execute manualmente `gh workflow run build.yml --ref arena/91c71699-f8translate` e acompanhe o resultado. Não dispare builds duplicados.
8. Se o GitHub falhar, informe e ofereça o ZIP local. Nunca diga que publicou sem confirmar.

## Armazenamento e limites

Este fluxo substitui o combinado antigo de commitar ZIPs: agora todos os pacotes são artefatos temporários. Nunca adicione novos ZIPs ao Git com `-f`. O pacote antigo ainda existe no histórico; não reescreva esse histórico sem autorização explícita.

Use `retention-days: 7`, limite de tempo, cancelamento de execuções antigas e apenas runners hospedados padrão. Não adicione caches grandes, tarefas agendadas ou pipelines desnecessários. Não abra PR, faça merge, crie release ou envie para `main` sem pedido explícito. O usuário copia manualmente os fontes para a main, sem a pasta `zips/`.

Retenção reduz acúmulo, mas não garante uso ilimitado nem controla a cobrança da conta. Não altere permissões ou orçamento da conta sem autorização. O job isolado de limpeza tem apenas contents:read e actions:write, valida os dois pacotes atuais antes de apagar os antigos e não pode apagar outros workflows/branches. Falhas de permissão devem ser informadas; nunca apague releases ou arquivos do usuário por engano.

## Distribuição

- Compacto: framework-dependent; requer .NET Desktop Runtime 10 x64 instalado uma vez.
- Não gerar variante portátil/self-contained nem incluir runtime ou seu instalador no pacote. Manter o requisito .NET Desktop Runtime 10 x64 explícito no README e no LEIA-ME. A limpeza pode remover artefatos portáteis legados.
- Não use trimming/NativeAOT com Windows Forms/UI Automation sem suporte e validação apropriados.
- Fontes: não são instalador. Extrair em pasta limpa; ZIP não apaga arquivos obsoletos.

## Cuidados técnicos

- Não registre textos, clipboard ou segredos. Seleções só vão para a rede após clique ou atalho e consentimento.
- Sugestões devem ser opcionais, não roubar foco e não simular Ctrl+C durante a observação.
- Campos de senha não devem gerar sugestões. Não prometa suporte universal; F8 é o fallback.
- Substituição é opcional e desativada ao abrir. Sugestões nunca substituem documentos.
- Fechar no X mantém o app na bandeja; Sair encerra explicitamente. Não impedir encerramento do Windows.
- Não edite o mesmo arquivo em chamadas paralelas.

## Interface e recursos visuais

- Use a referência minimalista: barra lateral, fundo claro, cantos arredondados, azul nos destaques. Preserve navegação de teclado, foco visível, redimensionamento e fechamento para a bandeja.
- Ícones Lucide e bandeiras Circle Flags são imagens locais compiladas. Preserve licenças em `assets/vendor/` e os avisos no pacote. Não acrescente downloads de imagem em tempo de execução.
- Histórico é somente em memória, no máximo 20 entradas, com ação de limpar; nunca persista textos. Tradução manual é sempre uma ação explícita, jamais por digitação.
- `F8Translate-interface` é um artefato auxiliar de prévias PNG com retenção de sete dias; a limpeza também o remove. Não commite prévias geradas.
