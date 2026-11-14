# Como usar o Futebol de Discos

## 1. Abrir e escolher o formato

Abra esta pasta **Projeto** no Unity Hub com o **Unity 6000.4.11f1**. Não selecione apenas a pasta `Assets`.

No Editor, use:

- **Tools > Futebol de Discos > Abrir Liga**: cena `Assets/Scenes/FutebolDiscos/Liga_Discos.unity`, com 8 seleções e 7 rodadas inicialmente.
- **Tools > Futebol de Discos > Abrir Mata-mata**: cena `Assets/Scenes/FutebolDiscos/MataMata_Discos.unity`, com 16 clubes inicialmente.
- **Tools > Futebol de Discos > Capitães > Abrir Liga** ou **Abrir Mata-mata**: novas cenas em `Assets/Scenes/FutebolDiscos/Capitaes/`, com personagens convidados como capitães e resultados próprios.

Selecione **FutebolDiscos_Manager** na Hierarchy. O componente **Projeto Futebol Discos** reúne as configurações e mostra **Configurações da Liga** ou **Configurações do Mata-mata**, conforme a cena. Os componentes internos exibem apenas um atalho para esse painel no Inspector normal, evitando controles duplicados. Não é necessário marcar e desmarcar os modos nos scripts `TournamentManager`, `LigaCampeonato` ou `MataMataCampeonato`: o controlador configura o formato escolhido. Mantenha os componentes do manager habilitados.

O campo, os discos e o placar aparecem quando o Play começa. Salve a cena depois de alterar as configurações fora do Play.

## 2. Definir competição, equipes e sorteio

Em **Competição e temporada**:

- **Nome na transmissão** altera o título exibido.
- **Identificador da competição** separa os campeonatos nos salvamentos.
- **Identificador da temporada** identifica a edição que será retomada. Use letras sem acentos, números, `_` e `-` nesses dois identificadores.
- **Formato** escolhe Liga ou Mata-mata.

Em **Participantes**, escolha a quantidade e preencha os campos com assets `TeamData` do catálogo. Cada equipe precisa de nome e pelo menos um representante válido em seu elenco. Os nomes dos assets podem ter espaços e acentos, mas não devem se repetir ou ter espaços nas extremidades.

**Sortear times do catálogo** escolhe as equipes da lista. **Embaralhar os confrontos** muda a distribuição dos confrontos usando as equipes já escolhidas. São controles com funções diferentes.

**Semente do sorteio**:

- `0`: sorteio automático ao criar a temporada; a semente efetiva é salva no primeiro Play.
- Um número fixo: permite reproduzir a distribuição com a mesma lista, ordem e configuração.

Dar Stop e Play mantém o sorteio da mesma temporada. Para uma nova edição com outro sorteio, pare o Play e clique em **Nova temporada (preserva as anteriores)**. O botão cria outro identificador e coloca a semente em `0`; os arquivos anteriores continuam guardados. Sorteios independentes podem coincidir, sobretudo com poucas equipes.

## 3. Liga: equipes e rodadas

Escolha de **2 a 32 equipes** e de **1 a 100 rodadas**. Na liga, todos os campos de participantes devem estar preenchidos.

Um turno completo tem:

| Participantes | Rodadas por turno | Jogos por rodada |
| --- | --- | --- |
| Número par | Número de equipes menos 1 | Metade das equipes |
| Número ímpar | Número de equipes | Metade arredondada para baixo; uma equipe folga |

Exemplos: 10 equipes precisam de 9 rodadas por turno; 15 equipes precisam de 15; 20 equipes precisam de 19. O botão **Usar um turno completo** preenche esse valor. Você pode escolher menos rodadas ou continuar por vários turnos. O segundo turno repete os confrontos invertendo os lados; os seguintes continuam esse ciclo. Com quantidade ímpar, cada equipe folga uma vez por turno completo.

A classificação usa **3 pontos por vitória, 1 por empate e 0 por derrota**. A ordenação considera pontos, saldo de gols e vitórias, seguida do nome e identificador em caso de igualdade. O empate no tempo regulamentar é válido na liga.

Os resultados das partidas são salvos individualmente. Ao terminar todos os jogos, a rodada é confirmada e a classificação é preservada antes do próximo avanço.

## 4. Mata-mata: chave e desempate

