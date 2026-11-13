# Painel central

O harness real do editor compila o painel principal, os Inspectors internos, o editor antigo do mata-mata e todos os scripts de runtime com fachadas das APIs Unity e UnityEditor.

Verifica que Liga e Mata-mata mostram seções separadas; rodadas aparecem somente na liga e folgas somente no mata-mata; cada tempo é apresentado uma única vez; aliases internos do relógio e da pausa não voltam ao painel. Os campos reais de áudio e comemoração continuam editáveis no painel principal e preservam seus valores personalizados, sem criar uma segunda configuração.

Os oito Inspectors internos deixam de apresentar comandos duplicados quando há ProjetoFutebolDiscos no mesmo objeto. Fora desse projeto, mantêm o Inspector normal. O redirecionamento não desativa componentes, altera dados de temporada ou cria progresso.

Também mantém as verificações anteriores de participantes, nova temporada, preservação de arquivos, tema e bloqueio das alterações no Play. Resultado: **84 verificações aprovadas**. Não executa o Unity Editor nativo.
