/* ---------------------------------------------------------------
   virtual-pet.js — as necessidades do pet.

   O programa para Windows guarda três números de cada pet: comida,
   carinho e energia. Eles caem com o tempo (8, 6 e 5 pontos por
   hora) e o pet reclama quando um deles fica baixo demais, com um
   botão para resolver. Aqui é a mesma coisa, portada para a demo.

   A diferença é o relógio: na demo o tempo passa 20 vezes mais
   rápido, senão o visitante ficaria olhando três barras paradas
   durante meia hora. O programa de verdade usa a velocidade normal.
   --------------------------------------------------------------- */

/* As animações de cada ação, por pet — as mesmas do programa
   (src/Portable/PetActions.cs). O primeiro nome que o pet conhece
   é o que ele faz; a lista comum entra atrás, para nenhum pet
   ficar mudo. */
const BY_PET = {
  rex: {
    comer: ["eat meat", "pizza", "popcorn", "drink"],
    carinho: ["in love", "love", "tail wag", "wink"],
    brincar: ["soccer", "dance", "juggle", "basketball", "guitar", "party"],
    dormir: ["sleep"]
  },
  pingo: {
    comer: ["eat fish", "hold fish"],
    carinho: ["love", "blow kiss", "giggle"],
    brincar: ["dance", "ice skate", "juggle fish", "belly drum", "hop hop"],
    dormir: ["sleep"]
  },
  boo: {
    comer: ["ice cream", "bubble gum", "jelly"],
    carinho: ["hearts", "wink kiss", "blow kiss", "giggle"],
    brincar: ["yo-yo", "dance", "juggle", "spin", "bounce"],
    dormir: ["sleep"]
  },
  mago: {
    comer: ["potion", "brew"],
    carinho: ["rabbit", "owl", "bow"],
    brincar: ["juggle", "fireworks", "dance", "bubbles"],
    dormir: ["sleep"]
  },
  nuvi: {
    comer: ["coffee", "download"],
    carinho: ["love", "happy", "wink"],
    brincar: ["dance", "matrix", "happy"],
    dormir: ["sleep"]
  },
  claude: {
    comer: ["eat", "coffee", "chef"],
    carinho: ["flower", "wave"],
    brincar: ["dance", "skate", "music", "confetti", "spin"],
    dormir: ["sleep"]
  },
  esheep64: {
    comer: ["eat"],
    carinho: ["flower", "boing"],
    brincar: ["jump", "boing", "run"],
    dormir: ["sleep1a", "sleep2a", "sleep3a"]
  }
};

/* Para os pets de outros autores: os nomes de sempre. */
const COMMON = {
  comer: ["eat", "eating", "drink", "food", "eat fish", "eat meat", "pizza", "cookie"],
  carinho: ["love", "in love", "hearts", "happy", "purr", "wave", "hello"],
  brincar: ["play", "dance", "jump", "spin", "juggle", "run", "cheer"],
  dormir: ["sleep", "sleeping", "nap", "sleep1a", "rest", "yawn"]
};

/** Os nomes de animação de uma ação, para este pet. */
export function actionNames(act, folder) {
  const own = (BY_PET[folder] && BY_PET[folder][act]) || [];
  return own.concat(COMMON[act] || []).filter((name, index, all) => all.indexOf(name) === index);
}

const CHAVE = "meupet.demo.necessidades";

/* Quantas vezes mais rápido o tempo passa na demo. */
const VELOCIDADE = 20;

export class VirtualPet {
  /**
   * @param {string} folder a pasta do pet (rex, claude, esheep64…)
   * @param {(estado: object) => void} aoMudar chamado quando os números mudam
   */
  constructor(folder, aoMudar) {
    this.folder = folder;
    this.aoMudar = aoMudar || (() => {});
    this.numeros = { food: 100, love: 100, energy: 100 };
    this.atualizadoEm = Date.now();
    this.ultimaReclamacao = 0;
    this.ultimoSave = 0;
    this.carregar();
    this.aplicarDecaimento();
  }

  /* ---------------------------------------------------------- guardar */
  carregar() {
    try {
      const guardado = JSON.parse(window.localStorage.getItem(CHAVE) || "{}");
      const um = guardado[this.folder];
      if (um) {
        this.numeros = { food: um.food, love: um.love, energy: um.energy };
        this.atualizadoEm = um.atualizadoEm || Date.now();
        this.ultimaReclamacao = um.ultimaReclamacao || 0;
      }
    } catch (error) {
      /* sem armazenamento local: as necessidades valem só nesta visita */
    }
  }