Escolha **2, 4, 8, 16 ou 32 vagas**. Para usar menos equipes que vagas, marque **Permitir folgas** e deixe os campos restantes vazios. São necessárias pelo menos duas equipes distintas. As folgas são avanços automáticos na chave.

Em **Distribuição**:

- **1 x 2, 3 x 4...** forma os pares pela ordem da lista usada no chaveamento.
- **Cabeças de chave** usa a distribuição de cabeças de chave já existente no sistema.

Marque **Embaralhar os confrontos** para embaralhar os participantes antes dessa distribuição. A semente salva mantém a chave consistente entre Play e Stop.

Se o placar estiver empatado ao fim do tempo regulamentar, começa o **Golden Goal**. Os discos continuam jogando até a bola branca entrar em um gol; esse gol define o vencedor. Não há vencedor escolhido por sorteio nem placar inventado. Por isso a partida pode ultrapassar o tempo configurado, e o tempo total do vídeo não é fixo.

## 5. Dinâmica da partida

Cada equipe tem **1 a 5 discos**, com **3 por padrão**. Essa quantidade permanece fixa durante a partida. Há uma única bola branca, e somente ela marca gols. Os discos se movimentam e chutam automaticamente, com colisões físicas no campo e nos gols.

Nas cenas de capitães convidados, a opção de capitão extra soma um disco a essa quantidade, permitindo até **5 jogadores do time + 1 capitão**.

Em **Discos e bola**, ajuste:

| Controle | Efeito |
| --- | --- |
| Discos por time | Quantidade de discos de cada equipe |
| Diâmetro dos discos / da bola | Tamanho dos objetos de jogo |
| Velocidade dos discos | Ritmo de movimentação |
| Velocidade máxima da bola | Limite de velocidade da bola branca |
| Força do chute | Impulso aplicado nos chutes |
| Pausa depois do gol | Tempo de reposição antes de voltar a jogar |
| Manter cada time em sua metade | Restringe os discos às suas metades; a bola cruza o campo |
| Considerar o overall dos times | Usa a força das equipes no comportamento da partida |
| Diferença mínima entre as cores | Ajusta a separação visual das cores das equipes |

Após um gol, os objetos são reposicionados. O cronômetro regulamentar fica pausado durante a reposição. A duração padrão da partida é **45 segundos de jogo**. O relógio mostra os **90 minutos virtuais** do futebol distribuídos nessa duração; aberturas, fotos, reposições, tabelas e Golden Goal aumentam a duração da gravação.

### Escanteio e tiro de meta quando a bola prende

No mesmo painel central, abra **Bola presa nos cantos**. A cobrança só acontece quando a bola permanece perto das duas bordas de um canto sem conseguir sair; uma batida ou passagem normal não gera reposição.

| Controle | Padrão e efeito |
| --- | --- |
| Repor a bola presa no canto | Ativado; pode ser desligado |
| Tempo sem sair do canto (s) | 3 segundos; ajuste de 1 a 10 |
| Área de detecção do canto | 1,6 unidades de campo junto às duas bordas; ajuste de 0,8 a 3 |
| Pausa antes da cobrança (s) | 0,8 segundo; ajuste de 0,2 a 3 |

O sistema observa se a bola faz progresso: pequenos tremores e trocas de toque não reiniciam a espera. Se ela sair do canto ou se deslocar de forma relevante, a contagem começa de novo.

Se o último toque foi do defensor, há **escanteio para o atacante**. Se foi do atacante, há **tiro de meta para o defensor**. Sem um último toque válido, o sistema mantém o recurso anterior para soltar a bola, sem escolher uma equipe para a cobrança.

A transmissão mostra **CORNER KICK** ou **GOAL KICK** com o time beneficiado. O relógio pausa, os discos voltam à formação para abrir espaço e a cobrança é automática, com a bola lançada para dentro do campo. O placar e o tempo já jogado são mantidos. Isso funciona tanto na liga quanto no mata-mata, inclusive durante o Golden Goal. Esses ajustes não exigem nova temporada; altere fora do Play e salve a cena.

### Postura de cada time e pressão

Selecione a ficha `TeamData` no Project e abra **Tática e IA > Postura tática**. As quatro opções são **Ultra defensivo** (0 jogadores podem avançar para pressionar), **Defensivo** (até 1), **Balanceado** (até 2) e **Ataque** (até 3), limitadas ao total disponível de discos.

