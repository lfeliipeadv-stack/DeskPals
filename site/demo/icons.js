/* ---------------------------------------------------------------
   Os ícones da área de trabalho, no jeito do Windows 11.

   Desenhos próprios do site, em SVG direto na página (nenhum arquivo
   novo para publicar): formas arredondadas, degradê suave e uma luz
   em cima, como os ícones coloridos do Windows 11. Os ícones da barra
   de tarefas e dos menus são traços finos (os "glifos"), que pegam a
   cor do texto e funcionam no tema claro e no escuro.
   --------------------------------------------------------------- */

let uid = 0;

/** Um degradê com nome único: a mesma página tem o mesmo ícone várias
    vezes (na área de trabalho, na barra de tarefas, no Iniciar). */
function grad(a, b, angle = 90) {
  const id = "g" + (++uid);
  const rad = (angle * Math.PI) / 180;
  const x2 = (0.5 + Math.cos(rad) * 0.5).toFixed(2);
  const y2 = (0.5 + Math.sin(rad) * 0.5).toFixed(2);
  const x1 = (1 - x2).toFixed(2);
  const y1 = (1 - y2).toFixed(2);
  return {
    id,
    def: `<linearGradient id="${id}" x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}"><stop offset="0" stop-color="${a}"/><stop offset="1" stop-color="${b}"/></linearGradient>`,
    url: `url(#${id})`
  };
}

const svg = (body, defs = "") =>
  `<svg viewBox="0 0 48 48" aria-hidden="true" focusable="false">${defs ? "<defs>" + defs + "</defs>" : ""}${body}</svg>`;

