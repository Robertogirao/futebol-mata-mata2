# Futebol de Discos — capitães, posições e áudio

Projeto Unity do backup enviado em 14/11/2026, com os efeitos de partida ligados nas quatro cenas e música durante o jogo.

## Elenco vazio com capitães convidados

[**BAIXAR SOMENTE ESTA ATUALIZAÇÃO — ZIP**](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/heads/entrega/audio-partidas/Downloads/ATUALIZACAO_ELENCO_VAZIO.zip)

Com **Usar capitães convidados** ligado, o **Elenco Completo** das fichas de time pode ficar com **Size = 0**. O capitão continua usando o nome e a foto da própria ficha; os demais discos mantêm o escudo, a sigla e o número, como **BVB 2** ou **SPO 3**. Não altera a quantidade de discos nem sua identificação.

O ZIP contém apenas três scripts e instruções. Com o Unity fechado, copie `Assets` sobre a pasta do projeto e substitua os scripts. Não apaga elencos ou fichas; você pode esvaziar a lista manualmente. Esvaziar só o elenco não exige outra temporada se os mesmos times, capitães e configurações forem mantidos. Modos sem capitães convidados continuam exigindo um representante.

O catálogo de capitães e os participantes continuam sendo validados. Passaram 81 verificações da lógica C# com substitutos Unity; teste no editor com as APIs reais continua pendente.

## Contorno do capitão na cor do time

[**BAIXAR SOMENTE ESTA ATUALIZAÇÃO — ZIP**](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/heads/entrega/audio-partidas/Downloads/ATUALIZACAO_CONTORNO_CAPITAO.zip)

O contorno circular do capitão agora acompanha a cor usada pelos demais discos do time na partida, inclusive a alternativa escolhida para contraste. Tamanho e demais detalhes permanecem iguais.

O ZIP contém somente `Assets/FutebolDiscos/Scripts/DiscoAtleta.cs` e as instruções. Com o Unity fechado, copie `Assets` sobre a pasta do projeto atual e substitua esse script. Não exige nova temporada.

## Editor de retratos dos capitães

[**BAIXAR O EDITOR HTML — ZIP**](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/heads/entrega/audio-partidas/Downloads/CAPTAIN_PORTRAIT_EDITOR.zip)

Extraia e abra `CAPTAIN_PORTRAIT_EDITOR.html` no Chrome ou Edge. É uma ferramenta independente em um único arquivo HTML, que funciona offline. [Ver o código HTML](Tools/CAPTAIN_PORTRAIT_EDITOR.html).

- Arraste uma ou várias fotos, selecione cada retrato e ajuste posição e zoom na prévia.
- Gire, centralize, encaixe a imagem inteira ou restaure o enquadramento.
- Baixe um PNG ou todas as imagens em ZIP. Cada imagem mantém seu próprio ajuste enquanto a página estiver aberta.
- A saída é sempre **PNG de 512 × 512**, com recorte circular e cantos transparentes. As guias e o quadriculado não são exportados.
- A interface é em inglês. Não há bibliotecas externas, servidor ou upload das fotos.

O recorte preserva o fundo existente dentro do círculo. Para os PNGs no Unity, selecione **Sprite (2D and UI)** e use o campo **Foto** da ficha do capitão. Este download é uma ferramenta separada; não substitui scripts ou cenas do jogo.

## Correção dos textos em inglês

[**BAIXAR SOMENTE A CORREÇÃO DE IDIOMA — ZIP**](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/heads/entrega/audio-partidas/Downloads/ATUALIZACAO_TEXTOS_INGLES.zip)

Aplique sobre a versão atual com música no sorteio e placar final. O pacote contém apenas dois scripts.

Os rótulos gerados pela transmissão do jogo ficam em **inglês**: **FULL TIME**, **FINAL SCORE**, **WINNER**, **DRAW**, e as posições **DEFENSE**, **MIDFIELD**, **ATTACK** (siglas **DEF**, **MID**, **FWD**). Novas fichas usam **New Captain** como nome inicial. Nomes próprios e textos personalizados cadastrados pelo usuário permanecem como dados da competição.

Com o Unity fechado, copie `Assets` do ZIP sobre a pasta do projeto e substitua os dois scripts. As posições salvas e os overalls continuam compatíveis.

## Música no sorteio e apresentação do placar final

[**BAIXAR SOMENTE ESTA ATUALIZAÇÃO — ZIP**](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/heads/entrega/audio-partidas/Downloads/ATUALIZACAO_SORTEIO_E_PLACAR.zip)

Aplique sobre a versão que já recebeu a atualização dos capitães. O pacote contém somente os cinco scripts alterados; não substitui cenas, músicas ou fotos.

- Em **Projeto Futebol Discos → Sorteio na tela**, arraste uma faixa para **Música do sorteio** e ajuste volume, entrada e saída. Sem faixa, o sorteio permanece em silêncio. A música acompanha o sorteio e o quadro final, inclusive na reexibição, e termina com fade antes do campeonato.
- Em **Tempos da transmissão**, ajuste **Mostrar apresentação do placar final** e sua duração. O padrão é **3 segundos**, com entrada/saída suaves, escudos, nomes, placar grande e vencedor ou empate.
- A apresentação final aguarda a pausa pós-jogo e todas as comemorações de gol. Só depois avança para a próxima partida ou tabela. Opção desligada ou tempo zero pula a apresentação.

Com o Unity fechado, copie a pasta `Assets` do ZIP sobre a do projeto, aceitando substituir os scripts. Os ajustes visuais e de música podem ser alterados fora do Play sem criar outra temporada.

Detalhes e conferência: [docs/SORTEIO_E_PLACAR.md](docs/SORTEIO_E_PLACAR.md).

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
