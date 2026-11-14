# Música no sorteio e placar final

## Escolher música do sorteio

Em **Projeto Futebol Discos → Sorteio na tela**, arraste uma faixa importada no Unity para **Música do sorteio**. Você pode usar uma das faixas que já estão em `Assets/MUSICAS` ou importar outra. O volume padrão é 35%, com fade in e fade out de 1,5 segundo. Sem faixa selecionada, o sorteio fica em silêncio.

A música toca em loop durante apresentação, sorteio e quadro final, tanto num sorteio novo quanto ao reexibir um sorteio salvo. Usa uma fonte separada da música da partida, das aberturas e dos efeitos. O fade termina antes de avançar para a apresentação do campeonato. Se o quadro final for curto ou tiver tempo zero, permanece visível pelo tempo necessário para concluir a saída do áudio. Cancelar ou desativar a apresentação interrompe a fonte.

## Apresentação do placar final

Em **Tempos da transmissão (segundos)**, ajuste **Mostrar apresentação do placar final** e **Apresentação do placar final (s)**. O padrão é ligado por três segundos, incluindo entrada e saída suaves. Opção desligada ou duração zero pula a apresentação.

O cartão central mostra escudos, nomes, placar grande, “FIM DE JOGO” e vencedor ou empate. Aparece depois da pausa pós-jogo e de todas as comemorações de gol. A simulação continua parada enquanto o cartão é exibido, e o campeonato só recebe o resultado e avança depois de sua saída. Também funciona no Golden Goal.

Cancelar, desativar ou configurar outra partida oculta o cartão e invalida a apresentação anterior, evitando que ela avance ou interfira no jogo seguinte.

## Atualizar

O ZIP `Downloads/ATUALIZACAO_SORTEIO_E_PLACAR.zip` contém cinco scripts e as instruções. Use sobre o projeto já atualizado com capitães/OVR/posição, áudio e toque na bola. Feche o Unity, extraia e copie `Assets` sobre a pasta do projeto, substituindo os scripts. Cenas, músicas e fotos não são substituídas. Esses controles de transmissão podem ser alterados fora do Play sem abrir uma nova temporada.

## Validação

Passaram **985 verificações C# com substitutos Unity**: 577 da música do sorteio, 142 da sequência de encerramento e 266 do HUD. O HUD e a UniversalUI completos foram compilados com stubs; a música foi testada com métodos extraídos do SorteioCapitaesUI e o AudioTransicao completo. A sintaxe dos 35 scripts e a aplicação do ZIP sobre a versão anterior também foram verificadas.

Cobertura: sorteio normal/replay, sem música, volume zero, fades, quadro final zero/curto, captura de vídeo, fontes após cancelamento; pausa, fila de gols, Golden Goal, apresentação desativada, cancelamento e nova partida; placar/vencedores/empate numéricos, duração, transições e limpeza do HUD.

O editor Unity não está disponível no ambiente. Conferir compilação com as APIs reais, aparência, reprodução e física em Play Mode continua necessário. Nos modos com capitães, teste sorteio novo e replay com e sem música; nas quatro cenas, teste resultado com vitória, empate de liga, Golden Goal e cancelamento durante o cartão final.