/* Os ícones coloridos: um por janela, mais o download e o Meu Pet. */
const DESENHOS = {
  /* o painel: uma janela com a pata do pet */
  painel() {
    const fundo = grad("#3ccf8e", "#14794a", 100);
    const barra = grad("#ffffff", "#e6f4ec", 90);
    return svg(
      `<rect x="5" y="7" width="38" height="34" rx="7" fill="${fundo.url}"/>
       <rect x="5" y="7" width="38" height="9" rx="7" fill="#0f5c38" opacity=".35"/>
       <circle cx="11" cy="11.6" r="1.6" fill="#fff" opacity=".85"/><circle cx="16" cy="11.6" r="1.6" fill="#fff" opacity=".6"/>
       <g fill="${barra.url}">
         <ellipse cx="24" cy="31" rx="7" ry="5.6"/>
         <circle cx="16.4" cy="24.2" r="2.7"/><circle cx="21.2" cy="20.6" r="2.7"/>
         <circle cx="26.8" cy="20.6" r="2.7"/><circle cx="31.6" cy="24.2" r="2.7"/>
       </g>`,
      fundo.def + barra.def);
  },
  /* o Pomodoro: um tomate com o relógio */
  pomodoro() {
    const fruto = grad("#ff7a5c", "#d62e1f", 110);
    return svg(
      `<circle cx="24" cy="27" r="16" fill="${fruto.url}"/>
       <ellipse cx="18" cy="20" rx="5" ry="3" fill="#fff" opacity=".28"/>
       <path d="M24 12c-3-3-7-3-9-1 3 0 5 1 6 3-3 0-6 1-7 3 3-1 6-1 8 0 1-2 2-3 2-5z" fill="#38b26b"/>
       <path d="M24 12c3-3 7-3 9-1-3 0-5 1-6 3 3 0 6 1 7 3-3-1-6-1-8 0z" fill="#2b9a59"/>
       <circle cx="24" cy="28" r="8.5" fill="#fff" opacity=".95"/>
       <path d="M24 23v5.4l3.4 2.2" fill="none" stroke="#c62a1c" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>`,
      fruto.def);
  },
  /* o bloco de notas: a folha com as linhas */
  notas() {
    const folha = grad("#ffffff", "#e8eef7", 90);
    const topo = grad("#4f9cff", "#1f63d8", 0);
    return svg(
      `<rect x="9" y="5" width="30" height="38" rx="5" fill="${folha.url}" stroke="#c6d3e6"/>
       <rect x="9" y="5" width="30" height="9" rx="5" fill="${topo.url}"/>
       <rect x="9" y="10" width="30" height="4" fill="${topo.url}"/>
       <g stroke="#9aaecb" stroke-width="2" stroke-linecap="round"><path d="M15 21h18M15 27h18M15 33h12"/></g>
       <path d="M33 30l6-6 3 3-6 6-4 1z" fill="#ffb02e" stroke="#c97c00" stroke-width=".8"/>`,
      folha.def + topo.def);
  },
  /* as animações: a película com o quadro */
  animacoes() {
    const fundo = grad("#a974ff", "#5b2fd0", 110);
    return svg(
      `<rect x="5" y="9" width="38" height="30" rx="6" fill="${fundo.url}"/>
       <g fill="#fff" opacity=".85">
         <rect x="8" y="12" width="4" height="3" rx="1"/><rect x="8" y="19" width="4" height="3" rx="1"/>
         <rect x="8" y="26" width="4" height="3" rx="1"/><rect x="8" y="33" width="4" height="3" rx="1"/>
         <rect x="36" y="12" width="4" height="3" rx="1"/><rect x="36" y="19" width="4" height="3" rx="1"/>
         <rect x="36" y="26" width="4" height="3" rx="1"/><rect x="36" y="33" width="4" height="3" rx="1"/>
       </g>
       <path d="M20 17.5v13l11-6.5z" fill="#fff"/>`,
      fundo.def);
  },
  /* sobre a demo: o círculo com o "i" */
  sobre() {
    const fundo = grad("#43b6ff", "#0a6fd6", 110);
    return svg(
      `<circle cx="24" cy="24" r="18" fill="${fundo.url}"/>
       <ellipse cx="18" cy="15" rx="7" ry="4" fill="#fff" opacity=".22"/>
       <circle cx="24" cy="15.5" r="2.6" fill="#fff"/>
       <rect x="21.6" y="20.5" width="4.8" height="14" rx="2.4" fill="#fff"/>`,
      fundo.def);
  },
  /* baixar: a seta para o disco */
  download() {
    const fundo = grad("#3ccf8e", "#11804c", 110);
    return svg(
      `<rect x="6" y="6" width="36" height="36" rx="10" fill="${fundo.url}"/>
       <path d="M24 13v15m-7-7 7 7 7-7" fill="none" stroke="#fff" stroke-width="3.4" stroke-linecap="round" stroke-linejoin="round"/>
       <rect x="14" y="32" width="20" height="3.4" rx="1.7" fill="#fff"/>`,
      fundo.def);
  },
  /* a página do Meu Pet: o globo */
  site() {
    const fundo = grad("#4fc3f7", "#1565c0", 110);
    return svg(
      `<circle cx="24" cy="24" r="18" fill="${fundo.url}"/>
       <g fill="none" stroke="#fff" stroke-width="2" opacity=".92">
         <ellipse cx="24" cy="24" rx="8" ry="18"/><path d="M6 24h36M9 15h30M9 33h30"/>
       </g>`,
      fundo.def);
  },
  /* o manual: o livro */
  wiki() {
    const capa = grad("#ffb74d", "#e65100", 110);
    return svg(
      `<path d="M8 10c5-2 11-2 16 1v29c-5-3-11-3-16-1z" fill="${capa.url}"/>
       <path d="M40 10c-5-2-11-2-16 1v29c5-3 11-3 16-1z" fill="#ffcc80"/>
       <path d="M24 11v29" stroke="#bf5a00" stroke-width="1.6"/>
       <g stroke="#fff" stroke-width="1.6" stroke-linecap="round" opacity=".85"><path d="M12 17c3-1 6-1 8 0M12 22c3-1 6-1 8 0M12 27c3-1 6-1 8 0"/></g>`,
      capa.def);
  },
  /* o Meu Pet, para o botão Iniciar e o canto do menu */
  meupet() {
    const fundo = grad("#3ccf8e", "#14794a", 120);
    return svg(
      `<rect x="4" y="4" width="40" height="40" rx="11" fill="${fundo.url}"/>
       <g fill="#fff">
         <ellipse cx="24" cy="30" rx="8" ry="6.4"/>
         <circle cx="15.2" cy="22.2" r="3.1"/><circle cx="20.8" cy="17.8" r="3.1"/>
         <circle cx="27.2" cy="17.8" r="3.1"/><circle cx="32.8" cy="22.2" r="3.1"/>
       </g>`,
      fundo.def);
  }
};

/** O ícone colorido de uma janela (ou "download", "site", "wiki", "meupet"). */
export function iconeApp(nome) {
  const desenho = DESENHOS[nome];
  return desenho ? desenho() : "";
}

/* Os glifos: traços de 1,5 px que pegam a cor do texto (currentColor). */
const G = (body, extra = "") =>
  `<svg viewBox="0 0 20 20" aria-hidden="true" focusable="false" fill="none" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round"${extra}>${body}</svg>`;

