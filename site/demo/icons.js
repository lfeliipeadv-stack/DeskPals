/* ---------------------------------------------------------------
   Os ícones da área de trabalho.

   O Windows 95 usava desenhos de 16×16 pixels, e não emoji: cada
   caractere da arte abaixo é um pixel, e a letra diz a cor. O desenho
   sai em SVG, direto na página — nenhum arquivo novo para publicar.

   A arte é do próprio site: dá para trocar um desenho por outro sem
   mexer em mais nada.
   --------------------------------------------------------------- */

/* Cada caractere da arte é um pixel; a letra é a cor. */
const PALETA = {
  W: "#f7f2e7",   // lã
  L: "#5a5044",   // pata
  E: "#2a2622",   // olho
  O: "#2b6ea8",   // aro do relógio
  F: "#f2f2ee",   // mostrador
  K: "#1c1c1c",   // ponteiros
  P: "#fbf7e4",   // papel
  R: "#b9443c",   // capa do caderno
  T: "#9fb4c7",   // linha escrita
  Q: "#f4f4f4",   // etiqueta do disquete
  B: "#3b3b3b",   // filete
  H: "#8b8b8b",   // furo do filete
  A: "#f0e2b4",   // quadro do filete
  Y: "#f2c744",   // lâmpada
  G: "#9aa0a6",   // base da lâmpada
  D: "#2b3f7a",   // disquete
  S: "#b9c0c8",   // metal do disquete
};

const ARTES = {
  // O painel do pet: a ovelha, de frente.
  painel: [
    "................",
    ".....WWWWWW.....",
    "...WWWWWWWWWW...",
    "..WWWWWWWWWWWW..",
    "..WWWWWWWWWWWW..",
    "..WWFFFFFFFFWW..",
    "..WWFEEFFEEFWW..",
    "..WWFEEFFEEFWW..",
    "..WWFFFFFFFFWW..",
    "..WWWWWWWWWWWW..",
    "...WWWWWWWWWW...",
    "....WWWWWWWW....",
    ".....WWWWWW.....",
    "..LLL......LLL..",
    "..LLL......LLL..",
    "................"
  ],

  // O Pomodoro: o relógio de borda azul, com os ponteiros finos.
  pomodoro: [
    "................",
    ".....OOOOOO.....",
    "...OOFFFFFFOO...",
    "..OFFFFKKKFFFO..",
    "..OFFFFKFFFFFO..",
    "..OFFFFKFFFFFO..",
    "..OFFFFKFFFFFO..",
    "..OFFFFFFFFFFO..",
    "..OFFFFFFFFFFO..",
    "..OFFFFFFFFFFO..",
    "...OOFFFFFFOO...",
    ".....OOOOOO.....",
    "................",
    "................",
    "................",
    "................"
  ],

  // O bloco de notas: o caderno de capa vermelha.
  notas: [
    "................",
    "..RRRRRRRRRRRR..",
    "..PPPPPPPPPPPP..",
    "..PPPPPPPPPPPP..",
    "..PTTTTTTTTTTP..",
    "..PTTTTTTTTTTP..",
    "..PPPPPPPPPPPP..",
    "..PTTTTTTTTTTP..",
    "..PTTTTTTTTTTP..",
    "..PPPPPPPPPPPP..",
    "..PTTTTTTTTTTP..",
    "..PTTTTTTTTTTP..",
    "..PPPPPPPPPPPP..",
    "..PPPPPPPPPPPP..",
    "................",
    "................"
  ],

  // As animações: um filete de filme.
  animacoes: [
    "................",
    ".BBBBBBBBBBBBBB.",
    ".BHHBBBHHBBBHHB.",
    ".BBBBBBBBBBBBBB.",
    ".BAAAAAAAAAAAAAB",
    ".BAAHHHHHHHHAAAB",
    ".BAAHHHHHHHHAAAB",
    ".BAAAAAAAAAAAAAB",
    ".BBBBBBBBBBBBBB.",
    ".BHHBBBHHBBBHHB.",
    ".BBBBBBBBBBBBBB.",
    "................",
    "................",
    "................",
    "................",
    "................"
  ],

  // A Sobre: a lâmpada.
  sobre: [
    "................",
    ".....YYYY.......",
    "....Y....Y......",
    "...Y......Y.....",
    "..Y........Y....",
    "..Y........Y....",
    "...Y......Y.....",
    "....Y....Y......",
    ".....YYYY.......",
    ".....YYYY.......",
    "....GGGGGG......",
    "...GGGGGGGG.....",
    "................",
    "................",
    "................",
    "................"
  ],

  // O atalho do download: um disquete de 3½ polegadas.
  download: [
    "................",
    ".DDDDDDDDDDDDDD.",
    ".DSSSSSSSSSSSSD.",
    ".DSSSSSSSSSSSSD.",
    ".DDDDDDDDDDDDDD.",
    ".DQQQQQQQQQQQQD.",
    ".DQQQQQQQQQQQQD.",
    ".DQQQQQQQQQQQQD.",
    ".DQQQQQQQQQQQQD.",
    ".DQQQQQQQQQQQQD.",
    ".DDDDDDDDDDDDDD.",
    ".DDDDDDDDDDDDDD.",
    ".DDDDDDDDDDDDDD.",
    "................",
    "................",
    "................"
  ]
};

/** Desenha a arte: cada caractere vira um quadradinho. */
export function iconePixels(nome) {
  const arte = ARTES[nome];
  if (!arte) return "";
  let svg = '<svg viewBox="0 0 16 16" width="16" height="16" shape-rendering="crispEdges" ' +
            'aria-hidden="true" focusable="false">';
  for (let y = 0; y < arte.length; y++) {
    const linha = arte[y];
    for (let x = 0; x < linha.length; x++) {
      const cor = PALETA[linha[x]];
      if (!cor) continue;
      svg += `<rect x="${x}" y="${y}" width="1" height="1" fill="${cor}"/>`;
    }
  }
  return svg + "</svg>";
}
