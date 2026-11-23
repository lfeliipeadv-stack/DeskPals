# Como criar um plugin do DeskPals

Um plugin do DeskPals é **um arquivo JSON de regras**: quando algo acontece, o pet faz alguma coisa. Ele não roda código, não abre programas e não lê seus arquivos. Por isso qualquer pessoa pode instalar um plugin da comunidade sem medo.

Para usar: **Configurações → Plugins → Plugins da comunidade**. Lá você instala os da galeria com um clique, ou clica em **Abrir a pasta** e coloca o seu arquivo `.json` lá dentro.

## Exemplo

```json
{
  "id": "meu-plugin",
  "name": { "pt": "Meu plugin", "en": "My plugin" },
  "description": { "pt": "O pet dá bom dia.", "en": "The pet says good morning." },
  "author": "Seu nome",
  "version": "1.0",
  "emoji": "☀️",
  "rules": [
    {
      "when": { "at": "09:00", "days": "weekdays" },
      "play": ["wave", "happy"],
      "say": { "pt": ["Bom dia!", "Bom dia! Café?"], "en": ["Good morning!", "Good morning! Coffee?"] }
    }
  ]
}
```

## Campos do plugin

| Campo | O que é |
|---|---|
| `id` | Nome único, só letras minúsculas, números e `-` (2 a 40 caracteres). |
| `name`, `description`, `author`, `version` | O que aparece na lista. |
| `emoji` | O ícone do plugin (também aparece nos bilhetes). |
| `rules` | De 1 a 50 regras. |

Qualquer texto pode ser uma frase só, uma lista (o pet escolhe uma por vez) ou um objeto por idioma: `{ "pt": …, "en": …, "es": … }`. Se o idioma da pessoa não estiver lá, vale o inglês e, se não houver, o primeiro da lista.

## Quando (`when`)

| Regra | Exemplo | Quando acontece |
|---|---|---|
| `"start"` | `"when": "start"` | Logo depois que o programa abre. |
| `at` | `{ "at": "09:00", "days": "weekdays" }` | Uma vez por dia, nesse horário. `days` pode ser `all`, `weekdays`, `weekend` ou dias como `"mon,fri"`. |
| `every` | `{ "every": 60, "between": "09:00-18:00" }` | A cada N minutos (de 15 a 1440), só nesse intervalo de horas (opcional). |
| `date` | `{ "date": "12-25" }` | Uma vez nesse dia do ano (mês-dia). |
| `app` | `{ "app": "spotify" }` | Quando esse programa vem para a frente (o nome do processo, sem `.exe`). No máximo a cada 30 minutos. |
| `event` | `{ "event": "focusDone" }` | Quando algo acontece no DeskPals: `focusDone` (um Pomodoro terminou), `levelUp` (o pet subiu de nível), `snow` ou `rain` (começou a nevar ou chover na tela). |

## O que o pet faz

Uma regra precisa de pelo menos uma destas coisas:

| Campo | O que faz |
|---|---|
| `say` | O pet fala no balão (até 160 caracteres; links e caminhos de arquivo não são mostrados). |
| `play` | Animações, na ordem de preferência: o pet faz a primeira que tiver (até 10 nomes). Exemplos: `wave`, `happy`, `dance`, `jump`, `think`, `read`, `eat`, `coffee`, `sleep`, `party`, `cheer`. |
| `note` | O pet traz um bilhete com esse texto (até 300 caracteres). |
| `effect` | Um efeito na tela por 20 segundos: `snow`, `rain`, `confetti` ou `leaves`. |
| `chance` | A chance (1 a 100) de a regra acontecer quando chega a hora. O padrão é 100. |

O DeskPals nunca faz mais de um plugin a cada 20 segundos, e fica quieto durante o foco e o "não perturbe" (os eventos ainda valem).

## Mandar o seu plugin para a galeria

1. Coloque o arquivo em `PluginGallery/` neste repositório.
2. Acrescente uma linha em `PluginGallery/index.json`, com `id`, `file`, `emoji`, `author`, `version`, `name` e `description`.
3. Abra um pull request.

## Plugins feitos como programa (API local)

Para quem programa: com **Integrações → API local** ligada, qualquer programa no mesmo computador pode mandar o pet agir. O endereço e a chave ficam em `%LOCALAPPDATA%\DesktopPet\agent.json`. Todos os pedidos são `POST`, com o cabeçalho `X-DesktopPet: <token>`:

| Endereço | Corpo | O que faz |
|---|---|---|
| `/say` | um texto | O pet fala (uma linha, até 140 caracteres). |
| `/play` | nomes de animações separados por vírgula | O pet faz a primeira que tiver. |
| `/effect` | `snow`, `rain`, `confetti` ou `leaves` | O efeito na tela por 20 segundos. |
| `/note` | um texto | O pet traz um bilhete. |
| `/react` | `thinking`, `working`, `success`, `error`… | A reação de quando um agente de IA trabalha. |

Exemplo no PowerShell:

```powershell
$c = Get-Content "$env:LOCALAPPDATA\DesktopPet\agent.json" | ConvertFrom-Json
Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:$($c.port)/play" -Headers @{ 'X-DesktopPet' = $c.token } -Body 'dance,happy'
```
