# Basquete de gude 3D — V2: cruz e alavanca

Atualização do protótipo para Unity **6000.4.11f1**, com quatro bolinhas vermelhas e quatro azuis. O chaveamento e o sistema de retorno da primeira versão permanecem independentes desta revisão.

## Instalar e testar

1. Feche o Unity e extraia o ZIP **ATUALIZACAO_BASQUETE_3D_V2.zip** fora de Assets.
2. Copie a pasta **Assets** deste pacote para a raiz da sua cópia do projeto, aceitando substituir os arquivos correspondentes de `Assets/MarbleBasket3D`. Preserve os `.meta`.
3. Reabra o Unity e use **Tools → Basquete 3D → Abrir cena de simulação**. Limpe as mensagens antigas do Console e entre em Play.
4. No topo, confira **BOLAS 4/4** de cada equipe e os contadores de arremessos. A contagem inclui as bolinhas que estão no retorno, mesmo quando não são visíveis no enquadramento.

Esta atualização repõe os parâmetros iniciais do protótipo, inclusive quatro bolinhas por time. Para ajustar, abra **Tools → Basquete 3D → Selecionar configurações**. Não é necessário executar o instalador antigo da versão 2D.

## Mudanças desta revisão

- **Cruz giratória no funil:** quatro braços com colisores acompanham a inclinação do funil. O motor inverte gradualmente o sentido, gerando contatos, bloqueios e desvios. Substitui a gangorra na montagem da arena.
- **Alavanca aberta:** a bolinha é acomodada junto à pá, a alavanca recua por aproximadamente 0,2 s e dá um golpe curto. A velocidade e a direção de saída correspondem à tangente do braço. O voo continua com Rigidbody, gravidade e colisões.
- **Entrada sem portão:** o portão foi removido da geometria. A alavanca retorna em 0,24 s e pode receber a próxima bola sem esperar que a tentativa anterior chegue ao chão. A fila passa a depender das guias, da pá e das colisões.
- **Colisões laterais da pá:** se a pá em movimento bater em outra bola, essa bola também recebe uma tentativa válida; sua velocidade física é preservada, sem corrigir a direção para a cesta.
- **Calibração geométrica:** a velocidade central é calculada uma vez na montagem, a partir da posição real de saída da alavanca. Depois disso há oscilações de ângulo, potência e direção, comuns aos dois times. Não há correção do voo nem escolha de vencedor pelo placar.
- **Aro ampliado:** raio de 0,66 m, com abertura de pontuação de 0,60 m. Mantidos tabela, rede visual e coletor.
- **Contagem explícita:** o placar mostra quantas bolinhas de cada time continuam ativas e quantas estão retornando. Também mostra arremessos de cada equipe.
- **Referência de prefab corrigida:** o identificador legado reservado `100100000` foi retirado do prefab. A configuração fornecida deixa `Marble Prefab` vazio de propósito; a criação nativa de esferas gera as oito bolinhas sem depender dessa referência. O prefab continua disponível como modelo opcional.

Se quiser recriar e atribuir um prefab salvo pelo próprio Unity, use **Tools → Basquete 3D → Recriar prefab nativo da bolinha**, fora do Play. Esse passo é opcional.

## O que observar agora

- Os contadores devem indicar **4/4** para vermelho e azul. Uma foto com seis bolinhas visíveis não informa, sozinha, se faltam duas: elas podem estar no retorno ou atrás da estrutura.
- A cruz deve girar e inverter o sentido, tocando as bolinhas no percurso.
- O golpe da alavanca deve ser reconhecível e a entrada deve continuar aberta entre as tentativas.
- Os contadores de arremessos devem subir para os dois times.
- Confira acertos e erros reais no Editor. A calibração matemática foi testada sem colisões; ela não garante uma taxa de acerto da partida com PhysX.

## Ajustes no Inspector

