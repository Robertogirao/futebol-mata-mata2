# Validação da V3 — plataforma, dois rotores e roda compacta

Versão alvo: Unity **6000.4.11f1**, identificada no projeto fornecido. O ambiente de desenvolvimento não possui Editor Unity instalado.

A V2 foi confirmada pelo usuário funcionando; a captura enviada mostra 34–36, quatro bolinhas por time e disparos de ambas as equipes. Essa observação é da versão anterior. Não constitui execução da V3.

## Executado nesta revisão

- **7.232 verificações em C# puro:** relógio, pausa, reinício, morte súbita, validação descendente de cesta, passagem completa e rejeição de pontuação duplicada.
- **201 verificações de execução com substitutos das APIs Unity:** oito identidades, retorno e segunda chance, ordem temporal das cestas, contatos incidentais sem corrigir velocidade, disparos consecutivos, dois rotores espelhados, saídas laterais desobstruídas, passagem frontal aberta, tamanho da roda e cinco ciclos de troca de pá. Todas as quatro pás chegam ao mesmo encaixe sem salto de orientação; o reinício restaura roda e rotores. Inclui a opção de alavanca da V2 e tentativas incidentais durante o avanço da roda.
- Compilação dos scripts de execução e de Editor contra substitutos das dependências. **Não equivale a compilação contra as bibliotecas Unity nem execução da física PhysX.**
- **11 grupos de verificações estruturais:** sintaxe C#, GUIDs, assemblies, cena, prefab, quatro bolinhas por time, folgas da fila/funil, folga máxima entre rotores e defletores, configuração V3 e independência do código antigo de futebol.
- Comparação de arquivos confirma que retorno, regras, validação de cesta, controle da partida, identidade das bolinhas, HUD e diagnóstico são idênticos à V2.
- Sondagem numérica de **360 trajetórias** com a posição real de saída da roda compacta, oscilação de ângulo/direção/potência, gravidade e amortecimento aproximado: **227 acertos e 133 erros sem colisões**. A sondagem verifica possibilidade matemática de acerto e erro. Não prevê a taxa real de cestas com aro, tabela, outras bolas e PhysX.
- Inspeção visual auxiliar da geometria exportada e renderizada fora do Unity. Essa imagem não comprova iluminação, importação ou comportamento físico no Editor.

As verificações puras incluídas no pacote podem ser repetidas com .NET 8: `dotnet run --project Verification/CoreChecks.csproj`. O .NET externo não é dependência do jogo.

## Testes fornecidos para executar no Unity

Em **Window → General → Test Runner**:

- **EditMode → MarbleBasket3D.EditorTests:** regras, cestas, reinício e simultaneidade na morte súbita.
- **PlayMode → MarbleBasket3D.PlayTests:** partida automática inteira, oito identidades, disparos dos dois times, cestas e resultado; pausa/recuperação; montagem com os dois rotores e quatro pás no lançador.

O teste longo admite até 185 s reais para incluir a prorrogação e falha se não houver cestas, se uma equipe não tiver disparos ou se a partida não terminar. Esses testes nativos não foram executados neste ambiente.

## Conferência manual da nova dinâmica

1. Abrir a cena, limpar mensagens antigas do Console e conferir BOLAS 4/4 para ambos os times.
2. Observar contatos dos dois rotores e quedas pelas aberturas laterais; conferir que a mesma bolinha retorna.
3. Conferir passagem para o pequeno funil frontal sem travamento na borda traseira.
4. Observar o golpe e o avanço de um quarto de volta da roda; a próxima pá deve receber a bola na mesma posição.
5. Conferir oportunidades para os dois times, acertos, erros e rebotes durante uma partida completa.
6. Pausar e reiniciar; confirmar posições, placar e contadores zerados.
7. Avaliar a leitura da ação numa gravação curta em 16:9.

A geometria agora permite saída lateral. A frequência de ejeções, a disputa, eventuais travamentos e o ritmo com a nova roda dependem da simulação real e ainda precisam desta validação.