No **FutebolDiscos_Manager**, desmarque **Manter cada time em sua metade** para permitir o avanço. Se estiver marcado, o bloqueio geral prevalece para todas as posturas.

O limite não mantém os jogadores permanentemente adiantados. A posição e a direção da bola abrem ou fecham a oportunidade: um jogador vai à bola e os apoios podem subir por faixas diferentes. Na ameaça defensiva eles retornam à formação, sem teleporte. Uma vaga só é liberada para outro jogador depois que o anterior regressa. O capitão também pode avançar.

As escolhas das fichas antigas são preservadas: **Defesa** passa a aparecer como **Defensivo**, com o mesmo valor salvo. Altere as posturas fora do Play; elas afetam os próximos jogos, sem alterar os resultados concluídos. Para mudar o bloqueio geral da metade em uma temporada já salva, crie outra temporada, como nas regras anteriores.

### Capitães convidados: personagens com overall próprio

Abra **Tools > Futebol de Discos > Capitães > Abrir Liga** ou **Abrir Mata-mata**. As cenas são `Capitaes/Liga_Capitaes.unity` e `Capitaes/MataMata_Capitaes.unity`, dentro de `Assets/Scenes/FutebolDiscos/`. Seus identificadores `discos_liga_capitaes` e `discos_mata_capitaes` separam os resultados das cenas anteriores.

No Project, use **Create > Futebol de Discos > Capitão convidado** para criar uma ficha. Defina **Nome Capitao**, **Foto** importada como Sprite e **Overall** de 1 a 100. O nome do arquivo é seu identificador no salvamento; não o renomeie durante uma temporada em andamento. Há 16 fichas genéricas editáveis em `Assets/FutebolDiscos/Capitaes/Modelos/`, inicialmente sem foto e com overall 75.

No painel central, abra **Capitães convidados** e preencha **Capitães disponíveis**. **Carregar capitães do catálogo** encontra as fichas cadastradas, incluindo os modelos; retire da lista os exemplos que não quiser usar. É necessário um capitão diferente por time preenchido. **Associações fixas (opcional)** reserva combinações, como Real Madrid + Goku. Os capitães restantes são sorteados entre os demais times sem repetição.

**Capitão como disco extra** vem marcado: o controle **Jogadores do time (sem o capitão)** configura o elenco de futebol, ao qual é somado o convidado. Assim, 3 jogadores + Goku formam quatro discos; 5 jogadores + Goku formam seis. Desmarcada a opção, o convidado ocupa uma das vagas de **Discos por time**. O elenco e o overall do asset `TeamData` permanecem intactos.

O overall individual afeta somente a velocidade, potência e precisão dos chutes do convidado. Isso vale mesmo com **Considerar o overall dos times** desligado. Os jogadores do elenco continuam com seu comportamento normal; aumentar o overall de Goku não fortalece o restante do Real Madrid.

O convidado usa a foto e a legenda **CAPTAIN** na abertura, e nome/foto ao marcar. Os demais gols usam o nome do jogador de futebol e o escudo/bandeira. Sem foto cadastrada, o capitão usa o escudo/bandeira como alternativa.

O sorteio é salvo em `capitaes_convidados.json` na pasta da temporada, antes das partidas. Stop/Play mantém o mesmo capitão em cada time. Use **Nova temporada** para mudar as associações, o overall usado ou a regra de disco extra, mantendo os arquivos anteriores. Nome e foto são ajustes visuais e podem ser alterados sem trocar o sorteio. Veja [o guia completo da atualização](../Atualizacoes/COMO_APLICAR_CAPITAES_CONVIDADOS.md).

## 6. Nome, fontes, logos, fundos e fotos

No mesmo painel, abra **Identidade visual > Fontes, imagens e cores da transmissão**. O tema inicial é `Assets/FutebolDiscos/Tema_Discos.asset`.

Ali você encontra o fundo das aberturas, fundo do chaveamento, troféu/logo da competição, logo do canal, fontes dos títulos e dos textos, cores. O som das aberturas fica em **Sons e comemoração de gol**. As fontes usam assets **TextMeshPro (TMP_FontAsset)**. O logo da competição também é usado como troféu central do chaveamento.

Editar o tema altera o asset compartilhado pelas cenas que o utilizam. Para identidades independentes, duplique o tema no Project e associe uma cópia a cada cena. O nome exibido vem de **Nome na transmissão**, no controlador central.

