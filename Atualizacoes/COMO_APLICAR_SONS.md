# Sons do painel — 2026.11.13.8

## Instalar

Esta atualização é para o Futebol de Discos com o painel central e a reposição nos cantos da versão 2026.11.13.7.

1. Pare o **Play** no Unity.
2. Extraia `Atualizacao_Sons_Painel.zip`.
3. Copie a pasta **Assets** extraída para a raiz do seu projeto e aceite as substituições.
4. Aguarde a recompilação e selecione **FutebolDiscos_Manager > Projeto Futebol Discos > Sons e comemoração de gol**.
5. Confira os clipes escolhidos, salve a cena e entre no Play.

O ZIP substitui somente `Assets/Scripts/UniversalUI.cs` e `Assets/FutebolDiscos/Editor/ProjetoFutebolDiscosEditor.cs`. As cenas, arquivos `.meta`, fontes, imagens, arquivos de áudio e resultados salvos permanecem no projeto. Não é necessário criar outra temporada.

## Gol e apitos

Escolha os áudios nos campos:

| Campo | Quando toca |
| --- | --- |
| Som do gol | Ao registrar um gol válido |
| Apito de início | Ao iniciar o cronômetro da partida |
| Apito de encerramento | Ao encerrar a partida, inclusive pelo Golden Goal |

Esses eventos usam os clipes selecionados e o **Volume dos efeitos**. Deixar um clipe vazio desliga aquele efeito, sem buscar outro som. As escolhas são salvas na cena; faça a troca fora do Play e salve a cena antes de testar.

**Fonte de áudio dos efeitos (opcional)** pode ficar em **None**. Assim, o jogo cria uma fonte exclusiva para os efeitos e mantém as fontes de música e das aberturas separadas. Se atribuir uma fonte manualmente, reserve-a para os efeitos da partida: o jogo interrompe a reprodução anterior e limpa o clipe fixo dessa fonte ao prepará-la. Os clipes continuam sendo escolhidos nos três campos do painel.

Ao preparar a partida seguinte, cancelar a partida ou desativar a interface, os efeitos pendentes são interrompidos.

## Aberturas e prévias

O campo **Som das aberturas e prévias**, no mesmo grupo, aparece quando há um tema atribuído e controla o áudio desse tema. Trocar gol e apitos não altera o som das apresentações.

Esse campo altera o asset do tema, portanto as cenas que usam o mesmo tema compartilham esse som. Para aberturas diferentes na liga e no mata-mata, duplique o tema no Project e associe uma cópia a cada cena.

## Conferir no Unity

Após salvar a cena, confira o apito quando o cronômetro começa, o som de um gol e o apito no encerramento. Para ouvir a mudança de abertura, troque também **Som das aberturas e prévias**.

A validação de código verifica o clipe e o volume enviados à fonte de áudio. O som audível, a importação dos arquivos e a mixagem final precisam ser conferidos no Editor Unity.
