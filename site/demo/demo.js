/* ---------------------------------------------------------------
   demo.js — a página de demonstração.

   Aqui ficam as três coisas que o motor não faz sozinho: o mundo
   (a área de trabalho da página), o desenho no canvas e as conversas
   com o pet.

   O mundo é uma mini área de trabalho: uma janela de cada vez pode
   ser arrastada, maximizada, minimizada ou fechada, e o pet anda
   em cima delas, perde o chão quando elas somem e acompanha a
   janela que se move — como no Windows.
   --------------------------------------------------------------- */

import { Pet, World, holdPets } from "./pet-engine.js";
import { VirtualPet, actionNames, RECLAMACOES, RESPOSTAS } from "./virtual-pet.js";
import { iconeApp, glifo } from "./icons.js";

/* Os pets que têm as animações publicadas na página. */
const PETS = [
  { folder: "claude", name: "Claude", note: "o mascote" },
  { folder: "esheep64", name: "eSheep 64", note: "a ovelha clássica" },
  { folder: "rex", name: "Rex", note: "o dinossauro" },
  { folder: "boo", name: "Boo", note: "" },
  { folder: "pingo", name: "Pingo", note: "" },
  { folder: "mago", name: "Mago", note: "" },
  { folder: "nuvi", name: "Nuvi", note: "" },
  { folder: "clippy", name: "Clippy", note: "flutua: fica onde você soltar" }
];

/* As falas do pet: as mesmas do programa (src/lang/pt-BR.json). */
const FORTUNES = [
  "Um passo pequeno hoje é um passo grande amanhã.",
  "Algo bom está para acontecer.",
  "Faça uma pausa: sua melhor ideia pode vir nela.",
  "Sua gentileza vai voltar para você.",
  "Hoje é um bom dia para aprender algo novo.",
  "Beba água. Sério.",
  "Uma tarefa terminada vale mais que um plano perfeito.",
  "Alguém está feliz por ter você por perto.",
  "Paciência também é progresso.",
  "O erro está na linha que você tem certeza que está certa."
];

const JOKES = [
  "Por que o computador foi ao médico? Porque estava com vírus!",
  "O que o zero disse para o oito? Que cinto bonito!",
  "Qual é o café mais perigoso do mundo? O ex-presso.",
  "Por que o livro de matemática estava triste? Tinha muitos problemas.",
  "O que o pato disse para a pata? Vem quá!",
  "Qual é o cúmulo da rapidez? Fechar a gaveta e trancar a mão lá dentro… antes de abrir."
];

const BALL_ASK = "Pense numa pergunta de sim ou não… e chacoalhe a bola!";
const BALL_SHAKE = "Chacoalhar";
const BALL_ANSWERS = [
  "Sim!", "Não.", "Com certeza.", "Melhor não.", "Pergunte de novo mais tarde.",
  "Sem dúvida.", "Hmm… pouco provável.", "Os sinais dizem que sim.",
  "Não dá para saber agora.", "Pode contar com isso.", "Minha resposta é não.", "Muito provável."
];

const MOOD_ASK = "Como você está hoje?";
const MOOD_FACES = ["😄", "🙂", "😐", "😕", "😢"];
const MOOD_REPLIES = [
  "Que maravilha! Vamos manter essa energia! ✨",
  "Que bom! Tenha um ótimo dia.",
  "Ok. Que tal uma voltinha ou um copo d'água?",
  "Sinto muito. Vai com calma, uma coisa de cada vez. Estou aqui.",
  "Sinto muito que você esteja mal. Converse com alguém de confiança e seja gentil com você. ❤️"
];

/* O que o pet responde quando não tem IA por perto. */
const ANSWERS = [
  "Estou andando pela janela, como sempre.",
  "Meu dia está bom! E o seu?",
  "Já pensou em dar uma pausa para beber água?",
  "Me arraste e me solte: eu sei quicar.",
  "Para me usar de verdade, baixe o programa para o Windows.",
  "Meu nome é {0} e eu moro nesta página.",
  "Shhh… deixa eu escalar mais um pouquinho.",
  "Aqui no navegador eu não leio e-mail, mas na versão para Windows eu leio.",
  "Clique com o botão direito em mim para ver o que eu sei fazer."
];

/* A primeira frase de cada visitante: quem chegou na página quer saber
   o que é isto e onde se baixa. Os dois botões do balão respondem a isso
   sem tirar o pet da tela. */
const PITCH = "Eu sou o pet do DeskPals: um bichinho que mora na área de trabalho do Windows. Quer me levar com você?";

/* O índice da fala que vem a seguir: as falas do pet vão passando uma a
   uma, sem repetir. */
let respostaIndice = -1;

/* ------------------------------------------------- as reações do pet
   Cada bichinho tem as suas animações, com os nomes dele: a ovelha não
   tem "wave" e o Nuvi não tem "eat". O motor já procura os nomes na
   ordem, então cada lista é uma cadeia: o primeiro nome que o pet
   conhece é o que ele faz. O fim da cadeia tem nomes que todos os
   pets da demo conhecem, para a reação nunca ficar muda.            */
const REACTIONS = {
  ola:     ["wave", "hello", "hi", "dance", "jump", "look around", "greeting", "get attention"],
  alegria: ["dance", "cheer", "happy", "confetti", "party", "jump", "flower", "happy 2"],
  pensei:  ["think", "idea", "detective", "look", "look around", "look_down", "sleep", "processing", "searching"],
  comer:   ["eat", "chef", "pizza", "meat", "coffee", "drink", "jump", "dance", "get techy", "happy"],
  tonto:   ["dizzy", "spin", "rotate", "turn", "boing", "jump", "dance", "confused", "surprised"],
  carinho: ["love", "hearts", "heart", "kiss", "hug", "flower", "sleep", "look_down", "happy", "acknowledge"],
  cansado: ["sleep", "sleep1a", "sit", "sleep2a", "look_down", "jump", "idle snooze", "rest pose"]
};

/* As dicas do primeiro contato: uma de cada vez, e somem sozinhas. */
const TIPS = [
  "Clique no pet para conversar. Ele responde na hora.",
  "Arraste o pet e solte com força: ele voa, bate na borda e quica.",
  "Clique com o botão direito no pet (ou na <b>pata</b> da barra de tarefas) para ver tudo o que ele sabe fazer.",
  "O <b>Iniciar</b>, a pesquisa, o som e o relógio da barra de tarefas funcionam, como no Windows 11.",
  "No programa para Windows ele também cuida da agenda, dos lembretes e do Pomodoro."
];

/* O Pomodoro: o mesmo do programa (25 minutos de foco, 5 de pausa). */
const POMODORO_FOCO = 25 * 60;
const POMODORO_PAUSA = 5 * 60;

/* --------------------------------------------------------------- o mundo */
// Modo foto: usado para tirar a imagem da documentação. A simulação roda
// adiantada e o quadro é desenhado uma vez só, com o pet já em cima da janela.
const SHOT = new URLSearchParams(window.location.search).has("shot");

const world = new World();
const stage = document.getElementById("stage");
const canvas = document.getElementById("canvas");
const context = canvas.getContext("2d");
const bubble = document.getElementById("bubble");
const bubbleText = document.getElementById("bubble-text");
const bubbleChoices = document.getElementById("bubble-choices");
const menu = document.getElementById("menu");
const loading = document.getElementById("loading");
const loadingText = document.getElementById("loading-text");
const loadingRetry = document.getElementById("loading-retry");
const clock = document.getElementById("clock");
const petList = document.getElementById("pet-list");
const askButton = document.getElementById("ask");
const soundButton = document.getElementById("sound");
const traySound = document.getElementById("tray-sound");
const iconList = document.getElementById("icons");
const taskbarApps = document.getElementById("taskbar-apps");
const tipsBox = document.getElementById("tips");
const tipsText = document.getElementById("tips-text");
const tipsNumber = document.getElementById("tips-n");
const tipsClose = document.getElementById("tips-close");
const tipsBtn = document.getElementById("tips-btn");
const menuBtn = document.getElementById("menu-btn");
const petTalk = document.getElementById("pet-fala");
const taskbarEl = document.querySelector(".demo-taskbar");

let pet = null;
let currentFolder = null;
let vpet = null;            // as necessidades do pet (VirtualPet)
let soundOn = true;
let drag = null;           // o que está sendo arrastado: o pet ou uma janela
let longPressTimer = 0;    // o toque comprido no pet (no celular)
let longPressOpened = false;
let topZ = 0;              // a ordem das janelas (maior = na frente)
let bubbleSize = { width: 0, height: 0 };
let bubbleTimer = 0;
let bubbleHeld = false;    // o balão segura o pet parado
let menuHeld = false;      // o menu segura o pet parado
let narrow = false;        // a tela é pequena: uma janela de cada vez
let tipIndex = 0;
let tipTimer = 0;
let greeted = false;      // a primeira fala já aconteceu

/* ----------------------------------------------------------- as janelas
   Cada janela é um retângulo do mundo, como as janelas de verdade do
   Windows: o pet anda no título, cai quando elas somem e acompanha
   a que se move.                                                    */

const WINDOW_DEFS = [
  // A largura de cada janela é uma fração da tela, com limites: assim o
  // arranjo não se desfaz num monitor grande nem fica minúsculo no
  // celular.
  { id: "painel", title: "DeskPals — Painel", kind: "panel", icon: "painel",
    x: 0.11, y: 0.04, wFrac: 0.30, wMin: 300, wMax: 500, h: 262, z: 2 },
  { id: "pomodoro", title: "Foco e Anotações", kind: "pomodoro", icon: "pomodoro",
    x: 0.42, y: 0.30, wFrac: 0.23, wMin: 260, wMax: 400, h: 300, z: 3 },
  { id: "notas", title: "Bloco de Notas", kind: "notes", icon: "notas",
    x: 0.03, y: 0.63, wFrac: 0.21, wMin: 240, wMax: 380, h: 210, z: 1 },
  { id: "animacoes", title: "Animações do pet", kind: "animacoes", icon: "animacoes",
    x: 0.40, y: 0.42, wFrac: 0.30, wMin: 300, wMax: 500, h: 380, z: 1, closed: true },
  { id: "sobre", title: "Sobre a demo", kind: "sobre", icon: "sobre",
    x: 0.70, y: 0.09, wFrac: 0.30, wMin: 320, wMax: 500, h: 470, z: 4 }
];

const windows = WINDOW_DEFS.map((def) => {
  const win = {
    id: def.id,
    title: def.title,
    kind: def.kind,
    icon: def.icon,
    def,
    x: 0, y: 0,              // posição normal, em pixels
    w: def.wMax || 320,      // tamanho normal, em pixels (a largura final
    h: def.h,                // sai do arranjo, conforme a tela)
    left: 0, top: 0, right: 0, bottom: 0,
    z: def.z,
    maximized: false,
    minimized: false,
    closed: !!def.closed,
    gone: !!def.closed,      // para o motor: janela que não existe mais
    el: null
  };
  world.windows.push(win);
  return win;
});

// A janela da frente começa na mais alta da lista: assim qualquer janela
// pode ser trazida para a frente, mesmo a que começou no meio.
topZ = Math.max(...windows.map((win) => win.z));

function windowById(id) {
  return windows.find((win) => win.id === id) || null;
}

function windowOf(element) {
  const holder = element && element.closest ? element.closest(".demo-window") : null;
  return holder ? windowById(holder.dataset.window) : null;
}

/** O corpo de cada janela: o painel do programa, o Pomodoro, o bloco de
    notas e o texto que explica a demo. O painel, o Pomodoro e o texto vêm
    do HTML (demo.html) e são movidos para dentro da janela aqui. */
function windowBody(win) {
  if (win.kind === "notes") {
    return `<div class="demo-note">
      <label class="demo-note-label" for="notes-text">Anotações (ficam neste navegador)</label>
      <textarea class="demo-notes" id="notes-text" rows="5" spellcheck="false">Lista de compras:
· pão de mel
· café
· mais uma ovelha
· Windows 11</textarea>
    </div>`;
  }
  if (win.kind === "pomodoro") {
    return `<div class="demo-slot" data-slot="demo-pomodoro"></div>`;
  }
  if (win.kind === "animacoes") {
    return `<div class="demo-slot" data-slot="demo-animacoes"></div>`;
  }
  if (win.kind === "sobre") {
    return `<div class="demo-slot" data-slot="demo-sobre"></div>`;
  }
  return `<div class="demo-slot" data-slot="demo-painel"></div>`;
}

