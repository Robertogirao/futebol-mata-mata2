# Barra e roleta dos capitães — atualização v2

Baixe `Atualizacao_Barra_Roleta_Capitaes_v2.zip` e extraia na pasta **Projeto**, onde fica **Assets**, substituindo os arquivos. O pacote é cumulativo para o BACKUP enviado: inclui somente seis scripts e o `.meta` da roleta. Não substitui suas cenas, fotos, times, temporada ou salvamentos.

## Mudanças

- Barra mais baixa, alinhada à foto externa, com preenchimento suave, brilho móvel e pulso.
- Fundo totalmente opaco na área da barra e da roleta.
- Roleta desenhada pelo ciclo nativo de `Image`, com renderer e textura branca explícitos, setores opacos e círculo maior. Não usa o antigo ajuste de tamanho por AspectRatioFitter.
- Textos gerados pela mecânica em inglês: `TOUCHES`, `HIT`, `MISS`, `SPECIAL READY`, `NEXT TOUCH` e `POWER SHOT!`.
- Após cinco contatos distintos, a roleta gira durante 2,5 segundos. O resultado **não executa o chute nem soma gol**: fica armado no capitão até o próximo contato distinto com a bola. Continuar encostado nela não consome o resultado.
- `HIT`: no próximo toque, o capitão prepara um chute de força com pulso, aura e rastro dourado. O gol só é registrado ao fim do voo da bola.
- `MISS`: no próximo toque, a bola é chutada fora da boca do gol e o jogo continua sem ponto.
- O relógio e os demais discos pausam durante o chute animado. Encerrar/cancelar uma partida limpa os efeitos e resultados; começar outra zera as cargas.

O overall continua definindo a probabilidade real: OVR 85 corresponde a 85% de HIT e 15% de MISS. As cargas e os resultados dos dois capitães são independentes.

## Controle da cor

Selecione **FutebolDiscos_Manager** e, no componente **Projeto Futebol Discos**, localize **Barra e roleta dos capitães → Cor da barra do capitão**. A cor controla o preenchimento e seu brilho; pode ser pré-visualizada durante o Play. Para manter a escolha, configure também fora do Play, conforme o comportamento normal do Inspector Unity.

Os rótulos do Inspector continuam em português; os novos textos da transmissão estão em inglês. Nomes de equipes/personagens e textos personalizados não são alterados.

## Arquivos do pacote

- `Assets/FutebolDiscos/Scripts/PartidaDiscos.cs`
- `Assets/FutebolDiscos/Scripts/BolaDiscos.cs`
- `Assets/FutebolDiscos/Scripts/ProjetoFutebolDiscos.cs`
- `Assets/FutebolDiscos/Editor/ProjetoFutebolDiscosEditor.cs`
- `Assets/Scripts/LayoutJogadorUnicoUI.cs`
- `Assets/Scripts/RoletaCapitaoUI.cs`
- `Assets/Scripts/RoletaCapitaoUI.cs.meta`

O ZIP preserva a versão mais recente do BACKUP (incluindo sons, posições e apresentação final). As fontes em `Projeto/Assets` no GitHub recebem as mesmas melhorias adaptadas à base que já estava publicada.

## Testes

Foram compilados os scripts reais com fachadas Unity/TMP: **1.241 verificações passaram na branch e 1.196 no BACKUP**. Os casos cobrem resultado armado, próximo toque, contatos contínuos, chute comum concorrente, voo HIT/MISS, pontuação única, reinício, cancelamento durante animação, cor, fundo opaco e configuração/malha da roleta. Veja `validation/captain-special/results.txt` para comandos, hashes e compatibilidade dos testes antigos.

O ambiente não dispõe do Editor Unity: renderização em GPU, fontes e colisões do solver nativo não foram executadas. A configuração de renderização e a lógica foram verificadas por código.
