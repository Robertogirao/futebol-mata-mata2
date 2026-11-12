# Pokémon Marble Race

Projeto Unity 2D separado, criado a partir da base Pokémon existente. Uma marble representa cada participante e corre automaticamente por pistas montadas por blocos, com obstáculos físicos, terrenos, poderes por tipo e classificação salva.

**[Baixar o projeto completo em ZIP](https://github.com/Robertogirao/futebol-mata-mata2/archive/refs/tags/pokemon-marble-race-v2026.11.12.zip)**

**Padrão da cena:** oito Pokémon, Mini GP de três corridas, tempo alvo de 45 segundos e limite de 60 segundos por prova. A interface do jogo está em inglês; os controles do Inspector estão em português.

## Abrir e jogar

1. Extraia o ZIP completo em uma pasta nova.
2. No Unity Hub, adicione a pasta **Projeto** que está dentro do ZIP.
3. Abra com **Unity 6000.4.11f1** e aguarde a importação dos assets e pacotes.
4. Abra **Assets/Scenes/MarbleRace/MarbleRace_Pokemon.unity**, ou use **Tools → Pokémon → Abrir Marble Race**.
5. Na aba Game, use **16:9**, preferencialmente **1920 × 1080**.
6. Selecione **MarbleRace_Manager** na Hierarchy, salve a cena e dê **Play**.

O jogo apresenta o grid, faz uma contagem de três segundos, executa a corrida, salva o resultado e exibe a ordem de chegada e a classificação acumulada. A prova seguinte começa automaticamente. Ao terminar o campeonato, a classificação final permanece na tela.

O circuito e a interface são montados ao dar Play. Durante a captura pelo Unity Recorder, os tempos de apresentação usam o passo de captura do vídeo.

O projeto completo inclui `Assets`, `Packages` e `ProjectSettings`. O Unity recria `Library`, arquivos de solução e outros caches na primeira abertura. A cena de liga anterior foi mantida como referência da base; somente a cena Marble Race está habilitada no Build Settings.

## Configurar o campeonato

No componente **Marble Race Manager**:

- **Nome do campeonato**, **Tema** e **Fundo**: alteram a identidade, as fontes do HUD e a imagem opcional atrás do circuito. As fontes do tema precisam ser `TMP Font Asset`.
- **Formato** e **Quantidade de participantes**: definem o campeonato. O Mini GP permite escolher a quantidade de corridas.
- **Participantes**: arraste as fichas `PokemonDados`, ou use **Sortear Pokémon do catálogo**. O catálogo e as imagens existentes foram reutilizados.
- **Gerações** e **Tipos**: permitem marcar vários filtros para o sorteio. As opções de um mesmo grupo são alternativas; os dois grupos são combinados. A escolha manual de participantes continua livre.
- **Tempo alvo**: ajusta a velocidade base para buscar a duração desejada. Colisões, terrenos, atalhos e poderes alteram a duração real.
- **Tempo máximo**: encerra uma prova longa. Quem não chegou é classificado pelo progresso no percurso, depois de quem terminou.
- **Abertura**, **Resultado** e **Intervalo**: controlam o ritmo de apresentação dos vídeos.
- **Poderes por tipo**: ativa ou desativa as vantagens dos tipos.
- **Tamanho da marble**: de 0,30 a 0,65 unidade; padrão 0,55. O visual e o colisor acompanham esse diâmetro, independentemente do tamanho original da imagem.
- **Uma corrida por Play**: para na classificação depois de cada prova. Use **Continuar no Play** para seguir sem encerrar o Play, ou pare e retome depois.

Altere as configurações fora do Play e salve a cena. Os filtros atuam no sorteio do catálogo; eles não geram todas as combinações possíveis de Pokémon.

## Formatos

| Formato | Participantes | Funcionamento |
| --- | --- | --- |
| Single Race | 2–16 | Uma corrida e classificação final |
| Mini GP | 2–16 | De 1 a 30 corridas com pontos acumulados |
| Best of Three | Exatamente 2 | Vence quem ganhar duas provas; termina em duas ou três corridas |
| Knockout | 2–32 | Baterias de até oito; a metade melhor colocada de cada bateria avança até a final |

No Mini GP, as primeiras oito posições recebem **10, 8, 6, 5, 4, 3, 2 e 1 ponto**. Posições seguintes recebem zero. Empates na classificação são resolvidos por vitórias, soma das posições, tempo acumulado e ordem estável dos participantes. No melhor de três, cada vitória vale um ponto. Nas eliminatórias, avançar de fase tem prioridade na classificação final.

Exemplo de eliminatória com 16 participantes: duas baterias de oito, quatro classificados de cada bateria e uma final com oito.

## Pistas automáticas

Cada prova recebe uma semente própria. O gerador combina blocos com curvas, terrenos e obstáculos em um circuito de três faixas conectadas por curvas de retorno. Ele varia a sequência dos blocos, pequenas mudanças no traçado, velocidades e posições dos obstáculos e a presença de uma passagem alternativa.

O circuito pode conter hélices, bumpers, barreiras móveis, pads de impulso e um Snorlax que abre e fecha a passagem. Atalhos sinalizados pela Equipe Rocket permitem contornar trechos; não são necessariamente mais curtos em distância. As marbles escolhem automaticamente se usam a passagem alternativa.

**Semente zero** sorteia um campeonato ao criar a temporada e salva a semente efetiva. O próximo Play retoma os mesmos percursos. Uma nova temporada com semente zero sorteia outra combinação. Uma semente fixa permite repetir os percursos com a mesma configuração. O percurso usa uma versão de algoritmo registrada no campeonato.

## Poderes e movimento

As marbles usam `Rigidbody2D`, colisores circulares, paredes e obstáculos reais do Unity 2D. A direção é assistida para seguir o percurso; elas continuam sujeitas aos contatos físicos. O movimento não usa o lançador, o sensor de gols ou o controlador do goleiro.

Água, Fogo, Gelo e Planta recebem vantagens nos respectivos terrenos. Elétrico usa turbo; Gelo pode reduzir a velocidade de rivais próximos; Fantasma atravessa obstáculos temporariamente, mantendo as paredes do circuito; Pedra tem maior resistência. Os demais tipos recebem um impulso periódico de seu tipo.

Se uma marble ficar sem avançar por alguns segundos, o sistema a recoloca no progresso já alcançado e aplica uma pequena pausa. A recuperação não concede avanço gratuito. Marbles que terminam deixam de bloquear a chegada das outras.

## Salvamento e retomada

O resultado é gravado antes de os pontos mudarem e antes de começar outra corrida. Se parar no meio de uma prova, o próximo Play reinicia somente essa prova, mantendo os resultados anteriores e o mesmo percurso. Um campeonato concluído reabre na classificação final.

Use **Abrir pasta da temporada** no Inspector. Os arquivos ficam em:

`Application.persistentDataPath/MarbleRace/<campeonatoId>/<temporadaId>/`

São gravados `championship.json`, um backup recuperável e `standings.csv`. Esses resultados são locais; não são enviados automaticamente ao GitHub. O Product Name **Pokemon Marble Race** separa os arquivos deste jogo dos da liga anterior.

Use **Nova temporada** antes de alterar participantes, formato, quantidade de provas, duração, tamanho, poderes ou semente de um campeonato já iniciado. O botão preserva as temporadas anteriores. Nome, fontes e fundo são ajustes visuais.

Uma falha de escrita pausa a corrida e mantém o resultado pendente. Depois de resolver o problema de acesso à pasta, use **Continuar no Play** para tentar salvar novamente. Uma falha apenas na exportação CSV não invalida o campeonato salvo em JSON. Se for necessário recuperar um backup, o Console informa que a última prova posterior ao backup pode precisar ser repetida.

## Validação e limites

Os relatórios estão em **Validacao**. Foram executados testes do gerador, quatro formatos de campeonato, persistência e recuperação, interface, geometria, controles do editor e integração do fluxo completo. As referências da cena e os GUIDs também foram conferidos.

**O Unity Editor não está instalado no ambiente de preparação.** O código de domínio foi executado diretamente em C#. A integração usa contratos simulados de Unity/UI e uma simulação de velocidades que não reproduz colisões. Esses testes não equivalem à importação, renderização ou ao Play Mode real. A aparência, as colisões e o ritmo final precisam ser conferidos no Unity 6000.4.11f1; o tempo alvo de 45 segundos não é uma medição da física real.
