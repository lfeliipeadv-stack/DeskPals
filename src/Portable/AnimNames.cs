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
angry steam|vapor de raiva|vapor de enojo
ball bounce|bola quicando|pelota rebotando
ball bounce up|bola subindo|pelota subiendo
ball drop|bola caindo|pelota cayendo
ball roll|bola rolando|pelota rodando
bath|banho|baño
brazil nut|castanha-do-pará|nuez de Brasil
butterfly away|borboleta indo embora|mariposa alejándose
butterfly flies off|borboleta voa|la mariposa vuela
carnival|carnaval|carnaval
chick flutter down|filhote descendo|polluelo bajando
chick hatches|filhote nasce|nace el polluelo
chick walk|filhote andando|polluelo caminando
climb|escalar|escalar
cloud leaves|nuvem vai embora|la nube se va
cloud rains|nuvem chovendo|la nube llueve
coconut|coco|coco
coconut falls|coco caindo|coco cayendo
coconut rests|coco no chão|coco en el suelo
code floats|código flutuando|código flotando
coffee steam|vapor do café|vapor del café
confetti 2|confete 2|confeti 2
confetti pop|estouro de confete|estallido de confeti
cookie|biscoito da sorte|galleta de la suerte
curious|curiosa|curiosa
download box|caixa de download|caja de descarga
drag|arrastar|arrastrar
dust|poeira|polvo
fall fast|cair rápido|caer rápido
fall hard|cair com força|caer fuerte
feather|pena|pluma
feather rests|pena no chão|pluma en el suelo
flower grows|flor crescendo|flor creciendo
flower wilts|flor murchando|flor marchitándose
fly|voar|volar
fly turn|virar voando|girar volando
fortune|sorte|fortuna
fortune slip|papel da sorte|papel de la suerte
friend flies by|amiga passa voando|una amiga pasa volando
friend flight|voo da amiga|vuelo de la amiga
glide|planar|planear
heart up|coração subindo|corazón subiendo
heart up 2|coração subindo 2|corazón subiendo 2
hi bubble|balão de oi|globo de hola
hop up|subir pulando|subir saltando
hug|abraço|abrazo
idea bulb|lâmpada da ideia|bombilla de la idea
kick ball|chutar a bola|patear la pelota
kill|ir embora|irse
kiss heart|coração do beijo|corazón del beso
kite away|pipa indo embora|cometa alejándose
land|pousar|aterrizar
let the kite go|soltar a pipa|soltar la cometa
lose a feather|perder uma pena|perder una pluma
mango|manga|mango
mango seed|caroço de manga|hueso de mango
nod|acenar com a cabeça|asentir
note up|nota subindo|nota subiendo
note up 2|nota subindo 2|nota subiendo 2
nut arc|castanha voando|nuez volando
nut fall|castanha caindo|nuez cayendo
nut in the beak|castanha no bico|nuez en el pico
nut rests|castanha no chão|nuez en el suelo
ok|ok|ok
pandeiro|pandeiro|pandero
plant a flower|plantar uma flor|plantar una flor
preen|arrumar as penas|acicalarse
review|revisar|revisar
samba|samba|samba
say hi|dizer oi|decir hola
scoop|bola de sorvete|bola de helado
scoop falls|sorvete cai|se cae el helado
scoop splat|sorvete no chão|helado en el suelo
seed arc|caroço voando|hueso volando
seed fall|caroço caindo|hueso cayendo
seed rests|caroço no chão|hueso en el suelo
shake|chacoalhar|sacudirse
shout hi|gritar oi|gritar hola
sing samba|cantar samba|cantar samba
sleepy|com sono|con sueño
snowfall|neve caindo|nevada
snowfall 2|neve caindo 2|nevada 2
soccer ball|bola de futebol|pelota de fútbol
spit seed|cuspir o caroço|escupir el hueso
splash|respingo|salpicadura
splash left|respingo à esquerda|salpicadura a la izquierda
splash right|respingo à direita|salpicadura a la derecha
squawk|gritar|graznar
sweat|suar|sudar
sweep|varrer|barrer
sync|sincronizar|sincronizar
throw nut|jogar a castanha|lanzar la nuez
toss|arremessar|lanzar
upside down|de cabeça para baixo|boca abajo
wake up|acordar|despertar
warning up|aviso subindo|aviso subiendo
whistle|assobiar|silbar
work|trabalhar|trabajar
ant|formiga|hormiga
ant walks|formiga andando|hormiga caminando
balloon away|balão indo embora|globo alejándose
butterfly flutters|borboleta voando|mariposa revoloteando
butterfly got away|a borboleta fugiu|la mariposa se escapó
bye mom|tchau, mamãe|adiós, mamá
chase butterfly|correr atrás da borboleta|perseguir la mariposa
confetti falls|confete caindo|confeti cayendo
confetti falls 2|confete caindo 2|confeti cayendo 2
corn kernel|grão de milho|grano de maíz
crow|cantar como galo|cantar como gallo
dandelion|dente-de-leão|diente de león
dust 2|poeira 2|polvo 2
dust bath|banho de terra|baño de tierra
eat worm|comer a minhoca|comer el gusano
egg rolls|ovo rolando|huevo rodando
follow mom|seguir a mamãe|seguir a mamá
footprints|pegadas|huellas
frog hops|sapo pulando|rana saltando
hawk|gavião|halcón
hawk arrives|gavião chegando|llega el halcón
hawk circles|gavião rondando|el halcón da vueltas
hawk circles back|gavião rondando de volta|el halcón vuelve
hawk gives up|gavião desiste|el halcón se rinde
hawk leaves|gavião vai embora|el halcón se va
hawk turns|gavião virando|el halcón gira
hawk turns again|gavião virando de novo|el halcón gira otra vez
hen walks|galinha andando|gallina caminando
hide in shell|esconder no ovo|esconderse en el huevo
ladybug|joaninha|mariquita
ladybug walk|joaninha andando|mariquita caminando
let the balloon go|soltar o balão|soltar el globo
mother hen|mamãe galinha|mamá gallina
nap in shell|cochilo no ovo|siesta en el huevo
peck|bicar|picotear
peek out|espiar|asomarse
peep|piar|piar
phew|ufa|uf
piu bubble|balão de piu|globo de pío
popcorn falls|pipoca caindo|palomita cayendo
popcorn rests|pipoca no chão|palomita en el suelo
popcorn up|pipoca subindo|palomita subiendo
rolling egg|ovo rolando|huevo rodando
run to worm|correr até a minhoca|correr hacia el gusano
seed floats|semente voando|semilla flotando
seed floats 2|semente voando 2|semilla flotando 2
snail|caracol|caracol
snail crawls|caracol andando|caracol arrastrándose
spot worm|avistar uma minhoca|ver un gusano
sweep dust|poeira da vassoura|polvo de la escoba
worm eaten|minhoca comida|gusano comido
worm pops out|minhoca saindo da terra|el gusano sale de la tierra
worm wiggles|minhoca se mexendo|el gusano se retuerce
z floats|z flutuando|z flotando
bee sting|ferroada da abelha|picadura de abeja
bubble pop|bolha estoura|la burbuja estalla
bubble up|bolha subindo|burbuja subiendo
bubble up 2|bolha subindo 2|burbuja subiendo 2
catch bee|pegar uma abelha|atrapar una abeja
catch bee prey|abelha|abeja
catch cricket|pegar um grilo|atrapar un grillo
catch cricket prey|grilo|grillo
catch dragonfly|pegar uma libélula|atrapar una libélula
catch dragonfly prey|libélula|libélula
catch dragonfly tongue 1|língua (libélula)|lengua (libélula)
catch firefly|pegar um vaga-lume|atrapar una luciérnaga
catch firefly prey|vaga-lume|luciérnaga
catch firefly tongue 1|língua (vaga-lume)|lengua (luciérnaga)
catch fly|pegar uma mosca|atrapar una mosca
catch fly prey|mosca|mosca
catch fly tongue 1|língua (mosca)|lengua (mosca)
catch mosquito|pegar um pernilongo|atrapar un mosquito
catch mosquito prey|pernilongo|mosquito
catch mosquito tongue 1|língua (pernilongo)|lengua (mosquito)
catch mosquito tongue 2|língua (pernilongo) 2|lengua (mosquito) 2
catch moth|pegar uma mariposa|atrapar una polilla
catch moth prey|mariposa|polilla
catch visitor|pegar a visita|atrapar la visita
cookie prey|biscoito|galleta
croak|coaxar|croar
croak bubble|balão de croac|globo de croac
crown falls|coroa caindo|corona cayendo
drink prey|gota d'água|gota de agua
eat prey|mosca|mosca
eat tongue 1|língua|lengua
firefly glow|barriga acesa|panza brillante
friend croaks|amigo coaxa|el amigo croa
friend hops away|amigo vai embora pulando|el amigo se va saltando
frog chorus|coral de sapos|coro de ranas
frog prince|sapo príncipe|rana príncipe
gulp|engolir|tragar
lick eye|lamber o olho|lamerse el ojo
long jump|salto longo|salto largo
miss a fly|errar a mosca|fallar la mosca
miss a fly prey|mosca que escapa|mosca que escapa
miss a fly tongue 1|língua (errou)|lengua (falló)
missed|errou|falló
rain dance|dança da chuva|danza de la lluvia
tadpoles|girinos|renacuajos
tadpoles swim|girinos nadando|renacuajos nadando
hop prepare|preparar o pulo|prepararse para saltar
hop land|aterrissar do pulo|aterrizar del salto
hop bump|bater no meio do pulo|chocar en el salto
long jump prepare|preparar o salto longo|prepararse para el salto largo
bat friends|amigos morcegos|amigos murciélagos
candy falls|doce caindo|caramelo cayendo
candy falls 2|doce caindo 2|caramelo cayendo 2
candy rain|chuva de doces|lluvia de caramelos
catch a snack|pegar um lanche|atrapar un bocadillo
chase moth|perseguir a mariposa|perseguir la polilla
eat moth|comer a mariposa|comer la polilla
fireflies|vaga-lumes|luciérnagas
fireflies glow|vaga-lumes piscando|luciérnagas brillando
fly away from garlic|fugir do alho|huir del ajo
fly down|voar para baixo|volar hacia abajo
fly to the top|voar até o alto|volar hasta arriba
fly up|voar para cima|volar hacia arriba
friends fly by|amigos passando|amigos pasando
garlic|alho|ajo
hang|pendurar-se|colgarse
hang sleep|dormir pendurado|dormir colgado
juice|suco|jugo
let go|soltar-se|soltarse
loop|cambalhota no ar|voltereta en el aire
moon|lua|luna
moon rises|lua nascendo|sale la luna
moth flies away|mariposa fugindo|polilla escapando
moth flutters|mariposa voando|polilla revoloteando
pumpkin glows|abóbora acesa|calabaza encendida
snack falls|lanche caindo|bocadillo cayendo
sonar|sonar|sonar
sonar ring|onda do sonar|onda del sonar
spider comes down|aranha descendo|araña bajando
spider goes up|aranha subindo|araña subiendo
spot moth|achar uma mariposa|encontrar una polilla
squeak|guinchar|chillar
squeak bubble|balão de guincho|globo de chillido
baby cactus|cactinho bebê|cactus bebé
baby cactus fades|cactinho bebê some|el cactus bebé desaparece
baby cactus grows|cactinho bebê cresce|el cactus bebé crece
baby cactus waves|cactinho bebê acena|el cactus bebé saluda
balloon drifts in|balão chegando|llega el globo
balloon pieces fall|pedaços do balão caindo|caen los trozos del globo
balloon pops|balão estoura|el globo explota
balloon wobbles|balão balançando|el globo se balancea
bee|abelha|abeja
bee arrives|abelha chega|llega la abeja
bee leaves|abelha vai embora|se va la abeja
bee visits|abelha visita|la abeja visita
bird|passarinho|pajarito
bird flies in|passarinho chega voando|llega volando el pajarito
bird flies off|passarinho vai embora|el pajarito se va volando
bird lands|passarinho pousa|el pajarito se posa
bird pricked|passarinho espetado|el pajarito se pincha
bloom|desabrochar|florecer
boing|boing|boing
bubble floats|bolha flutuando|burbuja flotando
bubble floats 2|bolha flutuando 2|burbuja flotando 2
bubble pops|bolha estoura|la burbuja explota
bubble wait|bolha esperando|burbuja esperando
butterfly arrives|borboleta chega|llega la mariposa
butterfly leaves|borboleta vai embora|se va la mariposa
butterfly rests|borboleta descansa|la mariposa descansa
calango|calango|lagartija
calango arrives|calango chega|llega la lagartija
calango push-ups|calango faz flexões|la lagartija hace flexiones
calango runs away|calango foge|la lagartija huye
can arrives|regador chega|llega la regadera
can leaves|regador vai embora|se va la regadera
can pours|regador rega|la regadera riega
comb spines|pentear os espinhos|peinar las espinas
crack pot|vaso rachado|maceta rajada
dizzy roll|tonto de rolar|mareado de rodar
flower closes|flor fecha|la flor se cierra
flower rain|chuva de flores|lluvia de flores
hug heart|coração do abraço|corazón del abrazo
hug heart 2|coração do abraço 2|corazón del abrazo 2
hug heart 3|coração do abraço 3|corazón del abrazo 3
hug heart wait|coração do abraço esperando|corazón del abrazo esperando
hug heart wait 2|coração do abraço esperando 2|corazón del abrazo esperando 2
hug heart wait 3|coração do abraço esperando 3|corazón del abrazo esperando 3
kick pebble|chutar pedrinha|patear una piedrita
party confetti|confete da festa|confeti de la fiesta
patch pot|consertar o vaso|arreglar la maceta
pebble rolls|pedrinha rolando|la piedrita rueda
peek|espiar|espiar
petals fall|pétalas caindo|pétalos que caen
petals rain|chuva de pétalas|lluvia de pétalos
petals rain 2|chuva de pétalas 2|lluvia de pétalos 2
settle|se ajeitar|acomodarse
spines fall|espinhos caindo|espinas cayendo
spines fly|espinhos voando|espinas volando
spines wait|espinhos esperando|espinas esperando
stand up|levantar|levantarse
start walking|começar a andar|empezar a caminar
stop walking|parar de andar|dejar de caminar
sun shines|sol brilhando|el sol brilla
tumbleweed|bola de feno|planta rodadora
tumbleweed rolls|bola de feno rolando|la planta rodadora rueda
z floats up|zzz subindo|zzz subiendo
back away|recuar|retroceder
candies fall|doces caindo|caramelos cayendo
candies fall 2|doces caindo 2|caramelos cayendo 2
dance party|festa dançante|fiesta de baile
dizzy flight|voo tonto|vuelo mareado
favourite food|comida preferida|comida favorita
fireflies blink|vaga-lumes piscando|luciérnagas parpadeando
garlic rests|alho no chão|ajo en el suelo
gulp moth|engolir a mariposa|tragar la polilla
hang upside down|de cabeça para baixo pendurado|colgado boca abajo
loop down|descer do looping|bajar del rizo
loop the loop|looping|rizo
moth|mariposa|polilla
moth flies|mariposa voando|polilla volando
sleep hanging|dormir pendurado|dormir colgado
sonar rings|ondas do sonar|ondas del sonar
spider dangles|aranha pendurada|araña colgando
asa-branca flies|asa-branca voa|vuela la asa-branca
asa-branca|asa-branca|asa-branca
balloon drifts|balão flutua|el globo flota
balloon pop|estourar balão|reventar globo
bandeirinhas|bandeirinhas|banderitas
bat (wait)|morcego (espera)|murciélago (espera)
bat flies by|morcego passa|pasa un murciélago
bird on arm|passarinho no braço|pajarito en el brazo
bump|trombada|choque
bye bird|tchau, passarinho|adiós, pajarito
calango runs in|calango chega correndo|el calango llega corriendo
calango runs off|calango foge|el calango huye
carcará arrives|carcará chega|llega el carcará
carcará circles again|carcará rodeia de novo|el carcará vuelve a rondar
carcará circles back|carcará rodeia de volta|el carcará ronda de vuelta
carcará circles|carcará rodeia|el carcará ronda
carcará leaves|carcará vai embora|el carcará se va
carcará turns again|carcará vira de novo|el carcará vuelve a girar
carcará turns|carcará vira|el carcará gira
carcará|carcará|carcará
chase hat|correr atrás do chapéu|perseguir el sombrero
cloud goes away|nuvem vai embora|la nube se va
cordel|cordel|cordel
crumbs (wait)|farelos (espera)|migas (espera)
crumbs fall|farelos caem|caen migas
cuscuz steam|vapor do cuscuz|vapor del cuscús
cuscuz|cuscuz|cuscús
dust devil|redemoinho de poeira|remolino de polvo
far star|estrela distante|estrella lejana
fire burns|fogo queima|el fuego arde
fire dies|fogo apaga|el fuego se apaga
fire lights|fogo acende|el fuego se enciende
flags fade 2|bandeirinhas somem 2|las banderitas desaparecen 2
flags fade|bandeirinhas somem|las banderitas desaparecen
fogueira|fogueira|hoguera
forró note (wait)|nota do forró (espera)|nota del forró (espera)
forró note 2 (wait)|nota do forró 2 (espera)|nota del forró 2 (espera)
forró|forró|forró
full moon|lua cheia|luna llena
galo-de-campina arrives|galo-de-campina chega|llega el cardenal
galo-de-campina flies off|galo-de-campina voa embora|el cardenal se va volando
galo-de-campina sings|galo-de-campina canta|el cardenal canta
galo-de-campina|galo-de-campina|cardenal
goat comes|bode chega|llega la cabra
goat jumps|bode pula|la cabra salta
goat nibbles|bode mordisca|la cabra mordisquea
goat runs away|bode foge|la cabra huye
goat|bode|cabra
hat (wait)|chapéu (espera)|sombrero (espera)
hat back|chapéu de volta|sombrero de vuelta
hat blown|chapéu voou|se voló el sombrero
hat falls|chapéu cai|cae el sombrero
hat flies|chapéu voa|vuela el sombrero
hat on the ground|chapéu no chão|sombrero en el suelo
hat trick|truque com o chapéu|truco con el sombrero
heart floats 2|coração flutua 2|el corazón flota 2
heart floats|coração flutua|el corazón flota
heat waves|ondas de calor|ondas de calor
jegue walks by|jegue passa|pasa el burrito
jegue|jegue|burrito
moon sets|lua se põe|la luna se pone
moon shines|lua brilha|la luna brilla
music note (wait)|nota da música (espera)|nota de la música (espera)
music note 2 (wait)|nota da música 2 (espera)|nota de la música 2 (espera)
night bloom|flor da noite|flor de noche
note floats 2|nota flutua 2|nota flota 2
note floats|nota flutua|nota flota
party flags 2|bandeirinhas 2|banderitas 2
party flags|bandeirinhas|banderitas
push-ups|flexões|flexiones
rain arrives|a chuva chega|llega la lluvia
rain cloud comes|nuvem de chuva chega|llega la nube de lluvia
rapadura|rapadura|panela
sertão sun|sol do sertão|sol del sertón
sparks (wait)|faíscas (espera)|chispas (espera)
sparks 2 (wait)|faíscas 2 (espera)|chispas 2 (espera)
sparks rise|faíscas sobem|suben chispas
spines (wait)|espinhos (espera)|espinas (espera)
spines 2 (wait)|espinhos 2 (espera)|espinas 2 (espera)
spines fly 2|espinhos voam 2|las espinas vuelan 2
star (wait)|estrela (espera)|estrella (espera)
star streaks by|estrela risca o céu|la estrella cruza el cielo
start walk|começar a andar|empezar a caminar
stop walk|parar de andar|dejar de caminar
sun fades|sol se apaga|el sol se apaga
sway|balançar ao vento|mecerse al viento
tapioca|tapioca|tapioca
tip hat|tirar o chapéu|quitarse el sombrero
top (wait)|pião (espera)|trompo (espera)
top falls|pião cai|el trompo cae
top spins|pião gira|el trompo gira
top wobbles|pião bambeia|el trompo se tambalea
triangle note (wait)|nota do triângulo (espera)|nota del triángulo (espera)
triangle note 2 (wait)|nota do triângulo 2 (espera)|nota del triángulo 2 (espera)
triangle|triângulo|triángulo
warning floats|aviso flutua|el aviso flota
xote back|xote de volta|xote de vuelta
xote note 2 (wait)|nota do xote 2 (espera)|nota del xote 2 (espera)
xote|xote|xote
zabumba note (wait)|nota da zabumba (espera)|nota de la zabumba (espera)
zabumba note 2 (wait)|nota da zabumba 2 (espera)|nota de la zabumba 2 (espera)
zabumba|zabumba|zabumba
";
	}
}
