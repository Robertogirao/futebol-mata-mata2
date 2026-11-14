# Sorteio das marbles dos capitães — 2026.11.14.2

## Instalar

Esta atualização é para o Futebol de Discos com capitães convidados da versão **2026.11.14.1**.

1. Pare o **Play** no Unity.
2. Extraia `Atualizacao_Sorteio_Marbles_Capitaes.zip`.
3. Copie a pasta **Assets** extraída para a raiz do seu projeto e aceite as substituições dos scripts.
4. Aguarde a recompilação e abra uma das cenas em **Tools > Futebol de Discos > Capitães > Abrir Liga** ou **Abrir Mata-mata**.
5. Selecione **FutebolDiscos_Manager > Projeto Futebol Discos > Capitães convidados > Sorteio na tela**, confira os controles e salve a cena.

O ZIP inclui cinco scripts e os arquivos `.meta` dos dois scripts novos. Não substitui cenas, fichas, imagens, sons ou `ProjectSettings`. Seus personagens, personalizações e resultados permanecem no projeto.

## Para ver um sorteio novo

**Se essa temporada já possui associações salvas, o jogo as retoma e pula o sorteio por padrão.** Isso também vale para temporadas criadas na versão 2026.11.14.1.

Para fazer um sorteio novo na tela, pare o Play e use **Nova temporada (preserva as anteriores)**. Depois mantenha **Mostrar sorteio antes do campeonato** marcado e dê Play. A temporada anterior continua guardada.

Para apenas assistir às associações que já existem, marque **Reexibir sorteio salvo**. Nesse caso a tela indica **SAVED DRAFT**: ela não muda capitães, resultados ou a temporada.

## Preparar capitães e times

Configure os participantes e o catálogo de **Capitães disponíveis** como na versão anterior. É necessário pelo menos um capitão diferente por time preenchido. A quantidade na pista acompanha os times ativos, de 2 a 32:

| Times preenchidos | Marbles no sorteio |
| --- | --- |
| 10 | 10 |
| 16 | 16 |
| 20 | 20 |
| 32 | 32 |

Folgas de uma chave não criam marbles. Se o catálogo contiver mais personagens, o sorteio escolhe apenas os necessários, sem repetir capitães. Retire da lista modelos ou personagens que não quiser selecionar.

As fotos vêm das fichas de capitão que você criou. As 16 fichas genéricas da versão anterior continuam disponíveis; esta atualização não inclui imagens de DBZ.

## O que acontece na tela

A abertura **CAPTAIN DRAFT** mostra os capitães como marbles e os clubes em um quadro ao lado. As marbles passam por pinos, rampas e obstáculos giratórios. As chegadas definem a distribuição dos clubes disponíveis e revelam cada par com nome e imagem do capitão e do time.

**Associações fixas (opcional)** continuam reservando os pares escolhidos, como Real Madrid + Goku; os demais capitães e times participam da distribuição aleatória. O overall não dá vantagem na pista. Ele continua afetando apenas o capitão durante a partida de futebol.

Quando todos os pares forem revelados, o resultado é salvo e fica em exibição por alguns segundos. Só então a liga ou o mata-mata começa automaticamente. Não há partida nem cronômetro de jogo rodando durante o sorteio.

## Ajustar a apresentação

Todos os controles ficam em **Capitães convidados > Sorteio na tela**:

| Controle | Padrão | Faixa |
| --- | --- | --- |
| Mostrar sorteio antes do campeonato | Ativado | Ativado / desativado |
| Apresentação dos participantes (s) | 2 segundos | 0 a 10 segundos |
| Velocidade das marbles no sorteio | 1 | 0,25 a 3 |
| Intervalo entre associações (s) | 0,35 segundo | 0,05 a 2 segundos |
| Exibição do resultado do sorteio (s) | 3 segundos | 0 a 30 segundos |
| Reexibir sorteio salvo | Desativado | Ativado / desativado |

Um multiplicador maior acelera as marbles; um intervalo menor revela os pares mais rapidamente. Faça ajustes fora do Play e salve a cena. Desligar a apresentação mantém o sorteio automático anterior. Esses controles não exigem apagar resultados nem criar outra temporada.

## Salvamento e interrupção

O sorteio completo é guardado em `Discos/<competição>/<temporada>/capitaes_convidados.json`, antes da leitura final e da primeira partida.

- **Stop durante a pista:** não grava um conjunto parcial de associações. No próximo Play, a apresentação recomeça com a semente já guardada para a temporada.
- **Stop depois de revelar e salvar todos os pares:** mantém o resultado completo. O próximo Play retoma os mesmos capitães.
- **Reexibir sorteio salvo:** apresenta os pares existentes e não grava outro sorteio.
- **Nova temporada:** permite outro sorteio e preserva os arquivos da anterior.

As regras de overall individual, capitão extra, vínculos fixos e salvamento de jogos continuam valendo. As cenas tradicionais de futebol não passam a usar esse sorteio de capitães.

A validação de código confere a simulação, as associações e os arquivos salvos. A importação e a aparência da apresentação precisam ser conferidas no Editor Unity; este ambiente não executa a renderização nem a física nativa do Unity.
