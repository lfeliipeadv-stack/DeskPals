using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopPet
{
	/// <summary>
	/// Names of the animations in the language in use, for the animation picker: the pet files name them in English
	/// (and sometimes technically: "walk_task2", "pissa"). A name without a translation is shown as it is in the file.
	/// </summary>
	internal static class AnimNames
	{
		/// <summary>The name to show for an animation, in the language in use.</summary>
		public static string Show(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return name;
			int col = Lang.Current.StartsWith("pt", StringComparison.OrdinalIgnoreCase) ? 1 : Lang.Current.StartsWith("es", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
			if (col == 0) return name;
			if (Table.Value.TryGetValue(name.Trim(), out var t)) return t[col - 1];
			// "look left return", "idle 1 2", "idle(3)": the known name, then what follows it
			var (stem, suffixes, number) = Split(name);
			if (stem == null) return name;
			string text = Table.Value[stem][col - 1];
			foreach (var s in suffixes) text += " (" + Suffix[s][col - 1] + ")";
			return number.Length > 0 ? text + " " + number : text;
		}

		/// <summary>True when the name has a translation (the self-test lists the ones of the gallery that do not).</summary>
		public static bool Has(string name) => !string.IsNullOrWhiteSpace(name) && (Table.Value.ContainsKey(name.Trim()) || Split(name).stem != null);

		/// <summary>Endings that keep the meaning of the name before them (the Office assistants have many).</summary>
		static readonly Dictionary<string, string[]> Suffix = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
		{
			["return"] = new[] { "volta", "vuelta" },
			["continued"] = new[] { "continua", "sigue" },
			["blink"] = new[] { "piscando", "parpadeando" },
		};

		/// <summary>
		/// A name made of a known name, then "return", "continued" or "blink", then a number or a letter
		/// ("look left blink", "idle 1 2", "idle tail wag b", "idle(4)"). The stem is null when it is not one.
		/// </summary>
		static (string stem, List<string> suffixes, string number) Split(string name)
		{
			var words = name.Trim().Replace("(", " ").Replace(")", " ").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			int end = words.Length;
			var number = new List<string>();
			while (end > 1 && (words[end - 1].All(char.IsDigit) || (words[end - 1].Length == 1 && char.IsLetter(words[end - 1][0])))) number.Insert(0, words[--end]);
			var suffixes = new List<string>();
			while (end > 1 && Suffix.ContainsKey(words[end - 1])) suffixes.Insert(0, words[--end]);
			if (end == words.Length) return (null, null, null);
			string stem = string.Join(" ", words, 0, end);
			return Table.Value.ContainsKey(stem) ? (stem, suffixes, string.Join(".", number)) : (null, null, null);
		}

		static readonly Lazy<Dictionary<string, string[]>> Table = new Lazy<Dictionary<string, string[]>>(() =>
		{
			var d = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
			foreach (var line in Names.Split('\n'))
			{
				var p = line.Split('|');
				if (p.Length == 3) d[p[0].Trim()] = new[] { p[1].Trim(), p[2].Trim() };
			}
			return d;
		});

		// name in the file | Português | Español
		const string Names = @"
alien_spawn|perseguição do ET|persecución del ET
angry|bravo|enojado
arcane fireball|bola de fogo arcana|bola de fuego arcana
astronaut|astronauta|astronauta
aurora|aurora boreal|aurora boreal
baby dragon|dragãozinho|dragoncito
baby ghost|fantasminha bebê|fantasmita bebé
backfire|feitiço que dá errado|hechizo que sale mal
balloon|balão|globo
basketball|basquete|baloncesto
bat|morcego|murciélago
bat on head|morcego na cabeça|murciélago en la cabeza
bat perch|morcego pendurado|murciélago colgado
bath_start|banho de banheira|baño en la bañera
batha|banho|baño
bats|morcegos|murciélagos
battery low|bateria fraca|batería baja
beanie|gorro|gorro
belly drum|tambor na barriga|tambor en la barriga
belly slide|escorregar de barriga|deslizarse de panza
big roar|rugido enorme|rugido enorme
big snowball|bolona de neve|bolota de nieve
bird friend|amigo passarinho|amigo pajarito
birthday|aniversário|cumpleaños
birthday cake|bolo de aniversário|pastel de cumpleaños
black cat|gato preto|gato negro
blacksheepa|ovelha negra|oveja negra
blastoff|foguete|cohete
blep|língua de fora|lengüita afuera
blink|piscar|parpadear
blow kiss|mandar beijo|mandar un beso
bobbing apples|pescar maçãs|pescar manzanas
boo|bu!|¡bu!
boomerang|bumerangue|bumerán
bounce|quicar|rebotar
bow|reverência|reverencia
boxing|boxe|boxeo
brazil flag|bandeira do Brasil|bandera de Brasil
brew|poção no caldeirão|poción en el caldero
bridgea|construir ponte|construir puente
broom|vassoura voadora|escoba voladora
bsheep_spawn|perseguição da ovelha negra|persecución de la oveja negra
bubble bath|banho de espuma|baño de burbujas
bubble gum|chiclete|chicle
bubbles|bolhas|burbujas
bug hunt|caça ao bug|caza de bugs
build snowman|fazer boneco de neve|hacer muñeco de nieve
builder|pedreiro|constructor
burp|arroto|eructo
butterflies|borboletas|mariposas
butterfly|borboleta|mariposa
candelabra|candelabro|candelabro
candle|vela|vela
card trick|truque de cartas|truco de cartas
cauldron|caldeirão|caldero
chasew1|corrida pelas paredes|carrera por las paredes
chasewstarta|perseguição pelas paredes|persecución por las paredes
cheer|comemorar|festejar
chef|cozinheiro|cocinero
clap|bater palmas|aplaudir
clone|clone|clon
cloud|nuvem|nube
cobweb|teia de aranha|telaraña
cocoa|chocolate quente|chocolate caliente
code|programar|programar
coffee|café|café
cold|frio|frío
commit|commit|commit
compile|compilar|compilar
computer|computador|computadora
confetti|confete|confeti
constellation|constelação|constelación
cookies|biscoitos|galletas
cool|descolado|genial
costume party|festa à fantasia|fiesta de disfraces
cowboy|caubói|vaquero
creaky door|porta rangendo|puerta que rechina
cry|chorar|llorar
crystal ball|bola de cristal|bola de cristal
dab|dab|dab
dance|dançar|bailar
detective|detetive|detective
digger_taskbar|cavar na barra de tarefas|cavar en la barra de tareas
digger_window|cavar na janela|cavar en la ventana
dive|mergulhar|zambullirse
divea|mergulho na banheira|clavado en la bañera
dizzy|tonto|mareado
dove|pomba|paloma
download|baixar|descargar
drink|beber|beber
drum|tambor|tambor
drums|bateria|batería
eat|comer|comer
eat fish|comer peixe|comer pescado
eat meat|comer carne|comer carne
ectoplasm bubbles|bolhas de ectoplasma|burbujas de ectoplasma
egg|ovo|huevo
enchanted broom|vassoura encantada|escoba encantada
error|erro|error
exercise|exercício|ejercicio
explorer|explorador|explorador
fall|queda|caída
fallreal|cair do céu|caer del cielo
fetch|buscar a bolinha|buscar la pelota
fire breath|cuspir fogo|escupir fuego
firefly|vaga-lume|luciérnaga
fireworks|fogos de artifício|fuegos artificiales
fishing|pescar|pescar
flag|bandeira|bandera
flap|bater as asas|aletear
flashlight story|história com lanterna|cuento con linterna
flicker lamp|lâmpada piscando|lámpara parpadeante
floating candles|velas flutuantes|velas flotantes
flower|flor|flor
flying books|livros voadores|libros voladores
frog|sapo|rana
geyser|gêiser|géiser
ghost cat|gato fantasma|gato fantasma
ghost choir|coral de fantasmas|coro de fantasmas
ghost conga|conga de fantasmas|conga de fantasmas
ghost dog|cachorro fantasma|perro fantasma
ghost hug|abraço fantasma|abrazo fantasma
ghost parade|desfile de fantasmas|desfile de fantasmas
ghost tornado|tornado fantasma|tornado fantasma
ghost tower|torre de fantasmas|torre de fantasmas
gift|presente|regalo
giggle|risadinha|risita
glow|brilhar|brillar
gold coins|moedas de ouro|monedas de oro
graduation|formatura|graduación
granny knits a scarf|vovó tricota um cachecol|la abuela teje una bufanda
group photo|foto em grupo|foto de grupo
guitar|violão|guitarra
gym|academia|gimnasio
hand fan|leque|abanico
handstand_walk|andar de ponta-cabeça|caminar de cabeza
happy|feliz|feliz
hat swallow|chapéu que engole|sombrero que traga
haunted broom|vassoura assombrada|escoba embrujada
haunted teacup|xícara assombrada|taza embrujada
headphones|fones de ouvido|audífonos
hearts|corações|corazones
hello|olá|hola
hiccup|soluço|hipo
hide and seek|esconde-esconde|escondidas
hide in box|esconder na caixa|esconderse en la caja
high five through|toca aqui atravessando|chócala atravesando
hold boomerang|bumerangue|bumerán
hold fish|segurar peixe|sostener un pez
hold plane|aviãozinho de papel|avioncito de papel
hold pumpkin|abóbora|calabaza
hold snowball|bola de neve|bola de nieve
hop|pulinho|saltito
hop hop|pula pula|salta salta
hopscotch|amarelinha|rayuela
hot|calor|calor
hot soup|sopa quente|sopa caliente
hourglass|ampulheta|reloj de arena
hula hoop|bambolê|hula hula
ice block|bloco de gelo|bloque de hielo
ice cream|sorvete|helado
ice cube|cubo de gelo|cubo de hielo
ice skate|patinar no gelo|patinar sobre hielo
idea|ideia|idea
jack-o-lantern|abóbora de Halloween|calabaza de Halloween
jelly|gelatina|gelatina
juggle|malabarismo|malabares
juggle fish|malabarismo com peixes|malabares con peces
jump|pular|saltar
jump rope|pular corda|saltar la cuerda
karate|caratê|karate
king|rei|rey
kite|pipa|cometa
knitting|tricô|tejer
lantern|lanterna|linterna
laptop|notebook|portátil
laugh|rir|reír
laundry line|varal|tendedero
lean look|espiar inclinado|asomarse inclinado
leap|salto|salto
leapfrog|pula-sela|pídola
leave|ir embora|irse
lemming spawner|chamar lemmings|llamar lemmings
levitate|levitar|levitar
levitate book|livro levitando|libro levitando
lightning|raio|rayo
loading|carregando|cargando
long beard|barba comprida|barba larga
look around|olhar em volta|mirar alrededor
look down|olhar para baixo|mirar abajo
love|amor|amor
luckybsheepstart|ovelha negra da sorte|oveja negra de la suerte
lullaby|canção de ninar|canción de cuna
magic carpet|tapete mágico|alfombra mágica
magic garden|jardim mágico|jardín mágico
magic hat|cartola mágica|sombrero mágico
magic letter|carta mágica|carta mágica
magic mirror|espelho mágico|espejo mágico
magic springs|molas mágicas|resortes mágicos
march|marchar|marchar
matrix|matrix|matrix
meditate|meditar|meditar
melting ice cream|sorvete derretendo|helado derritiéndose
message bottle|mensagem na garrafa|mensaje en una botella
meteor!|meteoro!|¡meteorito!
monster mask|máscara de monstro|máscara de monstruo
moon hammock|rede na lua|hamaca en la luna
moon howl|uivar para a lua|aullarle a la luna
moon song|canção da lua|canción de la luna
moonwalk|moonwalk|moonwalk
moonwalk_start|moonwalk|moonwalk
mouse scare|susto do ratinho|susto del ratón
music|música|música
ninja|ninja|ninja
old man walk|andar de velhinho|caminar de viejito
orbiting orbs|esferas em órbita|esferas en órbita
owl|coruja|búho
p2 start|segundo jogador|segundo jugador
paint|pintar|pintar
painting|pintura|pintura
paper boat|barquinho de papel|barquito de papel
paper lantern|lanterna de papel|farol de papel
paper plane|aviãozinho de papel|avioncito de papel
party|festa|fiesta
peekaboo|achou!|¡cucú!
peekaboo together|achou! juntos|¡cucú! juntos
phoenix|fênix|fénix
phone|telefone|teléfono
photo|foto|foto
pillow fight|guerra de travesseiros|guerra de almohadas
pink friend visits|visita da amiga rosa|visita de la amiga rosa
pirate|pirata|pirata
pissa|fazer xixi|hacer pis
pizza|pizza|pizza
point|apontar|señalar
police|policial|policía
poof in|aparecer|aparecer
poof out|sumir num puf|desaparecer en un puf
popcorn|pipoca|palomitas
portal|portal|portal
potion|poção|poción
puddle|poça|charco
puddle splash|pular na poça|saltar en el charco
pumpkin|abóbora|calabaza
quick_dead|fingir de morta|hacerse la muerta
rabbit|coelho da cartola|conejo del sombrero
rain|chuva|lluvia
rain cloud|nuvem de chuva|nube de lluvia
rainbow|arco-íris|arcoíris
rattle chain|arrastar correntes|arrastrar cadenas
read|ler|leer
roar|rugir|rugir
roll|rolar|rodar
roll_start|rolar|rodar
rotate1a|virar|girar
rotate2a|dar meia-volta|dar media vuelta
run|correr|correr
run_begin|correr|correr
runaway shadow|sombra fujona|sombra fugitiva
runes|runas|runas
salute|continência|saludo militar
scared|assustado|asustado
scarf|cachecol|bufanda
science|cientista|científico
scream|abdução do ET|abducción del ET
scroll|pergaminho|pergamino
secret whisper|cochicho secreto|susurro secreto
selfie|selfie|selfie
sheep_blush|ficar corada|sonrojarse
ship_spawn|disco voador|platillo volador
shiver|tremer|temblar
shocked|chocado|impactado
shoota|atirar|disparar
shooting star|estrela cadente|estrella fugaz
shrink|encolher|encogerse
sing|cantar|cantar
sit|sentar|sentarse
skate|andar de skate|patinar
skate start|andar de skate|patinar
skateboard|skate|monopatín
skeleton friend|amigo esqueleto|amigo esqueleto
sled|trenó|trineo
sleep|dormir|dormir
sleep1a|dormir|dormir
sleep2a|cochilar|echar una siesta
sleep3a|dormir sentada|dormir sentada
sleepwalk|sonâmbulo|sonámbulo
smoke rings|anéis de fumaça|anillos de humo
sneak|andar de fininho|andar de puntillas
sneeze|espirrar|estornudar
sniff|farejar|olfatear
snow|neve|nieve
snow cap|gorro de neve|gorro de nieve
snowball|bola de neve|bola de nieve
snowflakes|flocos de neve|copos de nieve
snowman|boneco de neve|muñeco de nieve
soap bubbles|bolhas de sabão|pompas de jabón
soccer|futebol|fútbol
sparkle hiccups|soluço de faíscas|hipo de chispas
spawn_shipa|chegar de disco voador|llegar en platillo volador
spell|feitiço|hechizo
spellbook|livro de feitiços|libro de hechizos
spider|aranha|araña
spin|girar|girar
spinning top|pião|trompo
spirit friend|amigo espírito|amigo espíritu
spooky fog|neblina assustadora|niebla tenebrosa
spooky story|história de terror|cuento de terror
spooky wail|lamento assustador|lamento tenebroso
sprint start|disparada|carrera rápida
spy|espião|espía
squats|agachamentos|sentadillas
standblink|piscar em pé|parpadear de pie
star catcher|pegar estrelas|atrapar estrellas
star fishing|pescar estrelas|pescar estrellas
star in a jar|estrela no pote|estrella en un frasco
startled|assustado|sobresaltado
static|chiado|estática
stomp|pisar forte|pisotear
stopper|bloqueador|bloqueador
storm cloud|nuvem de tempestade|nube de tormenta
stretch|espreguiçar|estirarse
stretchy|elástico|elástico
sunbathe|tomar sol|tomar el sol
sunbathing|tomar sol|tomar el sol
sunglasses|óculos escuros|lentes de sol
superhero|super-herói|superhéroe
superman start|voar de super-herói|volar como superhéroe
surprise|surpresa|sorpresa
swim|nadar|nadar
tag|pega-pega|las traes
tail chase|correr atrás do rabo|perseguir la cola
tail wag|abanar o rabo|mover la cola
take off|levantar voo|despegar
talk|falar|hablar
tea time|hora do chá|hora del té
teddy hug|abraço no ursinho|abrazo al osito
teleport|teletransporte|teletransporte
telescope|telescópio|telescopio
think|pensar|pensar
three mini boos|três mini-boos|tres mini-boos
through the floor|atravessar o chão|atravesar el suelo
thumbs up|joinha|pulgar arriba
thunder|trovão|trueno
tiny friend|amiguinho|amiguito
tired|cansado|cansado
toaster|torradeira|tostadora
tombstone|lápide|lápida
tongue_flick|língua de fora|sacar la lengua
tongue_flick2|língua de fora 2|sacar la lengua 2
top hat|cartola|sombrero de copa
toy piano|pianinho|pianito
trick or treat|doces ou travessuras|dulce o truco
trip|tropeçar|tropezar
trumpet|trompete|trompeta
turn|virar|girar
umbrella|guarda-chuva|paraguas
umbrella inside out|guarda-chuva virado|paraguas al revés
up1|subir|subir
upload|enviar|subir archivo
vanish|sumir|desvanecerse
wait|esperar|esperar
walk|andar|caminar
walk_task|andar|caminar
walk_task2|andar 2|caminar 2
walk_task3|andar 3|caminar 3
walk_task4|andar 4|caminar 4
walk_task5|andar 5|caminar 5
walk_task6|andar 6 (o rei)|caminar 6 (el rey)
walk_top|andar no teto|caminar por el techo
walk_win2|andar na janela|caminar en la ventana
water plant|regar planta|regar la planta
watering can|regador|regadera
watermelon|melancia|sandía
wave|acenar|saludar
weights|levantar peso|pesas
whirlwind|redemoinho|remolino
wifi|wi-fi|wifi
will-o-wisp|fogo-fátuo|fuego fatuo
windy day|dia de vento|día de viento
wink|piscadinha|guiño
wink kiss|piscadinha com beijo|guiño con beso
wisps|fogos-fátuos|fuegos fatuos
wizard|mago|mago
write|escrever|escribir
xmas spawner|Natal dos lemmings|Navidad de los lemmings
yawn|bocejar|bostezar
yo-yo|ioiô|yoyó
boulder|pedregulho|roca
dig|cavar|cavar
hatch|sair do ovo|salir del huevo
hike|trilha|caminata
idle|parado|quieto
jukebox|jukebox|rocola
rope|corda|cuerda
shield|escudo|escudo
sword|espada|espada
acknowledge|concordar|asentir
alert|alerta|alerta
announce|anunciar|anunciar
books|livros|libros
celebrity|celebridade|celebridad
character succeeds|deu certo|lo logró
checking something|conferindo algo|revisando algo
clicked on|clicado|clicado
confused|confuso|confundido
congratulate|parabéns|felicitar
cooking|cozinhando|cocinando
decline|recusar|rechazar
deep idle|parado de vez|quieto del todo
do magic|fazer mágica|hacer magia
dont recognize|não reconheço|no reconozco
embarrassed|envergonhado|avergonzado
empty trash|esvaziar a lixeira|vaciar la papelera
explain|explicar|explicar
gesture down|apontar para baixo|señalar abajo
gesture left|apontar para a esquerda|señalar a la izquierda
gesture right|apontar para a direita|señalar a la derecha
gesture up|apontar para cima|señalar arriba
get artsy|virar artista|ponerse artista
get attention|chamar a atenção|llamar la atención
get attention minor|chamar a atenção de leve|llamar un poco la atención
get techy|virar técnico|ponerse técnico
get wizardy|virar mago|ponerse mago
good bye|tchau|adiós
goodbye|tchau|adiós
hearing|ouvindo|escuchando
hide quick|esconder rápido|esconderse rápido
idle atom|átomo|átomo
idle blink with brows|piscar com as sobrancelhas|parpadear con las cejas
idle butter fly|borboleta|mariposa
idle cleaning|limpando|limpiando
idle cute toe twist|girar o pezinho|girar el piecito
idle eye brow raise|levantar a sobrancelha|levantar la ceja
idle falls asleep|pegar no sono|quedarse dormido
idle finger tap|tamborilar os dedos|tamborilear los dedos
idle head patting|tapinha na cabeça|palmadita en la cabeza
idle head scratch|coçar a cabeça|rascarse la cabeza
idle leans against wall|encostar na parede|apoyarse en la pared
idle leg lick|lamber a pata|lamerse la pata
idle look down|olhar para baixo|mirar abajo
idle look left|olhar para a esquerda|mirar a la izquierda
idle look right|olhar para a direita|mirar a la derecha
idle looks at user|olhar para você|mirarte
idle lowers brows|baixar as sobrancelhas|bajar las cejas
idle lowers to ground|abaixar até o chão|bajar hasta el suelo
idle rope pile|monte de corda|montón de cuerda
idle scratch|coçar|rascarse
idle side to side|de um lado para o outro|de lado a lado
idle snooze|cochilo|siesta
idle stretch|espreguiçar|estirarse
idle tail wag|abanar o rabo|mover la cola
idle twitch|tremelique|tic
idle yawn|bocejar|bostezar
image searching|procurando imagens|buscando imágenes
look down left|olhar para baixo à esquerda|mirar abajo a la izquierda
look down right|olhar para baixo à direita|mirar abajo a la derecha
look left|olhar para a esquerda|mirar a la izquierda
look right|olhar para a direita|mirar a la derecha
look up|olhar para cima|mirar arriba
look up left|olhar para cima à esquerda|mirar arriba a la izquierda
look up right|olhar para cima à direita|mirar arriba a la derecha
money|dinheiro|dinero
move down|ir para baixo|ir abajo
move left|ir para a esquerda|ir a la izquierda
move right|ir para a direita|ir a la derecha
move up|ir para cima|ir arriba
print|imprimir|imprimir
process|processar|procesar
processing|processando|procesando
reading|lendo|leyendo
rest pose|descansar|descansar
sad|triste|triste
save|salvar|guardar
search|procurar|buscar
searching|procurando|buscando
send mail|mandar e-mail|enviar correo
shopping|compras|compras
show|aparecer|aparecer
sports|esportes|deportes
start listening|começar a ouvir|empezar a escuchar
stop listening|parar de ouvir|dejar de escuchar
suggest|sugerir|sugerir
surprised|surpreso|sorprendido
travel|viagem|viaje
uncertain|em dúvida|indeciso
writing|escrevendo|escribiendo
";
	}
}
