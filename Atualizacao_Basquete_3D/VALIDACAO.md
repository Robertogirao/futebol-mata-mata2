# Validação da V2 — cruz, alavanca e contagem

Base: Unity 6000.4.11f1, identificado no backup. O ambiente de desenvolvimento não possui editor Unity instalado.

## Executado aqui

- **7.232 verificações de regras em C# puro**: relógio, pausa, reinício, morte súbita, cruzamento descendente, margem da abertura, confirmação da passagem completa, rejeição de pontos duplicados e empate de cestas simultâneas.
- **129 verificações de execução em C# com substitutos das APIs Unity**: oito identidades, quatro por time, contagem preservada durante o retorno, entrada sem portão, cruz giratória, golpe da alavanca, tentativas consecutivas sem aguardar o pouso anterior, contatos laterais da pá sem correção de velocidade, segunda chance, recuperação e reinício. Incluem cestas ordenadas pelo tempo de cruzamento, sem dar preferência ao primeiro time registrado.
- Compilação do código de execução e de editor com substitutos das dependências. **Não é compilação contra as bibliotecas reais nem execução de PhysX.**
- **10 grupos de verificações estruturais**: sintaxe, GUIDs, assemblies, cena, prefab sem o ID reservado 100100000, configuração sem referência a esse prefab, quatro bolinhas por time, geometria de fila/funil e ausência de portão. Também foi conferido que o arquivo de retorno é idêntico ao da V1.
- Sondagem numérica de **360 trajetórias** usando a posição real de saída da alavanca, oscilações de ângulo/direção/potência, gravidade, passo de 0,01 s e amortecimento aproximado: houve 233 acertos e 127 erros sem colisões. **Esses números não são uma previsão da taxa de acerto real.** Aro, tabela, contatos com outras bolinhas e integração PhysX mudam os resultados.

As verificações puras podem ser repetidas com .NET 8: `dotnet run --project Verification/CoreChecks.csproj`. Isso é opcional e não é dependência do jogo.

## Testes para executar no Unity

Em **Window → General → Test Runner**:

- **EditMode → MarbleBasket3D.EditorTests**: regras, cestas, reinício e cestas simultâneas na morte súbita.
- **PlayMode → MarbleBasket3D.PlayTests**: partida inteira de 90 s com oportunidades para os dois times, oito identidades e resultado; pausa/recuperação; montagem V2 com cruz e sem portão.

O teste longo admite até 185 s reais para incluir a prorrogação e falha se um time não tiver disparos, se não houver cestas ou se a partida não terminar. O diagnóstico é escrito no Console. **Esses testes de Unity não foram executados aqui.**

## Conferência manual indispensável

1. Abrir a cena da atualização e limpar mensagens antigas do Console.
2. Conferir BOLAS 4/4 de cada equipe e ausência de nova mensagem de cast do prefab.
3. Conferir que a cruz realmente empurra as bolas e inverte o sentido sem prender o funil.
4. Conferir recuo/golpe/retorno da alavanca e entrada aberta entre os arremessos.
5. Observar vários arremessos: acertos e erros, rebotes, contadores por time e registro único de cada cesta.
6. Conferir que o retorno continua claro e mantém quatro identidades por equipe.
7. Pausar e reiniciar; conferir contadores zerados, posições iniciais e mecanismos reiniciados.
8. Fazer uma gravação curta em 16:9 para avaliar o ritmo e a leitura da ação.

Se o resultado continuar 0–0, o relatório de ArenaDiagnostics3D permite distinguir falta de disparos, trajetória que não atinge altura suficiente e passagem fora do aro. Não há alegação de que a revisão já tenha executado uma partida física no Unity.