  /** Guarda os números. O tique de um segundo chama isto a toda hora,
      e escrever no armazenamento localtantas vezes por segundo é
      exagero: o save é adiado, e as ações forçam. */
  guardar(forcar = false) {
    const agora = Date.now();
    if (!forcar && agora - this.ultimoSave < 20000) return;
    this.ultimoSave = agora;
    try {
      const guardado = JSON.parse(window.localStorage.getItem(CHAVE) || "{}");
      guardado[this.folder] = {
        food: this.numeros.food,
        love: this.numeros.love,
        energy: this.numeros.energy,
        atualizadoEm: this.atualizadoEm,
        ultimaReclamacao: this.ultimaReclamacao
      };
      window.localStorage.setItem(CHAVE, JSON.stringify(guardado));
    } catch (error) {
      /* idem */
    }
  }

  /* ------------------------------------------------------- decaimento
     O mesmo do programa (VirtualPet.Decay): comida cai 8 pontos por
     hora, carinho 6 e energia 5. Se a página ficou fechada por mais
     de meia hora, o pet não passa fome: o piso é 10.               */
  aplicarDecaimento() {
    const agora = Date.now();
    const minutosReais = Math.max(0, (agora - this.atualizadoEm) / 60000);
    const minutos = minutosReais * VELOCIDADE;
    this.atualizadoEm = agora;

    // Como no programa (VirtualPet.Change), os números ficam entre 0 e
    // 100: dar comida três vezes não empurra a barra para fora.
    this.numeros.food = Math.max(0, Math.min(100, this.numeros.food));
    this.numeros.love = Math.max(0, Math.min(100, this.numeros.love));
    this.numeros.energy = Math.max(0, Math.min(100, this.numeros.energy));

    if (minutos <= 0) return;

    // O piso de 10 vale para quem ficou muito tempo fora, e esse tempo
    // é o de verdade: meia hora com a página fechada. Só a queda dos
    // números corre no relógio adiantado da demo.
    const longe = minutosReais > 30;
    const baixa = (valor, porHora) => {
      const resultado = Math.max(0, valor - porHora * minutos / 60);
      return longe ? Math.max(Math.min(valor, 10), resultado) : resultado;
    };

    this.numeros.food = baixa(this.numeros.food, 8);
    this.numeros.love = baixa(this.numeros.love, 6);
    this.numeros.energy = baixa(this.numeros.energy, 5);
    this.guardar();
  }

  /** As barras, no formato do programa: "🍖 80%   ❤️ 65%   ⚡ 40%". */
  bars() {
    const pct = (value) => Math.round(Math.max(0, Math.min(100, value))) + "%";
    return `🍖 ${pct(this.numeros.food)}   ❤️ ${pct(this.numeros.love)}   ⚡ ${pct(this.numeros.energy)}`;
  }

  /** O que está faltando: comida, carinho, energia — ou nada. */
  problema() {
    if (this.numeros.food < 25) return "hungry";
    if (this.numeros.love < 25) return "lonely";
    if (this.numeros.energy < 20) return "sleepy";
    return null;
  }

  /** O pet reclama sozinho, no máximo uma vez por hora. */
  reclamar() {
    const agora = Date.now();
    if (agora - this.ultimaReclamacao < 3600000) return null;
    const problema = this.problema();
    if (!problema) return null;
    this.ultimaReclamacao = agora;
    this.guardar(true);
    return problema;
  }

  /* --------------------------------------------------------- ações */
  darComida() {
    this.numeros.food += 35;
    this.numeros.energy += 5;
    this.aplicarDecaimento();
    this.aoMudar(this.numeros);
    this.guardar(true);
  }

  fazerCarinho() {
    this.numeros.love += 30;
    this.aplicarDecaimento();
    this.aoMudar(this.numeros);
    this.guardar(true);
  }

  brincar() {
    this.numeros.love += 15;
    this.numeros.energy -= 12;
    this.numeros.food -= 5;
    this.aplicarDecaimento();
    this.aoMudar(this.numeros);
    this.guardar(true);
  }

  dormir() {
    this.numeros.energy += 45;
    this.aplicarDecaimento();
    this.aoMudar(this.numeros);
    this.guardar(true);
  }
}

/* O que o pet diz quando está com fome, frio ou cansado — as frases do
   programa (src/lang/pt-BR.json). */
export const RECLAMACOES = {
  hungry: {
    texto: "🍖 Meu stomach está vazio…",
    acao: "🍖 Dar comida",
    later: "Depois"
  },
  lonely: {
    texto: "❤️ Eu senti sua falta!",
    acao: "❤️ Fazer carinho",
    later: "Depois"
  },
  sleepy: {
    texto: "😴 Eu preciso dormir…",
    acao: "😴 Deitar",
    later: "Depois"
  }
};

/* O que o pet responde depois da ação. */
export const RESPOSTAS = {
  comer: "🍖 Obrigado! Fiquei com a barriga cheia.",
  carinho: "❤️ Foi bom! Me faz isso de novo qualquer hora.",
  brincar: "🎉 Que divertido! Vamos jogar mais vezes!",
  dormir: "😴 Que soneca boa! Volto já já."
};
