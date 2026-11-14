# Pinos do sorteio e fotos no chaveamento — 2026.11.14.3

## Instalar

Esta atualização é para o Futebol de Discos com o sorteio de marbles da versão **2026.11.14.2**.

1. Pare o **Play** no Unity.
2. Extraia `Atualizacao_Pinos_Fotos_Capitaes.zip`.
3. Copie a pasta **Assets** extraída para a raiz do seu projeto e aceite as substituições dos scripts.
4. Aguarde a recompilação e abra sua cena da liga ou do mata-mata com capitães convidados.
5. Confira as fotos dos capitães e salve a cena se alterar alguma configuração.

O ZIP substitui somente quatro scripts:

- `Assets/FutebolDiscos/Scripts/MotorSorteioCapitaes.cs`
- `Assets/FutebolDiscos/Scripts/SorteioCapitaesUI.cs`
- `Assets/FutebolDiscos/Scripts/ProjetoFutebolDiscos.cs`
- `Assets/Scripts/ChaveamentoUI.cs`

Ele não inclui cenas, fichas, imagens, sons, arquivos `.meta` ou `ProjectSettings`. Suas personalizações e os resultados salvos permanecem no projeto.

## O que muda na pista

Os contatos com os obstáculos passam a ser resolvidos também depois dos empurrões entre as marbles. A pista tem mais espaço de passagem entre pinos, rampas e obstáculos giratórios. Os pinos permanecem ativos até o fim da apresentação.

Quando uma marble fica sem progresso, ela pode receber um impulso local para voltar a se mover. Ela continua precisando cruzar a linha de chegada: não é teleportada para fora dos obstáculos nem recebe um time antes de terminar.

As marbles que já chegaram aguardam em uma área abaixo da pista. Na revelação, o retrato segue por fora dos obstáculos até o cartão do clube, sem voltar à área onde as outras marbles ainda estão disputando.

## O time é aleatório e depois fica fixo?

Sim. Cada marble que chega recebe um dos times ainda disponíveis na distribuição aleatória. **Associações fixas (opcional)** continuam reservando os pares que você escolheu manualmente.

Quando todos os pares terminam, o resultado é salvo na temporada. O capitão permanece no mesmo time ao avançar de rodada ou fase e depois de Stop/Play. A atualização não troca associações existentes nem altera o overall dos capitães.

**Não precisa criar outra temporada para aplicar estas correções ou mostrar as fotos.** Se quiser rever a pista atualizada mantendo os pares, use **FutebolDiscos_Manager > Projeto Futebol Discos > Capitães convidados > Sorteio na tela > Reexibir sorteio salvo**. A tela indica **SAVED DRAFT** e conserva os capitães e resultados.

Use **Nova temporada (preserva as anteriores)** somente quando quiser uma nova distribuição de capitães e times.

## Foto do capitão no chaveamento

Nas cenas de mata-mata com convidados, a foto do capitão aparece ao lado do escudo, nome e placar do clube no quadro de chaves. Ela acompanha o clube nas fases seguintes e também aparece nas prévias de confronto e apresentações do campeão.

A imagem vem do campo **Foto** da ficha do capitão já atribuído ao time. Por exemplo, se o sorteio salvo associou Goku ao Real Madrid, é a foto desse Goku que aparece ao lado do Real Madrid. O chaveamento não escolhe outro representante.

Para trocar a imagem, selecione a ficha do capitão no Project e altere **Foto**. Se necessário, importe a imagem com **Texture Type = Sprite (2D and UI)** e clique em **Apply**. Nome e foto são ajustes visuais e podem ser alterados sem fazer outro sorteio.

Sem foto cadastrada, o chaveamento mantém o desenho anterior. Vagas **TBD** ou **BYE** não recebem retratos, e as cenas tradicionais sem convidados continuam com seu comportamento anterior.

## Conferir no Unity

Reexiba um sorteio salvo para conferir os contatos e o trajeto das marbles após a chegada. No mata-mata, confira o retrato ao lado dos times e sua permanência após avançar uma fase ou retomar com Stop/Play.

As verificações de código usam fachadas das APIs Unity. A importação, a aparência e os contatos físicos finais precisam ser conferidos no Editor Unity; este ambiente não executa a renderização nem a física nativa do Unity.
