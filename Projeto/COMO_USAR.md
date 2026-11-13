# Como usar o Futebol de Discos

## 1. Abrir e escolher o formato

Abra esta pasta **Projeto** no Unity Hub com o **Unity 6000.4.11f1**. Não selecione apenas a pasta `Assets`.

No Editor, use:

- **Tools > Futebol de Discos > Abrir Liga**: cena `Assets/Scenes/FutebolDiscos/Liga_Discos.unity`, com 8 seleções e 7 rodadas inicialmente.
- **Tools > Futebol de Discos > Abrir Mata-mata**: cena `Assets/Scenes/FutebolDiscos/MataMata_Discos.unity`, com 16 clubes inicialmente.

Selecione **FutebolDiscos_Manager** na Hierarchy. O componente **Projeto Futebol Discos** reúne as configurações. Não é necessário marcar e desmarcar os modos nos scripts `TournamentManager`, `LigaCampeonato` ou `MataMataCampeonato`: o controlador configura o formato escolhido. Mantenha os componentes do manager habilitados.

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

## 6. Nome, fontes, logos, fundos e fotos

No mesmo painel, abra **Identidade visual > Fontes, imagens e cores da transmissão**. O tema inicial é `Assets/FutebolDiscos/Tema_Discos.asset`.

Ali você encontra o fundo das aberturas, fundo do chaveamento, troféu/logo da competição, logo do canal, fontes dos títulos e dos textos, cores e som de abertura. As fontes usam assets **TextMeshPro (TMP_FontAsset)**. O logo da competição também é usado como troféu central do chaveamento.

Editar o tema altera o asset compartilhado pelas cenas que o utilizam. Para identidades independentes, duplique o tema no Project e associe uma cópia a cada cena. O nome exibido vem de **Nome na transmissão**, no controlador central.

**Imagem do campo (opcional)** recebe um Sprite. Vazio, usa o campo procedural desta versão. **Cor do gramado** e **Cor das linhas** ajustam sua aparência.

Em **Tempos da transmissão (segundos)**:

- **Duração da partida**: tempo regulamentar de jogo.
- **Abertura dos confrontos**: duração da apresentação anterior ao confronto.
- **Fotos ampliadas no centro**: tempo de destaque dos representantes antes do jogo.
- **Retorno das fotos aos cantos**: duração do deslocamento das fichas para suas posições de jogo.
- **Leitura da classificação / do chaveamento**: tempo de apresentação da competição.
- **Intervalo entre partidas**: pausa entre os jogos.

## 7. Gravar uma rodada, continuar e retomar

Com **Uma rodada por Play** ou **Uma fase por Play** desmarcado, a competição segue automaticamente. Marcado, ela espera após concluir a rodada ou fase. Use **Continuar no Play**, no painel central, para avançar sem sair do Play; também pode dar Stop e Play para continuar depois.

Os resultados concluídos ficam salvos. Uma partida interrompida antes de terminar recomeça no próximo Play; o jogo não guarda as posições dos discos nem o cronômetro de uma partida em andamento. Partidas já registradas e rodadas confirmadas não precisam ser disputadas novamente.

Para retomar, mantenha a competição, a temporada, a lista em sua ordem original, o formato, as rodadas, a semente e as regras da partida. Esses dados são conferidos contra os metadados salvos. Para mudar participantes ou regras, use **Nova temporada**. Nome, fontes, logos, fundos, cores e tempos de apresentação podem ser ajustados entre sessões.

Se houver falha de escrita, o avanço espera para preservar o resultado. Corrija o acesso à pasta e use **Continuar no Play** para tentar o salvamento pendente. Se a configuração divergir ou um arquivo estiver corrompido, o sistema preserva o arquivo e mostra o erro; não substitua arquivos de uma temporada por arquivos de outra.

## 8. Onde ficam os arquivos

O projeto usa o Product Name **Futebol de Discos**, isolando sua pasta de dados do projeto anterior. Os caminhos abaixo começam em `Application.persistentDataPath`:

```text
Discos/<competição>/<temporada>/configuracao_discos.json
Ligas/<competição>/<temporada>/temporada.json
MataMata/<competição>/<temporada>/torneio.json
```

Os metadados guardam a configuração, a semente efetiva e o calendário da liga. Os arquivos em `Ligas` e `MataMata` guardam os resultados e o andamento de seus respectivos formatos. A liga também mantém suas exportações por rodada.

**Abrir pasta desta temporada** localiza a pasta dentro de `Discos`. Volte à raiz de dados para encontrar `Ligas` e `MataMata`. No Windows, com as configurações atuais, a raiz normalmente é `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Futebol de Discos`.

Não é necessário apagar a liga para criar um mata-mata. As duas cenas têm identificadores de competição separados por padrão, e o botão de nova temporada preserva as edições anteriores. Copiar o projeto sem alterar Product Name, competição e temporada pode apontar para os mesmos salvamentos locais.

## 9. Verificação desta entrega

Esta entrega é o projeto-fonte, sem executável compilado. Scripts e assets de cena foram escritos, mas as cenas não foram abertas nem renderizadas no Unity neste ambiente. Não há screenshot apresentado como execução real.

Há verificações de código, calendário, CSV, arquivos salvos, continuidade dos torneios, geometria e referências das cenas. Para as APIs Unity, os testes usam fachadas de API; isso não certifica contatos físicos, importação, áudio ou aparência no Editor. O conjunto de calendário e metadados passou por **37.629 verificações** com o parser CSV original e arquivos reais. Os detalhes estão na pasta `validation`, ao lado de `Projeto`.

Antes da primeira gravação, rode as duas cenas no Unity e confira a escala dos objetos, a bola branca marcando gols, as fichas de representantes, as fontes, o encerramento e a retomada após Stop/Play.
