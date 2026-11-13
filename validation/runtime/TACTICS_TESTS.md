# Posturas e pressão condicional

Execute `bash validation/runtime/run-tactics.sh`, com Mono (`mcs` e `mono`) disponível. As variáveis `MCS` e `MONO` podem selecionar executáveis alternativos. O runner compila todos os arquivos reais de runtime e depois o harness. `SKIP_RUNTIME_COMPILE=1` permite usar uma compilação existente.

O conjunto passou por **572 verificações** usando os arquivos reais `TaticaDiscos`, `DiscoAtleta`, `PartidaDiscos`, `TeamData` e demais componentes. A fachada Unity representa hierarquia, corpos e forças; as posições e os eventos do jogo são controlados pelo teste. Não executa o solver nativo, contatos entre discos ou renderização.

Verificações:

- Os valores serializados existentes continuam `Ataque=0`, `Balanceado=1` e `Defesa=2`; a opção nova usa `UltraDefensivo=3`.
- Limites 0/1/2/3 respeitam equipes com 1 a 5 discos e ambos os lados do campo.
- A trava geral do meio-campo prevalece, inclusive sobre momento de ataque e impulso para cruzar a linha.
- O número liberado cresce conforme a oportunidade; bola no próprio campo, na margem do meio ou voltando rapidamente reduz/cancela a pressão.
- A seleção é determinística e tolera pequenas mudanças de distância sem trocar o perseguidor a cada etapa.
- Um disco parcialmente no campo adversário reserva sua vaga até voltar, impedindo uma substituição de ultrapassar a cota.
- O recuo acontece por força e movimento para casa, sem teleporte para a linha do meio.
- A postura de ataque permite que os três avancem, incluindo o capitão; os apoios usam faixas diferentes.
- A reposição após gol limpa os papéis anteriores e preserva a trava contra gols repetidos.

Antes de gravar, confira no Play do Unity a movimentação renderizada e as respostas de contato em cada postura.