/** Os três botões da janela, no desenho do Windows 11: glifos finos,
    sem moldura; o maximizar vira "restaurar" com a janela maximizada. */
const WINDOW_GLYPHS = {
  min: glifo("minimize"),
  max: glifo("maximize"),
  restore: glifo("restore"),
  close: glifo("close")
};

/** Cria o elemento de cada janela na tela. */
function buildWindows() {
  for (const win of windows) {
    const el = document.createElement("div");
    el.className = "demo-window";
    el.dataset.window = win.id;
    el.innerHTML = `
      <div class="demo-window-bar">
        <span class="demo-window-icon" aria-hidden="true">${iconeApp(win.icon)}</span>
        <span class="demo-window-title"></span>
        <span class="demo-window-buttons">
          <button type="button" class="demo-window-dot min" data-action="min"></button>
          <button type="button" class="demo-window-dot max" data-action="max"></button>
          <button type="button" class="demo-window-dot close" data-action="close"></button>
        </span>
      </div>
      <div class="demo-window-body">${windowBody(win)}</div>`;
    el.querySelector(".demo-window-title").textContent = win.title;

    // Os botões são botões de verdade: com nome para o leitor de tela
    // e um alvo de toque que não é um pixel.
    for (const button of el.querySelectorAll(".demo-window-dot")) {
      const action = button.dataset.action;
      const name = action === "min" ? "Minimizar" : action === "max" ? "Maximizar" : "Fechar";
      button.innerHTML = WINDOW_GLYPHS[action] || "";
      button.title = name + " " + win.title;
      button.setAttribute("aria-label", name + " " + win.title);
    }

    stage.appendChild(el);
    win.el = el;
  }

  // O painel, o Pomodoro e o texto da demo vêm do HTML: cada um entra
  // na janela que lhe pertence.
  for (const win of windows) {
    const slot = win.el.querySelector(".demo-slot");
    if (!slot) continue;
    const source = document.getElementById(slot.dataset.slot);
    if (!source) continue;
    source.hidden = false;
    slot.appendChild(source);
  }
}

/** Os ícones da área de trabalho: abrem as janelas de novo. */
function buildIcons() {
  iconList.innerHTML = "";
  for (const win of windows) {
    const icon = document.createElement("button");
    icon.type = "button";
    icon.className = "demo-icon";
    icon.dataset.window = win.id;
    icon.innerHTML = `<b>${iconeApp(win.icon)}</b><span></span>`;
    icon.querySelector("span").textContent = win.title;
    icon.title = "Abrir " + win.title;
    // Como no Windows: o mouse abre com dois cliques (um clique só
    // seleciona); o teclado (Enter) e o toque abrem na hora.
    icon.addEventListener("click", (event) => {
      if (event.detail === 0 || lastPointerType === "touch" || lastPointerType === "pen") openWindow(win);
      else icon.focus();
    });
    icon.addEventListener("dblclick", () => openWindow(win));
    iconList.appendChild(icon);
  }

  // O atalho para o programa de verdade: é o caminho mais curto da demo
  // para o download.
  const link = document.createElement("a");
  link.className = "demo-icon";
  link.href = DOWNLOAD;
  link.target = "_blank";
  link.rel = "noopener";
  link.innerHTML = `<b>${iconeApp("download")}</b><span></span>`;
  link.querySelector("span").textContent = "Baixar o DeskPals";
  iconList.appendChild(link);
}

function openWindow(win) {
  // A janela que estava fora da tela abre crescendo, como no Windows 11.
  if ((win.closed || win.minimized) && win.el && !SHOT) {
    win.el.classList.remove("abrindo");
    void win.el.offsetWidth;
    win.el.classList.add("abrindo");
    win.el.addEventListener("animationend", () => win.el.classList.remove("abrindo"), { once: true });
  }
  win.closed = false;
  win.minimized = false;
  // No celular cabe uma janela de cada vez: as outras saem de cena
  // (e o pet perde o chão delas, como no Windows).
  if (narrow) {
    for (const other of windows) {
      if (other !== win && !other.closed) other.minimized = true;
    }
  }
  raiseWindow(win);
}

/** A janela que está na frente (a que dá para ver). */
function frontWindow() {
  let best = null;
  for (const win of windows) {
    if (win.closed || win.minimized) continue;
    if (!best || win.z > best.z) best = win;
  }
  return best;
}

/** A tela do celular não é um desktop encolhido: uma janela de cada
    vez, e o resto minimizado. */
function updateNarrow() {
  const now = world.width < 760;
  if (now === narrow) return;
  narrow = now;

  if (narrow) {
    let front = frontWindow();
    if (!front) {
      front = windows.find((win) => !win.closed) || null;
    }
    for (const win of windows) {
      if (win.closed) continue;
      win.minimized = win !== front;
    }
  } else {
    for (const win of windows) {
      if (!win.closed) win.minimized = false;
    }
  }
  layoutWindows();
  buildTaskbar();
}

/* As janelas da barra de tarefas: os botões são criados uma vez só e
   depois só mudam de estado — assim o foco do teclado não se perde
   a cada clique. */
const taskButtons = new Map();

function buildTaskbar() {
  if (taskButtons.size) {
    updateTaskbar();
    return;
  }
  for (const win of windows) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "demo-task";
    button.innerHTML = `<b>${iconeApp(win.icon)}</b><span></span>`;
    button.querySelector("span").textContent = win.title;
    button.title = win.title;
    button.addEventListener("click", () => {
      // Como no Windows 11: o botão da janela da frente a minimiza; o de
      // uma janela de trás a traz para a frente; o de uma fechada a abre.
      if (win.closed || win.minimized) {
        openWindow(win);
      } else if (win === frontWindow() && !narrow) {
        windowAction(win, "min");
      } else {
        raiseWindow(win);
      }
    });
    taskButtons.set(win.id, button);
    taskbarApps.appendChild(button);
  }
  updateTaskbar();
}

function updateTaskbar() {
  const front = frontWindow();
  for (const win of windows) {
    const button = taskButtons.get(win.id);
    if (!button) continue;
    const visivel = !win.closed && !win.minimized;
    // o tracinho: cinza para a janela aberta, azul e comprido para a da frente
    button.classList.toggle("aberto", !win.closed);
    button.classList.toggle("on", visivel && win === front);
    button.classList.toggle("closed", win.closed);
    button.setAttribute("aria-pressed", String(visivel));
  }
}

/** Traz a janela para a frente, como no Windows. */
function raiseWindow(win) {
  if (win.closed) return;
  if (win.z < topZ) {
    topZ++;
    win.z = topZ;
  }
  layoutWindows();
  updateTaskbar();
}

/** O que acontece quando se clica nos botões da janela. */
function windowAction(win, action) {
  if (action === "close") {
    win.closed = true;
    win.minimized = false;
    win.gone = true;
    win.el.hidden = true;
  } else if (action === "min") {
    win.minimized = true;
    win.gone = true;
    win.el.hidden = true;
  } else if (action === "max") {
    win.maximized = !win.maximized;
  }
  layoutWindows();
  updateTaskbar();

  // No celular, fechar a janela deixa a tela vazia: a próxima abre.
  if (action === "close" && narrow) {
    const next = windows.find((other) => other !== win && !other.closed);
    if (next) openWindow(next);
  }
}

/** O tamanho de cada janela: a largura é a do arranjo e a altura segue
    o conteúdo — a janela não deixa sobra nem corta texto. */
/** A largura da janela: uma fração da tela, com limites, para não ficar
    minúscula no celular nem perdida num monitor grande. */
function windowWidth(win) {
  const def = win.def;
  let width = win.w;
  if (def.wFrac) {
    width = Math.round(world.width * def.wFrac);
    if (def.wMin) width = Math.max(def.wMin, width);
    if (def.wMax) width = Math.min(def.wMax, width);
  }
  width = Math.max(120, Math.min(width, world.width - 16));
  win.w = width;
  return width;
}

/** A altura da janela: a do conteúdo, para nada ficar cortado. Só dá
    para medir com a janela na tela — escondida, ela mede zero. */
function windowHeight(win) {
  const bar = win.el ? win.el.querySelector(".demo-window-bar") : null;
  const body = win.el ? win.el.querySelector(".demo-window-body") : null;
  if (!body) return win.h;
  const barHeight = bar ? bar.offsetHeight : 30;

  // O corpo fica solto da janela por um instante: preso na altura da
  // janela, ele mediria a caixa vazia em vez do texto, e a janela
  // abriria com o conteúdo cortado (ou com a rolagem sobrando).
  const estilo = body.getAttribute("style");
  body.style.flex = "none";
  body.style.height = "auto";
  const altura = body.offsetHeight;
  if (estilo === null) body.removeAttribute("style");
  else body.setAttribute("style", estilo);

  return Math.max(120, altura + barHeight + 2);
}

/** Mede o palco, coloca as janelas no lugar e prepara o canvas. */
function measure() {
  const rect = stage.getBoundingClientRect();
  const previousWidth = world.width;
  const previousHeight = world.height;
  world.width = Math.max(320, Math.round(rect.width));
  world.height = Math.max(240, Math.round(rect.height));

  // A altura da barra de tarefas mora no CSS: o motor usa a mesma,
  // para o pet pisar exatamente na borda de cima dela.
  world.taskbarHeight = taskbarEl ? taskbarEl.offsetHeight || 36 : 36;

  // Antes de colocar as janelas no lugar: numa tela estreita só fica
  // uma na frente, e ela é a que fica no meio.
  updateNarrow();

  // Quando a tela muda de tamanho, as janelas acompanham: as que ainda
  // não foram movidas ficam onde o arranjo manda, e as movidas escalam
  // junto com a tela.
  const scaleX = previousWidth > 0 ? world.width / previousWidth : 1;
  const scaleY = previousHeight > 0 ? world.height / previousHeight : 1;

  for (const win of windows) {
    if (!win.el) continue;
    const w = windowWidth(win);
    if (win.moved) {
      win.x = Math.round(win.x * scaleX);
      win.y = Math.round(win.y * scaleY);
    } else if (narrow) {
      // No celular a janela fica no meio, abaixo da faixa de ícones.
      win.x = Math.round((world.width - w) / 2);
      win.y = iconList.offsetHeight + 14;
    } else {
      win.x = Math.round(win.def.x * world.width);
      win.y = Math.round(win.def.y * world.height);
    }
    win.placed = true;
    // A janela cabe inteira na tela quando dá; se não der, fica pelo menos
    // um pedaço visível, como no Windows.
    win.x = Math.round(Math.max(0, Math.min(world.width - w, win.x)));
    if (narrow) {
      // No celular a janela começa abaixo da faixa de ícones: ela
      // encolhe para caber, mas não sobe por cima dos ícones.
      win.y = Math.round(Math.max(0, Math.min(world.area.bottom - 80, win.y)));
    } else {
      const altura = windowHeight(win);
      win.y = Math.round(Math.max(0, Math.min(world.area.bottom - altura, win.y)));
    }
  }

  layoutWindows();

  const ratio = Math.min(2, window.devicePixelRatio || 1);
  canvas.width = Math.round(world.width * ratio);
  canvas.height = Math.round(world.height * ratio);
  context.setTransform(ratio, 0, 0, ratio, 0, 0);
  context.imageSmoothingEnabled = false;
}

