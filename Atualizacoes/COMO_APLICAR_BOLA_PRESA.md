# Bola presa: escanteio e tiro de meta — 2026.11.13.7

## Instalar

Esta atualização é para o Futebol de Discos com o painel central da versão 2026.11.13.6.

1. Pare o **Play** no Unity.
2. Extraia `Atualizacao_Bola_Presa_Escanteio_Tiro_Meta.zip`.
3. Copie a pasta **Assets** extraída para a raiz do seu projeto e aceite as substituições.
4. Aguarde a recompilação. Selecione **FutebolDiscos_Manager > Projeto Futebol Discos** e abra **Bola presa nos cantos**.
5. Confira os controles, salve a cena e teste no Play.

O ZIP substitui quatro scripts: `ProjetoFutebolDiscos.cs`, `PartidaDiscos.cs`, `BolaDiscos.cs` e `ProjetoFutebolDiscosEditor.cs`. Ele preserva os arquivos `.meta` existentes e não inclui cenas, prefabs, fontes ou imagens. Suas personalizações e os resultados salvos permanecem no projeto. Não é necessário criar outra temporada.

## Como a regra funciona

A bola precisa permanecer próxima das **duas bordas de um canto**, sem conseguir sair, pelo tempo configurado. O padrão é **3 segundos**. Bater na parede ou passar pelo canto não gera cobrança.

Pequenos tremores da bola e a troca de toque entre jogadores não apagam essa espera. Se a bola sair da região, mudar de canto ou avançar de forma relevante, a contagem reinicia.

- Último toque do **defensor**: **escanteio para o atacante**.
- Último toque do **atacante**: **tiro de meta para o defensor**.

O último contato usado é o que existia quando a bola foi considerada presa. Sem um toque válido de um jogador da partida, o jogo mantém o recurso anterior de soltar a bola, sem inventar uma equipe para a cobrança.

A transmissão mostra **CORNER KICK** ou **GOAL KICK** e o nome do time. O relógio pausa, os discos voltam à formação para abrir espaço e a bola é cobrada automaticamente para dentro do campo. O placar e o tempo já jogado continuam iguais. A regra funciona na liga, no mata-mata e no Golden Goal.

## Controles

Todos ficam no mesmo painel, no grupo **Bola presa nos cantos**. Cada cena guarda seus próprios valores.

| Controle | Padrão | Faixa |
| --- | --- | --- |
| Repor a bola presa no canto | Ativado | Ativado / desativado |
| Tempo sem sair do canto (s) | 3 segundos | 1 a 10 segundos |
| Área de detecção do canto | 1,6 unidades de campo | 0,8 a 3 |
| Pausa antes da cobrança (s) | 0,8 segundo | 0,2 a 3 segundos |

Uma área maior inclui mais espaço perto do canto. Um tempo maior deixa a disputa continuar por mais tempo antes da reposição. Faça os ajustes fora do Play e salve a cena.

A física e a aparência final precisam ser conferidas no Editor Unity; este ambiente não executa a física nativa nem renderiza as cenas.
