# Basquete de gude 3D — V4: cruz contínua com ajuda leve

Atualização para Unity **6000.4.11f1**. A cruz lançadora gira continuamente durante a partida. As bolinhas caem sobre as pás e o contato físico inicia a tentativa, sem acomodação, preparação ou espera por outra bola. A assistência inicial é **leve**, conforme solicitado. Mantém a plataforma com dois rotores, quatro bolinhas por time e a integração com o chaveamento.

## Instalação

1. Feche o Unity e extraia **ATUALIZACAO_BASQUETE_3D_V4.zip** fora do projeto.
2. Copie a pasta **Assets** do pacote para a raiz da sua cópia do projeto NBA, onde já existem Assets, Packages e ProjectSettings. Aceite substituir os arquivos correspondentes em `Assets/MarbleBasket3D` e preserve os `.meta`.
3. Abra o projeto. Use **Tools → Basquete 3D → Abrir cena de simulação** e entre em **Play**.
4. Confira **BOLAS 4/4** para vermelho e azul. Cada contador inclui as bolinhas em retorno.

Não é necessário executar o instalador antigo de basquete 2D. A atualização repõe os valores iniciais do arquivo de configurações e a cena fornecida. O circuito é construído ao entrar em Play. Para vê-lo antes, use **Tools → Basquete 3D → Gerar arena visível no Editor**.

## O que mudou

- **Plataforma de disputa:** 6,4 m de largura, com inclinação suave de 4,5° para conduzir as bolas à frente. O grande funil inclinado deixou de envolver os obstáculos.
- **Dois rotores de três pás:** raio de 1,05 m, geometria espelhada e sentidos opostos. O motor inverte gradualmente o sentido a cada 5 s. As pás têm colisores e movem Rigidbodies cinemáticos; os desvios resultam dos contatos físicos.
- **Quedas pelas laterais:** cada lado tem uma abertura de 2,7 m, sem parede atravessando a passagem. As áreas de coleta ficam abaixo dessas aberturas e enviam a mesma bolinha ao retorno. Defletores passivos nas entradas direcionam o impulso das rampas para a disputa.
- **Queda sobre a cruz:** a saída frontal leva a uma canaleta elevada, com término aberto sobre a roda. Duas guias transparentes limitam o movimento para frente e para trás, deixando a bolinha cair na região das pás. A canaleta termina antes da parede de retenção, com espaço para a esfera passar inteira para baixo.
- **Lançador compacto:** quatro pás numa roda vertical com raio de 0,65 m. O conjunto das pás tem aproximadamente 1,6 m de diâmetro, cerca de um quarto da largura da plataforma. Não há um disco opaco grande escondendo as bolinhas.
- **Rotação contínua:** 90 RPM iniciais, com oscilação suave de ±6 RPM. As quatro pás seguem girando mesmo quando não há bolinhas próximas. Não há encaixe, captura, temporizador de arremesso ou bloqueio da entrada. A rotação para na pausa e no fim da partida.
- **Tentativa por impacto:** somente um contato real com a cruz cria uma tentativa. Contatos repetidos durante o mesmo voo não multiplicam o contador nem reaplicam a assistência. Uma nova passagem pelo circuito ou pela segunda chance permite nova tentativa. Bolinhas que atravessarem a região sem tocar na roda são recolhidas abaixo dela.

## Assistência leve no impacto

Com **Impact Assist** ligado, o contato recebe uma pequena correção de velocidade em direção a um arremesso possível para o aro. O valor inicial **Impact Assist Strength = 0,25** limita a mudança a 25% do módulo da velocidade física da pancada. Esse valor não é uma chance de cesta. Uma pancada forte para o lado errado continua podendo sair pelo lado errado. Contatos muito lentos não recebem um impulso artificial forte.

A posição não é alterada, a bola continua dinâmica e a assistência ocorre uma única vez. Depois disso, voo, aro, tabela, outras bolinhas e rebotes seguem pela física. O alvo aproximado oscila, conservando erros e variação. A mesma regra atende aos dois times, sem consultar o placar, o nome da equipe ou quem está perdendo.

Para usar somente o impacto físico, desligue **Impact Assist** ou coloque a força em zero. A taxa real de acerto depende das pancadas e ainda precisa ser observada no Unity; a ajuda leve não garante uma quantidade de cestas.

A validação de cestas, o controle da partida e o evento de resultado foram preservados. O retorno mantém as mesmas instâncias e tempos; o fim da segunda chance foi elevado para conectar à nova canaleta. A física da nova queda, da roda contínua e da assistência precisa ser validada no Editor.

## Ajustes no Inspector

Abra **Tools → Basquete 3D → Selecionar configurações**. Ajuste os parâmetros da geometria fora do Play e inicie novamente para reconstruí-la.

