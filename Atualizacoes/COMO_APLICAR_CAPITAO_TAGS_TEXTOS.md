# Capitão, tags dos jogadores e textos editáveis — 2026.11.13.2

1. Pare o **Play** no Unity.
2. Extraia `Atualizacao_Capitao_Tags_Textos.zip`.
3. Copie a pasta **Assets** extraída para a raiz do seu projeto atual e substitua os oito scripts incluídos. As cenas, fichas, fontes, imagens e configurações permanecem no projeto atual. Esta atualização também inclui a correção anterior dos gols repetidos.
4. Aguarde a recompilação. Selecione **FutebolDiscos_Manager**, ajuste os novos controles, salve a cena e dê Play.

## Nomes e capitão

No painel central, abra **Identificação dos jogadores**:

- **Mostrar nome nos discos** liga ou desliga as tags. Ajuste o tamanho da fonte e a largura.
- **Destacar o capitão** mostra uma borda dourada e a letra **C**.
- **Foto no disco do capitão** troca a imagem central do disco pela foto do representante. Desmarcado, o disco usa a bandeira/escudo; isso não muda a foto da comemoração.

Há exatamente um capitão por time: o jogador selecionado para a foto da ficha e da abertura. Os outros discos recebem os demais jogadores cadastrados em **Elenco** na ficha `TeamData` do time. O capitão não é repetido nos demais discos.

Para trocar nomes, selecione a ficha do time no Project e edite **Elenco > Nome Jogador**. Para escolher o capitão, use a seleção manual do representante nessa ficha; a seleção aleatória existente também continua funcionando. Se faltarem jogadores no elenco, os discos restantes mostram a sigla do time e seu número, como `BRA 2`, até você preencher o cadastro.

## Comemoração do gol

- Gol do capitão: **nome e foto**; se a foto não estiver cadastrada, usa a bandeira/escudo.
- Gol dos demais jogadores: **nome e bandeira/escudo do time**, mesmo que eles tenham foto no cadastro.
- Gol contra: identifica o autor do último toque com **(OG)** e mantém o ponto para o adversário.
- Sem um último toque conhecido: mostra o nome e a bandeira/escudo do time que marcou, sem atribuir um jogador inventado.

## Textos do campo e rodapé

Em **FutebolDiscos_Manager > Textos da arena**, altere:

- **Texto no campo**: a assinatura no gramado.
- **Título do rodapé**: o texto maior abaixo do campo.
- **Subtítulo do rodapé**: a linha que antes dizia `Automatic Disc Football`.
- **Mostrar formato e quantidade de discos**: controla separadamente `LEAGUE / KNOCKOUT` e `3 v 3`.

Você pode desligar cada texto ou deixá-lo vazio para ocultá-lo. As fontes continuam no tema da competição. Essas opções de identificação e apresentação podem ser alteradas entre sessões sem criar outra temporada.

Os testes de código cobrem identificação dos jogadores, imagens dos autores, fila de gols, textos editáveis e a correção dos gols repetidos. As fachadas de API não substituem uma conferência visual no Play do Unity.
