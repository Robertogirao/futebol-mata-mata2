# Futebol Mata-mata — troféu e tempo das fotos

O centro do chaveamento, a final e o campeão usam o mesmo **Logo Competição** exibido no topo, preservando a proporção e as cores da imagem.

## Regular as apresentações

Selecione **TournamentManager** na Hierarchy e encontre **Mata Mata Campeonato (Script)** no Inspector. Em **Duração das apresentações (segundos)** ficam juntos:

- **Tempo Inicio**: abertura inicial da competição.
- **Tempo Pre Jogo**: apresentação do confronto.
- **Tempo Fotos Jogadores**: fichas ampliadas no centro (padrão: 5 segundos).
- **Tempo Transição Fotos**: retorno das fichas aos cantos (padrão: 0,8 segundo).
- **Tempo Final**: encerramento.

Altere fora do Play e salve a cena. Cronômetro, apito e bolas começam após fotos e transição. Na liga, use os controles de fotos do **TournamentManager**.

Para trocar o troféu, abra o asset do campo **Tema** e ajuste **Logo Competicao**, em **Imagens da transmissão**. A mesma imagem aparece no topo e no centro; **Logo Canal** permanece no outro canto.

## Baixar

- [**Projeto completo — ZIP**](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/tags/v2026.11.10-trofeu-e-tempos/Downloads/PROJETO_CORRIGIDO.zip): extraia e abra a pasta `Projeto` no Unity Hub.
- [**Atualização do projeto atual — ZIP**](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/tags/v2026.11.10-trofeu-e-tempos/Downloads/ATUALIZACAO_BOLAS_E_ABERTURA.zip): com Unity fechado, copie `Assets` sobre a pasta correspondente do seu projeto.
- [Página da versão](https://github.com/Robertogirao/futebol-mata-mata2/releases/tag/v2026.11.10-trofeu-e-tempos).

Os pacotes incluem também a correção das bolas inativas para **0,5 segundo**. Consulte `LEIA-ME-CORRECOES.txt` para os passos de atualização.

## Abrir o repositório

Use **Unity 6000.4.11f1**. No Unity Hub, adicione a raiz deste repositório, que contém `Assets`, `Packages` e `ProjectSettings`, e aguarde a importação.

## Validação

A lógica dos scripts C# foi compilada e testada com substitutos das APIs Unity e TMPro. A aparência, a física e o funcionamento em **Play Mode** ainda precisam ser conferidos no editor Unity. Mais detalhes em `docs/VALIDACAO.md`.

O backup recebido não contém temporadas salvas nem gravações externas.