| Campo | Inicial | Efeito |
|---|---:|---|
| Marbles Per Team | 4 | Bolinhas de cada equipe |
| Deck Slope Degrees | 4,5° | Escoamento da plataforma para a saída frontal |
| Duel Rotor Radius | 1,05 m | Alcance das pás superiores |
| Cross Degrees Per Second | 105°/s | Velocidade máxima dos dois rotores |
| Cross Reversal Seconds | 5 s | Intervalo entre inversões graduais |
| Continuous Launcher | Ligado | Roda continuamente e lança por contato físico |
| Launcher Rpm | 90 | Rotações por minuto da cruz |
| Launcher Rpm Variation | 6 | Oscilação suave da rotação |
| Launcher Feed Offset | 1,1 | Ponto de queda em relação ao raio da roda |
| Impact Assist | Ligado | Habilita a correção única no impacto |
| Impact Assist Strength | 0,25 | Limite da ajuda em relação à velocidade da pancada |
| Impact Target Spread | 0,70 m | Dispersão do alvo aproximado usado pela ajuda |
| Compact Launcher | Ligado | Com o modo contínuo desligado, usa a roda por ciclos da V3 |
| Launcher Wheel Radius | 0,65 m | Raio do lançador compacto, entre 0,5 e 0,8 m |
| Calibrate Launch Speed | Ligado | Atua apenas no modo antigo por ciclos |
| Launch Speed | 8,65 | Atua apenas no modo antigo por ciclos |
| Launch Angle | 56° | Ângulo de referência para a ajuda; não fixa a direção da pancada |
| Angle Variation | 2,8° | Oscilação do ângulo |
| Launch Yaw Variation | 2,5° | Oscilação lateral da referência de assistência |
| Speed Variation | 0,25 m/s | Oscilação de velocidade da referência de assistência |
| Preparation Seconds | 0,20 s | Ignorado no modo contínuo |
| Launch Interval | 0,24 s | Ignorado no modo contínuo |
| Return Seconds | 3,2 s | Retorno completo, preservado |
| Second Chance Seconds | 1,7 s | Caminho curto, preservado |

Para mais tempo de disputa na plataforma, reduza sua inclinação. Ajuste **Launcher Rpm** para mudar a energia das pancadas e **Launcher Feed Offset** para mudar onde as bolinhas encontram as pás. Alterar esses campos pode aumentar ou reduzir as cestas; a resposta precisa ser observada no Editor. Para voltar à roda por ciclos da V3, desligue **Continuous Launcher**. Desligando também **Compact Launcher**, fica disponível a alavanca da V2.

## Regras, retorno e controles

Cada cesta vale dois pontos. A bolinha precisa vir de cima, cruzar a abertura e passar inteira abaixo do aro. Contato lateral, passagem de baixo para cima e repetição da mesma tentativa não pontuam. Uma nova tentativa permite nova cesta com a mesma bolinha. As cestas são ordenadas pelo instante de cruzamento; cestas adversárias efetivamente simultâneas não dão preferência à ordem de criação.

A partida dura inicialmente 90 s, com contagem regressiva e morte súbita no empate. **Iniciar**, **Pausar** e **Reiniciar** ficam abaixo da quadra. O reinício limpa placar, tentativas, retornos e estados dos mecanismos, recolocando as oito identidades nas posições iniciais.

O retorno conserva as mesmas instâncias e usa transporte programado visível pelos tubos. A segunda chance volta ao início da canaleta elevada, sem lançar a bola automaticamente. Uma bola presa na canaleta é recuperada mesmo que o motor continue girando. Água, lava e transporte por elevadores físicos continuam para outra etapa.

O objeto raiz `MarbleBasket3D` expõe **On Result**. O resultado `MatchResult3D` contém `redScore`, `blueScore`, `winner`, `suddenDeath` e `elapsedSeconds`. Também estão disponíveis `arena.Match.Finished`, `StartMatch()`, `TogglePause()` e `RestartMatch()`. O chaveamento existente não é reconstruído.

## O que conferir nesta versão

1. As duas equipes continuam com **4/4** e conseguem arremessar.
2. Os dois rotores geram contatos; as bolinhas podem atravessar as laterais e retornar.
3. A canaleta conduz as bolas até uma queda aberta sobre a roda.
4. A cruz gira continuamente, as bolas permanecem livres e o contador de arremessos sobe pelos impactos.
5. Acontecem acertos, erros e rebotes; pausa e reinício continuam funcionando.

Se alguma bolinha parecer ausente ou o placar não evoluir, selecione **GeneratedArena3D**, abra o menu de contexto de **ArenaDiagnostics3D** e escolha **Salvar diagnóstico da partida**. O Console informa o caminho do JSON. O relatório lista V1–V4 e A1–A4, estado, posição, tentativa, altura máxima e distância ao centro do aro. Valor -1 indica dado ainda não observado.

## Renderização, prefab e gravação

A configuração deixa **Marble Prefab** vazio de propósito: as esferas são criadas nativamente, preservando a correção da referência legada. O modelo opcional pode ser recriado pelo menu **Recriar prefab nativo da bolinha**, fora do Play.

A cena troca para o renderer Built-in durante o Play e restaura o pipeline anterior ao sair. O passo fixo e a gravidade também são restaurados. Nenhum arquivo de ProjectSettings é substituído.

Use Game View **16:9 / 1920 × 1080**. O Recorder 5.1.7 já consta no backup: **Window → General → Recorder → Recorder Window**, origem Game View. A câmera permanece fixa e elevada.

## Validação

Passaram as verificações locais de regras, geometria, estados e limite da assistência leve. Elas não executam PhysX. Não há Unity instalado neste ambiente; a importação, os contatos reais com a roda contínua e uma partida completa precisam ser confirmados no Editor. Os testes nativos estão em **Window → General → Test Runner**. Detalhes em **VALIDACAO.md**.