/** Coloca cada janela no seu lugar (e avisa o motor). */
function layoutWindows(forcar) {
  const area = world.area;
  let front = null;

  for (const win of windows) {
    if (!win.el) continue;

    if (win.closed) {
      win.gone = true;
      win.el.hidden = true;
      continue;
    }

    const w = windowWidth(win);
    const full = win.maximized;

    const left = full ? area.left : Math.round(win.x);
    const top = full ? area.top : Math.round(win.y);

    // A janela aparece e toma a largura antes de ser medida: escondida,
    // ela mede zero, e o conteúdo voltaria cortado.
    const estavaEscondida = win.el.hidden;
    win.gone = win.minimized;
    win.el.hidden = win.minimized;
    if (win.el.classList.contains("maximized") !== full || !win.glyphSet) {
      // o botão do meio: "maximizar" ou "restaurar", como no Windows 11
      win.glyphSet = true;
      const maxButton = win.el.querySelector('[data-action="max"]');
      if (maxButton) {
        const name = (full ? "Restaurar " : "Maximizar ") + win.title;
        maxButton.innerHTML = full ? WINDOW_GLYPHS.restore : WINDOW_GLYPHS.max;
        maxButton.title = name;
        maxButton.setAttribute("aria-label", name);
      }
    }
    win.el.classList.toggle("maximized", full);
    win.el.style.left = left + "px";
    win.el.style.top = top + "px";

    const largura = (full ? area.right - area.left : w) + "px";
    const mudouLargura = win.el.style.width !== largura;
    win.el.style.width = largura;

    // A altura é a do conteúdo, para nada ficar cortado por baixo. Ela
    // só é medida de novo quando a largura muda (o texto quebra
    // diferente) ou quando a janela volta a aparecer — arrastar não
    // muda nada, e medir a cada passo do mouse trava a página.
    let height = win.h;
    if (full) {
      height = area.bottom - area.top;
    } else if (!win.el.hidden && (forcar || mudouLargura || estavaEscondida)) {
      // Só mede com a janela na tela. Escondida, ela mede zero, e a
      // altura guardada se perderia: ao voltar, a janela abriria
      // achatada. Minimizada, ela espera a volta para ser medida.
      height = windowHeight(win);
    }
    height = Math.max(80, Math.min(height, area.bottom - top));
    win.h = height;

    win.left = left;
    win.top = top;
    win.right = left + (full ? area.right - area.left : w);
    win.bottom = top + height;

    win.el.style.height = height + "px";

    // A janela da frente é a acesa; as de trás ficam apagadas.
    if (!front || win.z > front.z) front = win;
  }

  // A ordem das janelas na tela é contada de novo a cada arranjo: os
  // números ficam sempre abaixo do pet, que anda por cima de tudo.
  const emOrdem = windows
    .filter((win) => win.el && !win.closed)
    .sort((a, b) => a.z - b.z);
  emOrdem.forEach((win, ordem) => {
    win.el.style.zIndex = String(10 + ordem);
  });

  for (const win of windows) {
    if (!win.el) continue;
    win.el.classList.toggle("active", win === front);
  }
}

/* ---------------------------------------------------------------- o desenho */
function drawPet(target, now) {
  if (!target.sheet) return;
  const state = target.renderState(now);
  const total = target.tilesX * target.tilesY;
  const frame = Math.max(0, Math.min(total - 1, state.frame));
  const column = frame % target.tilesX;
  const row = Math.floor(frame / target.tilesX);
  const sourceX = column * target.tileWidth;
  const sourceY = row * target.tileHeight;

  // A sombra do pet: ela cola o bichinho no chão (no topo da janela),
  // como a sombra que o programa desenha embaixo dele.
  if (target.window) {
    context.save();
    context.globalAlpha = 0.22;
    context.fillStyle = "#000";
    context.beginPath();
    context.ellipse(
      state.x + state.width / 2, state.y + state.height - 3,
      state.width * 0.3, 4, 0, 0, Math.PI * 2
    );
    context.fill();
    context.restore();
  }

  context.save();
  context.globalAlpha = Math.max(0, Math.min(1, state.opacity));
  if (state.flipped) {
    context.translate(state.x + state.width, state.y);
    context.scale(-1, 1);
    context.drawImage(target.sheet, sourceX, sourceY, target.tileWidth, target.tileHeight, 0, 0, state.width, state.height);
  } else {
    context.drawImage(target.sheet, sourceX, sourceY, target.tileWidth, target.tileHeight, state.x, state.y, state.width, state.height);
  }
  context.restore();

  // O foco do teclado fica no desenho do pet, e não na tela inteira.
  if (target === pet && document.activeElement === canvas) {
    context.save();
    context.strokeStyle = "rgba(111,211,155,.95)";
    context.lineWidth = 1;
    context.setLineDash([3, 3]);
    context.strokeRect(
      Math.round(state.x) + 0.5, Math.round(state.y) + 0.5,
      Math.round(state.width) - 1, Math.round(state.height) - 1
    );
    context.restore();
  }
}

function render(now) {
  if (!pet) return;
  // Se a tela mudou de tamanho (janela redimensionada, celular girado),
  // o mundo é remedido antes de desenhar. A comparação usa o mesmo piso
  // de 320 pixels do measure(): se usasse o valor cru, uma tela mais
  // estreita que isso remediria a cada quadro, sem parar.
  const largura = Math.max(320, stage.clientWidth);
  const altura = Math.max(240, stage.clientHeight);
  if (largura !== world.width || altura !== world.height) measure();
  pet.update(now);
  context.clearRect(0, 0, world.width, world.height);
  drawPet(pet, now);
  for (const child of pet.children) drawPet(child, now);
  positionBubble(now);
}

/* Uma exceção dentro do quadro não pode matar a demo: o laço continua
   rodando e, se o erro se repetir, a página avisa o que houve. */
let frameErrors = 0;

function frame(now) {
  requestAnimationFrame(frame);
  try {
    // No modo foto o tempo é o mesmo em todo quadro: a imagem sai
    // sempre igual, e o quadro volta a ser desenhado se a tela
    // redimensionar (o canvas é limpo quando muda de tamanho).
    render(frozenTime !== null ? frozenTime : now);
  } catch (error) {
    frameErrors++;
    console.error(error);
    if (frameErrors === 3) showFatalError(error);
  }
}

/** A demo travou: a página mostra o erro em vez de ficar congelada. */
function showFatalError(error) {
  loading.hidden = false;
  loadingText.textContent = "A demo encontrou um erro e parou.";
  loadingRetry.hidden = false;
  loadingRetry.textContent = "Recarregar";
  loadingRetry.onclick = () => window.location.reload();
  const detail = document.createElement("p");
  detail.className = "demo-loading-detail";
  detail.textContent = error && error.message ? error.message : String(error);
  loading.appendChild(detail);
}

/* --------------------------------------------------------------- o balão */

/** O pet fica parado enquanto o balão está aberto (como no programa). */
function holdForBubble(on) {
  if (on === bubbleHeld) return;
  bubbleHeld = on;
  holdPets(on);
}

function say(text, choices) {
  bubbleText.textContent = text;
  bubbleChoices.innerHTML = "";
  if (choices && choices.length) {
    for (const choice of choices) {
      const button = document.createElement("button");
      button.type = "button";
      button.textContent = choice.label;
      button.addEventListener("click", choice.run);
      bubbleChoices.appendChild(button);
    }
    bubbleChoices.hidden = false;
  } else {
    bubbleChoices.hidden = true;
  }
  bubble.hidden = false;
  measureBubble();
  positionBubble();
  holdForBubble(true);

  // O leitor de tela ouve o que o pet fala, mesmo com o balão escondido.
  if (petTalk.textContent !== text) {
    petTalk.textContent = "";
    petTalk.textContent = text;
  }

  // O balão não fica na tela para sempre.
  window.clearTimeout(bubbleTimer);
  bubbleTimer = window.setTimeout(() => hideBubble(), 14000);
}

/** O tamanho do balão é medido uma vez por texto: medir todo quadro
    obriga o navegador a refazer o layout a 60 quadros por segundo. */
function measureBubble() {
  bubbleSize = { width: bubble.offsetWidth, height: bubble.offsetHeight };
}

function positionBubble(now) {
  if (bubble.hidden || !pet) return;
  const state = pet.renderState(now);
  const width = bubbleSize.width || bubble.offsetWidth;
  const height = bubbleSize.height || bubble.offsetHeight;
  let left = state.x + state.width / 2 - width / 2;
  let top = state.y - height - 14;
  // Sem espaço em cima: o balão desce e a cauda vira para cima.
  const abaixo = top < 6;
  if (abaixo) top = state.y + state.height + 14;
  left = Math.max(6, Math.min(world.width - width - 6, left));
  top = Math.max(6, Math.min(world.height - (world.taskbarHeight || 48) - height - 6, top));
  bubble.style.left = Math.round(left) + "px";
  bubble.style.top = Math.round(top) + "px";
  bubble.classList.toggle("abaixo", abaixo);

  // A cauda do balão aponta para o pet, não para um canto fixo.
  const tail = state.x + state.width / 2 - left;
  bubble.style.setProperty("--cauda", Math.round(Math.max(18, Math.min(width - 18, tail))) + "px");
}

function hideBubble() {
  window.clearTimeout(bubbleTimer);
  if (bubble.hidden) return;
  bubble.hidden = true;
  bubbleChoices.innerHTML = "";
  holdForBubble(false);
}

/* ----------------------------------------------------------------- o menu */
const DOWNLOAD = "https://github.com/lfeliipeadv-stack/DeskPals/releases/latest/download/DesktopPet-Instalador.exe";

function openMenu(x, y) {
  menu.innerHTML = "";

  const head = document.createElement("div");
  head.className = "demo-menu-head";
  head.textContent = pet ? pet.header.name : "Pet";
  menu.appendChild(head);

  // As necessidades do pet, no formato do programa: "🍖 80%  ❤️ 65%  ⚡ 40%".
  if (vpet) {
    const bars = document.createElement("div");
    bars.className = "demo-menu-bars";
    bars.textContent = vpet.bars();
    menu.appendChild(bars);
  }

  // O download é o item em destaque, logo no topo: é o caminho mais
  // curto da demo para o programa de verdade.
  const cta = document.createElement("a");
  cta.className = "demo-menu-cta";
  cta.href = DOWNLOAD;
  cta.target = "_blank";
  cta.rel = "noopener";
  cta.textContent = "⬇ Baixar o programa (grátis)";
  cta.addEventListener("click", closeMenu);
  menu.appendChild(cta);
  menu.appendChild(document.createElement("hr"));

  const item = (label, run) => {
    const button = document.createElement("button");
    button.type = "button";
    button.textContent = label;
    button.addEventListener("click", () => { closeMenu(); run(); });
    menu.appendChild(button);
  };
  const separator = () => {
    const line = document.createElement("hr");
    menu.appendChild(line);
  };

  item("💬 Perguntar ao pet…", ask);
  item("🥠 Biscoito da sorte", () => {
    react(REACTIONS.comer);
    say("🥠 " + pick(FORTUNES));
  });
  item("🎱 Bola 8 mágica", () => {
    say(BALL_ASK, [{
      label: BALL_SHAKE,
      run: () => {
        react(REACTIONS.pensei);
        say("🎱 " + pick(BALL_ANSWERS));
      }
    }]);
  });
  item("😄 Conte uma piada", () => say(pick(JOKES)));
  item("🙂 " + MOOD_ASK, askMood);
  item("🎞 Animações do pet", () => {
    buildAnimations();
    const win = windowById("animacoes");
    if (win) openWindow(win);
  });

  separator();
  item("🍖 Dar comida", () => petAction("comer"));
  item("❤️ Fazer carinho", () => petAction("carinho"));
  item("🎾 Brincar", () => petAction("brincar"));
  item("😴 Deitar", () => petAction("dormir"));

  separator();
  item(pomoRunning ? "⏸ Foco: pausar" : "▶ Foco (Pomodoro)", () => {
    const win = windowById("pomodoro");
    if (win) openWindow(win);
    pomoStartStop();
  });
  item("📝 Notas e tarefas", () => {
    const win = windowById("notas");
    if (win) {
      openWindow(win);
      const field = document.getElementById("notes-text");
      if (field) field.focus();
    }
  });
  item("🪟 Abrir o painel", () => {
    const win = windowById("painel");
    if (win) openWindow(win);
  });

  separator();
  item(soundOn ? "🔊 Som: ligado" : "🔇 Som: desligado", toggleSound);
  item("🔄 Voltar a cair do céu", () => {
    pet.arriveFromSky(arrivalSpot());
    say("Até logo! Volto em seguida.");
  });

  separator();
  const link = document.createElement("a");
  link.className = "demo-menu-item";
  link.href = DOWNLOAD;
  link.target = "_blank";
  link.rel = "noopener";
  link.textContent = "⬇ Baixar o programa para Windows";
  link.addEventListener("click", closeMenu);
  menu.appendChild(link);

  menu.hidden = false;
  const width = menu.offsetWidth;
  const height = menu.offsetHeight;
  // O menu cabe na tela: se for alto demais, ele sobe em vez de
  // transbordar por baixo.
  // (acima da barra de tarefas, que fica na frente dele)
  const chao = world.height - (world.taskbarHeight || 48);
  const fitsBelow = y + height <= chao - 6;
  const top = fitsBelow ? y : Math.max(6, chao - height - 6);
  menu.style.left = Math.round(Math.max(6, Math.min(world.width - width - 6, x))) + "px";
  menu.style.top = Math.round(top) + "px";
  menu.querySelector("button")?.focus();

  // O menu também segura o pet parado.
  if (!menuHeld) {
    menuHeld = true;
    holdPets(true);
  }
}

