# Validação das correções

O projeto usa Unity **6000.4.11f1**. As alterações foram verificadas em C# com substitutos das APIs Unity e TMPro; o Unity Editor não estava instalado no ambiente de execução.

- BallController: 11 verificações de inatividade de 0,5 segundo, reinício ao se mover, pausa, proteção de gol e configuração.
- Interface e TournamentManager: 211 verificações de centralização, leitura de 5 segundos, transição de 0,8 segundo, espera das cortinas, cancelamento, repetição e passo de captura do Recorder.
- MataMataCampeonato e MataMataModelo: 33 verificações do início assíncrono e cancelamento do confronto.
- Compilação dos scripts alterados com substitutos das dependências: sem erros ou avisos.
- Atualização do troféu: ChaveamentoUI, TemaCompeticao e MataMataModelo reais compilados; 68 verificações da imagem compartilhada no topo/centro/final/campeão, preservação de proporções e cores, logo do canal, ausência de imagem e troca de temas.
- Atualização dos tempos: cinco scripts reais compilados juntos; 474 verificações dos controles personalizados de fotos/transição, independência da liga, início tardio, cortinas, cancelamentos e Recorder.
- Os dois ZIPs tiveram integridade CRC e hashes SHA256 conferidos.

Esses testes verificam a lógica dos scripts. A compilação com as bibliotecas reais, a aparência, o áudio e a física em Play Mode devem ser conferidos no Unity.

## Conferência no editor

1. Abra uma cena de liga e uma de mata-mata.
2. Após o anúncio e a cortina, confira as fichas grandes no centro por 5 segundos.
3. Confira que o relógio fica em 00:00 e não há lançamentos durante a apresentação.
4. Após a transição das fichas para os cantos, confira o apito e o início da partida.
5. Confira que bolas presas são removidas após cerca de 0,5 segundo de tempo do jogo.
6. Grave um trecho curto com Recorder para conferir o ritmo.

## Troféu e controles de duração

1. Confira que o troféu central corresponde a Logo Competicao do tema, inclusive no campeão.
2. Troque o tema e confira que não permanece a imagem anterior.
3. No Mata Mata Campeonato, ajuste Tempo Fotos Jogadores e Tempo Transição Fotos junto dos demais tempos.
4. Teste os valores personalizados e confira que a partida só inicia após a transição.
