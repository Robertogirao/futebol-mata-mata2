# Posição do capitão nas cenas normais — v6

Extraia `Atualizacao_Posicao_Capitao_Cenas_Normais_v6.zip` na pasta **Projeto**, onde fica **Assets**, substituindo os três scripts. A atualização aplica-se ao projeto com a **v5 instalada**. Não substitui cenas, times, fotos ou catálogos.

## Onde escolher

1. No Project, selecione a **ficha do time (`TeamData`)**.
2. Localize **Capitão nas cenas normais de discos**.
3. Em **Posição do capitão (cenas normais)**, escolha **Defesa**, **Meio-campo** ou **Ataque**.

Cada time pode usar uma posição diferente. A escolha vale para seu representante/capitão tanto na Liga normal quanto no Mata-mata normal. Configure a ficha fora do Play para manter a seleção salva no asset.

Defesa preserva a formação antiga e é o padrão das fichas existentes. Meio-campo e Ataque movem o capitão para a vaga correspondente; outro disco cobre a vaga anterior, evitando sobreposição. A posição é usada na saída, nas reposições e como referência para a movimentação da IA. A postura tática e os limites da arena continuam sendo respeitados.

## As quatro cenas

| Cena | Posição do capitão | Especial |
| --- | --- | --- |
| `Liga_Discos` | Campo novo na ficha do time | Sem especial |
| `MataMata_Discos` | Campo novo na ficha do time | Sem especial |
| `Capitaes/Liga_Capitaes` | Posição do próprio convidado, como antes | Mantido |
| `Capitaes/MataMata_Capitaes` | Posição do próprio convidado, como antes | Mantido |

O campo novo é ignorado nas cenas da pasta `Capitaes`. A formação dos convidados foi preservada. Nas cenas normais, o motor não acumula carga nem inicia roleta/chute especial, e os painéis de especial ficam ocultos; fotos, nomes e gols comuns continuam funcionando.

Não é necessário recriar a temporada ou editar os arquivos das cenas. Nenhum elenco, overall ou vínculo de convidado é alterado.

## Arquivos do pacote

- `Assets/Scripts/TeamData.cs`
- `Assets/FutebolDiscos/Scripts/PartidaDiscos.cs`
- `Assets/Scripts/LayoutJogadorUnicoUI.cs`

O pacote preserva as diferenças mais recentes do BACKUP enviado. As fontes da branch recebem a mesma melhoria sobre sua base já publicada. O relatório em `validation/captain-special/results.txt` cobre as posições, os dois modos normais, o isolamento do especial, o fluxo dos convidados e a configuração das quatro cenas. Os testes compilam scripts reais contra fachadas Unity/TMP; o Editor Unity e a física nativa não foram executados neste ambiente.
