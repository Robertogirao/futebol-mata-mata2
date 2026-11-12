# Liga Pokémon — projeto separado

Projeto Unity dedicado a pontos corridos, com calendário, classificação e progresso salvos automaticamente. A configuração inicial tem **10 Pokémon e 9 rodadas**.

## Correção do tamanho das bolas

A versão `pokemon-liga-v2026.11.11.3` corrige as bolas gigantes. O sprite substituto media 4,86 unidades antes da escala do prefab; agora a bola usa um sprite dedicado de uma unidade. Com a escala existente de 0,65, seu desenho acompanha o diâmetro físico de 0,65 unidade. A hélice e os logos mantêm a imagem original. Esta versão inclui as correções anteriores de inicialização, importação e APIs.

Para atualizar uma cópia já extraída, consulte [ATUALIZACAO.md](ATUALIZACAO.md).

- [Baixar ZIP pequeno de atualização](https://raw.githubusercontent.com/Robertogirao/futebol-mata-mata2/pokemon-liga-v2026.11.11.3/Downloads/Pokemon_Liga_Correcao_Tamanho_Bolas_2026-11-11.zip).
- [Baixar projeto completo atualizado](https://github.com/Robertogirao/futebol-mata-mata2/archive/refs/tags/pokemon-liga-v2026.11.11.3.zip).

## Abrir no Unity

1. Baixe o **Source code (zip)** desta versão e extraia em uma pasta nova.
2. No Unity Hub, adicione a pasta **Projeto** que está dentro do ZIP.
3. Use **Unity 6000.4.11f1**. Aguarde a importação e a resolução dos pacotes.
4. Abra **Assets/Scenes/Ligas/Liga_Pokemon.unity**, ou use **Tools → Pokémon → Abrir liga Pokémon**.
5. Selecione **Liga_Pokemon** na Hierarchy. Configure o componente **Liga Pokemon** e salve a cena antes do Play.

Este ZIP contém o projeto completo. Use uma pasta nova: a versão de liga remove scripts e cenas do mata-mata e não deve ser aplicada por cima do projeto antigo.

## Configurar a liga

No componente **Liga Pokemon**, você pode alterar:

- **Nome da liga**, tema base, **Fundo da arena** e **Fundo da classificação**. O fundo da classificação também aparece na abertura dos confrontos.
- **Quantidade de Pokémon**: botões para 10, 15 e 20, ou qualquer quantidade a partir de 2.
- **Pokémon da liga**: preencha cada vaga com uma ficha `PokemonDados`, ou use **Sortear Pokémon do catálogo**. O sorteio escolhe participantes distintos; não aplica filtros de geração ou tipo.
- **Quantidade de rodadas**, duração de cada partida, intervalo entre rodadas e abertura dos confrontos.
- **Semente do calendário**: zero sorteia uma vez ao criar a temporada. O calendário fica salvo e o próximo Play continua o mesmo campeonato. Uma semente fixa permite repetir o calendário com a mesma lista e ordem de participantes.

Uma rodada contém um jogo para cada Pokémon, com uma folga quando a quantidade é ímpar. A folga não dá pontos. Um turno completo tem:

| Participantes | Rodadas | Jogos no turno |
| --- | --- | --- |
| 10 | 9 | 45 |
| 15 | 15 | 105 |
| 20 | 19 | 190 |

Você pode escolher menos rodadas que um turno completo. Rodadas adicionais seguem para o returno, invertendo os lados dos confrontos. Com menos rodadas, nem todos enfrentarão todos.

## Classificação e continuidade

Vitória vale **3 pontos**, empate **1** e derrota **0**. A liga usa a classificação e os critérios de desempate do sistema existente. As partidas têm tempo definido e permitem empate.

Cada resultado concluído é salvo. Ao terminar a rodada, a classificação é confirmada automaticamente, a apresentação da tabela termina e a rodada seguinte começa. Se parar durante uma partida, essa partida será reiniciada no próximo Play; os jogos já concluídos não serão repetidos nem pontuados novamente.

**Uma rodada por Play** fica desmarcado por padrão. Marque se quiser parar na tabela ao fim de cada rodada. Nesse caso, o próximo Play retoma a seguinte; para avançar sem sair do Play, use **Continuar próxima rodada no Play**.

Ao concluir a temporada, o próximo Play mostra a classificação final salva, sem reiniciar os jogos.

## Nova temporada e arquivos salvos

Use **Nova temporada** antes de mudar os participantes, sua ordem, quantidade de Pokémon, quantidade de rodadas ou semente de uma temporada já iniciada. O botão cria outro identificador e preserva o histórico anterior. Salve a cena após a alteração.

**Abrir pasta da temporada** mostra os arquivos no computador. Eles ficam em `Application.persistentDataPath/Ligas/<ligaId>/<temporadaId>/`, incluindo o calendário Pokémon e o histórico da classificação. São arquivos locais; publicar o projeto no GitHub não envia automaticamente resultados futuros.

O projeto usa o nome de produto **Pokemon Beat The Keeper Liga**, separado do projeto de mata-mata, para isolar seus arquivos de progresso padrão.

## Conteúdo da cópia

Há uma cena de liga em `Assets/Scenes/Ligas` e somente ela está habilitada no Build Settings. Foram removidos o fluxo de mata-mata, o gerador de torneios, as cenas antigas e os assets de futebol sem dependências. Permanecem o catálogo de Pokémon, imagens, fundos ilustrados, arena, física, classificação e os especiais Snorlax, Snorlax gigante e Equipe Rocket. Classes compartilhadas necessárias ao jogo foram mantidas.

## Validação

O calendário passou por testes com 10, 15 e 20 participantes, folgas, returnos e quantidades variadas de rodadas. Os testes C# também cobrem salvamento, retomada parcial, confirmação automática, falhas de escrita, configuração da cena e controles do Inspector. Os relatórios estão em **Validacao**.

O Unity Editor não está instalado no ambiente de preparação. Os testes de integração usam substitutos de Unity/UI/física; a importação, a aparência e a execução em Play precisam ser conferidas no Unity 6000.4.11f1.
