# Futebol de Discos

**Barra e roleta dos capitães — v2:** [baixar somente a atualização](Atualizacoes/Atualizacao_Barra_Roleta_Capitaes_v2.zip) · [como aplicar e mudar a cor](Atualizacoes/LEIA-ME-Barra-Roleta-Capitaes-v2.md). Painel opaco, barra animada e roleta em inglês; HIT/MISS executado no próximo toque, com animação do chute.

**[Baixar o projeto completo em ZIP](https://github.com/Robertogirao/futebol-mata-mata2/archive/refs/tags/futebol-discos-v2026.11.14.3.zip)** · [Versão no GitHub](https://github.com/Robertogirao/futebol-mata-mata2/releases/tag/futebol-discos-v2026.11.14.3)

Projeto Unity de futebol automático entre discos, com liga e mata-mata. Reaproveita o catálogo de equipes, representantes, aberturas, classificação, chaveamento e salvamento do projeto de futebol. A partida usa um campo visto de cima, dois gols, discos das equipes e uma única bola branca: os gols acontecem pelas colisões e pelos chutes da simulação.

Esta versão substitui o Beat the Keeper. Cada time começa com uma quantidade fixa de discos; não há lançamento contínuo de bolinhas nem goleiro do jogo anterior. A apresentação tem identidade própria, com arena em tons de verde-azulado e ciano. Os textos da transmissão estão em inglês; os controles do Inspector estão em português.

**Já personalizou o projeto?** [Baixe a atualização de posturas e pressão](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/tags/futebol-discos-v2026.11.13.3/Atualizacoes/Atualizacao_Posturas_Pressao.zip) e siga [as instruções de atualização](Atualizacoes/COMO_APLICAR_TATICAS.md). Ela inclui dez scripts e o arquivo de importação da nova tática, preservando suas cenas e configurações.

**Legenda da foto:** [atualização PLAYER → CAPTAIN, de um único script](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/tags/futebol-discos-v2026.11.13.4/Atualizacoes/Atualizacao_Legenda_CAPTAIN.zip).

**Nova apresentação futurista:** [baixe a atualização das aberturas e prévias](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/tags/futebol-discos-v2026.11.13.5/Atualizacoes/Atualizacao_Aberturas_Futuristas.zip) · [guia dos novos controles](Atualizacoes/COMO_APLICAR_ABERTURAS_FUTURISTAS.md).

**Painel único e fundo externo:** [baixe a atualização](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/tags/futebol-discos-v2026.11.13.6/Atualizacoes/Atualizacao_Painel_Central_Fundo_Fotos.zip) · [instruções dos controles](Atualizacoes/COMO_APLICAR_PAINEL_CENTRAL.md).

**Bola presa nos cantos:** [baixe a atualização de escanteio e tiro de meta](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/tags/futebol-discos-v2026.11.13.7/Atualizacoes/Atualizacao_Bola_Presa_Escanteio_Tiro_Meta.zip) · [guia dos controles e da instalação](Atualizacoes/COMO_APLICAR_BOLA_PRESA.md).

**Sons escolhidos no painel:** [baixe a atualização dos efeitos e dos controles de áudio](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/tags/futebol-discos-v2026.11.13.8/Atualizacoes/Atualizacao_Sons_Painel.zip) · [instruções de instalação e configuração](Atualizacoes/COMO_APLICAR_SONS.md).

**Capitães convidados:** [baixe a atualização com as duas novas cenas](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/tags/futebol-discos-v2026.11.14.1/Atualizacoes/Atualizacao_Capitaes_Convidados.zip) · [guia para cadastrar personagens, definir overall e sortear os times](Atualizacoes/COMO_APLICAR_CAPITAES_CONVIDADOS.md).

**Sorteio das marbles antes do campeonato:** [baixe a atualização](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/tags/futebol-discos-v2026.11.14.2/Atualizacoes/Atualizacao_Sorteio_Marbles_Capitaes.zip) · [guia de instalação, controles e temporadas salvas](Atualizacoes/COMO_APLICAR_SORTEIO_MARBLES.md).

**Pinos do sorteio e fotos no chaveamento:** [baixe a correção](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/tags/futebol-discos-v2026.11.14.3/Atualizacoes/Atualizacao_Pinos_Fotos_Capitaes.zip) · [instruções de instalação e uso](Atualizacoes/COMO_APLICAR_PINOS_E_FOTOS.md).

## Abrir o projeto

1. Instale o **Unity 6000.4.11f1** pelo Unity Hub.
2. No Hub, escolha **Add / Adicionar projeto** e selecione a pasta **`Projeto`**, que contém `Assets`, `Packages` e `ProjectSettings`.
3. Aguarde a importação. Abra **Tools > Futebol de Discos > Abrir Liga** ou **Abrir Mata-mata**.
4. Selecione **FutebolDiscos_Manager** na Hierarchy e configure o componente **Projeto Futebol Discos**. Clique em **Validar configuração**, salve a cena e dê **Play**.

As cenas ficam em `Assets/Scenes/FutebolDiscos/`:

| Cena | Configuração inicial |
| --- | --- |
| `Liga_Discos.unity` | 8 seleções, 7 rodadas, 3 discos por equipe |
| `MataMata_Discos.unity` | 16 clubes, chave de 16, 3 discos por equipe |
| `Capitaes/Liga_Capitaes.unity` | Liga com capitães convidados; 3 jogadores do time + 1 capitão |
| `Capitaes/MataMata_Capitaes.unity` | Mata-mata com capitães convidados; 3 jogadores do time + 1 capitão |

O campo e a transmissão são construídos ao entrar em Play. O guia completo está em [Projeto/COMO_USAR.md](Projeto/COMO_USAR.md).

## Liga e mata-mata com capitães convidados

Abra **Tools > Futebol de Discos > Capitães > Abrir Liga** ou **Abrir Mata-mata**. As cenas anteriores continuam disponíveis, com seus próprios salvamentos. As novas cenas usam os identificadores `discos_liga_capitaes` e `discos_mata_capitaes`.

No Project, use **Create > Futebol de Discos > Capitão convidado** para criar uma ficha com nome, foto como Sprite e **overall de 1 a 100**. Em **FutebolDiscos_Manager > Projeto Futebol Discos > Capitães convidados**, preencha **Capitães disponíveis**. Há 16 fichas genéricas em `Assets/FutebolDiscos/Capitaes/Modelos/` que você pode editar ou substituir; elas não incluem imagens de DBZ.

Cada time recebe um capitão diferente por sorteio. **Associações fixas (opcional)** permite reservar combinações, como Real Madrid + Goku; os demais times continuam no sorteio. A associação fica salva na temporada e se mantém após Stop/Play. Para outro sorteio ou para mudar o overall usado naquela edição, crie uma **Nova temporada**.

Por padrão, **Capitão como disco extra** soma o personagem aos jogadores do elenco: 3 jogadores do Real Madrid + Goku formam quatro discos. O limite é 5 jogadores do time + 1 capitão. Desmarcada essa opção, o capitão ocupa uma das vagas do total configurado.

O overall do convidado afeta somente a velocidade, potência e precisão dos chutes dele. Os jogadores do time continuam com o comportamento normal do overall da equipe. Nome, foto e overall do time original não são alterados. O convidado aparece na abertura como **CAPTAIN** e, quando marca, usa seu nome e foto; os demais gols usam o nome do jogador e o escudo/bandeira.

## Sorteio dos capitães na tela

Nas cenas de convidados, a apresentação **CAPTAIN DRAFT** mostra as marbles dos capitães passando por pinos, rampas e obstáculos giratórios. Os times aparecem ao lado, e cada chegada revela uma associação entre capitão e clube. O resultado completo aparece antes de a liga ou o mata-mata começar automaticamente.

A quantidade acompanha os times preenchidos: 10 times usam 10 marbles; 16, 20 ou 32 usam a mesma quantidade de capitães. Quando o catálogo tem mais personagens, o sorteio seleciona apenas o necessário, sem repetições. Associações fixas são respeitadas; os demais clubes são distribuídos pelas chegadas. O overall não dá vantagem nessa etapa.

Os controles ficam em **FutebolDiscos_Manager > Projeto Futebol Discos > Capitães convidados > Sorteio na tela**. Ajuste a apresentação dos participantes, a velocidade das marbles, o intervalo entre associações e a leitura do resultado.

**Já existe um sorteio salvo na temporada?** Por padrão, ele é retomado sem repetir a abertura. Use **Nova temporada (preserva as anteriores)** para fazer um novo sorteio. Para apenas assistir às associações existentes, marque **Reexibir sorteio salvo**: a tela indica **SAVED DRAFT** e conserva os capitães e resultados.

As associações são salvas quando todas foram reveladas, antes do resumo e da primeira partida. Interromper no meio da pista não grava um sorteio parcial; o próximo Play recomeça a apresentação com a semente da mesma temporada.

Os obstáculos agora têm mais espaço de passagem, e a resolução dos contatos impede que um empurrão entre marbles as deixe dentro dos pinos. Os pinos permanecem ativos até o fim. Marbles sem progresso recebem um impulso para voltar a se mover; não são teleportadas para a chegada. Após cruzarem a linha, aguardam fora da pista e seguem até os cartões dos clubes por um trajeto externo aos obstáculos.

No mata-mata com convidados, o chaveamento mostra a foto do capitão ao lado do escudo, nome e placar do clube. A prévia do confronto e as apresentações do campeão também usam a foto do capitão já associado àquele time. Sem foto cadastrada, o desenho anterior é mantido. Essa exibição não faz outro sorteio e não exige criar uma temporada nova.

## Formatos e controles

- **Liga:** 2 a 32 equipes e 1 a 100 rodadas. O calendário distribui todos os confrontos de um turno, inclui folga para número ímpar de equipes e inverte os lados no returno. Vitória vale 3 pontos; empate, 1; derrota, 0.
- **Mata-mata:** chaves de 2, 4, 8, 16 ou 32 vagas, com folgas opcionais, ordem da lista ou cabeças de chave e opção de embaralhar. Empate leva a **Golden Goal**: a partida continua até um gol real da simulação, podendo ultrapassar a duração regulamentar. O sistema não escolhe um vencedor artificial.
- **Partidas:** 1 a 5 discos por time nas cenas tradicionais, com padrão de 3, mais uma única bola branca. Nas cenas de convidados, o capitão extra permite até seis discos por equipe. Velocidades, tamanhos, força do chute, pausa após gol, uso do overall e permanência dos discos em sua metade ficam no painel central.
- **Transmissão:** nome da competição, fontes, logos, fundos, cores, duração das aberturas, fotos ampliadas dos representantes e tempo das tabelas ficam no mesmo painel e no tema associado.

## Tags, capitães e textos da arena

No **FutebolDiscos_Manager**, o grupo **Identificação dos jogadores** permite mostrar nomes sobre os discos, ajustar as tags e destacar o capitão com borda dourada, letra C e foto. O capitão é o representante mostrado na ficha da partida. Os demais discos usam os outros nomes cadastrados no **Elenco** de cada `TeamData`.

Na comemoração, o capitão usa nome e foto; os demais usam nome e bandeira/escudo. Se faltarem jogadores no elenco, os discos restantes usam a sigla e seu número, sem repetir o capitão.

Em **Textos da arena**, edite ou oculte o texto do campo, o título e o subtítulo do rodapé. Os detalhes de formato e número de discos têm um controle separado. Esses ajustes não exigem outra temporada.

## Posturas e pressão

Na ficha de cada `TeamData`, escolha **Tática e IA > Postura tática**: Ultra defensivo permite 0 jogadores pressionando além do meio-campo; Defensivo, até 1; Balanceado, até 2; Ataque, até 3. O total também fica limitado aos discos disponíveis.

Para permitir esse avanço, desmarque **Manter cada time em sua metade** no `FutebolDiscos_Manager`. Marcado, o controle geral mantém todos no próprio campo. As posturas são limites: a posição e a direção da bola determinam quantos jogadores realmente sobem. Os apoios ocupam faixas diferentes, e os jogadores recuam quando a jogada pede defesa.

## Aberturas e prévias futuristas

A transmissão usa painéis holográficos, scanners, pulsos de energia e uma íris que abre do centro para as bordas superior e inferior. As fichas **CAPTAIN** se materializam e seguem em arco até seus lugares. Liga, prévia do mata-mata, classificação e campeão compartilham a nova identidade.

No **FutebolDiscos_Manager > Tempos da transmissão (segundos) > Textos das aberturas e prévias**, edite **ARENA**, **CHALLENGERS**, **START** e as demais chamadas. Os tempos, fontes, cores, logos e fundos existentes continuam configuráveis. O roteiro visual não altera a temporada salva.

## Painel central e imagem externa

Os ajustes ficam em **FutebolDiscos_Manager > Projeto Futebol Discos**, com a seção **Configurações da Liga** ou **Configurações do Mata-mata**, conforme a cena. Os Inspectors internos exibem um atalho para esse painel. Os tempos de jogo, abertura, fotos, intervalo e leitura ficam ali, junto de sons e comemoração. O intervalo não é mais aplicado duas vezes na liga.

Em **Identidade visual > Fundo externo da arena**, escolha uma imagem importada como Sprite. A imagem mantém a proporção e fica atrás do campo e dos gols, com opacidade e decoração opcionais. Em **Identificação dos jogadores**, a leitura luminosa da foto pode ser desligada ou ter sua velocidade ajustada; agora ela fica atrás do retrato e é mais lenta e discreta.

## Sons da partida e das aberturas

Em **FutebolDiscos_Manager > Projeto Futebol Discos > Sons e comemoração de gol**, escolha **Som do gol**, **Apito de início** e **Apito de encerramento**. Esses eventos usam diretamente os clipes escolhidos; deixar um campo vazio desliga aquele efeito. Salve a cena fora do Play para conservar as escolhas.

**Fonte de áudio dos efeitos (opcional)** pode ficar em **None**: o jogo cria uma fonte exclusiva para a partida. Uma fonte atribuída deve ser exclusiva para esses efeitos. A fonte automática não reutiliza a fonte de música ou de abertura, e os efeitos pendentes são interrompidos ao preparar outra partida ou cancelar a atual.

Com um tema atribuído, o campo **Som das aberturas e prévias** controla o áudio do tema, separado dos três efeitos da partida. Alterar esse som modifica o asset do tema e afeta as cenas que o compartilham. Duplique o tema se quiser aberturas diferentes na liga e no mata-mata.

## Bola presa nos cantos

Em **FutebolDiscos_Manager > Projeto Futebol Discos > Bola presa nos cantos**, ative a reposição e ajuste o tempo de detecção, a área do canto e a pausa da cobrança. O padrão espera **3 segundos sem progresso** junto às duas bordas do canto; bater na parede ou passar por ali não interrompe a jogada. Pequenos tremores e trocas de toque entre os discos não reiniciam essa espera, mas sair da região ou avançar com a bola reinicia a contagem.

Último toque do defensor dá **escanteio para o atacante**; último toque do atacante dá **tiro de meta para o defensor**. A transmissão mostra **CORNER KICK** ou **GOAL KICK**, pausa brevemente o relógio, reorganiza os discos e cobra automaticamente para dentro do campo. Funciona na liga, no mata-mata e no Golden Goal. Esses controles podem ser ajustados entre sessões mantendo a temporada e seus resultados.

## Temporadas e salvamento

Cada resultado concluído é salvo automaticamente. Na liga, a rodada completa é confirmada antes de avançar; o mata-mata também guarda o chaveamento. Após **Stop** e **Play**, a mesma temporada retoma os jogos ainda pendentes. Uma partida interrompida antes de terminar recomeça; sua posição e seu cronômetro não são salvos.

Com **Semente do sorteio = 0**, a temporada recebe uma semente aleatória no primeiro Play e a conserva nos próximos. Para outro sorteio, use **Nova temporada (preserva as anteriores)**. Alterar participantes, formato, rodadas, semente ou regras da partida exige outra temporada. Nome, fontes, imagens e tempos da apresentação podem ser ajustados sem apagar os resultados.

Os dados ficam em `Application.persistentDataPath`, sob o Product Name **Futebol de Discos**, separado dos salvamentos do projeto anterior:

```text
Discos/<competição>/<temporada>/configuracao_discos.json
Discos/<competição>/<temporada>/capitaes_convidados.json  (nas cenas com convidados)
Ligas/<competição>/<temporada>/temporada.json
MataMata/<competição>/<temporada>/torneio.json
```

Use **Abrir pasta desta temporada** para localizar seus metadados. As pastas `Ligas` e `MataMata` ficam na mesma raiz de dados, ao lado de `Discos`. Uma falha de escrita pausa o avanço; os resultados não são descartados para seguir a competição.

## Estado da validação

A entrega contém o **projeto-fonte**, com scripts, assets e cenas escritos nesta versão. As cenas ainda não foram abertas ou renderizadas no Unity neste ambiente; não há captura de tela apresentada como teste real.

As verificações em `validation/` exercitam o calendário, CSV, persistência, continuidade dos torneios, geometria e contratos do código C#. Os testes que dependem de APIs Unity usam fachadas dessas APIs. Eles não substituem um Play no Editor com importação real, contatos da física, fontes, áudio e renderização. O conjunto de calendário e metadados passou por **37.629 verificações** com o parser CSV original e arquivos reais.

Antes da primeira gravação, abra cada cena no Unity e confira a partida, os gols, as aberturas e a retomada da temporada.
