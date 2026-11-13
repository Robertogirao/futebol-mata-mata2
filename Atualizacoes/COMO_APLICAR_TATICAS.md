# Posturas e pressão por oportunidade — 2026.11.13.3

1. Pare o **Play** no Unity.
2. Extraia `Atualizacao_Posturas_Pressao.zip`.
3. Copie a pasta **Assets** para a raiz do seu projeto atual e aceite as substituições. São dez scripts e o `.meta` do novo script de tática. Este pacote também inclui as atualizações anteriores de gols, capitão, tags e textos.
4. Aguarde a recompilação, configure as fichas dos times e salve antes de dar Play.

Suas cenas, fontes, fichas, fotos, configurações e resultados continuam no projeto atual.

## Onde configurar

Selecione a ficha **TeamData** do time no Project e abra **Tática e IA > Postura tática**:

| Postura | Jogadores que podem avançar para pressionar |
| --- | --- |
| Ultra defensivo | Nenhum |
| Defensivo | Até 1 |
| Balanceado | Até 2 |
| Ataque | Até 3 |

Os limites consideram o número de discos disponível. Com apenas dois discos, por exemplo, Ataque pode liberar até dois.

No **FutebolDiscos_Manager**, desmarque **Manter cada time em sua metade** para permitir a pressão no campo adversário. Se esse controle geral estiver marcado, todos ficam em sua metade, independentemente da postura escolhida.

As fichas existentes conservam suas escolhas: Ataque continua Ataque, Balanceado continua Balanceado e a antiga Defesa passa a aparecer como Defensivo. Ultra defensivo é a quarta opção. Os valores antigos salvos não foram renumerados.

## Como a equipe decide

Esses números são limites, não uma ordem de manter todos adiantados. A equipe observa onde está a bola e se ela está avançando ou voltando em sua direção. Uma oportunidade curta pode liberar só um jogador; uma jogada mais adiantada pode receber os apoios adicionais permitidos pela postura.

O jogador mais bem posicionado vai à bola. Os apoios ocupam faixas diferentes. Se a jogada exigir defesa, os jogadores retornam à formação por movimentação normal. Quem ainda está voltando ocupa sua vaga até regressar, evitando mandar substitutos acima do limite. O capitão pode pressionar como qualquer outro jogador.

Ultra defensivo mantém os discos em seu próprio campo, mas eles continuam disputando a bola e defendendo nessa área.

Altere as posturas das fichas fora do Play. Isso vale para os próximos jogos, sem modificar resultados já salvos. O bloqueio geral de metade do campo continua sendo uma regra da temporada: para alterá-lo depois de salvar uma temporada, use **Nova temporada**, como nas versões anteriores.

Os testes de lógica passaram para as quatro posturas, ambos os lados, equipes de um a cinco discos, recuo, trocas de vagas e reposição após gol. A física nativa e o resultado visual continuam dependendo do Play no Unity.
