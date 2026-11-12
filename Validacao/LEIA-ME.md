# Escopo da validação — 11/11/2026

Os relatórios registram as fontes e o escopo de cada execução. Os grupos afetados pela correção de inicialização foram repetidos na versão atual; os demais registram a validação anterior dos scripts preservados.

| Grupo | Verificações | Método |
| --- | --- | --- |
| Calendário | 75.275 | Modelo e parser CSV reais, sem dependências Unity |
| Motor e persistência | 41 | Motor, armazenamento e parser reais, APIs Unity simuladas |
| Integração da liga | 154 | Controlador e oito scripts de domínio reais; UI, física e ciclo Unity simulados |
| Inspector | 105 | Editor e dados Pokémon reais, APIs UnityEditor simuladas |
| Apresentação | 376 | HUD, arena, classificação e menus reais, APIs Unity simuladas |
| Eventos e lançadores | 69 | Bola, especiais, goleiro, gol, lançador e Rocket reais, APIs Unity simuladas |

A integração usa JSON real com `DataContractJsonSerializer` como substituto de `JsonUtility`; não certifica a implementação do serializador do Unity. A validação de referências confere YAML, IDs locais, GUIDs e dependências dos assets. GUIDs de pacotes e recursos internos do Unity dependem da importação dos pacotes.

Não houve execução do Unity Editor ou Play Mode. Esses testes não verificam renderização, temporização real dos callbacks, colisões físicas ou importação de assets.

## Correção de importação e migração de APIs

A versão `pokemon-liga-v2026.11.11.1` repete os grupos de controlador, motor, apresentação e Inspector contra os scripts atualizados. Os contratos simulados removem as APIs obsoletas; os relatórios individuais e `api-migration-regression.json` foram atualizados.

`prefab-repair` registra as duas referências locais inválidas antes da correção e zero erros depois em todos os três prefabs/cenas do projeto, incluindo composição dos GameObjects e hierarquia. A importação real continua dependendo do Unity.

`atualizacao-zip-verificada.json` registra a versão de origem e confirma que aplicar o ZIP pequeno produz os mesmos arquivos do projeto atualizado, preservando a cena e as configurações.

## Inicialização antes de OnEnable

Na versão `pokemon-liga-v2026.11.11.2`, as regressões do controlador e do HUD distinguem configuração habilitada de registro de `OnEnable`, conforme a [documentação do Unity 6.4](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Behaviour-isActiveAndEnabled.html).

O controlador anterior reproduziu a mensagem exata **Ative os componentes da liga antes do Play** nesse cenário; o código corrigido passou no mesmo teste. Foram preservadas as 106 verificações anteriores e acrescentadas 48 sobre inicialização, componentes realmente desativados, objetos inativos e validação sem gravação. No HUD, as 360 verificações anteriores foram preservadas e 16 novas conferem o `Awake` real antes do registro de `OnEnable`, as referências do placar, o cronômetro e o tema.

Os relatórios `startup-regression-summary.json`, `startup-regression-before.txt` e `hud-awake-regression.json` detalham a execução. A sequência de ativação foi simulada em C#; isso não equivale a executar o Unity Editor.

## Escala do sprite da bola

Na versão `pokemon-liga-v2026.11.11.3`, a correção é restrita aos assets: um sprite dedicado com PPU normalizado e a referência desse sprite no prefab. O PNG mantém os pixels da imagem original. A checagem dimensional usa recorte, PPU, escala do prefab, raio do colisor e a ocupação do retrato para conferir os tamanhos antes e depois. Também confere que os demais registros do prefab, parâmetros físicos e o sprite original compartilhado permanecem iguais.

Os relatórios em `ball-scale` registram o alcance da verificação estática. Não foram alterados scripts nesta correção, e não houve renderização, importação ou execução em Play no Unity.

## Textos somente em inglês

Na versão `pokemon-liga-v2026.11.11.4`, dez textos visíveis em `ClassificacaoUI`, `AberturaJogoUI` e `TournamentManager` perderam os trechos italianos. Os nomes de campos, dados salvos, tempos e cálculos permanecem iguais. A revisão do diff conferiu que as mudanças de código se limitam aos textos; o teste existente da interface foi repetido e manteve 376 verificações aprovadas. Não foram criados testes adicionais para esta alteração de texto.

`english-ui-update.json` lista as fontes e o escopo da verificação. A revisão da interface usa APIs simuladas e não equivale a executar o Unity Editor.