**Imagem do campo (opcional)** recebe um Sprite. Vazio, usa o campo procedural desta versão. **Cor do gramado** e **Cor das linhas** ajustam sua aparência.

Em **Tempos da transmissão (segundos)**:

- **Duração da partida**: tempo regulamentar de jogo.
- **Abertura dos confrontos**: duração da apresentação anterior ao confronto.
- **Fotos ampliadas no centro**: tempo de destaque dos representantes antes do jogo.
- **Retorno das fotos aos cantos**: duração do deslocamento das fichas para suas posições de jogo.
- **Leitura da classificação / do chaveamento**: tempo de apresentação da competição.
- **Pausa após a partida**: intervalo mínimo aplicado uma vez após o jogo, aguardando o fim das comemorações.

### Tags dos jogadores e capitão

Em **FutebolDiscos_Manager > Identificação dos jogadores**, ative **Mostrar nome nos discos** e ajuste fonte e largura. O capitão é o representante da foto: recebe a letra **C**, uma borda dourada e, opcionalmente, a foto dentro do disco. Os outros discos usam os demais jogadores do **Elenco** da ficha `TeamData`. Cadastre mais jogadores para nomear todos os discos; vagas sem jogador mostram a sigla do time e o número do disco.

Nos gols, o capitão mostra nome e foto; os demais mostram nome e bandeira/escudo. Sem foto do capitão, usa a bandeira/escudo. Um gol contra identifica o autor com `(OG)`; sem último toque conhecido, a comemoração identifica somente o time.

### Textos do campo e rodapé

Abra **FutebolDiscos_Manager > Textos da arena** para mudar **Texto no campo**, **Título do rodapé** e **Subtítulo do rodapé**. Cada um pode ser desligado ou deixado vazio para ocultar. **Mostrar formato e quantidade de discos** controla a parte `LEAGUE/KNOCKOUT` e `3 v 3` separadamente. Você pode alterar esses textos e a identificação visual entre sessões, mantendo a mesma temporada.

### Aberturas e prévias futuristas

A liga e o mata-mata usam painéis holográficos, varredura luminosa e abertura vertical do centro para as bordas. As fichas dos capitães se materializam antes de seguir em arco para seus lugares; **CAPTAIN** continua sendo a legenda da foto. A classificação e o campeão também usam a nova apresentação.

Para alterar as palavras, expanda **FutebolDiscos_Manager > Tempos da transmissão (segundos) > Textos das aberturas e prévias**. Ali ficam os títulos **ARENA** e **CHALLENGERS**, a chamada **START**, o separador **VERSUS** e os textos de quadro, rodada, classificação e campeão. Textos vazios ficam ocultos.

Os controles de duração anteriores continuam valendo. Fontes, fundos, logos e cores são mantidos pelo tema. Essas alterações de apresentação não exigem outra temporada. Veja também [o guia desta atualização](../Atualizacoes/COMO_APLICAR_ABERTURAS_FUTURISTAS.md).

### Fundo externo, linha da foto e sons no painel central

Em **Identidade visual > Fundo externo da arena**, arraste um Sprite para preencher a área externa ao campo. Ajuste a opacidade e a opção de sobrepor a decoração. A proporção é preservada, com corte das bordas quando necessário para preencher a tela; campo e gols continuam acima da imagem. Importe arquivos novos como **Sprite (2D and UI)** e aplique a mudança no Inspector.

Em **Identificação dos jogadores**, a opção **Mostrar leitura luminosa atrás da foto** liga ou desliga o scanner do capitão. A linha está atrás do retrato e do texto, com brilho menor. **Velocidade da leitura luminosa** permite ajustar o movimento sem mudar a duração das fotos.

Em **Sons e comemoração de gol**, configure a duração da exibição do autor e o intervalo das comemorações. O intervalo entre partidas da liga é aplicado só uma vez; uma comemoração pendente ainda pode adiar a próxima apresentação para terminar sua exibição.

### Sons escolhidos no painel

No mesmo grupo, **Som do gol**, **Apito de início** e **Apito de encerramento** são os clipes usados pelos eventos da partida. Ajuste **Volume dos efeitos** e salve a cena fora do Play. Um campo de clipe vazio deixa aquele evento sem som; ele não busca um som antigo como alternativa.

