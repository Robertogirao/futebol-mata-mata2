# Escopo da validação — 11/11/2026

Todos os relatórios foram gerados antes da publicação, usando os arquivos C# reais desta entrega.

| Grupo | Verificações | Método |
| --- | --- | --- |
| Calendário | 75.275 | Modelo e parser CSV reais, sem dependências Unity |
| Motor e persistência | 41 | Motor, armazenamento e parser reais, APIs Unity simuladas |
| Integração da liga | 106 | Controlador e oito scripts de domínio reais; UI, física e ciclo Unity simulados |
| Inspector | 105 | Editor e dados Pokémon reais, APIs UnityEditor simuladas |
| Apresentação | 360 | HUD, arena, classificação e menus reais, APIs Unity simuladas |

A integração usa JSON real com `DataContractJsonSerializer` como substituto de `JsonUtility`; não certifica a implementação do serializador do Unity. A validação de referências confere YAML, IDs locais, GUIDs e dependências dos assets. GUIDs de pacotes e recursos internos do Unity dependem da importação dos pacotes.

Não houve execução do Unity Editor ou Play Mode. Esses testes não verificam renderização, temporização real dos callbacks, colisões físicas ou importação de assets.