| Campo | Valor inicial | Efeito |
|---|---:|---|
| Marbles Per Team | 4 | Quantidade de cada time |
| Cross Degrees Per Second | 65 | Velocidade máxima da cruz |
| Cross Reversal Seconds | 5 | Intervalo de inversão gradual do sentido |
| Calibrate Launch Speed | Ligado | Calcula a referência de velocidade pela geometria da alavanca |
| Launch Speed | 8,65 | Ajuste fino de velocidade: com calibração ligada, 8,65 representa o centro calculado |
| Launch Angle | 56° | Ângulo central de saída |
| Angle Variation | 2,8° | Oscilação do ângulo |
| Launch Yaw Variation | 2,5° | Oscilação lateral da alavanca |
| Speed Variation | 0,25 m/s | Oscilação de velocidade em torno da referência |
| Preparation Seconds | 0,20 | Tempo de recuo antes do golpe |
| Launch Interval | 0,24 | Tempo de retorno do braço |
| Return Seconds | 3,2 | Tempo do retorno completo, preservado |
| Second Chance Seconds | 1,7 | Tempo do caminho curto, preservado |

O campo antigo `Wait For Shot To Land` foi ocultado e deixou de bloquear a entrada. As mesmas regras da alavanca e da cruz são aplicadas aos dois times. Não há força ou chance de cesta especial por equipe.

## Pontuação e várias bolinhas no ar

Cada cesta vale dois pontos. A passagem precisa vir de cima, cruzar a abertura e ser confirmada quando a esfera passa inteira abaixo do aro. Contato lateral, passagem de baixo para cima e repetição da mesma tentativa não pontuam.

Com várias bolinhas no ar, as cestas do mesmo passo de física são processadas pela fração de tempo de cruzamento, e não pela ordem de criação das bolinhas. Em morte súbita, duas cestas adversárias com tempo efetivamente igual somam pontos aos dois times, mantêm o empate e a próxima cesta decide.

## Retorno, controle e integração

O retorno continua usando as mesmas instâncias, com transporte programado visível pelos tubos e colisões suspensas no trajeto. A segunda chance volta à fila. A bola não é duplicada ao retornar. Água/lava e elevadores físicos permanecem fora desta etapa.

A partida tem contagem regressiva, duração normal de 90 s, morte súbita, pausa e reinício. Os botões permanecem na parte inferior. O objeto raiz `MarbleBasket3D` expõe o evento **On Result** para seu chaveamento, com `MatchResult3D`: `redScore`, `blueScore`, `winner`, `suddenDeath` e `elapsedSeconds`. A interface C# `arena.Match.Finished`, `StartMatch()`, `TogglePause()` e `RestartMatch()` continua disponível.

## Diagnóstico se o placar continuar zerado ou uma bola parecer ausente

Selecione **GeneratedArena3D**, abra o menu de contexto de **ArenaDiagnostics3D** e escolha **Salvar diagnóstico da partida**. O Console informa o caminho do JSON. O relatório contém V1–V4 e A1–A4, posição, estado, tentativa, altura máxima e distância ao centro do aro no cruzamento. Valor -1 significa que o dado ainda não foi observado naquela tentativa. Os contadores diferenciam ausência real de bolinha e simples ocultação pela estrutura.

## Renderização e gravação

A cena usa materiais Standard e troca para o renderer Built-in durante o Play, restaurando o pipeline anterior ao sair. O passo fixo e a gravidade anteriores também são restaurados. Nenhum arquivo de ProjectSettings é substituído.

Use Game View 16:9 ou 1920 × 1080. O Recorder 5.1.7 já consta no backup: **Window → General → Recorder → Recorder Window**, origem Game View. A câmera continua fixa e elevada. **Gerar arena visível no Editor**, no menu Basquete 3D, permite montar a geometria antes do Play; os materiais podem depender da troca de pipeline feita durante a execução.

## Estado da validação

As regras e a integração dos estados passaram em testes locais. O ambiente não tem Unity instalado: a importação do prefab corrigido, os contatos reais da cruz/pá e uma partida completa com acertos ainda precisam ser confirmados no Editor. Os testes Unity fornecidos estão em **Window → General → Test Runner**. Veja **VALIDACAO.md**. Esta revisão trata mecânica e leitura da ação; o acabamento visual final continua para depois de validar a dinâmica.
