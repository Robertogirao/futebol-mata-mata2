# Validação

Base: backup multipartes fornecido nesta conversa, Unity 6000.4.11f1.

## Executado

- Análise sintática dos 13 scripts C# adicionados/alterados, sem erros.
- Compilação C# dos quatro scripts de execução novos e do repositório real de configuração/temporadas, junto do calendário original, com substitutos das APIs Unity, TMPro e componentes de integração.
- **120 verificações executáveis C#**: dez bolas, preparação pausada, posições iniciais sem sobreposição, início, eliminação, proteção contra eliminação duplicada, retorno, conquista do lançador, intervalo compartilhado, trajetória inicial, pontuação do time correto, pontuação única, erro lateral, retorno após erro, parada, reinício, cancelamento externo, compatibilidade de configurações antigas, gravação e retomada de temporada, e rejeição de mudanças das regras de uma temporada salva.
- **10 grupos de testes Python**: sintaxe C#, resolução das referências GUID novas (referências de pacotes/built-ins herdadas das cenas originais são conservadas), unicidade de GUIDs, preservação dos `.meta` dos scripts substituídos, quatro IDs NBA independentes, participantes e cinco bolas por equipe, distância inicial das bolas, largura útil do funil, cálculo balístico em diferentes gravidades, teste de cruzamento descendente do aro e diferenças dos presets de arena.
- Conferência da integridade e dos hashes dos arquivos do pacote.

Os testes C# usaram substitutos das APIs; não simulam o motor físico do Unity. Os testes matemáticos de trajetória não equivalem à integração física real. Os outros scripts compartilhados e o editor foram analisados sintaticamente, mas não compilados com as bibliotecas do Unity.

## Conferir no Unity

1. Importar todos os arquivos da atualização no projeto do backup e verificar o Console.
2. Rodar Liga NBA e Mata-mata NBA, depois as duas cenas com capitães.
3. Conferir a disputa e o retorno após eliminação nos três temas.
4. Conferir que o lançador não transporta duas bolas juntas nem permite que bolas da disputa interfiram no arremesso.
5. Conferir contato com aro/tabela, cestas de cima para baixo e ausência de pontos duplicados.
6. Conferir placar, banners, prorrogação, resultado final e início da partida seguinte em 60 segundos.
7. Parar e retomar a temporada; criar outra ao mudar regras da arena.
8. Conferir composição da transmissão em diferentes proporções de tela e ao gravar com Recorder.
9. Revisar o ritmo de eliminações e a taxa de acerto antes de gravar um campeonato longo.

Não foi executado Play Mode neste ambiente, que não possui Unity instalado.