function closeMenu(devolverFoco = false) {
  if (menu.hidden) return;
  menu.hidden = true;
  if (menuHeld) {
    menuHeld = false;
    holdPets(false);
  }
  // Quem abriu o menu volta a ter o foco: a tecla fecha o menu e deixa
  // a pessoa onde ela estava, como num menu de verdade.
  if (devolverFoco) {
    try { canvas.focus(); } catch (error) { /* o canvas não focou: tudo bem */ }
  }
}

function pick(list) {
  return list[Math.floor(Math.random() * list.length)];
}

/** Uma ação do pet virtual: dar comida, fazer carinho, brincar, deitar.
    O pet faz a animação que ele tem (PetActions) e as necessidades
    se mexem. */
function petAction(act) {
  if (!pet || !vpet) return;
  const folder = currentFolder || "";
  react(actionNames(act, folder));

  if (act === "comer") vpet.darComida();
  else if (act === "carinho") vpet.fazerCarinho();
  else if (act === "brincar") vpet.brincar();
  else if (act === "dormir") vpet.dormir();

  paintNeeds();
  say(RESPOSTAS[act] || "Pronto!");
}

/* As três necessidades, na ordem em que aparecem. */
const NEEDS = [
  { label: "Comida", key: "food", icon: "🍖" },
  { label: "Carinho", key: "love", icon: "❤️" },
  { label: "Energia", key: "energy", icon: "⚡" }
];

/** As barras de necessidades, dentro do painel. As barras são criadas
    uma vez só; depois é só mexer no tamanho delas, porque isto é
    repintado a cada segundo (as necessidades caem com o tempo). */
function paintNeeds() {
  const box = document.getElementById("needs-bars");
  if (!box || !vpet) return;
  const pct = (value) => Math.round(Math.max(0, Math.min(100, value)));

  if (box.querySelectorAll(".demo-need").length !== NEEDS.length) {
    box.innerHTML = "";
    for (const need of NEEDS) {
      const line = document.createElement("div");
      line.className = "demo-need";
      const name = document.createElement("span");
      name.className = "demo-need-name";
      name.textContent = need.icon + " " + need.label;
      const track = document.createElement("span");
      track.className = "demo-need-track";
      track.appendChild(document.createElement("i"));
      const value = document.createElement("span");
      value.className = "demo-need-value";
      line.append(name, track, value);
      box.appendChild(line);
    }
  }

  const linhas = box.querySelectorAll(".demo-need");
  NEEDS.forEach((need, i) => {
    const line = linhas[i];
    const p = pct(vpet.numeros[need.key]);
    const fill = line.querySelector("i");
    fill.style.width = p + "%";
    fill.className = p < 25 ? "baixo" : "";
    line.querySelector(".demo-need-value").textContent = p + "%";
  });
  paintWidget();
}

/** O tique das necessidades: elas caem com o tempo, como no programa
    (o VirtualPet.Check, que roda a cada segundo). */
window.setInterval(() => {
  if (!vpet) return;
  vpet.aplicarDecaimento();
  paintNeeds();
}, 1000);

/** Faz o pet reagir com o primeiro nome de animação que ele conhece. */
function react(names) {
  if (!pet) return false;
  return pet.react(names);
}

/* --------------------------------------------------------------- as falas */
function ask() {
  if (!pet) return;
  if (!greeted) {
    // A primeira pergunta de cada visitante é a de produto: é o que
    // importa para quem chegou na página sem saber o que é isto.
    greeted = true;
    say(PITCH, [
      { label: "⬇ Baixar (grátis)", run: () => window.open(DOWNLOAD, "_blank", "noopener") },
      { label: "Me conta mais", run: nextAnswer }
    ]);
    return;
  }
  nextAnswer();
}

/** As falas do pet vão passando uma a uma, sem repetir: quem fica
    conversando não ouve a mesma frase duas vezes. */
function nextAnswer() {
  if (!pet) return;
  respostaIndice = (respostaIndice + 1) % ANSWERS.length;
  const answer = ANSWERS[respostaIndice].replace("{0}", pet.header.name);
  // O balão vem primeiro: com o balão aberto o pet fica parado, e a
  // reação escolhe uma animação que não o tira do lugar.
  say(answer);
  react(REACTIONS.ola);
}

function askMood() {
  say(MOOD_ASK, MOOD_FACES.map((face, index) => ({
    label: face,
    run: () => {
      if (index <= 1) react(REACTIONS.alegria);
      else if (index >= 3) react(REACTIONS.carinho);
      say(MOOD_REPLIES[index]);
    }
  })));
}

function toggleSound() {
  soundOn = !soundOn;
  if (pet) pet.volume = soundOn ? volume : 0;
  paintSoundButton();
  paintTray();
  paintQuick();
}

/** O botão de som diz a verdade: um pet sem som não promete som. */
function paintSoundButton() {
  const meta = petMeta.get(currentFolder);
  const mute = meta && meta.sounds === 0;
  if (mute) {
    soundButton.textContent = "🔇 Sem som";
    soundButton.setAttribute("aria-pressed", "false");
    soundButton.disabled = true;
    soundButton.title = "Este pet não tem som. Os outros têm.";
  } else {
    soundButton.textContent = soundOn ? "🔊 Som" : "🔇 Som";
    soundButton.setAttribute("aria-pressed", String(soundOn));
    soundButton.disabled = false;
    soundButton.title = "";
  }
}

/* ---------------------------------------------------------------- as dicas */
function showTip(index) {
  if (index < 0 || index >= TIPS.length) {
    tipsBox.hidden = true;
    return;
  }
  tipIndex = index;
  tipsText.innerHTML = TIPS[index];
  tipsNumber.textContent = String(index + 1);
  tipsBox.hidden = false;
  window.clearTimeout(tipTimer);
  tipTimer = window.setTimeout(() => showTip(index + 1), 8000);
  if (typeof paintQuick === "function") paintQuick();
}

function hideTips() {
  window.clearTimeout(tipTimer);
  tipsBox.hidden = true;
}

/* -------------------------------------------------------------- o Pomodoro
   O mesmo do programa: 25 minutos de foco, 5 de pausa. O fim do tempo
   é uma hora absolute (e não uma contagem), para o relógio não se
   perder quando a aba fica em segundo plano.                         */
const pomoTime = document.getElementById("pomo-time");
const pomoFill = document.getElementById("pomo-fill");
const pomoStart = document.getElementById("pomo-start");
const pomoReset = document.getElementById("pomo-reset");
const pomoReminders = document.getElementById("pomo-reminders");

const POMO_KEY = "meupet.demo.pomodoro";
let pomoEndsAt = 0;      // quando o bloco de tempo acaba (0 = parado)
let pomoRunning = false;
let pomoIsBreak = false;
let pomoLeft = POMODORO_FOCO;
let pomoTimer = 0;

function pomoTotal() {
  return pomoIsBreak ? POMODORO_PAUSA : POMODORO_FOCO;
}

function pomoPaint() {
  if (!pomoTime) return;
  const left = pomoRunning ? Math.max(0, Math.round((pomoEndsAt - Date.now()) / 1000)) : pomoLeft;
  const minutes = String(Math.floor(left / 60)).padStart(2, "0");
  const seconds = String(left % 60).padStart(2, "0");
  pomoTime.textContent = minutes + ":" + seconds;
  if (pomoFill) pomoFill.style.width = Math.round((1 - left / pomoTotal()) * 100) + "%";
  if (pomoStart) {
    pomoStart.textContent = pomoRunning
      ? "⏸ Pausar"
      : (pomoLeft < pomoTotal() ? "▶ Continuar" : "▶ Começar");
    pomoStart.setAttribute("aria-pressed", String(pomoRunning));
  }
  const win = windowById("pomodoro");
  if (win && win.el) {
    const title = win.el.querySelector(".demo-window-title");
    // O nome da janela fica na barra de título, com a contagem ao lado:
    // é o que a pessoa procura quando lê o manual.
    if (title) title.textContent = win.title + " — " + (pomoIsBreak ? "Pausa " : "") + minutes + ":" + seconds;
  }
  paintCalendarFocus();
}

function pomoStartStop() {
  if (pomoRunning) {
    // Pausar: guarda o tempo que faltava.
    pomoLeft = Math.max(0, Math.round((pomoEndsAt - Date.now()) / 1000));
    pomoRunning = false;
    pomoEndsAt = 0;
    window.clearInterval(pomoTimer);
  } else {
    if (pomoLeft <= 0) pomoLeft = pomoTotal();
    pomoEndsAt = Date.now() + pomoLeft * 1000;
    pomoRunning = true;
    window.clearInterval(pomoTimer);
    pomoTimer = window.setInterval(pomoTick, 1000);
    react(REACTIONS.pensei);
  }
  pomoSave();
  pomoPaint();
}

function pomoTick() {
  const left = Math.round((pomoEndsAt - Date.now()) / 1000);
  if (left <= 0) {
    // Virou a fase: a pausa começa sozinha, como no programa.
    pomoIsBreak = !pomoIsBreak;
    pomoLeft = pomoTotal();
    pomoEndsAt = Date.now() + pomoLeft * 1000;
    pomoSave();
    react(pomoIsBreak ? REACTIONS.cansado : REACTIONS.alegria);
    say(pomoIsBreak
      ? "⏱ Fim do foco! Agora são cinco minutos de pausa. Alonga as costas!"
      : "⏱ A pausa acabou. Bora voltar?");
    showToast(pomoIsBreak ? "Fim do foco" : "Fim da pausa",
      pomoIsBreak ? "Cinco minutos de pausa. Levante, beba água, alongue." : "Hora de voltar para mais 25 minutos de foco.");
    beep();
  }
  pomoPaint();
}

function pomoResetTimer() {
  window.clearInterval(pomoTimer);
  pomoRunning = false;
  pomoEndsAt = 0;
  pomoIsBreak = false;
  pomoLeft = POMODORO_FOCO;
  pomoSave();
  pomoPaint();
}

function pomoSave() {
  try {
    window.localStorage.setItem(POMO_KEY, JSON.stringify({
      endsAt: pomoEndsAt, running: pomoRunning, isBreak: pomoIsBreak, left: pomoLeft
    }));
  } catch (error) {
    /* sem armazenamento local: o Pomodoro não fica guardado */
  }
}

function pomoLoad() {
  try {
    const saved = JSON.parse(window.localStorage.getItem(POMO_KEY) || "null");
    if (!saved) return;
    pomoIsBreak = !!saved.isBreak;
    pomoLeft = saved.left || pomoTotal();
    if (saved.running && saved.endsAt > Date.now()) {
      pomoEndsAt = saved.endsAt;
      pomoRunning = true;
      pomoTimer = window.setInterval(pomoTick, 1000);
    }
  } catch (error) {
    /* nada guardado */
  }
}

