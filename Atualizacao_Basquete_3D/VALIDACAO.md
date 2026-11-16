# Validação da V4 — cruz contínua e assistência leve

Versão alvo: Unity **6000.4.11f1**. O ambiente de desenvolvimento não possui Editor Unity instalado.

O usuário confirmou o funcionamento da V2 e observou na V3 que a roda esperava para lançar. Esta revisão muda a alimentação e o motor para permitir impactos com a roda girando continuamente, usando assistência leve conforme a preferência informada.

## Executado nesta revisão

- **7.232 verificações em C# puro:** relógio, pausa, reinício, morte súbita, validação descendente de cesta, passagem completa e rejeição de pontos duplicados.
- **1.277 verificações com substitutos das APIs Unity:** regressões da V2/V3, rotação sem ocupante, ausência de captura ou disparo automático por tempo, início/pausa/continuação/fim/reinício, contatos válidos e repetidos, segunda chance na canaleta elevada e recuperação de bolinha presa apesar do motor girando.
- As verificações da assistência cobrem a mesma resposta para impactos equivalentes de ambos os times, posição preservada, limite de 25% da velocidade da pancada, ausência de nova correção durante o voo, desativação da ajuda e contatos muito lentos. Foram amostradas 720 combinações de direção e fase para conferir o limite da correção.
- Folgas da queda verificadas a partir dos colisores montados: vão para passagem da esfera, entrada sem parede alta atravessada, paredes fora da largura das pás e canaleta acima do giro. Incluem os extremos permitidos de raio da roda, posição da queda e raio da bolinha.
- Compilação dos scripts de execução e de Editor contra substitutos das dependências. **Não equivale a compilação contra as bibliotecas Unity nem execução da física PhysX.**
- **12 grupos estruturais:** sintaxe C#, GUIDs, assemblies, cena, prefab, configuração inicial com ajuda leve, quatro bolinhas por time, folgas superiores e ausência de dependências do futebol antigo.
- Regras, validação de cesta, controle da partida, identidade das bolinhas, diagnóstico, recuperação, contato e sensores foram preservados. No retorno, a única alteração é usar a posição de entrada correspondente à canaleta atual, mantendo transporte, duração, identidade e controle de duplicação.

As verificações puras incluídas podem ser repetidas com .NET 8: `dotnet run --project Verification/CoreChecks.csproj`. O .NET externo não é dependência do jogo.

## Testes fornecidos para o Unity

Em **Window → General → Test Runner**:

- **EditMode → MarbleBasket3D.EditorTests:** regras, cestas e reinício; limite da assistência leve, preservação de uma pancada para o lado errado e assistência desligada.
- **PlayMode → MarbleBasket3D.PlayTests:** partida automática inteira, oito identidades, disparos dos dois times, cestas e resultado; montagem com dois rotores e roda contínua; rotação sem acomodar bolinhas; pausa e recuperação.

O teste longo admite até 185 s reais para incluir a prorrogação e falha se não houver cestas, se um time não tiver disparos ou se a partida não terminar. Esses testes nativos não foram executados neste ambiente.

## Conferência manual necessária

1. Abrir a cena, limpar mensagens antigas do Console e conferir BOLAS 4/4 para ambos os times.
2. Confirmar que a roda gira sem parar durante a partida e que não há acomodação ou espera entre bolas.
3. Observar a queda da canaleta nas pás: verificar contatos reais, bolas que passam sem bater e retorno abaixo da roda.
4. Conferir que o contador de arremessos sobe por impacto e não várias vezes pela mesma bola durante um voo.
5. Observar se a ajuda leve melhora tentativas próximas sem transformar pancadas ruins em arremessos dirigidos. Comparar uma rodada com Impact Assist desligado, se necessário.
6. Executar uma partida completa: oportunidades dos dois times, acertos, erros, rebotes, resultado, pausa e reinício.

Não foi medida uma taxa de cestas da V4. A ajuda inicial limita a correção de velocidade a 25%; esse número não é uma probabilidade de acerto. A frequência de contato, a dinâmica e as cestas dependem da física real e ainda precisam ser verificadas no Editor.