Deixe **Fonte de áudio dos efeitos (opcional)** em **None** para usar a fonte exclusiva criada automaticamente. O jogo não reaproveita fontes de abertura ou música. Se preferir atribuir uma fonte, use uma exclusiva para os efeitos; ao prepará-la, o jogo interrompe a reprodução anterior e limpa o clipe fixo da fonte, usando os clipes dos três campos. Ao preparar outra partida, cancelar ou desativar a interface, os efeitos pendentes são interrompidos.

**Som das aberturas e prévias** aparece quando há um tema atribuído e usa `somAbertura` do tema, separado do gol e dos apitos. Esse controle altera o asset compartilhado pelas cenas que usam o tema. Duplique o tema para manter aberturas independentes na liga e no mata-mata. Sons podem ser trocados sem apagar resultados nem criar outra temporada.

Veja [as instruções de atualização dos sons](../Atualizacoes/COMO_APLICAR_SONS.md).

## 7. Gravar uma rodada, continuar e retomar

Com **Uma rodada por Play** ou **Uma fase por Play** desmarcado, a competição segue automaticamente. Marcado, ela espera após concluir a rodada ou fase. Use **Continuar no Play**, no painel central, para avançar sem sair do Play; também pode dar Stop e Play para continuar depois.

Os resultados concluídos ficam salvos. Uma partida interrompida antes de terminar recomeça no próximo Play; o jogo não guarda as posições dos discos nem o cronômetro de uma partida em andamento. Partidas já registradas e rodadas confirmadas não precisam ser disputadas novamente.

Para retomar, mantenha a competição, a temporada, a lista em sua ordem original, o formato, as rodadas, a semente e as regras da partida. Esses dados são conferidos contra os metadados salvos. Para mudar participantes ou regras, use **Nova temporada**. Nome, fontes, logos, fundos, cores e tempos de apresentação podem ser ajustados entre sessões.

Se houver falha de escrita, o avanço espera para preservar o resultado. Corrija o acesso à pasta e use **Continuar no Play** para tentar o salvamento pendente. Se a configuração divergir ou um arquivo estiver corrompido, o sistema preserva o arquivo e mostra o erro; não substitua arquivos de uma temporada por arquivos de outra.

## 8. Onde ficam os arquivos

O projeto usa o Product Name **Futebol de Discos**, isolando sua pasta de dados do projeto anterior. Os caminhos abaixo começam em `Application.persistentDataPath`:

```text
Discos/<competição>/<temporada>/configuracao_discos.json
Discos/<competição>/<temporada>/capitaes_convidados.json  (nas cenas com convidados)
Ligas/<competição>/<temporada>/temporada.json
MataMata/<competição>/<temporada>/torneio.json
```

Os metadados guardam a configuração, a semente efetiva e o calendário da liga. Nas cenas com convidados, o arquivo adicional guarda a associação entre times e capitães, o overall utilizado e a regra de capitão extra. Os arquivos em `Ligas` e `MataMata` guardam os resultados e o andamento de seus respectivos formatos. A liga também mantém suas exportações por rodada.

**Abrir pasta desta temporada** localiza a pasta dentro de `Discos`. Volte à raiz de dados para encontrar `Ligas` e `MataMata`. No Windows, com as configurações atuais, a raiz normalmente é `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Futebol de Discos`.

Não é necessário apagar a liga para criar um mata-mata. As duas cenas têm identificadores de competição separados por padrão, e o botão de nova temporada preserva as edições anteriores. Copiar o projeto sem alterar Product Name, competição e temporada pode apontar para os mesmos salvamentos locais.

## 9. Verificação desta entrega

Esta entrega é o projeto-fonte, sem executável compilado. Scripts e assets de cena foram escritos, mas as cenas não foram abertas nem renderizadas no Unity neste ambiente. Não há screenshot apresentado como execução real.

Há verificações de código, calendário, CSV, arquivos salvos, continuidade dos torneios, geometria e referências das cenas. Para as APIs Unity, os testes usam fachadas de API; isso não certifica contatos físicos, importação, áudio ou aparência no Editor. O conjunto de calendário e metadados passou por **37.629 verificações** com o parser CSV original e arquivos reais. Os detalhes estão na pasta `validation`, ao lado de `Projeto`.

Antes da primeira gravação, rode as cenas no Unity e confira a escala dos objetos, a bola branca marcando gols, as fichas de representantes, as fontes, o encerramento e a retomada após Stop/Play. Nas cenas de convidados, confira também nome/foto do capitão, os jogadores do elenco e a associação mantida entre sessões.
