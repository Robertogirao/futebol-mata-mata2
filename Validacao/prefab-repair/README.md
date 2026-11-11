# Correção de importação de Bola_Pokemon — 11/11/2026

O GameObject `Bola_Pokemon` listava dois componentes sem documentos serializados correspondentes:

- `-6015267977959516197`: no prefab de futebol original, era um `TrailRenderer`.
- `-2723555834122662228`: no prefab original, era `DesprenderBolaDaHelice`.

No backup Pokémon, os dois documentos já haviam sido removidos, mas as entradas continuavam em `m_Component`. O Unity reportou os mesmos dois IDs como `Broken text PPtr`, seguidos por componentes que não podiam ser carregados e entradas do tipo `Object` repetidas. As duas entradas sem destino foram removidas. Não foram reintroduzidos o rastro ou o comportamento de desprendimento do projeto de futebol.

O diff do prefab remove exatamente duas linhas. Os 11 documentos serializados, configuração de física, SpriteRenderer, BallController, BolaPokemonVisual, texto filho e GUID do prefab foram preservados.

## Verificação

O validador usa Python 3 e PyYAML:

```sh
python3 validate_unity_assets.py /caminho/para/Projeto --output report.json
```

Ele verifica todas as cenas e prefabs encontrados em `Assets`: estrutura YAML, IDs únicos, referências locais sem GUID, componentes únicos por GameObject, correspondência dos componentes com seu proprietário, um Transform por GameObject, filhos e pais consistentes e ausência de ciclos. Os GUIDs encontrados nos `.meta` são conferidos contra os arquivos fonte; referências externas de pacotes ou recursos internos são listadas separadamente.

`before.json` demonstra os dois erros no prefab anterior. `after.json` registra zero erros nos três arquivos de cena/prefab do projeto. `preservation.json` demonstra que a mudança foi restrita à remoção das duas linhas inválidas.

Esta é uma validação estática da serialização. O Unity Editor não está instalado neste ambiente; importação real, recursos externos de pacotes e Play Mode precisam ser conferidos no Unity.
