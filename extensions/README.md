# Extensões do DeskPals

Programas à parte que o DeskPals baixa quando a pessoa quer e roda como parte dele. Cada um tem a sua própria licença.

## DeskPals Weather

O programa fica em [`weather/`](../weather) (C#, licença MIT, como o resto do DeskPals). Ele faz a neblina, os raios de sol, a grama e o vento, o som ambiente, as cenas, as cores, o clima da cidade, as pausas e os atalhos. A neve, as folhas, a chuva e as flores vêm do Snowhush, que ele roda ao lado como `DeskPals-Snowhush.exe`.

Na versão publicada ele vai como `DeskPals-Weather.zip`, com quatro arquivos: `DeskPalsWeather.exe`, `DeskPals-Snowhush.exe`, `Snowhush-LICENSE.txt` e `NOTICE.txt`. O `Tools/release.ps1` compila os dois programas, roda os testes da extensão (`DeskPalsWeather.exe --devtests`) e monta o zip. O DeskPals baixa o zip quando a pessoa liga a extensão e, depois, traz as versões novas sozinho.

### Snowhush

`snowhush-src/` é o código do [Snowhush](https://github.com/mbezruchko/Snowhush) v0.13.2, de Mykhailo Bezruchko.

Required Notice: Copyright 2026 Mykhailo Bezruchko (https://github.com/mbezruchko/Snowhush)

Licença: [PolyForm Noncommercial 1.0.0](snowhush-src/LICENSE). Pode ser usado, mudado e distribuído de graça; **não pode ser vendido**, nem nada feito a partir do código dele. O resto do DeskPals continua sob a licença MIT.

O que mudou (tudo marcado com `DeskPals:` no código; o conjunto das mudanças está em [`snowhush-deskpals.patch`](snowhush-deskpals.patch)):

- `windows/src/hosted.rs` (novo): o modo `--hosted [--parent <pid>]` e a cor do que cai.
- `windows/src/main.rs`: lê esses parâmetros; com `--hosted` usa a sua própria trava de instância única.
- `windows/src/prefs.rs`: com `--hosted`, as configurações ficam em `HKCU\Software\DeskPals\Snowhush`; o valor `tint` (cor do que cai, `0x01RRGGBB`, 0 = a cor dele).
- `windows/src/app.rs`: com `--hosted`, sem ícone na bandeja, sem janela de boas-vindas e sem procura de atualizações; as mensagens `DeskPalsSnowhushReload` (reler as configurações), `DeskPalsSnowhushClear` (limpar a neve) e `DeskPalsSnowhushPause` (pausar e voltar); fecha junto com o DeskPals Weather.
- `windows/src/render.rs`: o tamanho dos flocos (valor `flakeScale`, em centésimos: 100, 160 ou 230).
- `windows/src/screen.rs`: a luz sobre o que cai é multiplicada pela cor escolhida (neve azul, folhas vermelhas…).
- `windows/build.rs`: com o compilador GNU, o ícone e o manifesto passam pelo `windres` do MinGW (com o MSVC continua como no original). Sem o manifesto o Windows recusa a janela que deixa a neve e a chuva atrás das janelas, e o Snowhush volta a desenhar na frente delas. O manifesto padrão que o GCC põe em todo programa (`default-manifest.o`) é trocado por um vazio, para que o do Snowhush seja o único. O `Tools/release.ps1` confere o manifesto e não publica um Snowhush sem ele.

A física, a neve, as folhas, a chuva e as flores são as do Snowhush, sem outra mudança.

### Versões novas do Snowhush

```powershell
.\Tools\update-snowhush.ps1 -Check   # diz se o autor publicou uma versão mais nova
.\Tools\update-snowhush.ps1          # baixa a mais nova, põe as mudanças do DeskPals de volta, compila e só então troca a pasta
.\Tools\update-snowhush.ps1 -MakePatch   # escreve o .patch de novo depois de mudar o Snowhush à mão
```

Se uma mudança não couber mais (o autor mexeu nas mesmas linhas), nada aqui é alterado: a cópia nova fica numa pasta temporária, com as partes que não couberam em arquivos `.rej`, para terminar à mão. Depois de atualizar: rodar os testes da extensão, ver a neve na tela e fazer o commit.

### Compilar (Windows)

Precisa do Rust (rustup, conjunto `x86_64-pc-windows-gnu`) e de um MinGW-w64 completo, numa pasta **sem espaços no caminho** (por exemplo `C:\Users\Public\mingw64`):

```sh
export PATH="/c/Users/Public/mingw64/bin:$HOME/.cargo/bin:$PATH"
cd extensions/snowhush-src/windows && cargo build --release --bin Snowhush
```

O arquivo sai em `windows/target/release/Snowhush.exe`. Na versão publicada ele se chama `DeskPals-Snowhush.exe` e vai dentro do `DeskPals-Weather.zip`.
