/* ---------------------------------------------------------------
   verificar-demo.js — a conferência da página da demo.

   A página é feita de arquivos soltos (o HTML, o CSS, os módulos do
   navegador, as fontes, os XML dos pets). Um arquivo que falta ou um
   JavaScript com erro de escrita só aparece quando alguém abre a
   página — e a página quebrada já está no ar. Este script roda no
   workflow que publica, antes do deploy, e barra a publicação se
   algo estiver faltando.

   Uso: node site/verificar-demo.js _site
   --------------------------------------------------------------- */

const fs = require("fs");
const os = require("os");
const path = require("path");
const { execFileSync } = require("child_process");

const raiz = process.argv[2] || "_site";
const pagina = path.join(raiz, "demo.html");

let problemas = 0;

const falha = (mensagem) => {
  problemas++;
  console.error("✖ " + mensagem);
};

/* 1. A página existe? */
if (!fs.existsSync(pagina)) {
  falha("a página da demo não está em " + pagina);
  process.exit(1);
}

const html = fs.readFileSync(pagina, "utf8");

/* 2. Todo arquivo que a página aponta tem que estar na pasta montada.
      Só os caminhos locais: os endereços da internet não são da conta
      do repositório. */
const referencias = [];
const padrao = /(?:src|href)\s*=\s*"([^"]+)"/g;
let achado;
while ((achado = padrao.exec(html)) !== null) {
  const alvo = achado[1];
  if (!alvo || alvo.startsWith("http") || alvo.startsWith("//") ||
      alvo.startsWith("data:") || alvo.startsWith("#") || alvo.startsWith("mailto:")) continue;
  referencias.push(alvo.split("#")[0].split("?")[0]);
}

/* Os arquivos que o JavaScript da demo busca em tempo de execucao. */
const extras = [
  "pets.json",
  "pets/claude/animations.xml",
  "pets/esheep64/animations.xml",
  "pets/rex/animations.xml",
  "pets/boo/animations.xml",
  "pets/pingo/animations.xml",
  "pets/mago/animations.xml",
  "pets/nuvi/animations.xml",
  "pets/clippy/animations.xml"
];

for (const rel of referencias.concat(extras)) {
  if (!rel) continue;
  const destino = path.join(raiz, rel);
  if (!fs.existsSync(destino)) {
    falha("falta o arquivo " + rel + " (a página aponta para ele)");
  }
}

/* 3. As fontes: o CSS aponta para elas com url(), e elas não estão na
      lista do HTML. */
const css = ["style.css", "demo/demo.css"];
for (const folha of css) {
  const caminho = path.join(raiz, folha);
  if (!fs.existsSync(caminho)) {
    falha("falta a folha de estilo " + folha);
    continue;
  }
  const texto = fs.readFileSync(caminho, "utf8");
  const fontes = /url\(\s*["']?([^"')]+)["']?\s*\)/g;
  let url;
  while ((url = fontes.exec(texto)) !== null) {
    const alvo = url[1];
    if (!alvo || alvo.startsWith("http") || alvo.startsWith("data:")) continue;
    const destino = path.join(path.dirname(caminho), alvo.split("?")[0]);
    if (!fs.existsSync(destino)) {
      falha("falta o arquivo " + alvo + " (o CSS aponta para ele)");
    }
  }
}

/* 4. O JavaScript tem que passar no "node --check": um erro de escrita
      em um módulo só apareceria no navegador, com a página meio
      carregada. Os módulos do navegador usam import/export, e o
      "node --check" só entende disso em arquivos .mjs — por isso a
      cópia temporária. */
const modulos = [];
const pasta = path.join(raiz, "demo");
if (fs.existsSync(pasta)) {
  for (const arquivo of fs.readdirSync(pasta)) {
    if (arquivo.endsWith(".js")) modulos.push(path.join(pasta, arquivo));
  }
}
if (!modulos.length) falha("nenhum módulo do demo em " + pasta);

const temporario = path.join(os.tmpdir(), "verificar-demo-" + process.pid);
fs.mkdirSync(temporario, { recursive: true });

for (const modulo of modulos) {
  const copia = path.join(temporario, path.basename(modulo).replace(/\.js$/, ".mjs"));
  fs.copyFileSync(modulo, copia);
  try {
    execFileSync(process.execPath, ["--check", copia], { stdio: "pipe" });
  } catch (erro) {
    const saida = (erro.stderr || Buffer.from("")).toString().trim().split("\n")[0];
    falha("erro de escrita em " + path.basename(modulo) + ": " + saida);
  }
}

fs.rmSync(temporario, { recursive: true, force: true });

if (problemas) {
  console.error("\n" + problemas + " problema(s) na página da demo: a publicação foi barrada.");
  process.exit(1);
}

console.log("página da demo conferida: " + referencias.length + " referências, " +
            modulos.length + " módulos, tudo no lugar.");
