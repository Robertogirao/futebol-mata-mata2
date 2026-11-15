# Barra e roleta dos capitães

A barra fica ao lado da foto externa de cada capitão. Cinco contatos distintos com a bola completam a carga e iniciam automaticamente a roleta. Permanecer encostado na bola ou receber os callbacks de chute e colisão juntos conta uma única vez. Um novo contato depois da separação conta normalmente.

A chance de acertar é o overall efetivo do capitão dividido por 100: OVR 85 significa 85% de gol e 15% de erro. O overall individual tem prioridade; sem ele, vale o overall do time. Os setores verde/vermelho mostram as chances reais. O giro dura 2,5 segundos e o resultado permanece visível por 0,8 segundo. O acerto registra um gol para o time do capitão pelo fluxo normal do placar/banner/reposição; o erro não altera o placar. A carga desse capitão volta a zero. A carga do outro capitão é independente.

A partida segue durante o giro. Durante uma reposição, a roleta fica oculta e aguarda a retomada. Encerrar ou cancelar a partida cancela a roleta; uma nova partida começa com as duas cargas zeradas. A atribuição automática do cobrador na reposição não conta como contato físico.

## Aplicar no backup enviado

Baixe `Atualizacao_Barra_Roleta_Capitaes.zip` e extraia na pasta `Projeto`, onde fica `Assets`, substituindo os arquivos solicitados. O ZIP contém somente três scripts alterados e um novo script com seu `.meta`:

- `Assets/FutebolDiscos/Scripts/PartidaDiscos.cs`
- `Assets/FutebolDiscos/Scripts/BolaDiscos.cs`
- `Assets/Scripts/LayoutJogadorUnicoUI.cs`
- `Assets/Scripts/RoletaCapitaoUI.cs`
- `Assets/Scripts/RoletaCapitaoUI.cs.meta`

O ZIP usa como base o BACKUP enviado nesta conversa, preservando seus sons, posições e atributos dos capitães. Os scripts em `Projeto/Assets` da branch recebem os mesmos recursos adaptados à versão já existente da branch. Não é necessário substituir cenas, fotos, configurações ou dados de temporada, nem configurar objetos manualmente: a interface é criada em execução.

## Verificação

Os testes automatizados compilam os scripts reais em C# e exercitam a lógica com fachadas Unity/TMP. Não substituem a execução no Editor Unity 6000.4.11f1: o ambiente de teste não dispõe do Editor, nem do renderizador ou solver físico nativo.

Resultado: 1.216 verificações aprovadas na branch e 1.171 nos scripts do BACKUP que compõem o ZIP. A versão antiga de um teste de pausa final diverge também no BACKUP original, por causa da apresentação final mais recente já existente; esse comportamento foi preservado. O relatório completo e os comandos estão em `validation/captain-special/results.txt` no repositório.