/** Um bipe curtinho, sem arquivo: o navegador faz o som sozinho. */
let audioContext = null;
function beep() {
  if (!soundOn) return;
  try {
    if (!audioContext) audioContext = new (window.AudioContext || window.webkitAudioContext)();
    if (audioContext.state === "suspended") audioContext.resume();
    const osc = audioContext.createOscillator();
    const gain = audioContext.createGain();
    osc.type = "sine";
    osc.frequency.value = 660;
    gain.gain.setValueAtTime(0.0001, audioContext.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.12, audioContext.currentTime + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.0001, audioContext.currentTime + 0.35);
    osc.connect(gain).connect(audioContext.destination);
    osc.start();
    osc.stop(audioContext.currentTime + 0.4);
  } catch (error) {
    /* sem som, sem drama */
  }
}

/* ------------------------------------------------------------- o mouse */
function localPoint(event) {
  const rect = stage.getBoundingClientRect();
  return { x: event.clientX - rect.left, y: event.clientY - rect.top };
}

/** O pet é pequeno: o alvo de clique é um pouco maior que o desenho,
    para o dedo e para o mouse acertarem. */
function hitPet(point) {
  if (!pet) return false;
  const state = pet.renderState();
  const margin = 8;
  return point.x >= state.x - margin && point.x <= state.x + state.width + margin &&
         point.y >= state.y - margin && point.y <= state.y + state.height + margin;
}

/* O último tipo de ponteiro (mouse, toque ou caneta): o ícone da área de
   trabalho abre com dois cliques no mouse, e com um toque no celular. */
let lastPointerType = "mouse";
document.addEventListener("pointerdown", (event) => { lastPointerType = event.pointerType || "mouse"; }, true);

stage.addEventListener("pointerdown", (event) => {
  if (event.button === 2) return;
  // O menu, o balão, a barra de tarefas e os painéis ficam por cima do
  // palco: clicar neles não é tentar pegar o pet.
  if (event.target.closest && event.target.closest("#menu, #bubble, .demo-tips, .demo-taskbar, .demo-flyout, .demo-ctx, .demo-toast")) return;
  // A dica some quando a pessoa começa a mexer na demo.
  hideTips();
  const point = localPoint(event);

  // O botão do meio abre a janela de animações, como no programa.
  if (event.button === 1) {
    if (hitPet(point)) {
      event.preventDefault();
      buildAnimations();
      const win = windowById("animacoes");
      if (win) openWindow(win);
    }
    return;
  }

  // O pet vem primeiro: ele é desenhado por cima de tudo.
  if (hitPet(point)) {
    const state = pet.renderState();
    drag = {
      what: "pet",
      offsetX: point.x - state.x,
      offsetY: point.y - state.y,
      moved: false,
      startX: event.clientX,
      startY: event.clientY
    };
    pet.beginDrag();
    stage.classList.add("dragging-pet");
    stage.setPointerCapture(event.pointerId);
    hideBubble();
    hideTips();

    // Toque comprido: no celular não existe botão direito, e o toque
    // longo no pet é o jeito de abrir o menu dele.
    if (event.pointerType && event.pointerType !== "mouse") {
      clearTimeout(longPressTimer);
      longPressTimer = window.setTimeout(() => {
        longPressTimer = 0;
        longPressOpened = true;
        const agora = pet.renderState();
        openMenu(agora.x + agora.width / 2, agora.y + agora.height + 6);
      }, 500);
    }
    return;
  }

  // Os botões da janela (minimizar, maximizar, fechar) são tratados no
  // clique, e não aqui: é o clique que o teclado dispara no botão.

  // Os ícones da área de trabalho: abrem no clique (veja buildIcons).
  const icon = event.target.closest ? event.target.closest(".demo-icon") : null;
  if (icon) return;

  // A barra do título: arrasta a janela (os botões dela não arrastam).
  const bar = event.target.closest ? event.target.closest(".demo-window-bar") : null;
  if (bar && !event.target.closest("[data-action]")) {
    const win = windowOf(bar);
    if (!win) return;
    raiseWindow(win);
    drag = {
      what: "window",
      win,
      offsetX: point.x - win.left,
      offsetY: point.y - win.top,
      moved: false,
      startX: event.clientX,
      startY: event.clientY
    };
    stage.classList.add("dragging-window");
    stage.setPointerCapture(event.pointerId);
    return;
  }

  // O resto da janela: só traz para a frente.
  const holder = event.target.closest ? event.target.closest(".demo-window") : null;
  if (holder) {
    const win = windowOf(holder);
    if (win) raiseWindow(win);
  }
});

stage.addEventListener("pointermove", (event) => {
  if (!drag) {
    // O mouse avisa que o pet pode ser arrastado.
    const point = localPoint(event);
    stage.style.cursor = hitPet(point) ? "grab" : "";
    return;
  }
  const point = localPoint(event);

  if (drag.what === "pet") {
    pet.dragTo(point.x - drag.offsetX, point.y - drag.offsetY, performance.now());
    if (Math.abs(event.clientX - drag.startX) > 4 || Math.abs(event.clientY - drag.startY) > 4) {
      drag.moved = true;
      // O dedo saiu do lugar: o toque comprido não é mais toque
      // comprido, é arrasto.
      clearTimeout(longPressTimer);
      longPressTimer = 0;
    }
    return;
  }

  const win = drag.win;
  if (win.maximized) {
    // Arrastar uma janela maximizada a restaura, como no Windows.
    win.maximized = false;
    drag.offsetX = Math.min(drag.offsetX, win.w / 2);
    drag.offsetY = Math.min(drag.offsetY, 26);
  }
  win.x = point.x - drag.offsetX;
  win.y = point.y - drag.offsetY;
  drag.win.moved = true;
  layoutWindows();
});

function endPointer(event) {
  clearTimeout(longPressTimer);
  longPressTimer = 0;
  if (!drag) return;
  if (drag.what === "pet") {
    pet.endDrag(performance.now());
    // O toque comprido já abriu o menu: o clique não pergunta nada.
    if (!drag.moved && !longPressOpened) ask();
  }
  longPressOpened = false;
  drag = null;
  stage.classList.remove("dragging-pet", "dragging-window");
  if (event.pointerId !== undefined && stage.hasPointerCapture(event.pointerId)) {
    stage.releasePointerCapture(event.pointerId);
  }
}

stage.addEventListener("pointerup", endPointer);
stage.addEventListener("pointercancel", endPointer);

/* Os botões da janela (minimizar, maximizar, fechar) respondem no
   clique, e não no pointerdown: é o clique que o teclado dispara
   quando a pessoa aperta Enter ou Espaço no botão focado. */
stage.addEventListener("click", (event) => {
  const botao = event.target.closest ? event.target.closest("[data-action]") : null;
  if (!botao) return;
  const win = windowOf(botao);
  if (win) windowAction(win, botao.dataset.action);
});

stage.addEventListener("contextmenu", (event) => {
  const point = localPoint(event);
  // Só o pet tem menu próprio: no resto da tela o menu do navegador
  // continua valendo (para copiar, por exemplo).
  if (hitPet(point)) {
    event.preventDefault();
    // O menu do pet abre embaixo dele, como no programa: assim o bicho
    // continua em cima de tudo, do jeito que ele anda sobre as janelas.
    const state = pet.renderState();
    openMenu(state.x + state.width / 2, state.y + state.height + 6);
    return;
  }
  // Na área de trabalho vazia: o menu do Windows 11 (papel de parede,
  // tema, as janelas). Nas janelas continua o do navegador.
  const sobre = event.target.closest && event.target.closest(".demo-window, .demo-taskbar, .demo-flyout, .demo-ctx, #menu, #bubble, .demo-tips, .demo-toast");
  if (!sobre) {
    event.preventDefault();
    openDeskMenu(point.x, point.y);
  }
});

/* Dois cliques na barra de título maximizam (ou restauram) a janela. */
stage.addEventListener("dblclick", (event) => {
  const bar = event.target.closest ? event.target.closest(".demo-window-bar") : null;
  if (!bar || event.target.closest("[data-action]")) return;
  const win = windowOf(bar);
  if (win && !narrow) windowAction(win, "max");
});

document.addEventListener("pointerdown", (event) => {
  if (!menu.hidden && !menu.contains(event.target)) closeMenu();
});

/* ----------------------------------------------------------- o teclado */
document.addEventListener("keydown", (event) => {
  if (event.key === "Escape") {
    if (deskMenu && !deskMenu.hidden) { closeDeskMenu(); return; }
    if (anyFlyoutOpen()) { closeFlyouts(); return; }
    if (!menu.hidden) { closeMenu(true); return; }
    if (!bubble.hidden) { hideBubble(); return; }
    if (!tipsBox.hidden) { hideTips(); return; }
    return;
  }

  // O menu do pet pelo teclado: a tecla de menu do teclado (ContextMenu)
  // e Shift+F10 abrem o menu do pet, como no Windows.
  const alvo = event.target;
  const editando = alvo && (alvo.tagName === "TEXTAREA" || alvo.tagName === "INPUT" ||
                            alvo.isContentEditable);
  if ((event.key === "ContextMenu" || (event.key === "F10" && event.shiftKey)) &&
      menu.hidden && !editando) {
    if (pet) {
      event.preventDefault();
      const state = pet.renderState();
      openMenu(state.x + state.width / 2, state.y + state.height + 6);
      return;
    }
  }

  // Dentro do menu: as setas andam pelos itens, como num menu de verdade.
  // Só quando o foco está no menu — se a pessoa estiver escrevendo numa
  // anotação, as setas continuam sendo da anotação.
  if (!menu.hidden && menu.contains(document.activeElement)) {
    const itens = [...menu.querySelectorAll("button, a[href]")];
    if (!itens.length) return;
    const atual = itens.indexOf(document.activeElement);
    let proximo = -1;
    if (event.key === "ArrowDown") proximo = atual < 0 ? 0 : (atual + 1) % itens.length;
    else if (event.key === "ArrowUp") proximo = atual < 0 ? itens.length - 1 : (atual - 1 + itens.length) % itens.length;
    else if (event.key === "Home") proximo = 0;
    else if (event.key === "End") proximo = itens.length - 1;
    if (proximo >= 0) {
      event.preventDefault();
      itens[proximo].focus();
    }
    return;
  }

  if (event.key === "Enter" || event.key === " ") {
    // O canvas do pet é focável: Enter conversa com ele.
    if (event.target === canvas) {
      event.preventDefault();
      ask();
    }
  }
});

/* ------------------------------------------------------- as animações
   A janela do programa que mostra as animações de um pet: um mosaico
   por animação, com o primeiro quadro desenhado. Clicar num mosaico
   faz o pet fazer aquela animação.                                  */
const animList = document.getElementById("anim-list");

function buildAnimations() {
  if (!animList) return;
  animList.innerHTML = "";
  if (!pet || !pet.sheet) return;

  const list = [...pet.animations.values()].sort((a, b) =>
    String(a.name || "").localeCompare(String(b.name || ""), "pt-BR"));

  for (const animation of list) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "demo-anim";
    button.title = animation.name;

    const thumb = document.createElement("canvas");
    thumb.width = 48;
    thumb.height = 48;
    thumb.className = "demo-anim-thumb";
    const thumbContext = thumb.getContext("2d");
    thumbContext.imageSmoothingEnabled = false;
    const frame = animation.sequence.frames[0] || 0;
    const column = frame % pet.tilesX;
    const row = Math.floor(frame / pet.tilesX);
    thumbContext.drawImage(
      pet.sheet,
      column * pet.tileWidth, row * pet.tileHeight, pet.tileWidth, pet.tileHeight,
      0, 0, 48, 48
    );

    const label = document.createElement("span");
    label.textContent = animation.name;

    button.append(thumb, label);
    button.addEventListener("click", () => {
      pet.playAnimation(animation.id);
      say("🎬 " + animation.name);
    });
    animList.appendChild(button);
  }

  const win = windowById("animacoes");
  if (win) win.title = "Animações do " + (pet.header.name || "pet");
  const title = win && win.el ? win.el.querySelector(".demo-window-title") : null;
  if (title) title.textContent = win.title;
}

