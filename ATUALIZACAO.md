# Correção do tamanho das bolas — liga Pokémon

Esta atualização cumulativa é para o projeto de liga Pokémon publicado em `pokemon-liga-v2026.11.11`, `.1` ou `.2`. Ela inclui as correções anteriores de inicialização, importação e APIs.

1. Feche o Unity.
2. Extraia `Downloads/Pokemon_Liga_Correcao_Tamanho_Bolas_2026-11-11.zip`.
3. Copie a pasta **Assets** do ZIP para dentro da pasta **Projeto** da sua liga, substituindo os arquivos correspondentes.
4. Abra o projeto no Unity **6000.4.11f1** e aguarde a reimportação.

A atualização contém os 13 scripts alterados nas correções anteriores, o prefab da bola, o novo sprite dedicado e seus arquivos `.meta`. O novo sprite usa um GUID próprio; os GUIDs existentes foram preservados. A cena, os participantes, os fundos e as configurações da sua liga não são substituídos. Os arquivos de classificação salvos também não são alterados.

Se a mensagem `Broken text PPtr` em `Library/Unused` continuar após a atualização, feche o Unity e remova a pasta **Library** dentro do projeto. Ela contém o cache de importação e será recriada na próxima abertura; a primeira importação levará mais tempo.

## Correções

- **Tamanho da bola:** o prefab passa a usar `Bola_Base_Pokemon`, com o mesmo desenho da Pokébola e importação normalizada para uma unidade. Na escala 0,65 do prefab, o corpo passa de 3,159 para 0,65 unidade; o aro passa de 3,475 para 0,715. O retrato é ajustado automaticamente pelo código existente. Colisor, escala do objeto, massa, gravidade, força de lançamento, cores e especiais são preservados. O sprite original usado pela hélice e pelos logos não é alterado.
- `LigaPokemon`: a configuração dos componentes passa a ser conferida por `enabled` e `activeInHierarchy` no `Awake`. No Unity 6.4, `isActiveAndEnabled` pode ser falso antes de `OnEnable`, mesmo quando as caixas dos componentes estão marcadas. Isso produzia a mensagem incorreta **Ative os componentes da liga antes do Play** e interrompia a preparação. Se algum componente estiver realmente ausente ou desativado, a mensagem agora identifica seu nome.
- `PokemonHUD`: o `Awake` prepara o placar com base na configuração do componente e do objeto, sem depender do registro de `OnEnable`. A preparação do tema continua antes da inicialização da arena e do HUD; o início das partidas continua em `Start`.
- `Bola_Pokemon.prefab`: removidas duas entradas da lista de componentes que apontavam para registros inexistentes. Eram os IDs `-6015267977959516197` e `-2723555834122662228`, citados no erro de importação. Os componentes válidos, suas configurações, a física e o visual da bola foram preservados.
- `FindFirstObjectByType` substituído por `FindAnyObjectByType` nas buscas de referências únicas.
- `FindObjectsByType` atualizado para os overloads sem `FindObjectsSortMode`, preservando a inclusão ou exclusão de objetos inativos.
- `enableWordWrapping = false` substituído por `textWrappingMode = TextWrappingModes.NoWrap`.

Os relatórios em `Validacao` descrevem as verificações estáticas e de lógica C#. Não houve execução do Unity Editor neste ambiente; a importação e o Play devem ser conferidos no Unity.
