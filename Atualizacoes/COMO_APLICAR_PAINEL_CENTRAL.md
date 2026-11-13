# Painel central, imagem externa e linha das fotos — 2026.11.13.6

1. Pare o **Play** no Unity.
2. Extraia `Atualizacao_Painel_Central_Fundo_Fotos.zip`.
3. Copie a pasta **Assets** extraída para a raiz do seu projeto atual e aceite as substituições. São sete scripts e o `.meta` do novo editor do painel.
4. Aguarde a recompilação, selecione **FutebolDiscos_Manager** e salve a cena após configurar.

As cenas, fontes, imagens, fichas, tempos personalizados e resultados existentes permanecem no seu projeto.

## Um lugar para configurar

Use apenas o componente **Projeto Futebol Discos**, no objeto **FutebolDiscos_Manager**. O painel mostra **Configurações da Liga** na cena da liga e **Configurações do Mata-mata** na cena do mata-mata. Cada cena guarda sua própria configuração.

Ali estão participantes, calendário/chaveamento, tempo real de jogo, abertura/prévia, fotos dos capitães, retorno das fichas, classificação/quadro final e intervalo. O tempo real usa segundos; o relógio do placar distribui os 90 minutos virtuais nessa duração. Aberturas e prévias mantêm o mínimo de três segundos.

**Sons e comemoração de gol** reúne os sons, volume, exibição do autor do gol e intervalo entre comemorações. Esses controles editam os valores existentes nos componentes, sem reiniciar suas escolhas.

Os componentes internos, como TournamentManager, UniversalUI e MataMataCampeonato, passam a mostrar um aviso e um atalho para o painel principal na visualização normal do Inspector. Os controles duplicados deixam de aparecer nesses componentes. Mantenha os componentes habilitados.

O intervalo da liga agora é aplicado uma única vez após a partida. A sequência ainda espera a comemoração de um gol terminar quando necessário.

## Imagem fora da arena

Em **Identidade visual > Fundo externo da arena**, arraste a imagem desejada.

Se ela ainda não aparecer como Sprite, selecione o arquivo no Project, mude **Texture Type** para **Sprite (2D and UI)** no Inspector e clique em **Apply**.

A imagem preenche o espaço externo sem deformar sua proporção; as bordas podem ser cortadas para preencher a tela. Campo e gols continuam por cima dela. Ajuste a **opacidade** e, se quiser, ative **Manter arquibancadas sobre a imagem** para manter arquibancadas e luzes sobre a imagem. Sem imagem, a decoração anterior continua funcionando.

**Imagem do campo (opcional)** continua sendo o controle separado para a arte dentro do gramado.

## Linha na foto do capitão

Em **Identificação dos jogadores**, use **Mostrar leitura luminosa atrás da foto**. A linha agora aparece atrás do retrato e do texto, com brilho reduzido e velocidade padrão mais lenta.

Você pode reduzir ainda mais em **Velocidade da leitura luminosa** ou desmarcar a opção para retirar a linha. Isso não muda o tempo de exibição das fotos.

Os testes de código passaram para painel único, preservação de configurações, intervalo sem duplicação, imagem externa e hierarquia/velocidade do scanner. A aparência final deve ser conferida no Play do Unity.
