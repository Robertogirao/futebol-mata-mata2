# Correção dos gols repetidos — versão 2026.11.13.1

Esta atualização troca somente três scripts. Suas cenas, fontes, nome do projeto, cores, participantes, tempos e temporadas continuam configurados como estão.

1. Pare o **Play** no Unity.
2. Extraia `Correcao_Gols_Infinitos.zip`.
3. Copie a pasta **Assets** extraída para a raiz do seu projeto atual e aceite substituir os três arquivos:
   - `Assets/FutebolDiscos/Scripts/PartidaDiscos.cs`
   - `Assets/FutebolDiscos/Scripts/BolaDiscos.cs`
   - `Assets/FutebolDiscos/Scripts/DiscoAtleta.cs`
4. Aguarde a recompilação e dê **Play** novamente. A partida interrompida recomeça; as partidas concluídas continuam salvas.

A bola e os discos passam a sincronizar a posição visual e a posição física antes de voltar à simulação. Um gol bloqueia imediatamente novas contagens. A pausa sozinha não libera outro gol: a bola precisa ter sua nova saída confirmada no centro em etapas de física posteriores.

O teste de regressão mantém a bola no gol e repete os eventos do sensor durante várias pausas: o placar permanece em um gol. Os testes usam uma fachada adversarial das APIs Unity, com posições física e visual separadas; não executam a física nativa do Editor.

Se uma partida já terminou e foi salva com o placar errado, a correção não altera esse resultado. Para começar uma edição limpa, use **Nova temporada** no painel central.
