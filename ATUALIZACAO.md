# Correção de importação — liga Pokémon

Esta atualização é para o projeto de liga Pokémon publicado em `pokemon-liga-v2026.11.11`.

1. Feche o Unity.
2. Extraia `Downloads/Pokemon_Liga_Correcao_Unity_2026-11-11.zip`.
3. Copie a pasta **Assets** do ZIP para dentro da pasta **Projeto** da sua liga, substituindo os arquivos correspondentes.
4. Abra o projeto no Unity **6000.4.11f1** e aguarde a reimportação.

A atualização contém os 13 scripts alterados, o prefab da bola e seus arquivos `.meta`. Os GUIDs foram preservados. A cena, os participantes, os fundos e as configurações da sua liga não são substituídos. Os arquivos de classificação salvos também não são alterados.

Se a mensagem `Broken text PPtr` em `Library/Unused` continuar após a atualização, feche o Unity e remova a pasta **Library** dentro do projeto. Ela contém o cache de importação e será recriada na próxima abertura; a primeira importação levará mais tempo.

## Correções

- `Bola_Pokemon.prefab`: removidas duas entradas da lista de componentes que apontavam para registros inexistentes. Eram os IDs `-6015267977959516197` e `-2723555834122662228`, citados no erro de importação. Os componentes válidos, suas configurações, a física e o visual da bola foram preservados.
- `FindFirstObjectByType` substituído por `FindAnyObjectByType` nas buscas de referências únicas.
- `FindObjectsByType` atualizado para os overloads sem `FindObjectsSortMode`, preservando a inclusão ou exclusão de objetos inativos.
- `enableWordWrapping = false` substituído por `textWrappingMode = TextWrappingModes.NoWrap`.

Os relatórios em `Validacao` descrevem as verificações estáticas e de lógica C#. Não houve execução do Unity Editor neste ambiente; a importação e o Play devem ser conferidos no Unity.