/* --------------------------------------------------------------- os pets */
function buildPetList() {
  petList.innerHTML = "";
  for (const info of PETS) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "demo-pet";
    button.setAttribute("aria-pressed", String(info.folder === currentFolder));
    button.dataset.folder = info.folder;

    const image = document.createElement("img");
    image.src = "pets/" + info.folder + ".gif";
    image.alt = "";
    image.loading = "lazy";

    const label = document.createElement("span");
    label.textContent = info.name;
    // Os números vêm do pets.json que o site publica: é o mesmo arquivo
    // que o programa usa para listar os pets.
    const meta = petMeta.get(info.folder);
    const detail = meta
      ? `${meta.animations} animações · ${meta.sounds} sons`
      : (info.note || "");
    label.title = detail ? info.name + " — " + detail : info.name;
    button.setAttribute("aria-label", detail ? info.name + ", " + detail : info.name);

    button.append(image, label);
    button.addEventListener("click", () => loadPet(info.folder));
    petList.appendChild(button);
  }
}

/** Os números de cada pet: quantas animações e quantos sons ele tem. */
const petMeta = new Map();

/* Os pets já baixados ficam guardados: voltar para um pet que a pessoa
   já viu é instantâneo, sem baixar o XML de novo. */
const petCache = new Map();

async function loadPetMeta() {
  try {
    const response = await fetch("pets.json");
    if (!response.ok) return;
    const data = await response.json();
    for (const info of data.pets || []) {
      petMeta.set(info.folder, { animations: info.animations, sounds: info.sounds });
    }
  } catch (error) {
    /* sem os números: a demo funciona do mesmo jeito */
  }
  buildPetList();
}

/** Onde o pet deve cair: em cima de uma das janelas que estão abertas. */
function arrivalSpot() {
  const open = windows.filter((win) => !win.closed && !win.minimized);
  if (!open.length) return null;
  const win = open[Math.floor(Math.random() * open.length)];
  return (win.left + win.right) / 2 - (pet ? pet.width : 40) / 2;
}

async function loadPet(folder) {
  const url = "pets/" + folder + "/animations.xml";
  // A cortina só aparece quando o pet ainda não foi baixado: voltar
  // para um pet que a pessoa já viu é instantâneo.
  if (!petCache.has(url)) {
    loading.hidden = false;
    loadingText.textContent = "carregando o pet…";
  }
  loadingRetry.hidden = true;
  closeMenu();
  hideBubble();

  const next = new Pet(world);
  next.volume = soundOn ? volume : 0;
  next.onEvent = (event) => {
    if (event.type === "spawn") measure();
  };

  try {
    await next.load(url, petCache);
  } catch (error) {
    // A página não morre aqui: o pet que estava na tela continua
    // rodando atrás, e há um caminho para tentar de novo.
    loading.hidden = false;
    loadingText.textContent = "não foi possível carregar este pet.";
    loadingRetry.hidden = false;
    loadingRetry.textContent = "Tentar de novo";
    loadingRetry.onclick = () => loadPet(folder);
    const detail = document.querySelector(".demo-loading-detail");
    if (detail) detail.textContent = error && error.message ? error.message : String(error);
    console.error(error);
    return;
  }

  if (pet) pet.dispose();
  pet = next;
  currentFolder = folder;
  vpet = new VirtualPet(folder, () => paintNeeds());
  measure();

  // Como no primeiro arranque do programa, o pet chega do céu: ele cai
  // e pousa no topo da janela — é assim que ele aparece na tela do Windows.
  pet.arriveFromSky(arrivalSpot());
  pet.start();
  loading.hidden = true;

  // Desenha o primeiro quadro na hora: o pet aparece sem esperar o
  // próximo quadro da animação.
  if (SHOT) {
    // Na foto o Pomodoro começa zerado e as dicas ficam de fora: a
    // imagem mostra a mesa, não as dicas.
    pomoResetTimer();
    hideTips();
    const momento = advanceSimulation(4500);
    // Na foto, o pet fica em cima da janela do bloco de notas. Ele é
    // posto no lugar sem depender da física: a imagem tem que sair
    // igual sempre.
    const notas = windows.find((win) => win.id === "notas");
    if (notas) {
      const x = Math.round((notas.left + notas.right) / 2 - pet.width / 2);
      const y = notas.top - pet.height;
      pet.beginDrag();
      pet.dragTo(x, y - 10, momento);
      pet.endDrag(momento);
      advanceSimulation(2500);
      if (pet.window !== notas) {
        pet.x = x;
        pet.y = y;
        pet.motion.reset(x, y, momento);
        pet.window = notas;
        pet.currentWindowSize = { left: notas.left, top: notas.top, right: notas.right };
        const parado = pet.stillAnimation;
        if (parado >= 0) pet.setNewAnimation(parado);
      }
      advanceSimulation(1200);
      // O quadro para aqui: a foto sai sempre igual.
      frozenTime = simulatedNow;
    }
  } else {
    render(performance.now());
  }

  for (const button of petList.querySelectorAll(".demo-pet")) {
    button.setAttribute("aria-pressed", String(button.dataset.folder === folder));
  }

  // A janela de animações mostra as animações do pet que está na tela.
  buildAnimations();
  paintSoundButton();
  paintNeeds();

  // O pet reclama quando está com fome, sem carinho ou sem energia
  // (o mesmo do programa: uma vez por hora, no máximo).
  window.setTimeout(() => {
    if (!vpet || naoPerturbe) return;
    const problema = vpet.reclamar();
    if (!problema) return;
    const fala = RECLAMACOES[problema];
    const acao = {
      hungry: () => petAction("comer"),
      lonely: () => petAction("carinho"),
      sleepy: () => petAction("dormir")
    }[problema];
    say(fala.texto, [
      { label: fala.acao, run: acao },
      { label: fala.later, run: () => hideBubble() }
    ]);
  }, 4000);

  // A primeira fala da demo é a apresentação: quem chega pela página
  // precisa saber o que está olhando e o que dá para fazer com isso.
  // Se a pessoa perguntou antes, a apresentação já aconteceu.
  if (!SHOT) {
    // A fala espera o pet chegar: ele vem do céu, e um balão aberto antes
    // de ele aparecer ficaria preso no alto da tela, longe dele.
    const esperaPousar = (tentativas) => {
      if (greeted || naoPerturbe || pet !== este) return;
      const state = pet.renderState();
      const chegou = state.y >= 0 && !pet.isDragging && !pet.isTossing &&
                     (pet.window || pet.header.floating || state.y + state.height >= world.area.bottom - 2);
      if (!chegou && tentativas > 0) {
        window.setTimeout(() => esperaPousar(tentativas - 1), 300);
        return;
      }
      if (state.y < 0) return;   // ainda fora da tela: melhor não falar
      greeted = true;
      say(
        "Oi! Eu sou o " + (pet.header.name || "pet") + ". Esta é a área de trabalho de verdade: " +
        "o mesmo motor e as mesmas animações do programa para Windows.",
        [
          { label: "⬇ Baixar (grátis)", run: () => window.open(DOWNLOAD, "_blank", "noopener") },
          { label: "Me conta mais", run: () => {
              const win = windowById("sobre");
              if (win) openWindow(win);
              nextAnswer();
            } }
        ]
      );
    };
    const este = pet;
    window.setTimeout(() => esperaPousar(30), 900);
  }
}

/* ------------------------------------------------------------- o relógio */
function tickClock() {
  const now = new Date();
  const time = String(now.getHours()).padStart(2, "0") + ":" + String(now.getMinutes()).padStart(2, "0");
  const date = String(now.getDate()).padStart(2, "0") + "/" +
               String(now.getMonth() + 1).padStart(2, "0") + "/" + now.getFullYear();
  const timeEl = clock.querySelector("b");
  const dateEl = clock.querySelector("small");
  if (timeEl) timeEl.textContent = time;
  if (dateEl) dateEl.textContent = date;
}

/* -------------------------------------------------- a simulação adiantada
   Roda a simulação por alguns segundos, sem depender da animação do
   navegador: serve para testar e para a imagem da documentação.
   O tempo do quadro é que manda: assim a imagem sai sempre igual.   */
let simulatedNow = 0;
let frozenTime = null;   // no modo foto, o tempo para
function advanceSimulation(milliseconds) {
  const step = 40;
  let now = Math.max(performance.now(), simulatedNow);
  const end = now + milliseconds;
  while (now < end) {
    now += step;
    render(now);
  }
  simulatedNow = now;
  return now;
}

/* ----------------------------------------------------------------- o bloco de notas
   As anotações ficam no próprio navegador, como as notas do programa. */
const NOTES_KEY = "meupet.demo.notas";
function loadNotes() {
  const field = document.getElementById("notes-text");
  if (!field) return;
  try {
    const saved = window.localStorage.getItem(NOTES_KEY);
    if (saved) field.value = saved;
  } catch (error) {
    /* sem armazenamento local: as notas não ficam guardadas */
  }
  field.addEventListener("input", () => {
    try {
      window.localStorage.setItem(NOTES_KEY, field.value);
    } catch (error) {
      /* idem */
    }
  });
}

/** O navegador só deixa o som tocar depois de um gesto da pessoa.
    No primeiro clique ou tecla, a demo acorda o áudio: a partir daí os
    sons do pet entram junto com as animações. */
function unlockAudio() {
  try {
    if (pet) {
      const play = pet.audio.play();
      if (play && play.then) play.then(() => pet.audio.pause()).catch(() => {});
    }
  } catch (error) {
    /* sem som, sem drama */
  }
  if (audioContext && audioContext.state === "suspended") audioContext.resume().catch(() => {});
}
document.addEventListener("pointerdown", unlockAudio, { once: true });
document.addEventListener("keydown", unlockAudio, { once: true });

/* ===================================================== o Windows 11 da demo
   O menu Iniciar, a pesquisa, as configurações rápidas, o calendário,
   o menu do botão direito, as notificações, o tema e o papel de
   parede. Tudo é da página: nada sai do navegador.                  */

const startBtn = document.getElementById("start-btn");
const searchBtn = document.getElementById("search-btn");
const startMenu = document.getElementById("start-menu");
const startSearch = document.getElementById("start-search");
const quickBtn = document.getElementById("quick-btn");
const quickPanel = document.getElementById("quick");
const calendarPanel = document.getElementById("calendar");
const deskMenu = document.getElementById("desk-menu");
const toastBox = document.getElementById("toast");
const widgetBtn = document.getElementById("widget-btn");
const showDesktopBtn = document.getElementById("show-desktop");

const PAGE_URL = "index.html";
const WIKI_URL = "wiki/Home.html";
const THEME_KEY = "meupet.demo.tema";
const WALL_KEY = "meupet.demo.parede";
const WALLPAPERS = ["Flor", "Pôr do sol", "Aurora"];

let volume = 0.6;          // o volume do pet (0 a 1)
let naoPerturbe = false;   // o pet não puxa conversa sozinho
let wallpaper = 0;
let calendarOffset = 0;    // meses antes ou depois do atual
let toastTimer = 0;
let desktopShown = null;   // as janelas que o "mostrar a área de trabalho" escondeu

const lerGuardado = (key) => { try { return window.localStorage.getItem(key); } catch (error) { return null; } };
const guardar = (key, value) => { try { window.localStorage.setItem(key, value); } catch (error) { /* sem armazenamento */ } };

/* ------------------------------------------------- o tema e o papel de parede */
function applyTheme(dark, save) {
  stage.classList.toggle("escuro", dark);
  if (save) guardar(THEME_KEY, dark ? "escuro" : "claro");
  paintQuick();
}
function isDark() {
  return stage.classList.contains("escuro");
}
function applyWallpaper(index, save) {
  wallpaper = ((index % WALLPAPERS.length) + WALLPAPERS.length) % WALLPAPERS.length;
  stage.classList.remove("parede-2", "parede-3");
  if (wallpaper > 0) stage.classList.add("parede-" + (wallpaper + 1));
  if (save) guardar(WALL_KEY, String(wallpaper));
}
function nextWallpaper() {
  applyWallpaper(wallpaper + 1, true);
  showToast("Papel de parede", "Agora é “" + WALLPAPERS[wallpaper] + "”. O pet gostou.");
  react(REACTIONS.alegria);
}

