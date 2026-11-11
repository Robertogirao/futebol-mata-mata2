# Escopo da validação — 11/11/2026

Todos os relatórios foram gerados antes da publicação, usando os arquivos C# reais desta entrega.

| Grupo | Verificações | Método |
| --- | --- | --- |
| Calendário | 75.275 | Modelo e parser CSV reais, sem dependências Unity |
| Motor e persistência | 41 | Motor, armazenamento e parser reais, APIs Unity simuladas |
| Integração da liga | 106 | Controlador e oito scripts de domínio reais; UI, física e ciclo Unity simulados |
| Inspector | 105 | Editor e dados Pokémon reais, APIs UnityEditor simuladas |
| Apresentação | 360 | HUD, arena, classificação e menus reais, APIs Unity simuladas |
| Eventos e lançadores | 69 | Bola, especiais, goleiro, gol, lançador e Rocket reais, APIs Unity simuladas |

A integração usa JSON real com `DataContractJsonSerializer` como substituto de `JsonUtility`; não certifica a implementação do serializador do Unity. A validação de referências confere YAML, IDs locais, GUIDs e dependências dos assets. GUIDs de pacotes e recursos internos do Unity dependem da importação dos pacotes.

Não houve execução do Unity Editor ou Play Mode. Esses testes não verificam renderização, temporização real dos callbacks, colisões físicas ou importação de assets.

## Correção de importação e migração de APIs

A versão `pokemon-liga-v2026.11.11.1` repete os grupos de controlador, motor, apresentação e Inspector contra os scripts atualizados. Os contratos simulados removem as APIs obsoletas; os relatórios individuais e `api-migration-regression.json` foram atualizados.

`prefab-repair` registra as duas referências locais inválidas antes da correção e zero erros depois em todos os três prefabs/cenas do projeto, incluindo composição dos GameObjects e hierarquia. A importação real continua dependendo do Unity.

`atualizacao-zip-verificada.json` confirma que aplicar o ZIP pequeno à entrega anterior produz os mesmos 4.383 arquivos do projeto atualizado, preservando a cena e as configurações.
