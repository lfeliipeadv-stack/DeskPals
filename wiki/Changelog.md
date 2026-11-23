# Changelog

[Home](Home.md) · [Instalação](Instalacao.md) · [Primeiros passos](Primeiros-passos.md) · [Painel de pets](Painel-de-pets.md) · [Pergunte ao pet](Pergunte-ao-pet.md) · [Lembretes e Pomodoro](Lembretes-e-pomodoro.md) · [Agenda e e-mails](Agenda-e-e-mails.md) · [Pet virtual e brincadeiras](Pet-virtual-e-brinquedos.md) · [Plugins e integrações](Plugins-e-integracoes.md) · [Crie o seu pet](Crie-o-seu-pet.md) · [Atalhos e comandos](Atalhos-e-comandos.md) · [Para desenvolvedores](Para-desenvolvedores.md) · [Perguntas frequentes](Perguntas-frequentes.md)

Esta página reúne as versões do **Desktop-Pet** em ordem decrescente, com a data de cada uma e o que ela trouxe. As versões publicadas também ficam na página de releases do repositório: <https://github.com/lfeliipeadv-stack/DeskPals/releases>.

## Como as versões são publicadas

- A versão do programa está no arquivo `src/Properties/AssemblyInfoPortable.cs`, nas linhas `AssemblyVersion` e `AssemblyFileVersion` (hoje `1.0.0.0`).
- Cada versão publicada vira uma *release* no GitHub com a tag `vX.Y.Z`. O script `Tools/release.ps1` compila o programa e o Editor de Pets, roda os testes do editor, gera o instalador, empacota os idiomas, escreve o `SHA256SUMS` e publica a tag.
- Toda release traz os mesmos arquivos:

| Arquivo | Para que serve | Tamanho na 1.0.0 |
| --- | --- | --- |
| `DesktopPet.exe` | Só o programa. Precisa do .NET 10 Desktop Runtime instalado. | 3.041.971 bytes (2,9 MB) |
| `DesktopPet-completo.exe` | O mesmo programa com o runtime dentro. Abre em qualquer Windows 10/11. | 52.764.698 bytes (50 MB) |
| `DesktopPet-Instalador.exe` | Instalador em .NET Framework 4.8: confere o runtime, instala o programa e o Editor de Pets e cria os atalhos. | 4.399.104 bytes (4,2 MB) |
| `PetEditor.exe` | O Editor de Pets, para criar e mudar pets. | 955.053 bytes (0,9 MB) |
| `lang.zip` | Os arquivos de idioma do programa (`pt-BR.json`, `es.json`). | 46.311 bytes |
| `SHA256SUMS` | O hash de cada arquivo, conferido antes de o atualizador substituir o executável. | 418 bytes |
| `LICENSE.md` | A licença MIT. | — |

- O atualizador baixa a versão nova em segundo plano e a instala quando o programa fecha. Na primeira vez que a versão nova abre, a janela "Novidades da versão {0}" mostra as notas da release.

## Versão 1.0.0 — 20 de novembro de 2026 (tag `v1.0.0`)

A primeira versão pública do Desktop-Pet, feita a partir do desktopPet de Adriano Petrucci.

- **Painel:** Início com o dia, a semana, a previsão do tempo e o estado do programa; galeria de pets; lembretes e Pomodoro; assistente com IA; plugins; integrações com o Claude Code e outros agentes; configurações e a página Sobre, com créditos e licenças.
- **Galeria:** Rex, Pingo, Boo, Mago, Nuvi e Claude, as ovelhas gSheep, o eSheep Mate, o Pingus e os assistentes do Office (Clippy, Merlin, Rover, Peedy, Genie, Genius, F1, Links, Rocky e Bonzi). Os assistentes não têm gravidade: ficam onde você soltar.
- **Editor de Pets (`PetEditor.exe`):** assistente de pet novo a partir de imagens ou GIF, folha de sprites, animações com prévia ao vivo, entradas na tela, janelas filhas, sons, verificador, mapa das animações e XML. Abre na página Projeto, com os pets recentes em cartões; guarda o desfazer entre sessões; exporta o pet em .zip e o publica na galeria.
- **Idiomas:** português, espanhol e inglês no programa, e mais 20 idiomas para baixar em Configurações → Mais idiomas.
- **Atualização:** procura a versão nova ao abrir, baixa e confere em segundo plano e instala ao fechar.
- **Backup:** pets, lembretes, notas e todas as preferências num arquivo, e uma cópia automática diária numa pasta (OneDrive ou Google Drive).
## Antes do Desktop-Pet: o desktopPet original (0.1 → 2.0.0)