/* ------------------------------------------------------- os painéis (flyouts) */
const FLYOUTS = {
  start: { el: startMenu, buttons: [startBtn, searchBtn] },
  quick: { el: quickPanel, buttons: [quickBtn] },
  calendar: { el: calendarPanel, buttons: [clock] }
};

function openFlyout(name) {
  const alvo = FLYOUTS[name];
  const jaAberto = alvo && !alvo.el.hidden;
  closeFlyouts();
  closeDeskMenu();
  if (!alvo || jaAberto) return;
  alvo.el.hidden = false;
  for (const button of alvo.buttons) if (button) button.setAttribute("aria-expanded", "true");
  hideTips();
  if (name === "start") {
    buildStartMenu();
    startSearch.value = "";
    filterStart("");
    window.setTimeout(() => startSearch.focus(), 30);
  } else if (name === "quick") {
    paintQuick();
    const first = quickPanel.querySelector("button");
    if (first) first.focus();
  } else if (name === "calendar") {
    calendarOffset = 0;
    renderCalendar();
  }
}

function closeFlyouts() {
  for (const flyout of Object.values(FLYOUTS)) {
    if (!flyout.el) continue;
    flyout.el.hidden = true;
    for (const button of flyout.buttons) if (button) button.setAttribute("aria-expanded", "false");
  }
}

function anyFlyoutOpen() {
  return Object.values(FLYOUTS).some((flyout) => flyout.el && !flyout.el.hidden);
}

document.addEventListener("pointerdown", (event) => {
  const dentro = event.target.closest && event.target.closest(".demo-flyout, #start-btn, #search-btn, #quick-btn, #clock, .demo-ctx, #power-btn");
  if (!dentro) {
    closeFlyouts();
    closeDeskMenu();
  }
});

/* ------------------------------------------------------------ o menu Iniciar */
function launch(run) {
  closeFlyouts();
  closeDeskMenu();
  run();
}

/** Tudo o que o Iniciar e a pesquisa encontram. */
function startEntries() {
  const apps = windows.map((win) => ({
    kind: "app", name: win.title, icon: iconeApp(win.icon),
    hint: "Aplicativo", run: () => openWindow(win)
  }));
  apps.push(
    { kind: "app", name: "Baixar o DeskPals", icon: iconeApp("download"), hint: "Instalador para Windows 10 e 11",
      run: () => window.open(DOWNLOAD, "_blank", "noopener") },
    { kind: "app", name: "Página do DeskPals", icon: iconeApp("site"), hint: "Todos os recursos",
      run: () => { window.location.href = PAGE_URL; } },
    { kind: "app", name: "Manual (Wiki)", icon: iconeApp("wiki"), hint: "Como usar cada recurso",
      run: () => window.open(WIKI_URL, "_blank", "noopener") }
  );
  const pets = PETS.map((info) => ({
    kind: "pet", name: info.name, folder: info.folder,
    hint: info.note || "Pet", run: () => loadPet(info.folder)
  }));
  const acoes = [
    { kind: "acao", name: "Conversar com o pet", icon: iconeApp("painel"), hint: "Ele responde no balão", run: () => ask() },
    { kind: "acao", name: "Começar um Pomodoro", icon: iconeApp("pomodoro"), hint: "25 minutos de foco",
      run: () => { const win = windowById("pomodoro"); if (win) openWindow(win); if (!pomoRunning) pomoStartStop(); } },
    { kind: "acao", name: "Trocar o papel de parede", icon: iconeApp("site"), hint: WALLPAPERS.join(", "), run: () => nextWallpaper() },
    { kind: "acao", name: isDark() ? "Usar o tema claro" : "Usar o tema escuro", icon: iconeApp("sobre"), hint: "Como no Windows 11",
      run: () => applyTheme(!isDark(), true) },
    { kind: "acao", name: "Ver as animações do pet", icon: iconeApp("animacoes"), hint: "Lidas do animations.xml",
      run: () => { buildAnimations(); const win = windowById("animacoes"); if (win) openWindow(win); } },
    { kind: "acao", name: "Como você está hoje?", icon: iconeApp("notas"), hint: "O pet pergunta do seu humor", run: () => askMood() }
  ];
  return { apps, pets, acoes };
}

function startItem(entry, className) {
  const element = document.createElement("button");
  element.type = "button";
  element.className = className;
  const figure = document.createElement("b");
  if (entry.kind === "pet") {
    const image = document.createElement("img");
    image.src = "pets/" + entry.folder + ".gif";
    image.alt = "";
    image.loading = "lazy";
    figure.appendChild(image);
    element.setAttribute("aria-pressed", String(entry.folder === currentFolder));
  } else {
    figure.innerHTML = entry.icon;
  }
  element.appendChild(figure);
  if (className === "demo-sm-rec") {
    const text = document.createElement("span");
    const name = document.createElement("strong");
    name.textContent = entry.name;
    const hint = document.createElement("small");
    hint.textContent = entry.hint || "";
    text.append(name, hint);
    element.appendChild(text);
  } else {
    element.appendChild(document.createTextNode(entry.name));
  }
  element.title = entry.hint ? entry.name + " — " + entry.hint : entry.name;
  element.addEventListener("click", () => launch(entry.run));
  return element;
}

function buildStartMenu() {
  const { apps, pets, acoes } = startEntries();
  const pinned = document.getElementById("start-pinned");
  const petBox = document.getElementById("start-pets");
  const recs = document.getElementById("start-recs");
  pinned.replaceChildren(...apps.map((entry) => startItem(entry, "demo-sm-app")));
  petBox.replaceChildren(...pets.map((entry) => startItem(entry, "demo-sm-app")));
  recs.replaceChildren(...acoes.map((entry) => startItem(entry, "demo-sm-rec")));
}

const semAcento = (text) => text.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();

/** A pesquisa: os aplicativos, os pets e as ações que têm o texto. */
function filterStart(query) {
  const results = document.getElementById("start-results");
  const busca = semAcento(query.trim());
  const sections = startMenu.querySelectorAll(".demo-sm-head, #start-pinned, #start-pets, #start-recs");
  if (!busca) {
    results.hidden = true;
    for (const section of sections) section.hidden = false;
    return;
  }
  for (const section of sections) section.hidden = true;
  const { apps, pets, acoes } = startEntries();
  const achados = [...apps, ...pets, ...acoes].filter((entry) =>
    semAcento(entry.name + " " + (entry.hint || "")).includes(busca));
  results.hidden = false;
  if (!achados.length) {
    const vazio = document.createElement("p");
    vazio.className = "demo-sm-empty";
    vazio.textContent = "Nada encontrado para “" + query.trim() + "”. Tente “pet”, “foco” ou “tema”.";
    results.replaceChildren(vazio);
    return;
  }
  results.replaceChildren(...achados.map((entry) => startItem(entry, "demo-sm-rec")));
}

startSearch.addEventListener("input", () => filterStart(startSearch.value));
startSearch.addEventListener("keydown", (event) => {
  if (event.key === "Enter") {
    const first = startMenu.querySelector("#start-results:not([hidden]) button, #start-pinned button");
    if (first) first.click();
  } else if (event.key === "ArrowDown") {
    event.preventDefault();
    const first = startMenu.querySelector(".demo-sm-scroll button:not([hidden])");
    if (first) first.focus();
  }
});

/* --------------------------------------------- as configurações rápidas */
const QUICK_TILES = [
  { id: "som", label: () => soundOn ? "Som" : "Sem som", icon: () => glifo(soundOn ? "volume" : "mute"),
    on: () => soundOn, run: () => toggleSound() },
  { id: "tema", label: () => "Tema escuro", icon: () => glifo("moon"), on: () => isDark(),
    run: () => applyTheme(!isDark(), true) },
  { id: "parede", label: () => WALLPAPERS[wallpaper], icon: () => glifo("image"), on: () => false,
    run: () => nextWallpaper() },
  { id: "foco", label: () => pomoRunning ? "Foco ligado" : "Foco", icon: () => glifo("timer"), on: () => pomoRunning,
    run: () => { pomoStartStop(); const win = windowById("pomodoro"); if (win && pomoRunning) openWindow(win); } },
  { id: "silencio", label: () => "Não perturbe", icon: () => glifo("bell"), on: () => naoPerturbe,
    run: () => {
      naoPerturbe = !naoPerturbe;
      if (naoPerturbe) { hideTips(); hideBubble(); }
      showToast(naoPerturbe ? "Não perturbe ligado" : "Não perturbe desligado",
        naoPerturbe ? "O pet não puxa conversa sozinho. Clique nele quando quiser falar." : "O pet volta a conversar quando tiver algo a dizer.");
    } },
  { id: "dicas", label: () => "Dicas", icon: () => glifo("bulb"), on: () => !tipsBox.hidden,
    run: () => { if (tipsBox.hidden) showTip(0); else hideTips(); } }
];

function paintQuick() {
  const box = document.getElementById("quick-tiles");
  if (!box) return;
  if (!box.children.length) {
    for (const tile of QUICK_TILES) {
      const holder = document.createElement("div");
      holder.className = "demo-qs-tile";
      const button = document.createElement("button");
      button.type = "button";
      button.dataset.tile = tile.id;
      button.addEventListener("click", () => { tile.run(); paintQuick(); });
      const label = document.createElement("small");
      holder.append(button, label);
      box.appendChild(holder);
    }
  }
  for (const tile of QUICK_TILES) {
    const button = box.querySelector(`[data-tile="${tile.id}"]`);
    if (!button) continue;
    button.innerHTML = tile.icon();
    button.setAttribute("aria-pressed", String(!!tile.on()));
    button.setAttribute("aria-label", tile.label());
    button.nextElementSibling.textContent = tile.label();
  }
  const slider = document.getElementById("quick-volume");
  const out = document.getElementById("quick-volume-out");
  const icon = document.getElementById("quick-vol-icon");
  const valor = soundOn ? Math.round(volume * 100) : 0;
  if (slider && document.activeElement !== slider) slider.value = String(valor);
  if (out) out.textContent = String(valor);
  if (icon) icon.innerHTML = glifo(valor > 0 ? "volume" : "mute");
  const battery = document.getElementById("quick-battery");
  if (battery && !battery.innerHTML) battery.innerHTML = glifo("battery");
}

document.getElementById("quick-volume").addEventListener("input", (event) => {
  const valor = Number(event.target.value) / 100;
  if (valor > 0) volume = valor;
  soundOn = valor > 0;
  if (pet) pet.volume = soundOn ? volume : 0;
  paintSoundButton();
  paintTray();
  paintQuick();
});

/* ------------------------------------------------------------- o calendário */
function renderCalendar() {
  const hoje = new Date();
  const base = new Date(hoje.getFullYear(), hoje.getMonth() + calendarOffset, 1);
  const title = document.getElementById("cal-title");
  const grid = document.getElementById("cal-grid");
  const mes = base.toLocaleDateString("pt-BR", { month: "long", year: "numeric" });
  title.textContent = mes.charAt(0).toUpperCase() + mes.slice(1);
  const cells = [];
  for (const dia of ["D", "S", "T", "Q", "Q", "S", "S"]) {
    const head = document.createElement("span");
    head.className = "dow";
    head.textContent = dia;
    cells.push(head);
  }
  const inicio = new Date(base);
  inicio.setDate(1 - base.getDay());
  for (let i = 0; i < 42; i++) {
    const data = new Date(inicio);
    data.setDate(inicio.getDate() + i);
    const cell = document.createElement("span");
    cell.className = "day";
    if (data.getMonth() !== base.getMonth()) cell.classList.add("fora");
    if (data.toDateString() === hoje.toDateString()) cell.classList.add("hoje");
    cell.textContent = String(data.getDate());
    cells.push(cell);
  }
  grid.replaceChildren(...cells);
  paintCalendarFocus();
}

function paintCalendarFocus() {
  const button = document.getElementById("cal-focus");
  if (!button) return;
  button.textContent = pomoRunning ? "⏸ Pausar o foco" : (pomoLeft < pomoTotal() ? "▶ Continuar o foco" : "▶ Começar 25 min");
}

