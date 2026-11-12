# Cena e configuração inicial

A cena MarbleRace_Pokemon.unity contém apenas Main Camera, Global Light 2D,
EventSystem e MarbleRace_Manager. Não contém componentes de goleiro, cano,
partida de futebol, liga ou pontuação por gol.

O Build Settings habilita somente a cena de corrida. Product Name e Bundle
Identifier foram isolados para Pokemon Marble Race. O roster padrão possui
Bulbasaur, Charmander, Squirtle, Pikachu, Psyduck, Geodude, Gastly e Eevee.

O tema novo reaproveita as fontes, cores e logos da base, com imagens de arena
do goleiro vazias. O fundo opcional fica vazio para usar o cenário procedural.

A pasta Resources contém MarbleRaceSprite.mat, com referência direta ao shader
URP2D Sprite-Unlit-Default (GUID 13c02b14c4d048fa9653293d54f6e0e1). Esse GUID
foi conferido no código oficial Unity-Technologies/Graphics, branch
6000.4/staging, em Packages/com.unity.render-pipelines.universal/Shaders/2D/
Sprite-Unlit-Default.shader.meta. O asset Resources mantém a referência do
shader para builds; a importação efetiva depende do Unity e de seus pacotes.

validate_scene.py confere YAML, proprietários dos componentes, referências
locais, GUIDs únicos, roster, câmera, parâmetros padrão e Build Settings.
É uma verificação estática, sem execução do Unity Editor.
