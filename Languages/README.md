# Idiomas para baixar

Idiomas que o programa oferece em **Configurações › Aplicativo › Mais idiomas**. O programa lê
`languages.json` e baixa o `<código>.json` escolhido para `%LocalAppData%\DesktopPet\lang`; a partir daí
ele aparece na lista **Idioma** como os que vêm com o programa (inglês, português e espanhol, em `src/lang`).

Cada arquivo tem os mesmos textos de `src/lang/pt-BR.json` (chave → texto). Um texto que faltar aparece em inglês.

## Adicionar ou corrigir um idioma

1. Gere o modelo em inglês: `DeskPals.exe --export-lang=en.json`
2. Traduza os valores (mantenha as chaves, os `{0}`, as quebras de linha e um único `&` onde o inglês tem).
3. Salve como `Languages/<código>.json` (ex.: `fr.json`, `zh-CN.json`).
4. Atualize a lista: `powershell -ExecutionPolicy Bypass -File Tools\update-languages-json.ps1`
5. O autoteste confere todos: `DeskPals.exe --selftest=resultado.txt --only=languages`

Quem já baixou o idioma vê **Atualizar** quando o arquivo muda (a versão no `languages.json` é um resumo do conteúdo).