const GLIFOS = {
  search: G('<circle cx="8.5" cy="8.5" r="5.5"/><path d="M12.6 12.6 17 17"/>'),
  wifi: G('<path d="M2.5 8.2a11 11 0 0 1 15 0M5 11a7.2 7.2 0 0 1 10 0M7.6 13.8a3.4 3.4 0 0 1 4.8 0"/><circle cx="10" cy="16.2" r=".9" fill="currentColor" stroke="none"/>'),
  volume: G('<path d="M3 7.5h3l4-3.5v12l-4-3.5H3z"/><path d="M13 7.2a4 4 0 0 1 0 5.6M15.2 5a7 7 0 0 1 0 10"/>'),
  mute: G('<path d="M3 7.5h3l4-3.5v12l-4-3.5H3z"/><path d="M13.5 7.8l4 4.4M17.5 7.8l-4 4.4"/>'),
  battery: G('<rect x="2.5" y="6" width="13.5" height="8" rx="2"/><path d="M17.5 8.6v2.8"/><rect x="4.2" y="7.7" width="10" height="4.6" rx="1" fill="currentColor" stroke="none"/>'),
  chevronUp: G('<path d="M5 12.5l5-5 5 5"/>'),
  chevronRight: G('<path d="M7.5 5l5 5-5 5"/>'),
  bell: G('<path d="M5 13.5V9a5 5 0 0 1 10 0v4.5l1.5 2h-13z"/><path d="M8.3 17.2a1.9 1.9 0 0 0 3.4 0"/>'),
  bulb: G('<path d="M7 13.5a5.5 5.5 0 1 1 6 0v1.8H7z"/><path d="M7.8 17.5h4.4"/>'),
  paw: G('<ellipse cx="10" cy="13.2" rx="3.6" ry="2.9"/><circle cx="4.9" cy="9" r="1.5"/><circle cx="7.9" cy="5.6" r="1.5"/><circle cx="12.1" cy="5.6" r="1.5"/><circle cx="15.1" cy="9" r="1.5"/>'),
  power: G('<path d="M10 3v6.5"/><path d="M6 5.6a6 6 0 1 0 8 0"/>'),
  moon: G('<path d="M15.5 12.6A6.5 6.5 0 0 1 7.4 4.5a6.5 6.5 0 1 0 8.1 8.1z"/>'),
  sun: G('<circle cx="10" cy="10" r="3.4"/><path d="M10 2.5v1.8M10 15.7v1.8M2.5 10h1.8M15.7 10h1.8M4.7 4.7l1.3 1.3M14 14l1.3 1.3M4.7 15.3 6 14M14 6l1.3-1.3"/>'),
  timer: G('<circle cx="10" cy="11" r="6.3"/><path d="M10 7.6V11l2.2 1.6M8 2.8h4"/>'),
  image: G('<rect x="2.5" y="3.5" width="15" height="13" rx="2.5"/><circle cx="7" cy="8" r="1.5"/><path d="M2.8 14.5l4.4-4 3.4 3 2.4-2 4.4 3.8"/>'),
  refresh: G('<path d="M16 10a6 6 0 1 1-1.8-4.3M16 3.5v3.2h-3.2"/>'),
  info: G('<circle cx="10" cy="10" r="7.2"/><path d="M10 9v5"/><circle cx="10" cy="6.5" r=".8" fill="currentColor" stroke="none"/>'),
  download: G('<path d="M10 3v9.5M6 8.8l4 4 4-4M4 16.5h12"/>'),
  grid: G('<rect x="3" y="3" width="5.6" height="5.6" rx="1.4"/><rect x="11.4" y="3" width="5.6" height="5.6" rx="1.4"/><rect x="3" y="11.4" width="5.6" height="5.6" rx="1.4"/><rect x="11.4" y="11.4" width="5.6" height="5.6" rx="1.4"/>'),
  chat: G('<path d="M3.5 5.5a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v6.5a2 2 0 0 1-2 2H9l-3.8 3v-3h.3a2 2 0 0 1-2-2z"/>'),
  settings: G('<circle cx="10" cy="10" r="2.6"/><path d="M10 2.6l1.3 1.9 2.2-.6.4 2.3 2.2.8-.9 2.1 1.4 1.9-2 1.2.1 2.3-2.3.1-1.1 2-1.9-1.3-1.9 1.3-1.1-2-2.3-.1.1-2.3-2-1.2 1.4-1.9-.9-2.1 2.2-.8.4-2.3 2.2.6z"/>'),
  user: G('<circle cx="10" cy="7" r="3.4"/><path d="M3.6 17a6.4 6.4 0 0 1 12.8 0"/>'),
  close: G('<path d="M5 5l10 10M15 5L5 15"/>', ' stroke-width="1.2"'),
  minimize: G('<path d="M5 10h10"/>', ' stroke-width="1.1"'),
  maximize: G('<rect x="5" y="5" width="10" height="10" rx="1.6"/>', ' stroke-width="1.1"'),
  restore: G('<rect x="5" y="7" width="8" height="8" rx="1.4"/><path d="M7.5 5h5.6A1.9 1.9 0 0 1 15 6.9v5.6"/>', ' stroke-width="1.1"')
};

/** Um glifo da barra, dos menus ou dos botões da janela. */
export function glifo(nome) {
  return GLIFOS[nome] || "";
}
