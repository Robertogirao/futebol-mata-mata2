# Basquete de Discos — atualização das 4 cenas

[**Baixar somente a atualização**](ATUALIZACAO_BASQUETE_4_CENAS.zip)

Adaptação automática de basquete arcade para a cópia do projeto Unity 6000.4.11f1 enviada no backup de quatro partes. O pacote contém apenas os arquivos novos e alterados; inclui também o ajuste anterior de capitão e posições do elenco.

## Instalação

1. Abra **a cópia destinada ao basquete**. Saia do Play e feche o Unity antes de copiar os arquivos.
2. Extraia o ZIP. Copie a pasta `Assets` para a raiz dessa cópia, junto das pastas `Packages` e `ProjectSettings`. **Mescle as pastas e substitua os arquivos de mesmo nome**. Preserve os arquivos `.meta` do pacote.
3. Reabra no Unity 6000.4.11f1 e aguarde a importação/compilação.
4. Abra uma das quatro cenas abaixo. No objeto `FutebolDiscos_Manager`, use **Validar configuração** e depois Play.

As quatro cenas são substituídas pelas versões baseadas no backup enviado. Os caminhos e GUIDs foram mantidos para preservar as referências do projeto.

| Cena existente | Formato | Configuração inicial | Especial |
| --- | --- | --- | --- |
| `Assets/Scenes/FutebolDiscos/Liga_Discos.unity` | Liga | 3 jogadores por time | Desligado |
| `Assets/Scenes/FutebolDiscos/MataMata_Discos.unity` | Mata-mata | 3 jogadores por time | Desligado |
| `Assets/Scenes/FutebolDiscos/Capitaes/Liga_Capitaes.unity` | Liga | 2 jogadores + 1 capitão convidado por time | Ligado |
| `Assets/Scenes/FutebolDiscos/Capitaes/MataMata_Capitaes.unity` | Mata-mata | 2 jogadores + 1 capitão convidado por time | Ligado |

## Como funciona

- Quadra de madeira, garrafões, linhas de 3 pontos, tabelas, aros e redes vistos de cima.
- A IA controla posse, drible, passes, marcação, roubos e rebotes. A bola acompanha o portador e percorre um arco nos arremessos.
- Cestas valem **2 ou 3 pontos pela posição do disco no momento do arremesso**. Encostar na linha vale 2. O resultado do lance é influenciado pelo overall, distância e marcação.
- Partida inicial de **120 segundos**, com **12 segundos de posse**. Reposição e preparação do especial pausam o relógio. Há sons próprios de drible, cesta e buzzer.
- Empate leva a **20 segundos de prorrogação**, repetidos se necessário, inclusive na liga. Uma bola arremessada antes do buzzer termina o lance antes da decisão. A primeira cesta da prorrogação não encerra a partida.
- Nas cenas `Capitaes`, ações espaçadas do capitão carregam a barra. Ao completar a carga, o próximo arremesso elegível usa a roleta e uma chance de acerto maior. O especial continua valendo 2 ou 3 pontos pela origem do lance.
- A liga usa aproveitamento (`PCT`), jogos, vitórias, derrotas, pontos a favor e saldo. O mata-mata recebe os pontos completos do placar.
- Textos gerados na transmissão em inglês: `SHOT CLOCK`, `POSSESSION`, `BASKET`, `OVERTIME`, entre outros.

## Onde ajustar

Selecione o manager da cena. A seção **Regras e ritmo do basquete** permite alterar relógio de arremesso, prorrogação, duração do voo, passe, reposição e intervalo do drible. Para usar posse de 24 segundos, ajuste **Relógio de arremesso = 24**. A duração regulamentar fica em **Tempo real de jogo**.

Em **Sons e apresentação das cestas**, troque os efeitos e ajuste os volumes. Música da partida, fades, música do sorteio e continuação/fade da abertura continuam disponíveis. Os três efeitos novos ficam em `Assets/BasqueteDiscos/Audio` e foram sintetizados para esta atualização.

Nas fichas dos times, escolha o capitão das cenas normais pelo nome e a função de cada jogador: **Pivô / Center**, **Armador / Guard** ou **Ala / Wing**. A prévia da ficha usa as posições iniciais do basquete. Nas cenas de convidados, a função e o overall do capitão vêm da ficha dele. As vagas sem jogador continuam usando a abreviatura do time e o número do disco. As cenas normais ainda precisam de um representante cadastrado; nas de convidados, o elenco do time pode ficar vazio.

As fichas, nomes, fotos e escudos cadastrados no backup foram mantidos. Você pode substituí-los pelos times e jogadores da NBA que quiser usar. O novo tema `Assets/BasqueteDiscos/Tema_Basquete.asset` usa cores de basquete e deixa livres os campos de logo e fundos da competição.

Foram definidos novos identificadores `basquete_*` e temporada `basket_2026_01`, separados dos resultados de futebol. Use **Nova temporada** para reiniciar o sorteio ou mudar participantes/estrutura de uma temporada já salva.

## Verificação e limites

Compilação C# e testes com as classes reais e substitutos das APIs Unity: 50 partidas simuladas do motor, 12 jogos completos integrando relógio e simulador, 8 jogos da ponte de atores, pontuação de 2/3, roleta, rebotes, encerramento, prorrogações, cancelamento, liga/mata-mata, banners e seleção do capitão. Também foram conferidos os quatro arquivos de cena, GUIDs, referências e áudios.

**O Unity Editor não estava disponível neste ambiente. A aparência e o comportamento em Play Mode ainda precisam de conferência no seu Unity.** É uma adaptação arcade com período curto; faltas, lances livres, substituições e todas as regras oficiais da NBA não fazem parte desta atualização.
