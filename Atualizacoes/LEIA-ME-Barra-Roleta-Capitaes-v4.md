# Painéis ampliados e rodapé personalizado — v4

Extraia `Atualizacao_Barra_Roleta_Capitaes_v4.zip` na pasta **Projeto**, onde fica **Assets**, substituindo os três scripts. Este pacote é uma atualização visual para a **v3 já instalada**.

## Espaço inferior aproveitado

Os painéis ao lado das fotos agora ocupam as duas faixas laterais marcadas na imagem. Cada painel tem cerca de 480 × 144 px em 1920 × 1080, com barra mais longa, contador maior, cinco indicadores de carga e medidor percentual durante o carregamento. A roleta fica maior e voltada para o centro. Os painéis são espelhados e deixam uma área central exclusiva para seu texto.

A cor configurada, o fundo opaco e o pulso suave continuam funcionando. A carga, as chances, a espera pelo próximo toque e as animações do chute da v3 permanecem iguais.

## Só o que você escreve no rodapé

O rodapé usa apenas **Título do rodapé** e **Subtítulo do rodapé**, no componente **Projeto Futebol Discos → Textos da arena**. `KNOCKOUT`, `LEAGUE` e a quantidade `5 v 5` não são mais adicionados automaticamente. O título e o subtítulo são centralizados entre os novos painéis e ajustam o tamanho da fonte ao espaço disponível.

Se um campo ficar vazio, sua linha é ocultada. As opções de mostrar título/subtítulo continuam funcionando. O controle antigo de mostrar formato/quantidade foi retirado do Inspector; não é necessário alterar cenas nem reiniciar a temporada.

## Arquivos

- `Assets/Scripts/LayoutJogadorUnicoUI.cs`
- `Assets/FutebolDiscos/Scripts/HudFutebolDiscos.cs`
- `Assets/FutebolDiscos/Editor/ProjetoFutebolDiscosEditor.cs`

O ZIP usa a base do BACKUP enviado, preservando fotos, posições, sons e apresentação final já existentes. O relatório dos testes automatizados de geometria, rodapé e regressões está em `validation/captain-special/results.txt` no GitHub. Os scripts reais são compilados com fachadas Unity/TMP; o Editor Unity e a renderização em GPU não foram executados neste ambiente.
