# Validação da V6 — posição, tamanho e lado de contato da cruz

Versão alvo: Unity **6000.4.11f1**. O ambiente de desenvolvimento não possui Editor Unity instalado.

O vídeo da V5 mostrou quedas fora do alcance das pás, golpes de volta à canaleta e saídas baixas. A V6 usa raio maior, centro independente da alavanca antiga e alimentação no lado ascendente, com giro correspondente. Mantém ajuda leve, retorno ao reservatório do próprio time e oito identidades.

## Código executado fora do Unity

- **7.232 verificações em C# puro:** relógio, pausa, resultado, reinício e pontuação.
- **1.376 verificações com substitutos das APIs Unity:** regressões das versões anteriores, duplicação, retorno, ajuda leve, folgas das pás maiores, centro independente, direção real do ponto da pá durante o giro e trajetória nominal sem colisão com as guias ou o anteparo de entrada.
- **13 grupos estruturais:** sintaxe, GUIDs, assemblies, cena, prefab, parâmetros e preservação dos componentes essenciais.
- Compilação dos scripts de execução e de Editor contra substitutos das dependências. Não é compilação contra as bibliotecas reais do Unity nem execução de PhysX.
- Comparação de arquivos confirma que retorno, recuperação, assistência leve, HUD e rotores da V5 foram preservados. Regras, identidade, validação de cestas e evento de resultado também foram mantidos.

## Cruz em movimento na simulação auxiliar

MuJoCo 4.0.1, gravidade de 9,81 m/s², esferas de 0,45 kg e raio de 0,18 m, passo de 0,002 s. O motor tem posição e velocidade prescritas e as pás usam a geometria exportada. São quatro posições de entrada e oito fases distintas entre duas pás, totalizando 32 quedas independentes por versão. Usa RPM nominal, sem assistência, outros jogadores, aro ou tabela.

| Métrica | V5 | V6 |
|---|---:|---:|
| Contatos com a cruz | 16/32 | 27/32 |
| Saídas para cima e em direção à cesta | 2/32 | 17/32 |
| Arcos ideais com altura suficiente para o aro | 1/32 | 10/32 |
| Trajetórias ideais a menos de 1,5 m do centro do aro | 0/32 | 6/32 |

A velocidade de saída é observada 40 ms após o último contato. Uma saída ascendente exige componente vertical maior que 1,5 m/s e componente em direção à cesta maior que 0,8 m/s. As duas últimas linhas extrapolam o voo sem colisões; proximidade de 1,5 m não equivale à passagem na abertura do aro.

O objetivo da sondagem é verificar alcance das pás e direção do impulso. Não mede a taxa real de cestas do Unity. A ajuda leve permanece em 0,25 no jogo, mas está desligada nessa sondagem. O material de reprodução está em **Verification/LauncherProbe**.

## Passagens por gravidade

A sondagem estática da V5 foi repetida com a nova entrada, sem motor e sem recuperação programada. Os seis casos da V6 escoaram: cantos esquerdo/direito, duas juntas na descida, oito na frente, duas no coletor e oito com raio máximo de 0,22 m. São 22 esferas no total, todas atravessando a região esperada na janela de 12 s. A última do grupo de raio máximo passou em aproximadamente 6,84 s.

As geometrias e resultados estão em **Verification/FlowProbe**. Esses testes usam outro motor de contato e não substituem o Editor Unity.

## Testes nativos fornecidos

Em **Window → General → Test Runner**:

- **EditMode:** regras e limite da assistência leve.
- **PlayMode:** partida completa, oito identidades, resultado, pausa, recuperação, passagens da V5 e oito quedas livres sobre a cruz em fases diferentes. O teste das quedas exige ao menos seis contatos e quatro saídas ascendentes em direção à cesta.

Esses testes nativos não foram executados aqui.

## Conferência no Unity

1. Instalar a V6 com o arquivo de configurações fornecido e conferir raio de 0,85 m, centro (1,05; 1,85; −1,80), 75 RPM e ajuda 0,25.
2. Observar se a maioria das bolinhas encontra as pás e se os contatos produzem voos para cima em direção à cesta.
3. Conferir arcos na região do aro, acertos, erros e rebotes; o contador de arremessos inclui contatos malsucedidos.
4. Executar uma partida inteira e conferir oito bolas, retorno ao reservatório correto, resultado e reinício.
