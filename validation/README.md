# Verificações desta versão

Os testes C# usam os arquivos reais do projeto e fachadas das APIs Unity/TMP/Editor. Os cenários de integração controlam o relógio, o agendamento e a posição da bola para verificar a lógica. Não executam o solver da física, importação, renderização, fontes ou áudio do Unity. O serializador das fachadas usa DataContract/XML; o projeto usa JsonUtility no Unity.

| Conjunto | Verificações aprovadas | O que foi exercitado |
| --- | ---: | --- |
| Calendário e metadados | 37.629 | 2–32 equipes, turnos, folgas, CSV real, sementes e gravação em arquivos reais |
| Interface | 2.347 | Placar, fichas, scanner atrás da foto e controle de velocidade, prioridade de Canvas, 32 equipes, banners e Recorder |
| Campo | 774 | Geometria, limites, imagem externa proporcional, decoração opcional, sensores e descarte de recursos |
| Torneios existentes | 68 | Liga, classificação, mata-mata, persistência e recuperação de resultado pendente |
| Partida | 45 | Cruzamento completo da bola, gols repetidos, reposição, limites e ciclo dos componentes |
| Regressão da reposição | 299 | Corpo congelado com Transform desatualizado, centenas de callbacks, saída confirmada na física e novos gols legítimos |
| Capitães e identificação | 87 | Um capitão por time, nomes distintos, tags, foto/bandeira por autor, gols contra e fila de banners |
| Posturas e pressão | 572 | Quatro posturas, limites de 0/1/2/3, oportunidades, recuo sem teleporte, vagas de retorno e bloqueio geral |
| Aberturas e prévias futuristas | 1.588 | Íris vertical, cobertura opaca, captura 30/60 fps, textos, cancelamento, temas e quadros de 2–32 equipes |
| Controlador completo | 44 | Rodadas automáticas, retomada, tabela final, Golden Goal, falha de escrita e intervalo sem duplicação |
| Editor real | 84 | Painel único por modo, controles internos redirecionados, preservação dos valores e compilação com todos os scripts |

Para executar em um sistema com Bash, Mono (`mcs` e `mono`), Python 3 e `rg`, use `bash validation/run-all.sh` a partir da raiz. `MCS` e `MONO` podem selecionar executáveis alternativos. As fachadas estão fora de `Projeto/Assets` e não são importadas pelo Unity. Os executáveis de teste e os dados temporários não fazem parte da entrega.

As auditorias YAML validam referências locais, componentes duplicados e GUIDs dos scripts/arquivos. Referências aos pacotes do Unity são identificadas separadamente e precisam ser resolvidas pela importação real no Editor.

Abra as duas cenas no Unity 6000.4.11f1 para verificar o resultado visual e os contatos físicos antes de gravar vídeos.

Na versão 2026.11.13.1, a fachada de runtime foi reforçada para permitir divergência entre a posição física de um corpo congelado e seu Transform. Reativar o corpo restaura a posição visual nesse modelo adversarial. Isso expõe a reposição antiga que atualizava apenas Rigidbody2D.position e verifica que o código novo sincroniza as duas posições. Esse modelo não pretende reproduzir todos os detalhes da física nativa do Unity.
