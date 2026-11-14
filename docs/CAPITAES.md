# Capitão: overall e posição

## Como editar

No Project, selecione a ficha do capitão convidado usada no catálogo da cena. Os modelos ficam em `Assets/FutebolDiscos/Capitaes/Modelos`.

- **Overall**: força individual, de 1 a 100.
- **Posicao**: Defesa, Meio ou Ataque.

O OVR e a posição aparecem na ficha pré-jogo, na ficha lateral durante a partida, na tag do disco (com nomes dos jogadores ligados) e no anúncio da revelação do sorteio. Sem capitão convidado, não é atribuído um overall individual que o jogador não usa.

## O overall realmente influencia?

Sim. O capitão convidado usa o valor salvo no sorteio da temporada para:

- Velocidade: multiplicador `0,88 + OVR/100 × 0,24`.
- Potência individual do chute: multiplicador `0,75 + OVR/100 × 0,5`.
- Mira: a parcela adicional de variação é proporcional a `1 − OVR/100`, portanto diminui com overall maior.

Por exemplo, comparando OVR 90 com OVR 60, a velocidade desejada é cerca de 7% maior e a potência individual do chute é cerca de 14% maior, com as demais condições iguais. A mira ainda tem outras variações; overall não é probabilidade de vitória.

As fórmulas não foram alteradas nesta atualização. A opção de desligar o overall dos times não desliga o overall individual do capitão convidado. Os demais jogadores não recebem o overall do capitão.

## O que a posição muda

Antes, o disco de índice zero (capitão) ficava na vaga de base mais recuada. Agora:

- **Defesa** mantém a vaga e a preferência antigas.
- **Meio** escolhe uma vaga intermediária e uma preferência moderada para pressionar.
- **Ataque** escolhe uma vaga adiantada e recebe mais preferência para pressionar.

A troca de vaga permite que outro disco cubra a posição defensiva original. Com um único disco, sua profundidade é ajustada. A função é uma base/preferência: a IA continua perseguindo a bola. As vagas são espelhadas corretamente entre os dois times.

A postura tática do time ainda limita quantos discos podem pressionar. A opção **Manter cada time em sua metade** ainda bloqueia o avanço além do meio-campo. Para liberar esse avanço, desmarque-a e ajuste a postura tática na ficha do time.

## Temporadas salvas

O valor zero da posição corresponde a Defesa. Assets e arquivos JSON antigos sem esse campo continuam válidos como defensores, sem regravar ou alterar resultados.

Posição e overall são salvos com as associações dos capitães. Para alterá-los depois do sorteio salvo, use **Nova temporada** no painel Projeto Futebol Discos. Reexibir um sorteio usa os dados salvos.

## Aplicar somente a atualização

O pacote `Downloads/ATUALIZACAO_CAPITAES.zip` contém dez scripts e as instruções. Aplique sobre o projeto já atualizado com o áudio/toque na bola. Feche o Unity e copie `Assets` do ZIP para a raiz do projeto, aceitando substituir os scripts. As cenas, músicas e fotos não fazem parte desse pacote.

## Verificações da lógica

Passaram 1.242 verificações em C# com .NET 8 e substitutos mínimos das APIs Unity. Foram compilados os modelos, repositório, motor de sorteio e tática completos; os métodos de formação e consulta da configuração foram extraídos automaticamente dos fontes para o harness.

A cobertura inclui JSON legado e roundtrip, posição inválida, preservação do arquivo ao rejeitar alteração de temporada, serializador que perde a posição, sorteio/replay/cópia, formação com 1–6 discos, espelhamento, cobertura defensiva, padrões antigos e limites táticos. A sintaxe dos 35 scripts também foi conferida, e aplicar o ZIP sobre a última versão de áudio reproduz os arquivos atualizados.

Esses testes não executam a física, o renderizador nem o `JsonUtility` real do Unity.

## Verificação no Unity

O editor Unity não está disponível no ambiente de alteração. Conferir compilação, aparência e física em Play Mode ainda é necessário.

Confira capitães de OVRs diferentes nas três posições, nos dois lados, nas saídas e reposições. Teste também com bloqueio da metade ligado e desligado, com postura ultra defensiva e ofensiva, e ao retomar temporadas antigas como Defesa. A tag do disco depende de **Mostrar nome nos discos** estar ligado; a ficha continua mostrando os atributos.
