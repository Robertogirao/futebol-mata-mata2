# Validação da interface de Futebol de Discos

Execute `sh validation/hud/run.sh` com Mono (`mcs` e `mono`) instalado. As variáveis `MCS` e `MONO` permitem selecionar outros executáveis.

O teste compila os arquivos reais `HudFutebolDiscos`, `LayoutJogadorUnicoUI`, `GoalBannerUI`, `ClassificacaoUI`, `TeamData` e `TemaCompeticao`. A fachada de Unity/UI/TMP representa hierarquia, ativação, referências, campos de layout e avanço de iteradores; `UniversalUI`, linhas e jogos da liga e a configuração raiz usam adaptadores com apenas os campos do contrato.

Verifica a ligação do placar, relógio, escudos, logotipos e fontes; a fila real de gols; margens do campo em 1920 × 1080; fichas abaixo do campo e abertura central; duração com `captureDeltaTime`; cancelamento; reconstrução no mesmo frame e descarte apenas dos objetos pertencentes ao HUD. Confere a prioridade do Canvas diante dos valores presentes nos arquivos reais de classificação, abertura e chaveamento. Exercita classificações com 2, 8, 10, 15, 20, 26, 28, 30 e 32 times: todas as linhas e confrontos cabem antes do rodapé; nomes, escudos e placares permanecem nas linhas; uma rodada termina e a próxima pode abrir.

Exercita a entrada, leitura e saída da classificação e a fila de dois gols a 30 e 60 fps de captura, além do avanço normal por `unscaledDeltaTime`, com a partida pausada (`timeScale = 0`). Os banners preservam três segundos de exibição e 0,15 segundo de intervalo, arredondados ao frame seguinte, sem depender do tempo gasto para gravar cada frame. Inclui cancelamento da fila e da classificação durante essas esperas.

A fachada não renderiza texto nem certifica apresentação visual, contatos de física ou o Editor Unity. A execução em Unity 6.4 continua sendo necessária para avaliar esses pontos.
