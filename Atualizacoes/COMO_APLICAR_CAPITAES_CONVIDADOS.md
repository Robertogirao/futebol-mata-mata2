# Capitães convidados — 2026.11.14.1

## Instalar

Esta atualização é para o Futebol de Discos com os sons e controles da versão **2026.11.13.8**.

1. Pare o **Play** no Unity.
2. Extraia `Atualizacao_Capitaes_Convidados.zip`.
3. Copie a pasta **Assets** extraída para a raiz do seu projeto e aceite as substituições dos scripts.
4. Aguarde a importação e abra **Tools > Futebol de Discos > Capitães > Abrir Liga** ou **Abrir Mata-mata**.
5. Selecione **FutebolDiscos_Manager > Projeto Futebol Discos** e configure o grupo **Capitães convidados**. Salve a cena antes de dar Play.

O ZIP inclui oito scripts, os metadados dos dois scripts novos, duas cenas novas e 16 fichas de capitães de exemplo com seus metadados. Ele não inclui `ProjectSettings` nem substitui as cenas antigas. Suas cenas personalizadas, fontes, fundos, sons, fichas de times e resultados existentes permanecem no projeto.

O projeto completo possui as quatro cenas no Build Settings. Ao aplicar somente o ZIP de atualização, as novas cenas já podem ser abertas pelos menus; adicione-as ao seu Build Settings se for gerar um executável.

## As duas cenas

| Cena | Identificador da competição | Padrão por time |
| --- | --- | --- |
| `Assets/Scenes/FutebolDiscos/Capitaes/Liga_Capitaes.unity` | `discos_liga_capitaes` | 3 jogadores do time + 1 capitão |
| `Assets/Scenes/FutebolDiscos/Capitaes/MataMata_Capitaes.unity` | `discos_mata_capitaes` | 3 jogadores do time + 1 capitão |

Cada cena guarda suas próprias configurações e resultados. Use o painel principal para escolher times, duração, fundo, tema, fontes e sons. As opções de liga, mata-mata, classificação, Golden Goal e salvamento continuam funcionando.

## Cadastrar Goku ou outro personagem

No **Project**, clique com o botão direito e escolha **Create > Futebol de Discos > Capitão convidado**. Dê ao arquivo um nome único, por exemplo `Goku`.

Na ficha, preencha:

| Campo | O que colocar |
| --- | --- |
| Nome Capitao | Nome mostrado na abertura, tag e comemoração |
| Foto | Imagem do personagem importada como Sprite |
| Overall | Valor individual de 1 a 100 |

Para importar uma foto, selecione a imagem no Project, defina **Texture Type = Sprite (2D and UI)** e clique em **Apply**.

As cenas incluem 16 fichas neutras, `Capitao_01` a `Capitao_16`, em `Assets/FutebolDiscos/Capitaes/Modelos/`, com nome `Captain 01` etc., overall 75 e sem foto. Você pode editar essas fichas ou criar outras. Sem foto, a apresentação usa o escudo/bandeira como alternativa. Imagens de DBZ não fazem parte desta entrega.

O nome do arquivo identifica o capitão no salvamento. Evite renomear ou remover o arquivo enquanto ele estiver associado a uma temporada em andamento.

## Sortear personagens entre os times

Em **FutebolDiscos_Manager > Projeto Futebol Discos > Capitães convidados**:

1. Mantenha **Usar capitães convidados** marcado.
2. Preencha **Capitães disponíveis** com as fichas que quer usar. É necessário pelo menos um capitão diferente para cada time preenchido na competição.
3. Se usar **Carregar capitães do catálogo**, ele carrega as fichas encontradas no projeto, incluindo os modelos. Remova da lista os exemplos ou personagens que não quiser no sorteio.
4. Deixe **Associações fixas (opcional)** vazio para sortear todos. Para garantir Real Madrid + Goku, adicione uma associação com esse time e esse capitão; os outros times continuam sendo sorteados.
5. Confira os participantes e use **Validar configuração** antes do Play.

Um capitão não é repetido em times diferentes. A escolha usa a semente efetiva da temporada e é salva antes das partidas. Stop/Play conserva as associações, inclusive entre rodadas ou fases. **Nova temporada (preserva as anteriores)** prepara outra edição sem apagar a anterior.

## Disco extra ou vaga do time

**Capitão como disco extra** vem marcado. Nesse caso, **Jogadores do time (sem o capitão)** controla somente os jogadores do elenco de futebol:

- 3 jogadores do Real Madrid + Goku = 4 discos por equipe.
- 5 jogadores do Real Madrid + Goku = 6 discos por equipe.

Desmarcando **Capitão como disco extra**, o controle **Discos por time** passa a contar o personagem dentro do total. Por exemplo, 5 discos são 4 jogadores do Real Madrid + Goku.

Os jogadores de futebol vêm do elenco da ficha do time, em sua ordem. O capitão convidado não é inserido no asset do time e não remove jogadores do elenco original.

## Overall individual e apresentação

O overall do convidado modifica somente a movimentação, potência e precisão dos chutes do disco dele. Ele continua usando seu overall mesmo que **Considerar o overall dos times** esteja desligado. Os demais discos continuam seguindo as configurações normais do time; o overall do Real Madrid não é aumentado por causa do Goku.

O convidado é o representante da foto e aparece como **CAPTAIN**. Seu gol usa nome e foto; gols dos outros discos usam nome do jogador e escudo/bandeira. A vantagem individual influencia a simulação, sem escolher resultados antecipadamente.

## Salvar e alterar uma temporada

As associações e o overall usado ficam em:

```text
Discos/<competição>/<temporada>/capitaes_convidados.json
```

Os resultados continuam nas pastas de liga ou mata-mata. **Abrir pasta desta temporada** localiza os metadados. Não é preciso apagar a liga ou o mata-mata anterior.

Nome e foto do personagem podem ser ajustados como apresentação. Para mudar um capitão atribuído, seu overall, associações fixas ou a regra de disco extra, use outra temporada. O sistema interrompe configurações incompatíveis preservando os arquivos existentes; ele não troca os capitães silenciosamente durante a edição.

A validação de código usa fachadas das APIs Unity. A importação, a aparência e a física nativa das novas cenas precisam ser conferidas no Editor Unity.
