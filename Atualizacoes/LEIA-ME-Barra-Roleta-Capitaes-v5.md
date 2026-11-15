# Roleta refinada e comemoração do gol especial — v5

Extraia `Atualizacao_Barra_Roleta_Capitaes_v5.zip` na pasta **Projeto**, onde fica **Assets**, substituindo os quatro scripts. Este pacote aplica-se à **v4 já instalada** e não exige alterar cenas, prefabs, fotos ou configurações.

## Roleta

A roleta tem novos aros metálicos dourados, 48 marcações, setores verde/vermelho com profundidade, divisórias e acabamento central. O ponteiro recebeu sombra, haste dourada e uma ponta em destaque. As chances por overall, a posição final do ponteiro e o tamanho dos painéis continuam os mesmos.

## Banner do gol especial

Somente um gol marcado pelo chute especial de acerto recebe a chamada **SPECIAL GOAL!**, com o nome e a foto do autor, moldura dourada, brilho em movimento, entrada suave com deslocamento/escala e saída gradual.

O resultado da roleta e o início do chute não exibem o banner antecipadamente: ele entra na fila quando o voo especial termina e o gol é registrado. Os gols comuns continuam usando **GOAL!** e a comemoração normal. A fila guarda o tipo de cada gol e restaura a aparência normal entre as comemorações, inclusive em sequências de gols comuns e especiais.

O tempo configurado de exibição é respeitado. A animação também funciona durante as pausas de fim de jogo/Golden Goal e usa o tempo de captura do Recorder quando disponível. Os novos textos exibidos na transmissão estão em inglês.

As barras, o pulso lento, a espera pelo próximo toque, os tempos dos chutes e o rodapé personalizado da v4 permanecem em uso.

## Arquivos

- `Assets/Scripts/RoletaCapitaoUI.cs`
- `Assets/Scripts/LayoutJogadorUnicoUI.cs`
- `Assets/Scripts/GoalBannerUI.cs`
- `Assets/FutebolDiscos/Scripts/PartidaDiscos.cs`

O ZIP preserva as diferenças do BACKUP enviado (incluindo sons, posições e apresentação final). As fontes do GitHub recebem as mesmas melhorias sobre a base da branch.

Os scripts reais são compilados e testados com fachadas Unity/TMP; o relatório, os comandos e os hashes estão em `validation/captain-special/results.txt`. As verificações cobrem proporções da roleta, pontuação única, tipo de banner por gol, fila mista, restauração do visual, cancelamento, reconstrução do HUD e tempo de captura. O Editor Unity e a renderização em GPU não foram executados neste ambiente.
