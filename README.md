# Futebol de Discos

**[Baixar o projeto completo em ZIP](https://github.com/Robertogirao/futebol-mata-mata2/archive/refs/tags/futebol-discos-v2026.11.13.2.zip)** · [Versão no GitHub](https://github.com/Robertogirao/futebol-mata-mata2/releases/tag/futebol-discos-v2026.11.13.2)

Projeto Unity de futebol automático entre discos, com liga e mata-mata. Reaproveita o catálogo de equipes, representantes, aberturas, classificação, chaveamento e salvamento do projeto de futebol. A partida usa um campo visto de cima, dois gols, discos das equipes e uma única bola branca: os gols acontecem pelas colisões e pelos chutes da simulação.

Esta versão substitui o Beat the Keeper. Cada time começa com uma quantidade fixa de discos; não há lançamento contínuo de bolinhas nem goleiro do jogo anterior. A apresentação tem identidade própria, com arena em tons de verde-azulado e ciano. Os textos da transmissão estão em inglês; os controles do Inspector estão em português.

**Já personalizou o projeto?** [Baixe a atualização de capitão, tags e textos](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/tags/futebol-discos-v2026.11.13.2/Atualizacoes/Atualizacao_Capitao_Tags_Textos.zip) e siga [as instruções de atualização](Atualizacoes/COMO_APLICAR_CAPITAO_TAGS_TEXTOS.md). Ela substitui apenas oito scripts e preserva suas cenas e configurações.

## Abrir o projeto

1. Instale o **Unity 6000.4.11f1** pelo Unity Hub.
2. No Hub, escolha **Add / Adicionar projeto** e selecione a pasta **`Projeto`**, que contém `Assets`, `Packages` e `ProjectSettings`.
3. Aguarde a importação. Abra **Tools > Futebol de Discos > Abrir Liga** ou **Abrir Mata-mata**.
4. Selecione **FutebolDiscos_Manager** na Hierarchy e configure o componente **Projeto Futebol Discos**. Clique em **Validar configuração**, salve a cena e dê **Play**.

As cenas ficam em `Assets/Scenes/FutebolDiscos/`:

| Cena | Configuração inicial |
| --- | --- |
| `Liga_Discos.unity` | 8 seleções, 7 rodadas, 3 discos por equipe |
| `MataMata_Discos.unity` | 16 clubes, chave de 16, 3 discos por equipe |

O campo e a transmissão são construídos ao entrar em Play. O guia completo está em [Projeto/COMO_USAR.md](Projeto/COMO_USAR.md).

## Formatos e controles

- **Liga:** 2 a 32 equipes e 1 a 100 rodadas. O calendário distribui todos os confrontos de um turno, inclui folga para número ímpar de equipes e inverte os lados no returno. Vitória vale 3 pontos; empate, 1; derrota, 0.
- **Mata-mata:** chaves de 2, 4, 8, 16 ou 32 vagas, com folgas opcionais, ordem da lista ou cabeças de chave e opção de embaralhar. Empate leva a **Golden Goal**: a partida continua até um gol real da simulação, podendo ultrapassar a duração regulamentar. O sistema não escolhe um vencedor artificial.
- **Partidas:** 1 a 5 discos por time, com padrão de 3, mais uma única bola branca. Velocidades, tamanhos, força do chute, pausa após gol, uso do overall e permanência dos discos em sua metade ficam no painel central.
- **Transmissão:** nome da competição, fontes, logos, fundos, cores, duração das aberturas, fotos ampliadas dos representantes e tempo das tabelas ficam no mesmo painel e no tema associado.

## Tags, capitães e textos da arena

No **FutebolDiscos_Manager**, o grupo **Identificação dos jogadores** permite mostrar nomes sobre os discos, ajustar as tags e destacar o capitão com borda dourada, letra C e foto. O capitão é o representante mostrado na ficha da partida. Os demais discos usam os outros nomes cadastrados no **Elenco** de cada `TeamData`.

Na comemoração, o capitão usa nome e foto; os demais usam nome e bandeira/escudo. Se faltarem jogadores no elenco, os discos restantes usam a sigla e seu número, sem repetir o capitão.

Em **Textos da arena**, edite ou oculte o texto do campo, o título e o subtítulo do rodapé. Os detalhes de formato e número de discos têm um controle separado. Esses ajustes não exigem outra temporada.

## Temporadas e salvamento

Cada resultado concluído é salvo automaticamente. Na liga, a rodada completa é confirmada antes de avançar; o mata-mata também guarda o chaveamento. Após **Stop** e **Play**, a mesma temporada retoma os jogos ainda pendentes. Uma partida interrompida antes de terminar recomeça; sua posição e seu cronômetro não são salvos.

Com **Semente do sorteio = 0**, a temporada recebe uma semente aleatória no primeiro Play e a conserva nos próximos. Para outro sorteio, use **Nova temporada (preserva as anteriores)**. Alterar participantes, formato, rodadas, semente ou regras da partida exige outra temporada. Nome, fontes, imagens e tempos da apresentação podem ser ajustados sem apagar os resultados.

Os dados ficam em `Application.persistentDataPath`, sob o Product Name **Futebol de Discos**, separado dos salvamentos do projeto anterior:

```text
Discos/<competição>/<temporada>/configuracao_discos.json
Ligas/<competição>/<temporada>/temporada.json
MataMata/<competição>/<temporada>/torneio.json
```

Use **Abrir pasta desta temporada** para localizar seus metadados. As pastas `Ligas` e `MataMata` ficam na mesma raiz de dados, ao lado de `Discos`. Uma falha de escrita pausa o avanço; os resultados não são descartados para seguir a competição.

## Estado da validação

A entrega contém o **projeto-fonte**, com scripts, assets e cenas escritos nesta versão. As cenas ainda não foram abertas ou renderizadas no Unity neste ambiente; não há captura de tela apresentada como teste real.

As verificações em `validation/` exercitam o calendário, CSV, persistência, continuidade dos torneios, geometria e contratos do código C#. Os testes que dependem de APIs Unity usam fachadas dessas APIs. Eles não substituem um Play no Editor com importação real, contatos da física, fontes, áudio e renderização. O conjunto de calendário e metadados passou por **37.629 verificações** com o parser CSV original e arquivos reais.

Antes da primeira gravação, abra cada cena no Unity e confira a partida, os gols, as aberturas e a retomada da temporada.