document.getElementById("cal-prev").addEventListener("click", () => { calendarOffset--; renderCalendar(); });
document.getElementById("cal-next").addEventListener("click", () => { calendarOffset++; renderCalendar(); });
document.getElementById("cal-focus").addEventListener("click", () => {
  pomoStartStop();
  paintCalendarFocus();
  const win = windowById("pomodoro");
  if (win && pomoRunning) openWindow(win);
});

/* ------------------------------------------- o menu do botão direito (e o de energia) */
function fillMenu(list, items) {
  list.replaceChildren();
  for (const item of items) {
    if (item === "-") {
      list.appendChild(document.createElement("hr"));
      continue;
    }
    const button = document.createElement("button");
    button.type = "button";
    button.setAttribute("role", "menuitem");
    button.innerHTML = item.icon ? glifo(item.icon) : "";
    button.appendChild(document.createTextNode(item.label));
    if (item.kbd) {
      const kbd = document.createElement("kbd");
      kbd.textContent = item.kbd;
      button.appendChild(kbd);
    }
    button.addEventListener("click", () => launch(item.run));
    list.appendChild(button);
  }
}

function placeMenu(list, x, y) {
  list.hidden = false;
  const width = list.offsetWidth;
  const height = list.offsetHeight;
  const left = Math.max(6, Math.min(world.width - width - 6, x));
  const top = Math.max(6, Math.min(world.height - world.taskbarHeight - height - 6, y));
  list.style.left = Math.round(left) + "px";
  list.style.top = Math.round(top) + "px";
  const first = list.querySelector("button");
  if (first) first.focus({ preventScroll: true });
}

function openDeskMenu(x, y) {
  closeFlyouts();
  closeMenu();
  fillMenu(deskMenu, [
    { icon: "refresh", label: "Atualizar", run: () => { react(REACTIONS.ola); } },
    "-",
    { icon: "image", label: "Próximo papel de parede", run: () => nextWallpaper() },
    { icon: isDark() ? "sun" : "moon", label: isDark() ? "Usar o tema claro" : "Usar o tema escuro", run: () => applyTheme(!isDark(), true) },
    "-",
    { icon: "grid", label: "Abrir o painel do DeskPals", run: () => { const win = windowById("painel"); if (win) openWindow(win); } },
    { icon: "chat", label: "Conversar com o pet", run: () => ask() },
    { icon: "bulb", label: "Mostrar as dicas", run: () => showTip(0) },
    "-",
    { icon: "download", label: "Baixar o DeskPals", run: () => window.open(DOWNLOAD, "_blank", "noopener") },
    { icon: "info", label: "Sobre a demo", run: () => { const win = windowById("sobre"); if (win) openWindow(win); } }
  ]);
  placeMenu(deskMenu, x, y);
}

function openPowerMenu() {
  const rect = document.getElementById("power-btn").getBoundingClientRect();
  const stageRect = stage.getBoundingClientRect();
  fillMenu(deskMenu, [
    { icon: "moon", label: "Suspender (o pet dorme)", run: () => petAction("dormir") },
    { icon: "refresh", label: "Reiniciar a demo", run: () => window.location.reload() },
    "-",
    { icon: "download", label: "Baixar o DeskPals de verdade", run: () => window.open(DOWNLOAD, "_blank", "noopener") }
  ]);
  deskMenu.hidden = false;
  placeMenu(deskMenu, rect.right - stageRect.left - deskMenu.offsetWidth, rect.top - stageRect.top - deskMenu.offsetHeight - 8);
}

function closeDeskMenu() {
  if (deskMenu) deskMenu.hidden = true;
}

/* ------------------------------------------------------------ a notificação */
function showToast(title, text) {
  if (!toastBox) return;
  document.getElementById("toast-icon").innerHTML = iconeApp("meupet");
  document.getElementById("toast-title").textContent = title;
  document.getElementById("toast-text").textContent = text;
  toastBox.hidden = false;
  window.clearTimeout(toastTimer);
  toastTimer = window.setTimeout(() => { toastBox.hidden = true; }, 5000);
}
toastBox.addEventListener("click", () => { toastBox.hidden = true; });

/* ------------------------------------------------------------ o "widget" */
function paintWidget() {
  if (!widgetBtn) return;
  const nome = pet && pet.header ? pet.header.name : "Pet";
  let face = "😊";
  let texto = "feliz";
  if (vpet) {
    const n = vpet.numeros;
    const menor = Math.min(n.food, n.love, n.energy);
    if (menor < 25) {
      if (n.food === menor) { face = "😋"; texto = "com fome"; }
      else if (n.love === menor) { face = "🥺"; texto = "quer carinho"; }
      else { face = "😴"; texto = "com sono"; }
    } else if (menor < 60) {
      face = "🙂"; texto = "tranquilo";
    }
  }
  document.getElementById("widget-face").textContent = face;
  document.getElementById("widget-title").textContent = nome;
  document.getElementById("widget-sub").textContent = texto;
  widgetBtn.title = nome + " está " + texto + ". Clique para abrir o painel.";
}
widgetBtn.addEventListener("click", () => {
  const win = windowById("painel");
  if (win) openWindow(win);
});

/* ------------------------------------------- a bandeja: som, rede, bateria */
function paintTray() {
  const wifi = document.getElementById("tray-wifi");
  const battery = document.getElementById("tray-battery");
  if (wifi && !wifi.innerHTML) wifi.innerHTML = glifo("wifi");
  if (battery && !battery.innerHTML) battery.innerHTML = glifo("battery");
  if (traySound) traySound.innerHTML = glifo(soundOn ? "volume" : "mute");
  quickBtn.title = "Configurações rápidas — som " + (soundOn ? "ligado" : "desligado");
}

/* -------------------------------------------- mostrar a área de trabalho */
function toggleDesktop() {
  if (desktopShown) {
    for (const win of desktopShown) if (!win.closed && win.minimized) openWindow(win);
    desktopShown = null;
    return;
  }
  desktopShown = windows.filter((win) => !win.closed && !win.minimized);
  for (const win of desktopShown) {
    win.minimized = true;
    win.gone = true;
  }
  layoutWindows();
  updateTaskbar();
}

/* ------------------------------------------------------------ os botões da barra */
startBtn.innerHTML = iconeApp("meupet");
for (const glyph of document.querySelectorAll(".demo-search-glyph")) glyph.innerHTML = glifo("search");
tipsBtn.innerHTML = glifo("bulb");
menuBtn.innerHTML = glifo("paw");
document.getElementById("power-btn").innerHTML = glifo("power");
document.querySelector(".demo-sm-avatar").innerHTML = glifo("user");
startBtn.addEventListener("click", () => openFlyout("start"));
searchBtn.addEventListener("click", () => openFlyout("start"));
quickBtn.addEventListener("click", () => openFlyout("quick"));
clock.addEventListener("click", () => openFlyout("calendar"));
showDesktopBtn.addEventListener("click", toggleDesktop);
document.getElementById("power-btn").addEventListener("click", (event) => {
  event.stopPropagation();
  if (!deskMenu.hidden) { closeDeskMenu(); return; }
  openPowerMenu();
});

/* O tema começa como o do computador (ou o escolhido da última vez). */
{
  const temaSalvo = lerGuardado(THEME_KEY);
  const escuroDoSistema = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
  applyTheme(temaSalvo ? temaSalvo === "escuro" : escuroDoSistema, false);
  applyWallpaper(Number(lerGuardado(WALL_KEY)) || 0, false);
  paintTray();
}

/* ----------------------------------------------------------------- início */
askButton.addEventListener("click", ask);
soundButton.addEventListener("click", toggleSound);
tipsClose.addEventListener("click", hideTips);
tipsBtn.addEventListener("click", () => showTip(0));
menuBtn.addEventListener("click", () => {
  // O menu do pet também é um botão: no celular e no teclado não há
  // botão direito.
  if (!pet) return;
  const state = pet.renderState();
  openMenu(state.x + state.width / 2, state.y + state.height + 8);
});
if (pomoStart) pomoStart.addEventListener("click", pomoStartStop);
if (pomoReset) pomoReset.addEventListener("click", pomoResetTimer);
if (pomoReminders) {
  for (const item of pomoReminders.querySelectorAll("button")) {
    item.addEventListener("click", () => {
      react(REACTIONS.carinho);
      say(item.dataset.text || "Anotado!");
      // A anotação vai para o bloco de notas: ela fica guardada, como
      // as notas do programa. Nada de lembrete que não existe.
      const field = document.getElementById("notes-text");
      if (field) {
        const linha = item.textContent.trim();
        if (linha && !field.value.includes(linha)) {
          const base = field.value.replace(/\s*$/, "");
          field.value = (base ? base + "\n" : "") + linha;
          field.dispatchEvent(new Event("input"));
        }
        const win = windowById("notas");
        if (win) openWindow(win);
      }
    });
  }
}
window.addEventListener("resize", () => {
  measure();
  measureBubble();
});

// O palco ocupa a janela inteira: quando ela muda de tamanho (ou quando a
// página é aberta numa aba que ainda não tem medida), o mundo é remedido.
if (window.ResizeObserver) {
  new ResizeObserver(() => measure()).observe(stage);
}

// Um gancho para quem quiser inspecionar a demo pelo console do navegador.
window.meuPetDemo = {
  get pet() { return pet; },
  world,
  windows,
  ask,
  render,
  /** Mexe nas janelas do desktop, como se fossem clicadas. */
  windowAction(id, action) {
    const win = windowById(id);
    if (win) windowAction(win, action);
  },
  raiseWindow(id) {
    const win = windowById(id);
    if (win) raiseWindow(win);
  },
  moveWindow(id, x, y) {
    const win = windowById(id);
    if (!win) return;
    win.x = x;
    win.y = y;
    layoutWindows();
  },
  /** Onde o pet está em relação às janelas (para conferir o movimento). */
  state() {
    return {
      animacao: pet ? pet.current.name : null,
      x: pet ? Math.round(pet.x) : null,
      y: pet ? Math.round(pet.y) : null,
      janela: pet && pet.window ? pet.window.id : null
    };
  },
  /** Roda a simulação por alguns segundos, para testar sem depender da animação. */
  advance: advanceSimulation
};

buildWindows();
buildIcons();
buildTaskbar();
buildPetList();
loadNotes();
measure();
tickClock();
pomoLoad();
pomoPaint();
loadPetMeta();
// As fontes do site chegam depois da página: o texto muda de tamanho
// quando elas entram, e a janela — medida antes — ficaria com o
// conteúdo cortado. Ela é medida de novo quando as fontes prontas.
if (document.fonts && document.fonts.ready) {
  document.fonts.ready.then(() => {
    layoutWindows(true);
    measure();
  });
}
// No modo foto o quadro fica parado: a imagem sai sempre igual.
requestAnimationFrame(frame);
/* Parâmetros para as fotos da documentação e para testar a página:
   ?pet=clippy (o pet que abre), ?tema=escuro|claro, ?parede=1..3 e
   ?abrir=iniciar|rapidas|calendario|menu|pet (o painel aberto na foto). */
const PARAMS = new URLSearchParams(window.location.search);
if (PARAMS.get("tema")) applyTheme(PARAMS.get("tema") === "escuro", false);
if (PARAMS.get("parede")) applyWallpaper(Number(PARAMS.get("parede")) - 1, false);
const petInicial = PETS.some((info) => info.folder === PARAMS.get("pet")) ? PARAMS.get("pet") : PETS[0].folder;
loadPet(petInicial).then(() => {
  const abrir = PARAMS.get("abrir");
  if (abrir === "iniciar") openFlyout("start");
  else if (abrir === "rapidas") openFlyout("quick");
  else if (abrir === "calendario") openFlyout("calendar");
  else if (abrir === "menu") openDeskMenu(world.width * 0.55, world.height * 0.35);
  else if (abrir === "pet" && pet) { const s = pet.renderState(); openMenu(s.x + s.width / 2, s.y + s.height + 6); }
});
showTip(0);

// O relógio se acerta com o minuto, para não atrasar na tela.
window.setTimeout(() => {
  tickClock();
  window.setInterval(tickClock, 15000);
}, 60000 - (Date.now() % 60000));
