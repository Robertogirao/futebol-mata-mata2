# Basquete de gude 3D — V3: plataforma com dois rotores

Atualização para Unity **6000.4.11f1**. Substitui a cruz dentro do funil por uma plataforma com dois rotores e saídas laterais abertas. O lançador passa a ser uma cruz compacta de quatro pás. Mantém quatro bolinhas por time, a pontuação, o retorno e a integração com o chaveamento.

## Instalação

1. Feche o Unity e extraia **ATUALIZACAO_BASQUETE_3D_V3.zip** fora do projeto.
2. Copie a pasta **Assets** do pacote para a raiz da sua cópia do projeto NBA, onde já existem Assets, Packages e ProjectSettings. Aceite substituir os arquivos correspondentes em `Assets/MarbleBasket3D` e preserve os `.meta`.
3. Abra o projeto. Use **Tools → Basquete 3D → Abrir cena de simulação** e entre em **Play**.
4. Confira **BOLAS 4/4** para vermelho e azul. Cada contador inclui as bolinhas em retorno.

Não é necessário executar o instalador antigo de basquete 2D. A atualização repõe os valores iniciais do arquivo de configurações e a cena fornecida. O circuito é construído ao entrar em Play. Para vê-lo antes, use **Tools → Basquete 3D → Gerar arena visível no Editor**.

## O que mudou

- **Plataforma de disputa:** 6,4 m de largura, com inclinação suave de 4,5° para conduzir as bolas à frente. O grande funil inclinado deixou de envolver os obstáculos.
- **Dois rotores de três pás:** raio de 1,05 m, geometria espelhada e sentidos opostos. O motor inverte gradualmente o sentido a cada 5 s. As pás têm colisores e movem Rigidbodies cinemáticos; os desvios resultam dos contatos físicos.
- **Quedas pelas laterais:** cada lado tem uma abertura de 2,7 m, sem parede atravessando a passagem. As áreas de coleta ficam abaixo dessas aberturas e enviam a mesma bolinha ao retorno. Defletores passivos nas entradas direcionam o impulso das rampas para a disputa.
- **Saída frontal:** a passagem central de 1,34 m leva a um pequeno funil e à fila existente. A borda traseira desse funil fica aberta para não criar uma barreira na entrada.
- **Lançador compacto:** quatro pás numa roda vertical com raio de 0,65 m. O conjunto das pás tem aproximadamente 1,6 m de diâmetro, cerca de um quarto da largura da plataforma. Não há um disco opaco grande escondendo as bolinhas.
- **Movimento do lançador:** a pá acomoda uma bolinha, prepara por 0,20 s, dá o golpe e continua girando até a pá seguinte ocupar o mesmo encaixe. Cada ciclo avança um quarto de volta. A entrada continua sem portão e não espera o pouso do disparo anterior.

A velocidade central continua sendo calculada uma vez, usando a posição real de saída e a distância até a cesta. As oscilações de potência, ângulo e direção continuam iguais para os dois times. O voo usa Rigidbody, gravidade e colisões, sem corrigir a trajetória no ar. Bolinhas atingidas incidentalmente pelas pás conservam a velocidade da colisão e também recebem uma tentativa válida.

O código do retorno, da validação de cestas, do placar, do controle da partida e do evento de resultado é idêntico ao da V2 que você testou. A nova geometria ainda precisa ser validada com a física do Editor.

## Ajustes no Inspector

Abra **Tools → Basquete 3D → Selecionar configurações**. Ajuste os parâmetros da geometria fora do Play e inicie novamente para reconstruí-la.

