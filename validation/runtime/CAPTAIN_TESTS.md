# Validação dos nomes, capitães e autores dos gols

Execute `bash validation/runtime/run-captain.sh` com Mono (`mcs` e `mono`) instalado. As variáveis `MCS` e `MONO` podem selecionar executáveis alternativos. O script compila os arquivos reais do projeto com a fachada Unity/TMP já usada pelos testes de integração.

As 87 verificações aprovadas cobrem:

- Um capitão por equipe, associado ao representante escolhido para a foto, inclusive quando ele não é o primeiro jogador do elenco.
- Jogadores distintos nos outros discos; elencos curtos não duplicam o capitão nem inventam nomes.
- Tags de nome, marcador C, opções de ocultação e foto opcional no disco do capitão.
- Gol do capitão com nome e foto; gol de outro jogador com nome e bandeira/escudo, mesmo que esse jogador também tenha foto cadastrada.
- Foto ausente do capitão, gol contra com o autor real marcado como OG e gol sem toque conhecido identificado pelo time.
- Preservação dos nomes, fotos e ordem do elenco ao atribuir os discos e registrar os gols.
- Fila de dois gols com dados copiados no momento do evento; alterações posteriores no cadastro não trocam nome e imagem de uma comemoração já enfileirada.

Os cruzamentos da linha do gol e o relógio são controlados pelo teste. Esta validação não executa colisões nativas, importação ou renderização de fontes no Unity Editor.
