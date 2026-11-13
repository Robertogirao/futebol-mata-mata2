# Aberturas e prévias futuristas — 2026.11.13.5

1. Pare o **Play** no Unity.
2. Extraia `Atualizacao_Aberturas_Futuristas.zip`.
3. Copie a pasta **Assets** extraída para a raiz do seu projeto atual e aceite as substituições. O pacote contém sete scripts e o `.meta` do novo roteiro de textos.
4. Aguarde a recompilação, selecione **FutebolDiscos_Manager**, ajuste as chamadas se desejar e salve a cena.

O pacote atualiza somente a apresentação e seus controles. Suas cenas, fichas, fontes, imagens, partidas salvas e configurações do jogo permanecem no projeto atual.

## O que mudou

- **Abertura da liga:** placas luminosas montadas verticalmente, escudos em projeção, linhas de varredura e eixo de energia. As chamadas iniciais são **ARENA**, **CHALLENGERS**, **VERSUS** e **START**.
- **Prévias do mata-mata:** o quadro se recolhe para dar lugar a painéis holográficos dos dois times, com scanners e núcleo de energia.
- **Transição para o campo:** dois painéis se abrem do centro para cima e para baixo, substituindo a cortina lateral. A tela permanece coberta enquanto a partida é preparada.
- **Fotos dos capitães:** materialização por leitura luminosa e deslocamento em arco até as posições de jogo. A legenda **CAPTAIN** foi mantida.
- **Classificação e campeão:** bordas segmentadas, varredura e novas chamadas, incluindo **LEAGUE STATUS**, **NEXT ENCOUNTERS**, **MATCH DATA** e **ARENA CHAMPION**.

## Onde mudar as palavras e os tempos

Em **FutebolDiscos_Manager > Tempos da transmissão (segundos)**, expanda **Textos das aberturas e prévias**. Ali você pode alterar os títulos, o separador dos times, a chamada para começar, os rótulos de rodada/partida e os textos de classificação e campeão. Um texto vazio oculta a respectiva chamada.

Os controles de duração existentes continuam valendo para abertura, fotos, deslocamento das fichas e leitura da tabela/chaveamento. A montagem dos novos efeitos ocorre dentro desses tempos. A cobertura de abertura/prévia mantém o mínimo de três segundos já existente.

Fontes, logos, fundos e cores continuam vindo do tema da competição. Os ajustes de texto e apresentação podem ser alterados entre sessões sem criar outra temporada.

## Verificação

Os testes de código passaram para liga, mata-mata, cancelamento e retomada, textos personalizados, cobertura da tela e captura a 30/60 fps. As animações foram verificadas por contratos de API e geometria; o Unity Editor não foi executado neste ambiente. Confira o visual no Play antes de gravar seus vídeos.
