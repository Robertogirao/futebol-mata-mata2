# Futebol de Discos — capitães, posições e áudio

Projeto Unity do backup enviado em 14/11/2026, com os efeitos de partida ligados nas quatro cenas e música durante o jogo.

## Atualização dos capitães

[**BAIXAR SOMENTE A ATUALIZAÇÃO DOS CAPITÃES — ZIP**](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/heads/entrega/audio-partidas/Downloads/ATUALIZACAO_CAPITAES.zip)

São apenas **10 scripts** para aplicar sobre o projeto que já recebeu a última atualização de áudio/toque na bola. Com o Unity fechado, copie a pasta `Assets` do ZIP sobre a do projeto e substitua os scripts. O pacote não substitui cenas, músicas ou fotos.

- O overall e a posição do capitão convidado ficam visíveis na ficha da partida, junto ao disco e no anúncio de revelação do sorteio.
- Na ficha do capitão no Project, ajuste `Overall` (1 a 100) e `Posicao`: **Defesa, Meio ou Ataque**. Configure antes do sorteio; numa temporada já salva, use **Nova temporada** para mudar esses valores.
- A posição muda sua vaga de base e a preferência para pressionar. Outro disco cobre sua antiga vaga defensiva quando há companheiros. A IA continua reagindo à bola e respeitando a postura do time e a restrição à própria metade.
- Fichas e temporadas antigas sem posição mantêm **Defesa**, como antes.

O overall já influencia a velocidade de movimento, a potência do chute e a variação adicional da mira; os multiplicadores anteriores foram preservados. Valor maior dá vantagem individual, sem garantir vitória. O capitão convidado usa seu overall mesmo quando o overall dos times está desligado.

Detalhes e conferência no Unity: [docs/CAPITAES.md](docs/CAPITAES.md).

## Atualização anterior de áudio

[**ATUALIZAÇÃO DE ÁUDIO — ZIP**](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/heads/entrega/audio-partidas/Downloads/ATUALIZACAO_AUDIO.zip)

O ZIP contém somente os **13 arquivos alterados/adicionados** em relação ao `BACKUP.part1.rar`, `BACKUP.part2.rar` e `BACKUP.part3.rar` enviados nesta conversa, além das instruções. As músicas e os efeitos já presentes no backup não são duplicados.

Com o Unity fechado, extraia o ZIP e copie a pasta `Assets` sobre a pasta `Assets` do projeto desse backup, aceitando mesclar pastas e substituir os arquivos. O pacote atualiza os scripts e as quatro cenas. Depois abra o projeto no **Unity 6000.4.11f1**.

## Cenas

- `Assets/Scenes/FutebolDiscos/Liga_Discos.unity`
- `Assets/Scenes/FutebolDiscos/MataMata_Discos.unity`
- `Assets/Scenes/FutebolDiscos/Capitaes/Liga_Capitaes.unity`
- `Assets/Scenes/FutebolDiscos/Capitaes/MataMata_Capitaes.unity`

## Áudio

- **Toque na bola:** novo efeito original curto, com volume próprio (45% do volume dos efeitos). Toca nos contatos com jogadores e nos chutes, incluindo cobranças. Um intervalo de 0,08 s por atleta evita duplicar o mesmo toque; outro jogador pode tocar logo em seguida. Paredes e contato contínuo sem chute não repetem o efeito.
- **Apito inicial, apito final e gol:** configurados nas quatro cenas, usando os efeitos de `Assets/APITOS`.
- **Abertura e prévias:** o som continua por **0,6 segundo** após a apresentação e termina com **fade out de 1,5 segundo**. A transição entre abertura geral e prévia do mata-mata preserva a saída suave.
- **Música do jogo:** `Warpath`, já incluída no backup, toca em loop durante a partida, em **25% de volume**, com **fade in e fade out de 2 segundos**. Continua no Golden Goal e sai ao encerrar a partida.
- Os efeitos, a abertura e a música usam fontes independentes. Os fades continuam funcionando no pós-jogo com a simulação pausada.

Para mudar os sons e as durações, selecione o objeto com **Projeto Futebol Discos** e abra **Sons e comemoração de gol** no Inspector, fora do Play Mode. Salve a cena após alterar os valores. O clipe de abertura pertence ao tema e é compartilhado pelas cenas que usam esse tema.

Cancelar uma partida inicia a saída suave da música. Desativar ou destruir a cena interrompe suas fontes para que o áudio não continue em outra cena.

## Verificação

A sintaxe dos 35 scripts C# e as referências dos cinco áudios de partida nas quatro cenas foram verificadas. **Compilação, reprodução e equilíbrio dos volumes em Play Mode ainda precisam ser conferidos no Unity**, que não está instalado no ambiente de edição.

Confira em cada cena: abertura, início, gol, fim, próximo jogo e cancelamento; no mata-mata, confira também a abertura geral seguida da prévia e o Golden Goal.

Esta branch contém a versão recebida no backup e os ajustes de áudio. A branch `main` mantém sua versão anterior.