| Campo | Inicial | Efeito |
|---|---:|---|
| Marbles Per Team | 4 | Bolinhas de cada equipe |
| Deck Slope Degrees | 4,5° | Escoamento da plataforma para a saída frontal |
| Duel Rotor Radius | 1,05 m | Alcance das pás superiores |
| Cross Degrees Per Second | 105°/s | Velocidade máxima dos dois rotores |
| Cross Reversal Seconds | 5 s | Intervalo entre inversões graduais |
| Compact Launcher | Ligado | Usa a roda de quatro pás; desligado mantém a alavanca da V2 |
| Launcher Wheel Radius | 0,65 m | Raio do lançador compacto, entre 0,5 e 0,8 m |
| Calibrate Launch Speed | Ligado | Calcula a velocidade de referência pela geometria |
| Launch Speed | 8,65 | Com calibração ligada, representa o centro calculado; alterar faz ajuste fino |
| Launch Angle | 56° | Ângulo central de saída |
| Angle Variation | 2,8° | Oscilação do ângulo |
| Launch Yaw Variation | 2,5° | Oscilação lateral do disparo |
| Speed Variation | 0,25 m/s | Oscilação em torno da velocidade calculada |
| Preparation Seconds | 0,20 s | Preparação antes do golpe |
| Launch Interval | 0,24 s | Giro até a próxima pá |
| Return Seconds | 3,2 s | Retorno completo, preservado |
| Second Chance Seconds | 1,7 s | Caminho curto, preservado |

Para mais tempo de disputa na plataforma, reduza sua inclinação. Para mais impulso dos rotores, aumente sua velocidade dentro do intervalo permitido. As aberturas possibilitam quedas físicas; a frequência delas depende dos contatos e precisa ser observada no Unity. Não existe sorteio de queda ou vantagem por equipe.

## Regras, retorno e controles

Cada cesta vale dois pontos. A bolinha precisa vir de cima, cruzar a abertura e passar inteira abaixo do aro. Contato lateral, passagem de baixo para cima e repetição da mesma tentativa não pontuam. Uma nova tentativa permite nova cesta com a mesma bolinha. As cestas são ordenadas pelo instante de cruzamento; cestas adversárias efetivamente simultâneas não dão preferência à ordem de criação.

A partida dura inicialmente 90 s, com contagem regressiva e morte súbita no empate. **Iniciar**, **Pausar** e **Reiniciar** ficam abaixo da quadra. O reinício limpa placar, tentativas, retornos e estados dos mecanismos, recolocando as oito identidades nas posições iniciais.

O retorno conserva as mesmas instâncias e usa transporte programado visível pelos tubos. A segunda chance volta à fila do lançador. Água, lava e transporte por elevadores físicos continuam para outra etapa.

O objeto raiz `MarbleBasket3D` expõe **On Result**. O resultado `MatchResult3D` contém `redScore`, `blueScore`, `winner`, `suddenDeath` e `elapsedSeconds`. Também estão disponíveis `arena.Match.Finished`, `StartMatch()`, `TogglePause()` e `RestartMatch()`. O chaveamento existente não é reconstruído.

## O que conferir nesta versão

1. As duas equipes continuam com **4/4** e conseguem arremessar.
2. Os dois rotores geram contatos; as bolinhas podem atravessar as laterais e retornar.
3. A saída frontal recebe bolinhas sem prendê-las numa borda do funil.
4. As quatro pás do lançador se revezam no mesmo encaixe e o golpe fica visível.
5. Acontecem acertos, erros e rebotes; pausa e reinício continuam funcionando.

Se alguma bolinha parecer ausente ou o placar não evoluir, selecione **GeneratedArena3D**, abra o menu de contexto de **ArenaDiagnostics3D** e escolha **Salvar diagnóstico da partida**. O Console informa o caminho do JSON. O relatório lista V1–V4 e A1–A4, estado, posição, tentativa, altura máxima e distância ao centro do aro. Valor -1 indica dado ainda não observado.

## Renderização, prefab e gravação

A configuração deixa **Marble Prefab** vazio de propósito: as esferas são criadas nativamente, preservando a correção da referência legada. O modelo opcional pode ser recriado pelo menu **Recriar prefab nativo da bolinha**, fora do Play.

A cena troca para o renderer Built-in durante o Play e restaura o pipeline anterior ao sair. O passo fixo e a gravidade também são restaurados. Nenhum arquivo de ProjectSettings é substituído.

Use Game View **16:9 / 1920 × 1080**. O Recorder 5.1.7 já consta no backup: **Window → General → Recorder → Recorder Window**, origem Game View. A câmera permanece fixa e elevada.

## Validação

Passaram as verificações locais de regras, geometria e estados. Elas não executam PhysX. Não há Unity instalado neste ambiente; a importação e uma partida completa com os novos rotores e a roda precisam ser confirmadas no Editor. Os testes nativos estão em **Window → General → Test Runner**. Detalhes em **VALIDACAO.md**.
