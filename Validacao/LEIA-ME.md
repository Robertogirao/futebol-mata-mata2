# Validação — Pokémon Marble Race

Esta pasta contém os relatórios da preparação do projeto. Cada relatório descreve seu escopo; nenhum deles representa uma execução do Unity Editor.

| Grupo | Verificação |
| --- | --- |
| track | Modelo e gerador reais, sem Unity: 750 circuitos e casos de borda |
| competition | Calendário, pontuação, quatro formatos, JSON e falhas reais de escrita |
| hud | Interface real compilada com contratos Unity/TMP/UI |
| view | Geometria, listas de colisores, obstáculos, atalhos e limpeza de recursos |
| editor | Controles do Inspector e correspondência por reflexão com o controlador compilado |
| scene | YAML, referências locais, GUIDs, fontes, materiais, defaults e Build Settings |
| integration | Todos os novos scripts de runtime compilados juntos; ciclo de corrida, resultado, retomada e persistência |

Os testes puros de domínio não usam stubs Unity. Nos testes de integração, `JsonUtility` é substituído por `DataContractJsonSerializer`, com JSON real, e os corpos físicos integram apenas velocidades. A simulação não resolve contatos entre marbles, paredes ou obstáculos. O adaptador verifica o fluxo e os contratos utilizados, não certifica as implementações nativas do Unity.

O teste de movimento cobre a normalização de imagens grandes, o grid de 16 participantes, a ordem dos checkpoints, recuperação de marbles bloqueadas e rejeição de avanço fora do atalho. O teste do controlador cobre abertura, countdown, Mini GP completo, resultados, pausa por prova, retomada, melhor de três, eliminatórias, timeout e falha de gravação sem duplicar pontos.

`SHA256SUMS-Projeto.txt` identifica todos os arquivos do projeto publicado. Os relatórios de fonte registram os hashes dos arquivos verificados em cada execução. As tabelas de domínio e geometria foram verificadas antes da integração final; os testes do controlador e das marbles foram repetidos nas fontes finais.

A importação de pacotes, a aparência, o shader na GPU, as colisões físicas e o Play Mode precisam ser conferidos no Unity 6000.4.11f1.
