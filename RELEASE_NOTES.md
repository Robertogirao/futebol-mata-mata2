Efeitos da partida com fonte exclusiva e seleção de áudio reforçada no painel central.

- Gol e apitos usam os três clipes escolhidos em **Sons e comemoração de gol**. Campos vazios deixam o respectivo efeito sem som.
- Com **Fonte de áudio dos efeitos (opcional)** em None, o jogo cria uma fonte exclusiva, sem reutilizar fontes de abertura ou música.
- Fontes atribuídas aos efeitos são preparadas sem um clipe fixo antigo; efeitos pendentes param ao preparar outra partida, cancelar ou desativar a interface.
- O Inspector mantém as referências de áudio entre atualizações e registra as alterações na cena ou no prefab.
- **Som das aberturas e prévias** aparece no mesmo grupo e continua usando o asset do tema, separado do gol e dos apitos.

**Atualizar seu projeto:** [baixe o ZIP de dois scripts](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/tags/futebol-discos-v2026.11.13.8/Atualizacoes/Atualizacao_Sons_Painel.zip). Pare o Play, extraia e copie Assets para seu projeto 2026.11.13.7, aceitando as substituições. Confira os clipes e salve a cena. [Instruções completas](https://github.com/Robertogirao/futebol-mata-mata2/blob/futebol-discos-v2026.11.13.8/Atualizacoes/COMO_APLICAR_SONS.md).

Cenas, fontes, imagens, arquivos de áudio e resultados existentes ficam preservados. O projeto completo está em **Source code (zip)** abaixo; abra a pasta **Projeto** no Unity 6000.4.11f1.

Passaram 47 verificações de áudio, 130 do painel e os testes anteriores de liga, mata-mata, gols, bola presa e apresentação. A validação usa fachadas das APIs Unity para conferir os clipes e volumes enviados à reprodução. O áudio audível, a mixagem e a importação no Editor não foram executados neste ambiente.
