# Validação da versão de áudio

- Sintaxe dos 35 scripts C# analisada com tree-sitter-c-sharp.
- Referências dos clipes de gol, apito inicial, apito final e música resolvidas pelas GUIDs e metadados em todas as quatro cenas.
- Fluxo de encerramento, Golden Goal, pausa e troca entre abertura geral e prévia revisado por inspeção.
- Fontes separadas para efeitos, abertura e música; cauda de saída da abertura do mata-mata preservada durante a próxima prévia.

O editor Unity não está instalado neste ambiente. Estes controles estáticos não substituem a compilação nem confirmam reprodução, mixagem ou comportamento em Play Mode.

## Conferência no Unity 6000.4.11f1

Em cada uma das quatro cenas: iniciar a partida, ouvir o apito e a música entrando, marcar gol, encerrar a partida e conferir o fade da música, avançar para o próximo jogo e cancelar. Nos modos mata-mata, conferir também a abertura geral seguida da prévia e o Golden Goal. Confirmar que fechar/desativar a cena interrompe as fontes de áudio.
