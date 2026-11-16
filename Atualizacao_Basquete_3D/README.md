# Basquete de gude 3D — protótipo da partida

Módulo independente para Unity **6000.4.11f1**, versão identificada no backup enviado. Dois times, quatro bolinhas cada, partida automática de 90 segundos. **Não substitui nem reconstrói o chaveamento.** A arena é construída por código com primitivas, um prefab reutilizável de bolinha e malhas simples; não requer assets pagos ou plugins adicionais.

## Abrir e jogar

1. Feche o Unity e extraia este ZIP fora de `Assets`.
2. Copie a pasta **Assets/MarbleBasket3D**, junto com `Assets/MarbleBasket3D.meta`, para a pasta `Assets` da sua cópia do projeto. Este pacote acrescenta uma pasta; não substitua o projeto inteiro nem os scripts antigos.
3. Reabra o Unity, aguarde a compilação e abra **Assets/MarbleBasket3D/Scenes/MarbleBasket3D.unity**. Também pode usar **Tools → Basquete 3D → Abrir cena de simulação**.
4. Entre em **Play**. O circuito é criado, aparece a contagem de três segundos e os dois times são liberados no mesmo passo de física.
5. Use os botões **Iniciar**, **Pausar/Continuar** e **Reiniciar** na parte inferior da Game View.

A cena fornecida contém a configuração e o construtor. **Para ver a geometria antes de entrar em Play**, use **Tools → Basquete 3D → Gerar arena visível no Editor**. Esse comando salva a montagem na cena e os materiais/malhas gerados em `Assets/MarbleBasket3D/Generated`. Ao entrar em Play, a montagem é reconstruída a partir dos parâmetros, sem acumular outra arena.

Abra a cena 3D própria para este teste. As cenas antigas `MataMata_Discos`, `MataMata_NBA` e o módulo `BasqueteArena` pertencem à versão anterior e não são usados aqui. O pacote 3D não remove nenhum desses arquivos.

## O circuito implementado

Reservatórios elevados → rampas espelhadas → gangorra com Rigidbody/HingeJoint → funil → fila estreita → lançador único → voo físico → aro/tabela → coletor → segunda chance ou retorno ao reservatório.

- Oito identidades persistentes, sem criar outra bolinha a cada retorno.
- Posições iniciais espelhadas e ordem de criação intercalada. Todas ficam cinemáticas durante a contagem; são liberadas no mesmo FixedUpdate.
- Gangorra limitada a 18 graus para cada lado, influenciada pelo peso e pelos contatos das bolinhas.
- Um só ocupante no lançador. O portão fecha atrás da bolinha, há uma preparação visível e a mira oscila continuamente.
- Ângulo e velocidade seguem um ciclo de tempo comum. O mecanismo não consulta time, placar, overall, nome ou chance de vitória. A proximidade física escolhe a bolinha da frente; um empate geométrico exato aguarda a física separar as bolas.
- Disparo por velocidade inicial no Rigidbody, gravidade e colisões durante todo o voo. A preparação pode acomodar a bola no encaixe.
- Aro tubular com abertura real, tabela com colisor e rede apenas visual. O aro não é um disco sólido.
- Cesta de dois pontos validada pelo cruzamento descendente dentro da abertura, com margem para o raio da bolinha. Há proteção por tentativa e um registro adicional contra eventos duplicados.
- Contatos físicos podem causar erro e rebote na tabela, no aro e no coletor.
- A posição física de saída do coletor determina a rota: erros que saem pelo lado interno podem receber segunda chance. Essa rota devolve à fila, sem conceder um disparo imediato. Cestas e as outras saídas voltam ao reservatório do time.
- Duas áreas laterais de queda e recuperação de bolinhas fora dos limites ou imóveis por tempo prolongado. Uma fila parada enquanto o lançador opera normalmente não é tratada como travamento.

**Nesta primeira versão, retorno completo e segunda chance são transportes programados, com a própria bolinha visível e colisões suspensas durante o trajeto.** A física volta a atuar ao chegar ao reservatório ou à fila. Os tubos representam os mecanismos de transporte; ainda não há esteira/elevador físico com peças móveis. Água e lava não foram implementadas.

## Regras e integração

O tempo normal encerra a pontuação quando há um vencedor. Em empate, o circuito continua em morte súbita e a próxima cesta válida encerra a partida. O fim é emitido uma vez, com placares, vencedor, tempo simulado e indicação de prorrogação.

No objeto raiz `MarbleBasket3D`, o evento **On Result** permite conectar um componente seu pelo Inspector. A assinatura do método receptor é:

```csharp
public void ReceberResultado(MarbleBasket3D.Core.MatchResult3D resultado)
{
    // resultado.redScore, blueScore, winner, suddenDeath e elapsedSeconds
    // Encaminhe esses dados ao seu chaveamento.
}
```

Outra opção é assinar o evento C# da partida depois que o construtor concluir seu Awake:

```csharp
var arena = UnityEngine.Object.FindFirstObjectByType<MarbleBasket3D.ArenaBuilder3D>();
arena.Match.Finished += ReceberResultado;
```

Use `arena.Match.StartMatch()`, `TogglePause()` e `RestartMatch()` para comandos externos. O reinício reposiciona as oito bolinhas, limpa tentativas, pontuação, trânsito de retorno, ocupação do lançador, gangorra, efeitos e contadores. Não há salvamento de campeonato neste módulo.

## Ajustar a dinâmica

Use **Tools → Basquete 3D → Selecionar configurações** e edite `Arena3D_Settings` fora do Play. Mantenha os `.meta` fornecidos.

| Controle | Inicial | Efeito |
|---|---:|---|
| Match Seconds | 90 | Duração normal |
| Countdown Seconds | 3 | Espera antes da liberação simultânea |
| Marbles Per Team | 4 | Quantidade em cada time |
| Launch Speed | 8,65 m/s | Regulagem de força, expressa como velocidade inicial |
| Launch Angle | 56° | Inclinação central da mira |
| Angle Variation | 2,8° | Oscilação visível da mira |
| Speed Variation | 0,38 m/s | Oscilação de potência |
| Power Cycle Seconds | 3,8 | Período do ciclo de mira/potência |
| Preparation Seconds | 0,85 | Preparação antes do disparo |
| Launch Interval | 0,55 | Intervalo mínimo após disparar |
| Wait For Shot To Land | Sim | Uma tentativa aérea por vez para facilitar a leitura |
| Return Seconds | 3,2 | Custo de tempo do retorno completo |
| Second Chance Seconds | 1,7 | Custo do caminho curto |
| Stuck Seconds | 7 | Recuperação de bolinhas sem deslocamento |
| Mechanism Timeout | 12 | Limite para considerar a fila/mecanismo sem progresso |
| Physics Step | 0,01 s | Passo fixo, aplicado somente enquanto esta cena está ativa |

Não mude a massa para dar vantagem a um time. As mesmas configurações se aplicam às duas equipes. O número de disparos/acertos reais depende dos contatos e deve ser medido no Editor; os valores iniciais são um ponto de partida para ajuste.

## Visual, renderização e gravação

Há quadra de madeira em peças, bolas brilhantes vermelhas/azuis, estrutura metálica, guias transparentes, aro/rede, luzes de arena e efeitos/sons curtos. Os sons são sintetizados quando os campos de AudioClip ficam vazios; você pode substituí-los.

O projeto do backup usa URP 2D nas configurações de qualidade. **Esta cena usa o renderer Built-in para desenhar as malhas 3D com Standard**, trocando o pipeline somente durante a execução e restaurando-o ao sair da cena/Play. O passo fixo e a gravidade anteriores também são restaurados. Nenhum arquivo de ProjectSettings é substituído. A montagem antecipada pode aparecer com materiais incompatíveis no Scene View do projeto 2D; a troca é aplicada ao entrar em Play.

Na Game View, escolha **16:9** ou **1920 × 1080**. A câmera é fixa e elevada, com enquadramento 16:9. O pacote do Recorder 5.1.7 já consta no backup: use **Window → General → Recorder → Recorder Window**, adicione um Movie Recorder com origem Game View e comece a gravação antes da contagem regressiva. Ajuste `Hide Buttons During Play` se quiser retirar os comandos durante a ação. Verifique resolução, som e desempenho numa gravação curta antes de uma partida longa.

A imagem de referência orientou o circuito e a organização espacial. **Este é o protótipo com formas simples pedido no escopo**, sem cenário final modelado, câmera de close, replay ou fluidos. Não há garantia de replay fiel por semente: uma etapa futura deverá registrar movimento/estados da simulação.

## O que está validado

Consulte **VALIDACAO.md**. As regras, estados e referências foram testados em C# e verificações estruturais. **Não há Unity instalado no ambiente de desenvolvimento: importação, compilação com as bibliotecas reais e uma partida completa com PhysX ainda precisam ser executadas no seu Editor.** Os testes de Play Mode fornecidos verificam esses critérios; não foram executados aqui.

Para diagnóstico após um teste, selecione `GeneratedArena3D`, abra o menu de contexto do componente **ArenaDiagnostics3D** e use **Salvar diagnóstico da partida**. O Console informa o caminho do JSON com disparos por time, pontos, retornos e recuperações.
