# Atualização — NBA Survival Arena

Atualização para **a cópia do projeto Unity enviada no BACKUP.part1–4.rar**, Unity **6000.4.11f1**. Este pacote depende dos scripts, pacotes e recursos que já existem naquele projeto; não é um projeto completo nem um instalador para as versões antigas do repositório.

## Instalar na cópia destinada ao basquete

1. Feche o Unity.
2. Copie a pasta **Assets desta atualização** para a raiz da sua cópia do projeto (ao lado de Packages e ProjectSettings), aceitando substituir os arquivos correspondentes. Preserve os arquivos `.meta` fornecidos.
3. Abra o projeto e aguarde a importação.
4. Use **Tools → Basquete → Abrir Liga NBA** e entre em Play.
5. Para editar o campeonato e a arena, abra **Tools → Basquete → Painel da arena**, fora do Play.

As quatro novas cenas estão em `Assets/Scenes/Basquete`. As cenas de futebol existentes continuam no caminho anterior. O pacote substitui oito scripts compartilhados e acrescenta o módulo de basquete; não copie somente a pasta BasqueteArena.

## O jogo

- Até cinco bolas por equipe; nas cenas de capitães, quatro jogadores + um capitão convidado.
- Duas equipes caem no mesmo percurso e se empurram. Rampas e uma barra giratória conduzem à entrada do lançador.
- Buracos laterais eliminam a bola. Um efeito aparece no local e, após dois segundos por padrão, a bola retorna ao topo.
- Um lançador compartilhado recebe uma bola por vez. A tentativa fica separada das colisões da disputa.
- Cesta fixa com duas bordas físicas de aro e tabela. A pontuação só vale ao atravessar a abertura de cima para baixo, uma única vez.
- Tentativas comuns valem 2 pontos; aproximadamente 30% partem de mais longe e valem 3. A seleção de distância é automática.
- Cestas e erros devolvem a bola à disputa. As bolas não têm vidas limitadas.
- Cada partida dura 60 segundos; empate abre prorrogações de 15 segundos, repetidas até haver vencedor. Não termina na primeira cesta da prorrogação.
- O overall influencia moderadamente a dispersão do arremesso; o capitão convidado usa o overall salvo no sorteio. Todas as bolas têm o mesmo tamanho e peso. Esta versão não acrescenta um especial de capitão.
- Proteções tentam soltar bolas paradas e devolvem bolas fora da arena ou presas por tempo prolongado ao início.

## Campeonatos e transmissão

Foram mantidos o calendário, liga, mata-mata, apresentação de jogadores, sorteio de capitães, transições, banners e resultado final. O placar aceita 2 e 3 pontos. A tabela do basquete mostra aproveitamento de vitórias e diferença de pontos; internamente a coluna PTS conta vitórias, preservando o formato de arquivo existente. As colunas restantes usam a estrutura original; empates ficam zerados no basquete. O mata-mata continua em jogo único, sem séries de playoffs ou conferências.

As cenas de liga começam com 8 times e as de mata-mata com 16. Há **30 fichas NBA** em `Assets/BasqueteArena/Times`, com nomes, siglas e cores de referência. Todos começam com overall 75 e cinco jogadores genéricos. Cadastre os nomes, fotos, escudos e ratings desejados: o pacote não inclui artes oficiais nem elencos atuais da NBA. As cenas de capitães mantêm os catálogos de convidados que você já tinha no backup; altere-os no painel se quiser outros personagens.

## Fogo, gelo e água

Escolha o asset no campo **Tema Basquete** do painel:

- **Fogo:** cores quentes, eliminação e retorno ao início.
- **Gelo:** atrito menor e quique maior.
- **Água:** correntes laterais alternadas durante a disputa.

O campo Elemento seleciona o comportamento; cores, atrito e quique são controles independentes. Ao mudar somente Elemento em um asset personalizado, ajuste esses controles conforme desejar. Os três presets já têm valores diferentes.

Cada arena aceita **Fundo Cenario**, **Fundo Quadra**, **Imagem Cesta**, **Imagem Lancador** e sons de eliminação/lançamento. Importe suas imagens como Sprite (2D and UI) e arraste para os campos. A arte é decorativa: os colisores permanecem independentes. A imagem da cesta ocupa a região da rede; aro e tabela físicos continuam desenhados.

Use **Duplicar arena para este campeonato** antes de personalizar um tema compartilhado. O botão cria e atribui outro asset. Salve o projeto depois de editar.

## Mais obstáculos e eliminações

Na lista **Obstaculos** do tema, aumente Size e configure cada item:

- **Posicao:** coordenadas do centro; área da disputa: X de -9,5 a 0,5 e Y de -4 a 4.
- **Tamanho:** largura e altura do retângulo, positivas.
- **Angulo:** inclinação inicial em graus.
- **Graus Por Segundo:** zero fica parado; valor positivo ou negativo gira.
- **Elimina:** marcado vira uma zona de eliminação; desmarcado vira uma plataforma sólida.

Esses itens são acrescentados ao percurso básico, que já inclui rampas, barra, buracos e lançador. Esta primeira versão não inclui editor para remover/reorganizar os obstáculos básicos nem efeitos de fogo/água com partículas: as eliminações usam um efeito simples de cor e expansão. Imagens, física e disposição podem ser refinadas depois do primeiro teste visual.

## Temporadas

As cenas NBA têm identificadores próprios. O calendário, resultados e sorteios continuam usando o mecanismo existente em `Application.persistentDataPath`, separados pelos novos IDs. Os arquivos de dados do backup não são enviados neste pacote.

O modo de jogo e a assinatura das regras da arena ficam salvos com a configuração da temporada. Mudar física, elemento ou obstáculos exige **uma nova temporada**; mudar imagens, cores ou sons é permitido. No painel, troque Temporada Id por outro identificador, ou use o botão Nova temporada do Inspector original. Os resultados anteriores permanecem salvos. Não reutilize IDs de futebol nas cenas NBA.

## Validação e primeiro Play

Confira [VALIDACAO.md](VALIDACAO.md). Os testes de lógica passaram; **o editor Unity não está disponível no ambiente de desenvolvimento**, portanto compilação com as bibliotecas reais, importação, apresentação visual, áudio, balanceamento e física no Play Mode ainda precisam de conferência no Unity. Este pacote é a primeira implementação da mecânica combinada.

No primeiro Play, confira: dez bolas na disputa; empurrões; eliminação e retorno; apenas uma entrada por intervalo no lançador; acertos de 2 e 3 pontos; fim da partida; avanço e retomada da temporada. Teste também as cenas com capitães e os outros dois temas.
