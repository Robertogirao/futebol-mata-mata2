# Validação e limites do protótipo 3D

## Inspeção do projeto

- Backup: Unity 6000.4.11f1; URP 17.4.0 e Test Framework 1.6.0; Recorder 5.1.7 já configurado.
- A versão 3D tem assemblies e namespace próprios e não depende dos controladores de futebol, da arena 2D ou do chaveamento.
- O ambiente de desenvolvimento não tem editor Unity instalado.

## Executado aqui

- **7.230 verificações C# puras**: contagem, pausa, reinício, fechamento da pontuação, morte súbita e validação de cesta. A maior parte cobre uma grade de posições e três raios de bolinha, verificando passagem descendente e margem da abertura.
- **107 verificações de execução em C# com substitutos de APIs Unity**: oito identidades, pares espelhados sem sobreposição inicial, liberação no mesmo passo, captura pela proximidade, ocupação única, disparo, pontos sem duplicação, retorno, segunda chance passando pela fila, recuperação fora dos limites e reinício limpo, inclusive resultado estável quando um consumidor reinicia a partida durante o evento de conclusão.
- Compilação dos scripts de execução e do editor com substitutos das dependências. Isso confere as referências e a lógica, **não compila contra as bibliotecas reais do Unity nem executa PhysX**.
- **8 grupos de verificações estruturais**: sintaxe dos scripts, GUIDs, assemblies, cena/prefab, parâmetros iniciais, abertura do funil, largura da fila, independência do time e amostragem balística ideal do ciclo.
- A amostragem balística ideal, sem colisões e com integração contínua, inclui acertos e erros. **Não é uma taxa de acerto prevista para a partida real.** A integração discreta e os contatos mudam os resultados.
- O enquadramento inicial foi conferido por projeção geométrica; a avaliação final da Game View ainda é necessária.

O projeto inclui `Verification/CoreChecks.csproj` para repetir as verificações puras com .NET 8 (`dotnet run --project Verification/CoreChecks.csproj`). Isso é opcional; o jogo não depende do SDK .NET instalado no computador.

## Testes fornecidos para o Unity

Em **Window → General → Test Runner**:

- **EditMode → MarbleBasket3D.EditorTests**: três testes de regras, reinício e validação de cesta.
- **PlayMode → MarbleBasket3D.PlayTests**: partida automática de 90 s, integridade das oito bolinhas, disparos para os dois times, cestas, resultado emitido uma vez e reinício. Outro teste cobre pausa e recuperação fora da arena.

O teste longo admite até 185 s reais para incluir eventual morte súbita; ele falha se não houver resultado ou se um dos times não tiver disparos. O diagnóstico da partida é escrito no Console para orientar ajustes. **Esses testes de Unity ainda não foram executados.**

## Critérios que dependem do Editor

| Critério | Evidência existente | Conferência pendente |
|---|---|---|
| Partida inteira automática | Estados e relógio implementados/testados | Executar 90 s + eventual prorrogação com PhysX |
| Oportunidades para ambos | Geometria/slots espelhados, liberação simultânea, regras comuns | Medir disparos por time em várias partidas |
| Acertos, erros e disputa | Gangorra, fila, aro, coletor e ciclo do lançador implementados | Ajustar velocidade, ângulo e ritmo com colisões reais |
| Oito bolas sem perda/duplicação | Registro único e retorno da mesma instância | Rodar teste longo e observar saídas fora da arena |
| Pontos corretos | Cruzamento descendente e proteção por tentativa testados | Conferir contatos reais com o aro |
| Recuperação | Retorno/transporte e espera normal de fila testados com substitutos | Observar congestões prolongadas no funil e portão |
| Vencedor e integração | Regras, evento C# e UnityEvent implementados | Conectar receptor real do chaveamento |
| Reinício limpo | Regras e mecanismos testados com substitutos | Conferir junta física, colisores e gravação no Editor |
| Visual e áudio | Materiais, iluminação e sons implementados | Revisar enquadramento, contraste, transparência e Recorder |

Não há alegação de que uma partida de 90 segundos foi executada no Unity ou de que o balanceamento já esteja aprovado. Não foi implementado replay fiel, transporte físico por esteira/elevador, água/lava ou acabamento equivalente à ilustração de referência.