O Desktop-Pet é uma continuação do projeto desktopPet de Adrianotiger, que já tinha 40 versões registradas. O histórico completo está no `Changelog.md` da raiz do repositório; o `Changelog.txt` traz o mesmo histórico no formato antigo, até a 1.0.6. As versões que mais marcaram:

| Versão | Data | O que trouxe |
| --- | --- | --- |
| 2.0.0 | 08 nov 2026 | Suporte a skins de pet, interação entre pets e detecção de colisão com janelas; correções na transição de animações e nos movimentos suaves |
| 1.6.0 | 09 out 2026 | Option Window dentro de uma janela, como o Painel de Controle do Windows 95; a versão da loja do Windows passa a ser igual à portátil, com a inicialização automática no menu |
| 1.5.0 | 01 out 2026 | Movimentos mais suaves, melhorias nos sons e Option Window redesenhada |
| 1.4.0 | 09 set 2026 | Arremesso do pet, com código de ZechsVariety; versão para a Play Store |
| 1.3.2 | 08 ago 2025 | Correção: não era possível carregar um XML local |
| 1.3.1 | 30 ago 2022 | Escala na versão portátil; pets baixáveis filtrados por data; o editor cria o ícone sem conversor |
| 1.2.6 | 05 mai 2021 | Mais estabilidade; o pet não rouba mais o foco da barra de tarefas (mas dá para reativar nas opções); gSheep com mais de 250 animações |
| 1.2.0 | 12 jun 2019 | Versão portátil do app do Windows 10, multimonitor e novo editor offline de XML |
| 1.0.0 | 01 jan 2017 | Detecção de tela cheia e "trazer para o topo" pelo clique no ícone da bandeja |
| 0.5 | 23 dez 2015 | Código reescrito para ser aberto; banheira, sono e queda |

## Onde está o changelog completo

- `Changelog.md` (raiz do repositório) — o changelog completo, do 0.1 (dezembro de 2015) ao 2.0.0 (novembro de 2026), com as 40 versões do projeto original.
- `Changelog.txt` (raiz do repositório) — o mesmo histórico no formato antigo, até a 1.0.6.
- As notas completas de cada versão do Desktop-Pet estão nas *releases* do GitHub: <https://github.com/lfeliipeadv-stack/DeskPals/releases>.

## Ver também

- [Instalação](Instalacao.md) — qual arquivo baixar, o que é preciso ter instalado e como atualizar.
- [Para desenvolvedores](Para-desenvolvedores.md) — como compilar, rodar o autoteste e publicar uma release.
- [Perguntas frequentes](Perguntas-frequentes.md) — dúvidas sobre versões, atualização e compatibilidade.
- [Plugins e integrações](Plugins-e-integracoes.md) — o que cada plugin faz em detalhe.
- [Home](Home.md) — a visão geral do programa.

## Onde fica no código

- `src/Properties/AssemblyInfoPortable.cs` — a versão do programa (`AssemblyVersion` e `AssemblyFileVersion`).
- `Tools/release.ps1` — o script que compila, gera o instalador, empacota o `lang.zip`, escreve o `SHA256SUMS` e publica a release.
- `src/Portable/Updater.cs` — a consulta à API de releases, a conferência do SHA-256 e a troca do executável.
- `src/Portable/Onboarding.cs` — a janela "Novidades da versão {0}" com as notas da release.
- `src/Portable/Updater.cs` — também guarda as notas da versão instalada (`whatsnew.txt`) para a janela de novidades.
- `src/Portable/SelfTest.cs` — o autoteste (`--selftest`), rodado em cada build pelo workflow.
- `src/Portable/BuiltInPets.cs` — a lista de pets que vêm embutidos no executável.
- `Pets/pets.json` — a galeria de pets que o painel lê do repositório.
- `Changelog.md` e `Changelog.txt` — o histórico do projeto original, na raiz do repositório.
