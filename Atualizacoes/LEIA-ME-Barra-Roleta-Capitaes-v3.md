# Chute mais lento e painel com pulso suave — v3

Extraia `Atualizacao_Barra_Roleta_Capitaes_v3.zip` na pasta **Projeto**, onde fica **Assets**, substituindo os dois scripts solicitados. Este pacote aplica-se à atualização **v2 já instalada** e contém somente:

- `Assets/FutebolDiscos/Scripts/PartidaDiscos.cs`
- `Assets/Scripts/LayoutJogadorUnicoUI.cs`

A animação do chute ficou mais lenta: HIT dura **1,4 segundo** e MISS dura **0,9 segundo**. O gol continua sendo registrado somente ao fim do voo da bola.

Depois que a roleta termina, o fundo do painel clareia e escurece suavemente enquanto aguarda o próximo toque do capitão. O ciclo completo dura **2,6 segundos**, sem cortes ou piscadas rápidas. O fundo permanece totalmente opaco, mantendo foto, roleta e textos legíveis. A barra e o texto de prontidão acompanham esse ritmo mais lento. O pulso de espera para ao executar o chute, ocultar o painel ou encerrar a partida.

Os textos da transmissão continuam em inglês. A cor escolhida no Inspector e as regras da v2 permanecem em uso.

Foram compilados os scripts reais contra fachadas Unity/TMP e passaram **1.257 verificações na branch e 1.212 nos arquivos do BACKUP atualizado**. Os testes confirmam os novos tempos, pontuação única, retomada/cancelamento e pulso somente na espera de HIT ou MISS, com opacidade constante. O relatório está em `validation/captain-special/results.txt`. O Editor Unity/GPU não foi executado neste ambiente.
